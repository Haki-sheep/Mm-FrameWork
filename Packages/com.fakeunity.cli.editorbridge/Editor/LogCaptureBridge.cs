#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace FakeUnityCLI.EditorBridge
{
    internal static class LogCaptureBridge
    {
        internal const string ProtocolVersion = "1.1";
        private const int QueueLimit = 8192;
        private const long FileLimitBytes = 16L * 1024L * 1024L;
        private const int RetainedFiles = 3;
        private static readonly ConcurrentQueue<Entry> Queue = new ConcurrentQueue<Entry>();
        private static readonly string StateDirectory = Path.Combine("Library", "FakeUnityCLI");
        private static readonly string ConsolePath = Path.Combine(StateDirectory, "console.jsonl");
        private static readonly string CompileStatePath = Path.Combine(StateDirectory, "compile-state.json");
        private static readonly string SessionId = DateTime.UtcNow.ToString("yyyyMMddTHHmmss.fffffff") + "-" +
                                                   Process.GetCurrentProcess().Id + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        internal static string CurrentSessionId { get { return SessionId; } }
        private static readonly string CaptureStartedAt = DateTime.UtcNow.ToString("o");
        private static long _sequence;
        private static int _queued;
        private static int _dropped;
        private static bool _started;
        private static readonly object QueueSync = new object();
        private static bool _acceptingEntries;
        private static long _unpersistedEntries;
        private static long _pendingDroppedEntries;
        private static long _persistenceFailures;
        private static Exception _lastPersistenceFailure;
        private static string _lastPersistenceOperation;
        private static string _lastPersistencePath;
        private static DateTime _nextPersistenceAttemptAt;
        private static DateTime _nextProviderRefreshAt;
        private static readonly object CompileSync = new object();
        private static CompileState _compileState;

        internal static long CurrentCompileGeneration { get { lock (CompileSync) return _compileState == null ? 0 : _compileState.generation; } }
        internal static string CurrentCompileStatus { get { lock (CompileSync) return _compileState == null ? "not_observed" : _compileState.state; } }

        internal static void Start()
        {
            if (_started) return;
            Directory.CreateDirectory(StateDirectory);
            InitializeCompileState();
            WriteHandshake();
            lock (QueueSync) _acceptingEntries = true;
            Application.logMessageReceivedThreaded -= OnLogMessage;
            Application.logMessageReceivedThreaded += OnLogMessage;
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            EditorApplication.update -= Drain;
            EditorApplication.update += Drain;
            _started = true;
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            Enqueue(new Entry
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                level = Level(type), category = "runtime", message = condition ?? "",
                stack_trace = stackTrace ?? "", structured = true
            });
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            var assembly = String.IsNullOrEmpty(assemblyPath) ? null : Path.GetFileNameWithoutExtension(assemblyPath);
            lock (CompileSync)
            {
                EnsureCompileGeneration("assembly_finished_without_start");
                foreach (var diagnostic in messages)
                {
                    if (diagnostic.type == CompilerMessageType.Error) _compileState.error_count++;
                    else _compileState.warning_count++;
                    Enqueue(new Entry
                    {
                        timestamp = DateTime.UtcNow.ToString("o"),
                        level = diagnostic.type == CompilerMessageType.Error ? "error" : "warning",
                        category = "compiler", event_type = "assembly_diagnostic",
                        compile_generation = _compileState.generation,
                        message = diagnostic.message ?? "", file = NormalizePath(diagnostic.file),
                        absolute_file = AbsolutePath(diagnostic.file), line = diagnostic.line, column = diagnostic.column,
                        code = ExtractCompilerCode(diagnostic.message), assembly = assembly, stack_trace = "", structured = true
                    });
                }
                Enqueue(new Entry { timestamp = DateTime.UtcNow.ToString("o"), level = "log", category = "compiler",
                    event_type = "assembly_compile_finished", compile_generation = _compileState.generation,
                    message = "Assembly compilation finished.", assembly = assembly, stack_trace = "", structured = true });
                PersistCompileState();
            }
        }

        private static void OnCompilationStarted(object context)
        {
            lock (CompileSync)
            {
                if (_compileState.state == "in_progress" && !_compileState.complete &&
                    _compileState.generation_session_id == SessionId &&
                    _compileState.gap_reason == "observer_started_during_compile")
                {
                    _compileState.history_gap = false;
                    _compileState.gap_reason = null;
                    _compileState.started_at = DateTime.UtcNow.ToString("o");
                }
                else BeginCompile(false, null);
                Enqueue(new Entry { timestamp = _compileState.started_at, level = "log", category = "compiler",
                    event_type = "compile_started", compile_generation = _compileState.generation,
                    message = "Compilation started.", stack_trace = "", structured = true });
                PersistCompileState();
            }
        }

        private static void OnCompilationFinished(object context)
        {
            lock (CompileSync)
            {
                EnsureCompileGeneration("compile_finished_without_start");
                _compileState.completed_at = DateTime.UtcNow.ToString("o");
                _compileState.complete = true;
                _compileState.state = _compileState.error_count > 0 || _compileState.warning_count > 0 ? "diagnostics" : "clean";
                Enqueue(new Entry { timestamp = _compileState.completed_at, level = "log", category = "compiler",
                    event_type = "compile_finished", compile_generation = _compileState.generation,
                    compile_complete = true, compile_error_count = _compileState.error_count,
                    compile_warning_count = _compileState.warning_count, compile_history_gap = _compileState.history_gap,
                    message = "Compilation finished.", stack_trace = "", structured = true });
                PersistCompileState();
            }
            Drain();
        }

        private static void InitializeCompileState()
        {
            lock (CompileSync)
            {
                try
                {
                    _compileState = File.Exists(CompileStatePath)
                        ? JsonUtility.FromJson<CompileState>(File.ReadAllText(CompileStatePath, new UTF8Encoding(false)))
                        : null;
                }
                catch { _compileState = null; }
                if (_compileState == null) _compileState = new CompileState { state = "not_observed" };
                _compileState.observer_session_id = SessionId;
                _compileState.observer_started_at = CaptureStartedAt;
                if (EditorApplication.isCompiling)
                    BeginCompile(true, "observer_started_during_compile");
                else if (_compileState.state == "in_progress" && !_compileState.complete)
                {
                    _compileState.state = "history_gap";
                    _compileState.history_gap = true;
                    _compileState.gap_reason = "observer_restarted_after_incomplete_compile";
                }
                PersistCompileState();
            }
        }

        private static void BeginCompile(bool historyGap, string gapReason)
        {
            _compileState.generation++;
            _compileState.state = "in_progress";
            _compileState.generation_session_id = SessionId;
            _compileState.started_at = DateTime.UtcNow.ToString("o");
            _compileState.completed_at = null;
            _compileState.error_count = 0;
            _compileState.warning_count = 0;
            _compileState.complete = false;
            _compileState.history_gap = historyGap;
            _compileState.gap_reason = gapReason;
        }

        private static void EnsureCompileGeneration(string gapReason)
        {
            if (_compileState == null) _compileState = new CompileState { state = "not_observed" };
            if (_compileState.state != "in_progress" || _compileState.complete)
                BeginCompile(true, gapReason);
        }

        private static void PersistCompileState()
        {
            try
            {
                var temporary = CompileStatePath + ".tmp-" + Guid.NewGuid().ToString("N");
                File.WriteAllText(temporary, JsonUtility.ToJson(_compileState, true), new UTF8Encoding(false));
                if (File.Exists(CompileStatePath)) File.Delete(CompileStatePath);
                File.Move(temporary, CompileStatePath);
            }
            catch (Exception exception) { System.Diagnostics.Debug.WriteLine("FakeUnityCLI compile state persistence failed: " + exception.Message); }
        }

        private static void Enqueue(Entry entry)
        {
            lock (QueueSync)
            {
                if (!_acceptingEntries) return;
                if (Interlocked.Increment(ref _queued) > QueueLimit)
                {
                    Interlocked.Decrement(ref _queued); Interlocked.Increment(ref _dropped); return;
                }
                Queue.Enqueue(entry);
            }
        }

        private static void Drain()
        {
            if (DateTime.UtcNow >= _nextProviderRefreshAt)
            {
                _nextProviderRefreshAt = DateTime.UtcNow.AddSeconds(2);
                try { if (OnlineProviderRegistry.Refresh()) WriteHandshake(); }
                catch (Exception exception) { System.Diagnostics.Debug.WriteLine("FakeUnityCLI provider refresh failed: " + exception.Message); }
            }
            if (DateTime.UtcNow >= _nextPersistenceAttemptAt) DrainBuffered(256);
        }

        private static void DrainBuffered(int limit)
        {
            var batch = new List<string>(limit);
            var generations = new HashSet<long>();
            var entries = 0;
            if (_persistenceFailures > 0)
            {
                batch.Add(SerializeEntry(new Entry { timestamp = DateTime.UtcNow.ToString("o"), level = "warning",
                    category = "bridge", event_type = "persistence_gap",
                    message = "Bridge journal could not confirm persistence of " + _unpersistedEntries +
                        " entries across " + _persistenceFailures + " write failures. The failed batch was not replayed because append may have partially succeeded. Last operation=" +
                        _lastPersistenceOperation + " path=" + _lastPersistencePath + " hresult=" +
                        (_lastPersistenceFailure == null ? 0 : _lastPersistenceFailure.HResult) + ".",
                    path = _lastPersistencePath, operation = _lastPersistenceOperation, phase = "log_persistence",
                    hresult = _lastPersistenceFailure == null ? 0 : _lastPersistenceFailure.HResult,
                    exception_type = _lastPersistenceFailure == null ? null : _lastPersistenceFailure.GetType().FullName,
                    unpersisted_entries = _unpersistedEntries, persistence_failures = _persistenceFailures,
                    stack_trace = "", structured = true }));
            }
            _pendingDroppedEntries += Interlocked.Exchange(ref _dropped, 0);
            var dropped = _pendingDroppedEntries;
            if (dropped > 0)
            {
                batch.Add(SerializeEntry(new Entry { timestamp = DateTime.UtcNow.ToString("o"), level = "warning",
                    category = "bridge", event_type = "queue_overflow", dropped_entries = dropped,
                    message = "Bridge queue dropped " + dropped + " messages due to backpressure.",
                    stack_trace = "", structured = true }));
            }
            Entry entry;
            while (batch.Count < limit && Queue.TryDequeue(out entry))
            {
                Interlocked.Decrement(ref _queued);
                if (entry.compile_generation > 0) generations.Add(entry.compile_generation);
                batch.Add(SerializeEntry(entry));
                entries++;
            }
            if (batch.Count == 0) return;
            var operation = "inspect_journal";
            var operationPath = Path.GetFullPath(ConsolePath);
            try
            {
                RotateIfNeeded(ref operation, ref operationPath);
                operation = "append_journal";
                operationPath = Path.GetFullPath(ConsolePath);
                File.AppendAllText(ConsolePath, String.Join("\n", batch.ToArray()) + "\n", new UTF8Encoding(false));
                _unpersistedEntries = 0;
                _pendingDroppedEntries = 0;
                _persistenceFailures = 0;
                _lastPersistenceFailure = null;
                _nextPersistenceAttemptAt = DateTime.MinValue;
            }
            catch (Exception exception)
            {
                // Append is not atomic: replay could duplicate a partially persisted batch.
                // Retain explicit uncertainty instead, and prevent a compiler watermark from claiming completeness.
                _unpersistedEntries += entries;
                _persistenceFailures++;
                _lastPersistenceFailure = exception;
                _lastPersistenceOperation = operation;
                _lastPersistencePath = operationPath;
                _nextPersistenceAttemptAt = DateTime.UtcNow.AddSeconds(1);
                lock (CompileSync)
                {
                    if (_compileState != null && generations.Contains(_compileState.generation))
                    {
                        _compileState.history_gap = true;
                        _compileState.gap_reason = "journal_persistence_failed";
                        PersistCompileState();
                    }
                }
                System.Diagnostics.Debug.WriteLine("FakeUnityCLI Bridge persistence failed: operation=" + operation + " path=" +
                    operationPath + " hresult=" + exception.HResult + " exception_type=" +
                    exception.GetType().FullName + " unpersisted_entries=" + _unpersistedEntries + ": " + exception.Message);
            }
        }

        private static string SerializeEntry(Entry entry)
        {
            entry.sequence = Interlocked.Increment(ref _sequence);
            entry.entry_id = "bridge:" + Process.GetCurrentProcess().Id + ":" + SessionId + ":" + entry.sequence;
            entry.source_id = "bridge:" + Process.GetCurrentProcess().Id;
            entry.source = "editor_bridge"; entry.session_id = SessionId;
            return JsonUtility.ToJson(entry, false);
        }

        internal static void Stop()
        {
            if (!_started) return;
            Application.logMessageReceivedThreaded -= OnLogMessage;
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            EditorApplication.update -= Drain;
            // Close producers first; one bounded flush covers the entire queue plus its two gap markers.
            lock (QueueSync) _acceptingEntries = false;
            DrainBuffered(QueueLimit + 2);
            _started = false;
        }

        private static void RotateIfNeeded(ref string operation, ref string path)
        {
            if (!File.Exists(ConsolePath) || new FileInfo(ConsolePath).Length < FileLimitBytes) return;
            for (var i = RetainedFiles - 1; i >= 1; i--)
            {
                var current = ConsolePath + "." + i;
                var next = ConsolePath + "." + (i + 1);
                if (File.Exists(next))
                {
                    operation = "delete_rotated_journal"; path = Path.GetFullPath(next);
                    File.Delete(next);
                }
                if (File.Exists(current))
                {
                    operation = "move_rotated_journal"; path = Path.GetFullPath(current);
                    File.Move(current, next);
                }
            }
            operation = "rotate_current_journal"; path = Path.GetFullPath(ConsolePath);
            File.Move(ConsolePath, ConsolePath + ".1");
        }

        private static void WriteHandshake()
        {
            var handshake = new Handshake
            {
                protocol_version = ProtocolVersion,
                bridge_version = BridgeBootstrap.BridgeVersion,
                process_role = "main_editor",
                project_path = Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                editor_pid = BridgeBootstrap.EditorPid,
                editor_instance_id = BridgeBootstrap.EditorInstanceId,
                process_started_at_ticks = BridgeBootstrap.ProcessStartedAtTicks,
                unity_version = Application.unityVersion,
                session_id = SessionId,
                capture_started_at = CaptureStartedAt,
                editor_log_path = Application.consoleLogPath,
                activity_schema_version = "1.0",
                capabilities = new[] { "development_modes", "runtime_inspector", "ugui_automation", "prefab_edit", "typed_inspector", "console", "structured_compile", "compile_watermark", "compile_request", "editor_activity", "execution_mode", "editor_log_path", "editor_preservation", "external_asset_import", "jsonl_buffer", "domain_reload", "roslyn_exec", "structured_result_json", "editor_refresh", "asset_import", "editor_context", "scene_hierarchy", "scene_find", "component_list", "component_get", "component_set", "screenshot", "gameview_resolution", "test_runner", "prefab_extract_child", "prefab_extract_undo", "online_providers", "bridge_live_update", "request_expiry", "file_queue" },
                providers = OnlineProviderRegistry.HandshakeProviders(),
                provider_diagnostics = OnlineProviderRegistry.Diagnostics()
            };
            var path = Path.Combine(StateDirectory, "handshake.json");
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(handshake, true), new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }

        private static string Level(LogType type)
        {
            switch (type)
            {
                case LogType.Warning: return "warning";
                case LogType.Error: return "error";
                case LogType.Exception: return "exception";
                case LogType.Assert: return "assert";
                default: return "log";
            }
        }

        private static string NormalizePath(string path)
        {
            if (String.IsNullOrEmpty(path)) return null;
            var full = AbsolutePath(path);
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (full != null && full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return full.Substring(root.Length + 1).Replace('\\', '/');
            return path.Replace('\\', '/');
        }

        private static string AbsolutePath(string path)
        {
            if (String.IsNullOrEmpty(path)) return null;
            return Path.IsPathRooted(path) ? Path.GetFullPath(path) :
                Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
        }

        private static string ExtractCompilerCode(string message)
        {
            if (String.IsNullOrEmpty(message)) return null;
            var index = message.IndexOf("CS", StringComparison.Ordinal);
            if (index < 0 || index + 6 > message.Length) return null;
            for (var i = index + 2; i < Math.Min(message.Length, index + 7); i++)
                if (!Char.IsDigit(message[i])) return i >= index + 6 ? message.Substring(index, i - index) : null;
            return message.Substring(index, Math.Min(6, message.Length - index));
        }

        [Serializable] private sealed class Handshake
        {
            public string protocol_version; public string bridge_version; public string process_role; public string project_path;
            public int editor_pid; public string editor_instance_id; public long process_started_at_ticks; public string unity_version;
            public string session_id; public string capture_started_at; public string[] capabilities;
            public string editor_log_path; public string activity_schema_version;
            public OnlineProviderRegistry.ProviderHandshake[] providers;
            public OnlineProviderRegistry.ProviderDiagnostic[] provider_diagnostics;
        }

        [Serializable] private sealed class Entry
        {
            public string entry_id; public string source_id; public string source; public string session_id;
            public long sequence; public string timestamp; public bool timestamp_inferred;
            public string level; public string category; public string message; public string stack_trace;
            public string code; public string file; public string absolute_file; public int line; public int column;
            public string assembly; public int process_id = Process.GetCurrentProcess().Id; public bool structured; public string raw;
            public string event_type; public long compile_generation; public bool compile_complete;
            public int compile_error_count; public int compile_warning_count; public bool compile_history_gap;
            public string operation; public string path; public string phase; public int hresult; public string exception_type;
            public long unpersisted_entries; public long persistence_failures; public long dropped_entries;
        }

        [Serializable] private sealed class CompileState
        {
            public string protocol_version = "1.0";
            public string state;
            public long generation;
            public string generation_session_id;
            public string observer_session_id;
            public string observer_started_at;
            public string started_at;
            public string completed_at;
            public int error_count;
            public int warning_count;
            public bool complete;
            public bool history_gap;
            public string gap_reason;
        }
    }
}
#endif
