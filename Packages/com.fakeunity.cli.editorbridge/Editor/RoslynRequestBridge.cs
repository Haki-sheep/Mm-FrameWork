#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using Process = System.Diagnostics.Process;

namespace FakeUnityCLI.EditorBridge
{
    [Serializable]
    internal sealed class OnlineRequest
    {
        public string protocol_version;
        public string request_id;
        public string submitted_at;
        public string editor_instance_id;
        public string submitted_session_id;
        public string operation;
        public string code;
        public string code_sha256;
        public string payload_sha256;
        public string payload_json;
        public string[] paths;
        public int timeout_seconds;
        public string expires_at;
        public int replay_count;
    }

    [Serializable]
    internal sealed class OnlineResponse
    {
        public string protocol_version = "1.1";
        public string request_id;
        public string state;
        public bool success;
        public string submitted_at;
        public string started_at;
        public string completed_at;
        public int editor_pid;
        public string editor_instance_id;
        public string editor_session_id;
        public string unity_version;
        public string operation;
        public string code_sha256;
        public string payload_sha256;
        public string result_json;
        public string result_text;
        public string result_type;
        public string error_type;
        public string error_message;
        public string error_stack_trace;
        public RoslynDiagnostic[] diagnostics = new RoslynDiagnostic[0];
        public string[] artifacts = new string[0];
        public int compile_ms;
        public int execute_ms;
    }

    [Serializable]
    internal sealed class OnlineStatus
    {
        public string protocol_version = "1.1";
        public string state;
        public string detail;
        public string updated_at;
        public int editor_pid;
        public string editor_instance_id;
        public string bridge_version;
        public string process_role;
        public long process_started_at_ticks;
        public string editor_session_id;
        public string unity_version;
        public bool is_compiling;
        public bool is_updating;
        public long compile_generation;
        public string compile_status;
        public string active_request_id;
        public int pending_requests;
        public EditorActivityBridge.ActivitySnapshot activity;
    }

    public static class RoslynRequestBridge
    {
        internal const string ProtocolVersion = "1.1";
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        private static readonly string Root = Path.Combine("Library", "FakeUnityCLI");
        private static readonly string RequestDirectory = Path.Combine(Root, "requests");
        private static readonly string ProcessingDirectory = Path.Combine(Root, "processing");
        private static readonly string ResponseDirectory = Path.Combine(Root, "responses");
        private static readonly string StatusPath = Path.Combine(Root, "online-status.json");
        private static int EditorPid { get { return BridgeBootstrap.EditorPid; } }
        private static double _nextTick;
        private static double _nextStatus;
        private static bool _executing;
        private static string _activeRequestId;
        private static string _smokeRequestId;
        private static string _smokeOutputPath;
        private static DateTime _smokeDeadline;
        private static bool _started;

        internal static void Start()
        {
            if (_started) return;
            Directory.CreateDirectory(RequestDirectory);
            Directory.CreateDirectory(ProcessingDirectory);
            Directory.CreateDirectory(ResponseDirectory);
            RecoverInterruptedRequests();
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            WriteStatus("ready", "Waiting for online requests");
            _started = true;
        }

        internal static void Stop(string state, string detail)
        {
            if (!_started) return;
            WriteStatus(state, detail);
            EditorApplication.update -= Tick;
            _started = false;
        }

        private static void Tick()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now >= _nextStatus)
            {
                _nextStatus = now + 1.0;
                WriteStatus(EditorApplication.isCompiling || EditorApplication.isUpdating ? "deferred" : _executing ? "busy" : "ready",
                    EditorApplication.isCompiling ? "Unity is compiling" : EditorApplication.isUpdating ? "Unity is updating assets" : _executing ? "Executing request" : "Waiting for online requests");
            }
            if (_executing || EditorApplication.isCompiling || EditorApplication.isUpdating || now < _nextTick) return;
            _nextTick = now + 0.1;
            ProcessNextRequest();
        }

        private static void ProcessNextRequest()
        {
            var requestPath = Directory.GetFiles(RequestDirectory, "*.json").OrderBy(path => path, StringComparer.Ordinal).FirstOrDefault();
            if (requestPath == null) return;
            var processingPath = Path.Combine(ProcessingDirectory, Path.GetFileName(requestPath));
            try { File.Move(requestPath, processingPath); }
            catch (IOException) { return; }

            OnlineRequest request = null;
            var markerPath = processingPath + ".started";
            _executing = true;
            try
            {
                request = JsonUtility.FromJson<OnlineRequest>(File.ReadAllText(processingPath, Utf8));
                ValidateRequest(request);
                _activeRequestId = request.request_id;
                var startedAt = DateTime.UtcNow.ToString("o");
                if (IsExpired(request))
                {
                    WriteResponse(new OnlineResponse
                    {
                        request_id = request.request_id, operation = request.operation,
                        state = "expired", success = false, submitted_at = request.submitted_at,
                        started_at = startedAt, completed_at = DateTime.UtcNow.ToString("o"),
                        editor_pid = EditorPid, editor_instance_id = BridgeBootstrap.EditorInstanceId,
                        editor_session_id = LogCaptureBridge.CurrentSessionId, unity_version = Application.unityVersion,
                        code_sha256 = request.code_sha256, payload_sha256 = request.payload_sha256,
                        error_type = "RequestExpired", error_message = "The online request expired before Editor execution started."
                    });
                    return;
                }
                File.WriteAllText(markerPath, startedAt, Utf8);
                var execution = ExecuteGuarded(request);
                WriteResponse(new OnlineResponse
                {
                    request_id = request.request_id,
                    operation = request.operation,
                    state = execution.State,
                    success = execution.Success,
                    submitted_at = request.submitted_at,
                    started_at = startedAt,
                    completed_at = DateTime.UtcNow.ToString("o"),
                    editor_pid = EditorPid,
                    editor_instance_id = BridgeBootstrap.EditorInstanceId,
                    editor_session_id = LogCaptureBridge.CurrentSessionId,
                    unity_version = Application.unityVersion,
                    code_sha256 = request.code_sha256,
                    payload_sha256 = request.payload_sha256,
                    result_json = execution.ResultJson,
                    result_text = execution.ResultText,
                    result_type = execution.ResultType,
                    error_type = execution.ErrorType,
                    error_message = execution.ErrorMessage,
                    error_stack_trace = execution.ErrorStackTrace,
                    diagnostics = execution.Diagnostics,
                    artifacts = execution.Artifacts,
                    compile_ms = execution.CompileMilliseconds,
                    execute_ms = execution.ExecuteMilliseconds
                });
            }
            catch (Exception exception)
            {
                WriteResponse(new OnlineResponse
                {
                    request_id = request == null || String.IsNullOrWhiteSpace(request.request_id) ? Path.GetFileNameWithoutExtension(processingPath) : request.request_id,
                    operation = request == null ? null : request.operation,
                    state = "bridge_error", success = false, submitted_at = request == null ? null : request.submitted_at,
                    started_at = DateTime.UtcNow.ToString("o"), completed_at = DateTime.UtcNow.ToString("o"),
                    editor_pid = EditorPid, editor_session_id = LogCaptureBridge.CurrentSessionId,
                    editor_instance_id = BridgeBootstrap.EditorInstanceId,
                    unity_version = Application.unityVersion, code_sha256 = request == null ? null : request.code_sha256,
                    payload_sha256 = request == null ? null : request.payload_sha256,
                    error_type = exception.GetType().FullName, error_message = exception.Message, error_stack_trace = exception.ToString()
                });
            }
            finally
            {
                TryDelete(markerPath); TryDelete(processingPath);
                _activeRequestId = null; _executing = false;
            }
        }

        private static void ValidateRequest(OnlineRequest request)
        {
            if (request == null) throw new InvalidDataException("Request JSON is invalid.");
            if (request.protocol_version != ProtocolVersion) throw new InvalidDataException("Unsupported request protocol version: " + request.protocol_version);
            if (String.IsNullOrWhiteSpace(request.request_id)) throw new InvalidDataException("request_id is required.");
            if (!String.Equals(request.editor_instance_id, BridgeBootstrap.EditorInstanceId, StringComparison.Ordinal))
                throw new InvalidDataException("Request targets a different Editor instance.");
            if (String.IsNullOrWhiteSpace(request.operation)) request.operation = "roslyn-exec";
            if (request.operation == "roslyn-exec" && String.IsNullOrWhiteSpace(request.code))
                throw new InvalidDataException("code is required for roslyn-exec.");
            if (request.operation != "roslyn-exec" && request.operation != EditorOnlineOperations.RefreshOperation &&
                request.operation != EditorOnlineOperations.ImportOperation &&
                request.operation != EditorOnlineOperations.CompileRequestOperation &&
                request.operation != EditorOnlineOperations.ContextOperation &&
                request.operation != EditorOnlineOperations.SceneHierarchyOperation &&
                request.operation != EditorOnlineOperations.SceneFindOperation &&
                request.operation != EditorOnlineOperations.ComponentListOperation &&
                request.operation != EditorOnlineOperations.ComponentGetOperation &&
                request.operation != EditorOnlineOperations.ComponentSetOperation &&
                request.operation != EditorOnlineOperations.ScreenshotOperation &&
                request.operation != EditorOnlineOperations.GameViewResolutionListOperation &&
                request.operation != EditorOnlineOperations.GameViewResolutionGetOperation &&
                request.operation != EditorOnlineOperations.GameViewResolutionSetOperation &&
                request.operation != EditorOnlineOperations.TestRunOperation &&
                request.operation != EditorOnlineOperations.TestStatusOperation &&
                request.operation != EditorOnlineOperations.TestResultOperation &&
                request.operation != EditorOnlineOperations.TestRecoverOperation &&
                request.operation != EditorOnlineOperations.PrefabExtractOperation &&
                request.operation != EditorOnlineOperations.PrefabExtractUndoOperation &&
                request.operation != EditorOnlineOperations.ExternalImportOperation &&
                request.operation != EditorOnlineOperations.ExternalImportUndoOperation &&
                request.operation != EditorOnlineOperations.ExternalImportStatusOperation &&
                request.operation != OnlineProviderRegistry.ExecuteOperation &&
                !EditorInspectorOperations.Handles(request.operation) && !EditorPrefabOperations.Handles(request.operation) &&
                !EditorUiOperations.Handles(request.operation) && !LiveBridgeUpdate.IsOperation(request.operation) &&
                !EditorDevelopmentOperations.Handles(request.operation))
                throw new InvalidDataException("Unsupported online operation: " + request.operation);
            if (EditorDevelopmentOperations.Handles(request.operation))
            {
                if (String.IsNullOrWhiteSpace(request.payload_json)) throw new InvalidDataException("Development operations require payload_json.");
                if (String.IsNullOrWhiteSpace(request.editor_instance_id) || String.IsNullOrWhiteSpace(request.submitted_session_id) ||
                    request.submitted_session_id != LogCaptureBridge.CurrentSessionId)
                    throw new InvalidDataException("Development operation requires the current Editor instance and session identity.");
            }
            if (request.operation == EditorOnlineOperations.ImportOperation && (request.paths == null || request.paths.Length == 0))
                throw new InvalidDataException("paths are required for asset-import.");
            if (request.operation == EditorOnlineOperations.PrefabExtractOperation &&
                (request.paths == null || request.paths.Length != 2 || String.IsNullOrWhiteSpace(request.payload_json)))
                throw new InvalidDataException("prefab-extract-child requires two paths and payload_json.");
            if (request.operation == EditorOnlineOperations.PrefabExtractUndoOperation && String.IsNullOrWhiteSpace(request.payload_json))
                throw new InvalidDataException("prefab-extract-child-undo requires payload_json.");
            if (request.operation == OnlineProviderRegistry.ExecuteOperation && String.IsNullOrWhiteSpace(request.payload_json))
                throw new InvalidDataException("provider-exec requires payload_json.");
            if (request.operation == EditorOnlineOperations.ComponentSetOperation && String.IsNullOrWhiteSpace(request.payload_json))
                throw new InvalidDataException("component-set requires payload_json.");
            if ((request.operation == EditorOnlineOperations.TestRunOperation ||
                 request.operation == EditorOnlineOperations.TestStatusOperation ||
                 request.operation == EditorOnlineOperations.TestResultOperation ||
                 request.operation == EditorOnlineOperations.TestRecoverOperation) && String.IsNullOrWhiteSpace(request.payload_json))
                throw new InvalidDataException("test operations require payload_json.");
            if ((request.operation == EditorOnlineOperations.ExternalImportOperation ||
                 request.operation == EditorOnlineOperations.ExternalImportUndoOperation ||
                 request.operation == EditorOnlineOperations.ExternalImportStatusOperation) && String.IsNullOrWhiteSpace(request.payload_json))
                throw new InvalidDataException("External asset import operations require payload_json.");
            if (LiveBridgeUpdate.IsOperation(request.operation) && String.IsNullOrWhiteSpace(request.payload_json))
                throw new InvalidDataException("Bridge live update operations require payload_json.");
        }

        private static void RecoverInterruptedRequests()
        {
            foreach (var processingPath in Directory.GetFiles(ProcessingDirectory, "*.json"))
            {
                var markerPath = processingPath + ".started";
                var requestId = Path.GetFileNameWithoutExtension(processingPath);
                var completedPath = Path.Combine(ResponseDirectory, requestId + ".json");
                if (File.Exists(completedPath))
                {
                    TryDelete(markerPath); TryDelete(processingPath);
                    continue;
                }
                if (File.Exists(markerPath))
                {
                    OnlineRequest interrupted = null;
                    try { interrupted = JsonUtility.FromJson<OnlineRequest>(File.ReadAllText(processingPath, Utf8)); }
                    catch { }
                    if (interrupted != null && IsStructuredOperation(interrupted.operation) &&
                        interrupted.replay_count < 1 && !IsExpired(interrupted))
                    {
                        // Refresh/import are idempotent. A domain reload may interrupt response writing,
                        // so replay once after reload instead of leaving an automation request pending.
                        interrupted.replay_count++;
                        var replayPath = Path.Combine(RequestDirectory, Path.GetFileName(processingPath));
                        if (!File.Exists(replayPath))
                            AtomicWrite(replayPath, JsonUtility.ToJson(interrupted, true));
                        TryDelete(markerPath); TryDelete(processingPath);
                        continue;
                    }
                    WriteResponse(new OnlineResponse
                    {
                        request_id = requestId, operation = interrupted == null ? null : interrupted.operation,
                        state = "outcome_unknown", success = false,
                        completed_at = DateTime.UtcNow.ToString("o"), editor_pid = EditorPid,
                        editor_instance_id = BridgeBootstrap.EditorInstanceId,
                        editor_session_id = LogCaptureBridge.CurrentSessionId, unity_version = Application.unityVersion,
                        payload_sha256 = interrupted == null ? null : interrupted.payload_sha256,
                        error_type = IsExpired(interrupted) ? "RequestExpiredAfterStart" : "AssemblyReload",
                        error_message = IsExpired(interrupted)
                            ? "The online request expired after execution started and the Editor reloaded; the outcome is unknown."
                            : "The Editor reloaded after execution started; the request was not replayed."
                    });
                    TryDelete(markerPath); TryDelete(processingPath);
                    continue;
                }
                var requestPath = Path.Combine(RequestDirectory, Path.GetFileName(processingPath));
                if (!File.Exists(requestPath)) File.Move(processingPath, requestPath);
                else TryDelete(processingPath);
            }
        }

        private static bool IsStructuredOperation(string operation)
        {
            return String.Equals(operation, EditorOnlineOperations.RefreshOperation, StringComparison.Ordinal) ||
                   String.Equals(operation, EditorOnlineOperations.ImportOperation, StringComparison.Ordinal) ||
                   String.Equals(operation, EditorOnlineOperations.ContextOperation, StringComparison.Ordinal) ||
                   String.Equals(operation, EditorOnlineOperations.SceneHierarchyOperation, StringComparison.Ordinal) ||
                   String.Equals(operation, EditorOnlineOperations.SceneFindOperation, StringComparison.Ordinal) ||
                   String.Equals(operation, EditorOnlineOperations.ComponentListOperation, StringComparison.Ordinal) ||
                   String.Equals(operation, EditorOnlineOperations.ComponentGetOperation, StringComparison.Ordinal) ||
                   String.Equals(operation, EditorOnlineOperations.GameViewResolutionListOperation, StringComparison.Ordinal) ||
                   String.Equals(operation, EditorOnlineOperations.GameViewResolutionGetOperation, StringComparison.Ordinal) ||
                   String.Equals(operation, EditorOnlineOperations.TestStatusOperation, StringComparison.Ordinal) ||
                   String.Equals(operation, EditorOnlineOperations.TestResultOperation, StringComparison.Ordinal);
        }

        private static RoslynExecutionResult ExecuteGuarded(OnlineRequest request)
        {
            try
            {
                var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var readOnlyOperation = request.operation == EditorOnlineOperations.ExternalImportStatusOperation ||
                    EditorUiOperations.ReadOnly(request.operation) ||
                    request.operation == EditorOnlineOperations.ContextOperation ||
                    request.operation == EditorInspectorOperations.GetOperation ||
                    request.operation == EditorPrefabOperations.ReadOperation ||
                    request.operation == EditorPrefabOperations.ResultOperation ||
                    request.operation == EditorOnlineOperations.SceneHierarchyOperation ||
                    request.operation == EditorOnlineOperations.SceneFindOperation ||
                    request.operation == EditorOnlineOperations.ComponentListOperation ||
                    request.operation == EditorOnlineOperations.ComponentGetOperation ||
                    request.operation == EditorOnlineOperations.ScreenshotOperation && EditorOnlineOperations.IsReadOnlyScreenshot(request.payload_json) ||
                    request.operation == EditorOnlineOperations.GameViewResolutionListOperation ||
                    request.operation == EditorOnlineOperations.GameViewResolutionGetOperation ||
                    request.operation == EditorOnlineOperations.TestStatusOperation ||
                    request.operation == EditorOnlineOperations.TestResultOperation ||
                    request.operation == OnlineProviderRegistry.ExecuteOperation && OnlineProviderRegistry.IsReadOnlyOperation(request.payload_json);
                using (var protection = readOnlyOperation ? null : FakeUnity.Preservation.EditorPreservationGuard.EnterWrite(root))
                {
                    var requiredMode = RequiredExecutionMode(request);
                    if (requiredMode == "any" && (request.operation == EditorOnlineOperations.RefreshOperation ||
                        request.operation == EditorOnlineOperations.ImportOperation || request.operation == EditorOnlineOperations.ExternalImportOperation ||
                        request.operation == EditorOnlineOperations.ExternalImportUndoOperation) &&
                        !EditorActivityBridge.ModeMatches("edit") && !EditorActivityBridge.ModeMatches("play"))
                        return new RoslynExecutionResult { Success = false, State = "execution_mode_required",
                            ErrorType = "EditorExecutionModeRequired", ErrorMessage = "Import requires stable Edit or Play; mode transition is still in progress. No import was performed." };
                    if (!EditorActivityBridge.ModeMatches(requiredMode))
                        return new RoslynExecutionResult { Success = false, State = "execution_mode_required",
                            ErrorType = "EditorExecutionModeRequired", ErrorMessage = "Operation requires " + requiredMode + " mode; no Editor API was invoked." };
                    return request.operation == "roslyn-exec" ? RoslynExecutor.Execute(request.code) :
                        EditorDevelopmentOperations.Handles(request.operation) ? EditorDevelopmentOperations.Execute(request) :
                        EditorOnlineOperations.Execute(request.operation, request.paths, request.payload_json, request.request_id);
                }
            }
            catch (FakeUnity.Preservation.PreservationException ex)
            {
                return new RoslynExecutionResult { Success = false, State = "editor_protected",
                    ErrorType = "EditorPreservationRequired", ErrorMessage = ex.Message };
            }
        }

        private static string RequiredExecutionMode(OnlineRequest request)
        {
            if (request.operation == EditorUiOperations.ClickOperation) return "play";
            if (request.operation == EditorDevelopmentOperations.RuntimeSetOperation) return "play";
            if (request.operation == EditorDevelopmentOperations.StopPlayOperation) return "any";
            if (request.operation == EditorOnlineOperations.CompileRequestOperation) return "edit";
            var assetMutation = request.operation == EditorPrefabOperations.EditOperation || request.operation == EditorPrefabOperations.UndoOperation;
            var inspectorMutation = request.operation == EditorInspectorOperations.SetOperation || request.operation == EditorInspectorOperations.UndoOperation;
            if (assetMutation || inspectorMutation)
            {
                var mutationPolicy = EditorInspectorOperations.Parse(request.payload_json);
                if ((string)mutationPolicy["development_mode"] == "free" && (string)mutationPolicy["execution_mode"] == "any" &&
                    (assetMutation || EditorInspectorOperations.IsAssetWrite(request.operation, request.payload_json))) return "any";
                return "edit";
            }
            if (String.IsNullOrWhiteSpace(request.payload_json)) return "any";
            var policy = JsonUtility.FromJson<ExecutionPolicy>(request.payload_json);
            return policy == null || String.IsNullOrEmpty(policy.execution_mode) ? "any" : policy.execution_mode;
        }

        [Serializable] private sealed class ExecutionPolicy { public string execution_mode; }

        private static bool IsExpired(OnlineRequest request)
        {
            if (request == null) return false;
            DateTime expires;
            if (!String.IsNullOrWhiteSpace(request.expires_at) && DateTime.TryParse(request.expires_at,
                    CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out expires))
                return DateTime.UtcNow > expires.ToUniversalTime();
            DateTime submitted;
            return request.timeout_seconds > 0 && !String.IsNullOrWhiteSpace(request.submitted_at) &&
                   DateTime.TryParse(request.submitted_at, CultureInfo.InvariantCulture,
                       DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out submitted) &&
                   DateTime.UtcNow > submitted.ToUniversalTime().AddSeconds(request.timeout_seconds);
        }

        private static void WriteResponse(OnlineResponse response)
        {
            Directory.CreateDirectory(ResponseDirectory);
            AtomicWrite(Path.Combine(ResponseDirectory, response.request_id + ".json"), JsonUtility.ToJson(response, true));
        }

        private static void WriteStatus(string state, string detail)
        {
            try
            {
                var status = new OnlineStatus
                {
                    state = state, detail = detail, updated_at = DateTime.UtcNow.ToString("o"), editor_pid = EditorPid,
                    editor_instance_id = BridgeBootstrap.EditorInstanceId, bridge_version = BridgeBootstrap.BridgeVersion,
                    process_role = "main_editor", process_started_at_ticks = BridgeBootstrap.ProcessStartedAtTicks,
                    editor_session_id = LogCaptureBridge.CurrentSessionId, unity_version = Application.unityVersion,
                    is_compiling = EditorApplication.isCompiling, is_updating = EditorApplication.isUpdating,
                    compile_generation = LogCaptureBridge.CurrentCompileGeneration,
                    compile_status = LogCaptureBridge.CurrentCompileStatus,
                    active_request_id = _activeRequestId,
                    pending_requests = Directory.Exists(RequestDirectory) ? Directory.GetFiles(RequestDirectory, "*.json").Length : 0,
                    activity = EditorActivityBridge.Snapshot()
                };
                AtomicWrite(StatusPath, JsonUtility.ToJson(status, true));
            }
            catch (Exception exception) { System.Diagnostics.Debug.WriteLine(exception); }
        }

        private static void AtomicWrite(string path, string content)
        {
            var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(temporary, content, Utf8);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        public static void RunSmoke()
        {
            if (!BridgeBootstrap.EnsureStarted())
            {
                Debug.LogError("FakeUnityCLI Bridge could not acquire main Editor ownership.");
                EditorApplication.Exit(1);
                return;
            }
            _smokeOutputPath = CommandLine("-fucRoslynResult");
            if (String.IsNullOrWhiteSpace(_smokeOutputPath))
            {
                Debug.LogError("-fucRoslynResult is required."); EditorApplication.Exit(1); return;
            }
            _smokeRequestId = "smoke-" + Guid.NewGuid().ToString("N");
            _smokeDeadline = DateTime.UtcNow.AddSeconds(60);
            var request = new OnlineRequest
            {
                protocol_version = ProtocolVersion, request_id = _smokeRequestId,
                submitted_at = DateTime.UtcNow.ToString("o"),
                editor_instance_id = BridgeBootstrap.EditorInstanceId,
                submitted_session_id = LogCaptureBridge.CurrentSessionId,
                operation = "roslyn-exec",
                // The unused local intentionally produces a non-Hidden Roslyn warning. The real smoke
                // therefore covers diagnostic formatting as well as successful execution.
                code = "using UnityEngine; public static class __FakeUnityCLISmoke { public static object Execute() { int diagnosticSmoke = 1; return new { marker = \"roslyn-online-ok\", unityVersion = Application.unityVersion, nested = new { ok = true, count = 2 }, values = new[] { 1, 2, 3 } }; } }",
                timeout_seconds = 30,
                expires_at = DateTime.UtcNow.AddSeconds(30).ToString("o")
            };
            AtomicWrite(Path.Combine(RequestDirectory, _smokeRequestId + ".json"), JsonUtility.ToJson(request, true));
            EditorApplication.update -= PollSmoke;
            EditorApplication.update += PollSmoke;
        }

        private static void PollSmoke()
        {
            var responsePath = Path.Combine(ResponseDirectory, _smokeRequestId + ".json");
            if (File.Exists(responsePath))
            {
                EditorApplication.update -= PollSmoke;
                var text = File.ReadAllText(responsePath, Utf8);
                AtomicWrite(_smokeOutputPath, text);
                var response = JsonUtility.FromJson<OnlineResponse>(text);
                EditorApplication.Exit(response != null && response.success && response.result_text == "roslyn-online-ok" ? 0 : 1);
                return;
            }
            if (DateTime.UtcNow <= _smokeDeadline) return;
            EditorApplication.update -= PollSmoke;
            AtomicWrite(_smokeOutputPath, "{\"success\":false,\"state\":\"timeout\"}");
            EditorApplication.Exit(1);
        }

        private static string CommandLine(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (var index = 0; index + 1 < args.Length; index++) if (args[index] == key) return args[index + 1];
            return null;
        }
    }
}
#endif
