#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace FakeUnityCLI.EditorBridge
{
    // Main-thread-only telemetry. Diagnostics consume the persisted heartbeat, never the request queue.
    internal static class EditorActivityBridge
    {
        private static bool _started;
        private static bool _initialized;
        private static bool _compiling;
        private static bool _updating;
        private static string _mode;
        private static string _transition;
        private static string _modeSince;
        private static string _compilingSince;
        private static string _updatingSince;
        private static string _lastEvent;
        private static string _lastEventAt;
        private static string _lastCompileStart;
        private static string _lastCompileFinish;
        private static string _reload = "observer_attached";
        private static string _lastOperation;
        private static string _lastRequest;
        private static string _lastOperationAt;
        private static string _lastRefreshRequest, _lastRefreshAt, _lastImportRequest, _lastImportAt;
        private static string _lastCompileRequest, _lastCompileRequestAt;
        private static readonly HashSet<string> Assemblies = new HashSet<string>(StringComparer.Ordinal);

        internal static void Start()
        {
            if (_started) return;
            EditorApplication.playModeStateChanged += OnPlayMode;
            CompilationPipeline.compilationStarted += OnCompileStart;
            CompilationPipeline.compilationFinished += OnCompileFinish;
            CompilationPipeline.assemblyCompilationStarted += OnAssemblyStart;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyFinish;
            _started = true;
        }

        internal static void Stop()
        {
            if (!_started) return;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            CompilationPipeline.compilationStarted -= OnCompileStart;
            CompilationPipeline.compilationFinished -= OnCompileFinish;
            CompilationPipeline.assemblyCompilationStarted -= OnAssemblyStart;
            CompilationPipeline.assemblyCompilationFinished -= OnAssemblyFinish;
            _started = false;
        }

        internal static void BeforeReload() { _reload = "before_reload"; }
        private static string Now() { return DateTime.UtcNow.ToString("o"); }
        private static void OnPlayMode(PlayModeStateChange change)
        {
            // Invalidate even when a full stop/start occurs between heartbeat samples with domain reload disabled.
            _mode = null;
            _transition = change == PlayModeStateChange.ExitingEditMode ? "entering_play" :
                change == PlayModeStateChange.ExitingPlayMode ? "exiting_play" : null;
        }
        private static void Event(string name) { _lastEvent = name; _lastEventAt = Now(); }
        private static void OnCompileStart(object context)
        {
            Assemblies.Clear(); _lastCompileStart = Now(); _lastCompileFinish = null;
            Event("compile_started");
        }
        private static void OnCompileFinish(object context)
        {
            _lastCompileFinish = Now(); Assemblies.Clear(); Event("compile_finished");
        }
        private static void OnAssemblyStart(string assembly) { Assemblies.Add(assembly); Event("assembly_started"); }
        private static void OnAssemblyFinish(string assembly, CompilerMessage[] messages)
        {
            Assemblies.Remove(assembly); Event("assembly_finished");
        }

        internal static void RecordOperation(string operation, string requestId)
        {
            _lastOperation = operation; _lastRequest = requestId; _lastOperationAt = Now();
            if (operation == EditorOnlineOperations.RefreshOperation)
            { _lastRefreshRequest = requestId; _lastRefreshAt = _lastOperationAt; }
            if (operation == EditorOnlineOperations.ImportOperation)
            { _lastImportRequest = requestId; _lastImportAt = _lastOperationAt; }
            if (operation == EditorOnlineOperations.CompileRequestOperation)
            { _lastCompileRequest = requestId; _lastCompileRequestAt = _lastOperationAt; }
        }

        internal static ActivitySnapshot Snapshot()
        {
            var now = Now();
            var playing = EditorApplication.isPlaying;
            var willPlay = EditorApplication.isPlayingOrWillChangePlaymode;
            var mode = _transition ?? (playing ? "play" : willPlay ? "entering_play" : "edit");
            if (!_initialized || mode != _mode) { _mode = mode; _modeSince = now; }
            var compiling = EditorApplication.isCompiling;
            var updating = EditorApplication.isUpdating;
            if (!_initialized || compiling != _compiling) _compilingSince = compiling ? now : null;
            if (!_initialized || updating != _updating) _updatingSince = updating ? now : null;
            _initialized = true; _compiling = compiling; _updating = updating;
            var assemblies = new List<string>(Assemblies); assemblies.Sort(StringComparer.Ordinal);
            var scriptChanges = ScriptChangesPreference();
            return new ActivitySnapshot
            {
                sampled_at = now, is_playing = playing, is_paused = EditorApplication.isPaused,
                is_playing_or_will_change_playmode = willPlay, execution_mode = mode,
                mode_observed_since = _modeSince, is_compiling = compiling, is_updating = updating,
                compiling_observed_since = _compilingSince, updating_observed_since = _updatingSince,
                last_compile_event = _lastEvent, last_compile_event_at = _lastEventAt,
                last_compile_started_at = _lastCompileStart, last_compile_finished_at = _lastCompileFinish,
                active_assemblies = assemblies.ToArray(), assembly_reload_state = _reload,
                last_operation = _lastOperation, last_operation_request_id = _lastRequest,
                last_operation_at = _lastOperationAt, script_changes_while_playing = scriptChanges.value,
                script_changes_while_playing_supported = scriptChanges.supported,
                script_changes_while_playing_source = scriptChanges.source,
                script_changes_while_playing_reason = scriptChanges.reason,
                script_changes_while_playing_raw_value = scriptChanges.raw_value,
                script_changes_while_playing_default_used = scriptChanges.default_used,
                last_refresh_request_id = _lastRefreshRequest, last_refresh_at = _lastRefreshAt,
                last_import_request_id = _lastImportRequest, last_import_at = _lastImportAt,
                last_compile_request_id = _lastCompileRequest, last_compile_request_at = _lastCompileRequestAt
            };
        }

        private sealed class ScriptChangesSetting
        {
            internal string value = "unknown", source = "EditorPrefs.ScriptCompilationDuringPlay", reason;
            internal bool supported, default_used;
            internal int raw_value = -1;
        }

        private static ScriptChangesSetting ScriptChangesPreference()
        {
            var result = new ScriptChangesSetting();
            // Verified against UnityCsReference/2022.3: Editor/Mono/EditorApplication.cs (enum)
            // and Editor/Mono/PreferencesWindow/PreferencesSettingsProviders.cs (key and default).
            if (!Application.unityVersion.StartsWith("2022.3.", StringComparison.Ordinal))
            { result.reason = "preference_mapping_not_verified_for_unity_version"; return result; }
            try
            {
                result.default_used = !EditorPrefs.HasKey("ScriptCompilationDuringPlay");
                result.raw_value = EditorPrefs.GetInt("ScriptCompilationDuringPlay", 0);
                switch (result.raw_value)
                {
                    case 0: result.value = "recompile_and_continue_playing"; break;
                    case 1: result.value = "recompile_after_finished_playing"; break;
                    case 2: result.value = "stop_playing_and_recompile"; break;
                    default: result.reason = "unrecognized_preference_value"; return result;
                }
                result.supported = true;
                result.reason = result.default_used ? "unity_2022_3_default" : "editor_preference_read";
            }
            catch (Exception exception) { result.reason = "preference_read_failed: " + exception.GetType().Name; }
            return result;
        }

        internal static bool ModeMatches(string required)
        {
            var state = Snapshot();
            return required == "any" || required == "edit" && state.execution_mode == "edit" &&
                !state.is_playing && !state.is_playing_or_will_change_playmode ||
                required == "play" && state.execution_mode == "play" && state.is_playing;
        }

        [Serializable]
        internal sealed class ActivitySnapshot
        {
            public string sampled_at;
            public bool is_playing, is_paused, is_playing_or_will_change_playmode;
            public string execution_mode, mode_observed_since;
            public bool is_compiling, is_updating;
            public string compiling_observed_since, updating_observed_since;
            public string last_compile_event, last_compile_event_at, last_compile_started_at, last_compile_finished_at;
            public string[] active_assemblies;
            public string assembly_reload_state, last_operation, last_operation_request_id, last_operation_at;
            public string script_changes_while_playing;
            public bool script_changes_while_playing_supported, script_changes_while_playing_default_used;
            public string script_changes_while_playing_source, script_changes_while_playing_reason;
            public int script_changes_while_playing_raw_value;
            public string last_refresh_request_id, last_refresh_at, last_import_request_id, last_import_at;
            public string last_compile_request_id, last_compile_request_at;
        }
    }
}
#endif
