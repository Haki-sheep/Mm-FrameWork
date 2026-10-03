#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FakeUnityCLI.EditorBridge
{
    /// <summary>
    /// Fixed, structured online Editor operations. These commands deliberately avoid arbitrary
    /// Roslyn source so routine refresh/import automation has a stable machine contract.
    /// </summary>
    internal static class EditorOnlineOperations
    {
        internal const string RefreshOperation = "asset-database-refresh";
        internal const string ImportOperation = "asset-import";
        internal const string CompileRequestOperation = "compile-request";
        internal const string PrefabExtractOperation = "prefab-extract-child";
        internal const string PrefabExtractUndoOperation = "prefab-extract-child-undo";
        internal const string ExternalImportOperation = ExternalAssetImportBridge.ImportOperation;
        internal const string ExternalImportUndoOperation = ExternalAssetImportBridge.UndoOperation;
        internal const string ExternalImportStatusOperation = ExternalAssetImportBridge.StatusOperation;
        internal const string ContextOperation = "editor-context";
        internal const string SceneHierarchyOperation = "editor-scene-hierarchy";
        internal const string SceneFindOperation = "editor-scene-find";
        internal const string ComponentListOperation = "editor-component-list";
        internal const string ComponentGetOperation = "editor-component-get";
        internal const string ComponentSetOperation = "editor-component-set";
        internal const string ScreenshotOperation = "editor-screenshot";
        internal const string GameViewResolutionListOperation = "editor-gameview-resolution-list";
        internal const string GameViewResolutionGetOperation = "editor-gameview-resolution-get";
        internal const string GameViewResolutionSetOperation = "editor-gameview-resolution-set";
        internal const string TestRunOperation = "editor-test-run";
        internal const string TestStatusOperation = "editor-test-status";
        internal const string TestResultOperation = "editor-test-result";
        internal const string TestRecoverOperation = "editor-test-recover";

        [Serializable]
        private sealed class TestRunState
        {
            public string run_id;
            public string test_mode;
            public string filter;
            public string status;
            public string started_at;
            public string finished_at;
            public string interrupted_at;
            public int total;
            public int passed;
            public int failed;
            public int skipped;
            public int inconclusive;
            public int other;
            public string[] failed_tests = new string[0];
            public TestFailureDetail[] failure_details = new TestFailureDetail[0];
            public string error;
            public string request_id;
            public string unity_run_id;
            public string editor_instance_id;
            public string editor_session_id;
            public string result_state;
            public string interruption_reason;
            public string deadline_at;
            public bool deadline_exceeded;
            public int completed;
            public bool failure_details_truncated;
            public bool owned_final_result;
            public bool native_completion_verified;
            public bool ownership_lost;
            public string persistence_state;
            public string cleanup_state;
            public string cleanup_queued_at;
            public string cleanup_finished_at;
            public int cleanup_attempts;
            public EditorTestRunContract.Failure persistence_error;
            public EditorTestRunContract.Failure last_persistence_error;
            public int persistence_attempts;
            public EditorTestRunContract.Failure ownership_error;
            public EditorTestRunContract.Failure native_error;
            public EditorTestRunContract.Failure cleanup_error;
        }

        [Serializable]
        private sealed class TestFailureDetail
        {
            public string test_name;
            public string message;
            public string stack_trace;
        }

        private static readonly Dictionary<string, TestRunState> TestRuns = new Dictionary<string, TestRunState>(StringComparer.Ordinal);
        private static readonly Dictionary<string, TestRunnerApi> ActiveTestRunners = new Dictionary<string, TestRunnerApi>(StringComparer.Ordinal);
        private static readonly Dictionary<string, TestRunCallbacks> ActiveTestCallbacks = new Dictionary<string, TestRunCallbacks>(StringComparer.Ordinal);

        private sealed class PendingTestDisposal
        {
            internal string RunId;
            internal TestRunState State;
            internal TestRunnerApi Api;
        }

        private static readonly EditorTestRunContract.DeferredDisposals<PendingTestDisposal> PendingTestDisposals =
            new EditorTestRunContract.DeferredDisposals<PendingTestDisposal>();

        [InitializeOnLoadMethod]
        private static void RecoverInterruptedTestRuns()
        {
            if (!BridgeBootstrap.EnsureStarted()) return;
            EditorApplication.update -= ObserveTestRuns;
            EditorApplication.update += ObserveTestRuns;
            try
            {
                var directory = Path.Combine(ProjectRoot(), "Library", "FakeUnityCLI", "test-runs");
                if (!Directory.Exists(directory)) return;
                foreach (var path in Directory.GetFiles(directory, "*.json"))
                {
                    TestRunState state;
                    try { state = JsonUtility.FromJson<TestRunState>(File.ReadAllText(path, Encoding.UTF8)); }
                    catch { continue; }
                    if (state == null || !EditorTestRunContract.IsValidRunId(state.run_id) ||
                        Path.GetFileNameWithoutExtension(path) != state.run_id || state.status != "running") continue;
                    state.status = "outcome_unknown";
                    state.interruption_reason = "editor_or_domain_reload";
                    state.error = "Unity Editor domain reloaded while the TestRunner was active; final outcome is unknown.";
                    state.interrupted_at = DateTime.UtcNow.ToString("o");
                    state.finished_at = state.interrupted_at;
                    TestRuns[state.run_id] = state;
                    PersistTestRun(state);
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning("[FakeUnityCLI] TestRunner recovery skipped: " + exception.Message);
            }
        }

        internal static RoslynExecutionResult Execute(string operation, string[] requestedPaths,
            string payloadJson, string requestId)
        {
            var result = new RoslynExecutionResult { State = "runtime_error" };
            bool? structuredSuccess = null;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                if (String.Equals(operation, RefreshOperation, StringComparison.Ordinal))
                {
                    EditorActivityBridge.RecordOperation(operation, requestId);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    result.ResultText = "AssetDatabase refresh completed";
                    result.ResultType = typeof(string).FullName;
                    result.ResultJson = ResultJson.Serialize(new Dictionary<string, object>
                    {
                        { "operation", RefreshOperation },
                        { "refreshed", true }
                    });
                }
                else if (String.Equals(operation, ImportOperation, StringComparison.Ordinal))
                {
                    EditorActivityBridge.RecordOperation(operation, requestId);
                    var paths = (requestedPaths ?? new string[0]).Select(NormalizeAssetPath).Distinct(StringComparer.Ordinal).ToArray();
                    if (paths.Length == 0)
                        throw new ArgumentException("asset-import requires at least one Assets/ or Packages/ path.");
                    foreach (var path in paths)
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    result.Artifacts = paths;
                    result.ResultText = paths.Length + " asset path(s) imported";
                    result.ResultType = typeof(string[]).FullName;
                    result.ResultJson = ResultJson.Serialize(new Dictionary<string, object>
                    {
                        { "operation", ImportOperation },
                        { "imported", paths }
                    });
                }
                else if (String.Equals(operation, PrefabExtractOperation, StringComparison.Ordinal))
                {
                    var paths = (requestedPaths ?? new string[0]).Select(NormalizeAssetPath).ToArray();
                    if (paths.Length != 2 || paths[0] == paths[1])
                        throw new ArgumentException("prefab-extract-child requires distinct host and output Prefab paths.");
                    var payload = JsonUtility.FromJson<PrefabExtractPayload>(payloadJson ?? "{}");
                    var value = ExtractPrefabChild(paths[0], paths[1], payload, requestId);
                    result.Artifacts = payload.dry_run ? new string[0] : paths;
                    result.ResultText = payload.dry_run
                        ? "Prefab child extraction dry-run completed"
                        : "Prefab child extracted and connected";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                    result.ResultJson = ResultJson.Serialize(value);
                }
                else if (String.Equals(operation, PrefabExtractUndoOperation, StringComparison.Ordinal))
                {
                    var payload = JsonUtility.FromJson<PrefabExtractUndoPayload>(payloadJson ?? "{}");
                    var value = UndoPrefabExtract(payload.operation_id);
                    result.ResultText = "Prefab child extraction restored";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                    result.ResultJson = ResultJson.Serialize(value);
                }
                else if (String.Equals(operation, CompileRequestOperation, StringComparison.Ordinal))
                {
                    EditorActivityBridge.RecordOperation(operation, requestId);
                    CompilationPipeline.RequestScriptCompilation();
                    result.ResultText = "Script compilation requested";
                    result.ResultType = typeof(string).FullName;
                    result.ResultJson = ResultJson.Serialize(new Dictionary<string, object>
                    {
                        { "operation", CompileRequestOperation }, { "requested", true }
                    });
                }
                else if (String.Equals(operation, ContextOperation, StringComparison.Ordinal))
                {
                    var value = ReadEditorContext();
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = "Editor context read";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, SceneHierarchyOperation, StringComparison.Ordinal))
                {
                    var value = ReadSceneHierarchy(payloadJson);
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = "Scene hierarchy read";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, SceneFindOperation, StringComparison.Ordinal))
                {
                    var value = FindSceneObjects(payloadJson);
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = "Scene objects found";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, ComponentListOperation, StringComparison.Ordinal))
                {
                    var value = ListComponents(payloadJson);
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = "Component list read";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, ComponentGetOperation, StringComparison.Ordinal))
                {
                    var value = GetComponentSummary(payloadJson);
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = "Component summary read";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, ComponentSetOperation, StringComparison.Ordinal))
                {
                    var value = SetComponentProperty(payloadJson, requestId);
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = ((bool)value["changed"] ? "Component property set" : "Component property unchanged");
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (EditorUiOperations.Handles(operation))
                {
                    result.ResultJson = EditorUiOperations.Execute(operation, payloadJson, requestId);
                    result.ResultText = "UGUI operation completed";
                    result.ResultType = "UiOperationResult";
                }
                else if (EditorPrefabOperations.Handles(operation))
                {
                    result.ResultJson = EditorPrefabOperations.Execute(operation, payloadJson, requestId);
                    result.ResultText = "Scoped Prefab operation completed";
                    result.ResultType = "ScopedPrefabResult";
                }
                else if (EditorInspectorOperations.Handles(operation))
                {
                    result.ResultJson = EditorInspectorOperations.Execute(operation, payloadJson, requestId);
                    result.ResultText = "Typed Inspector operation completed";
                    result.ResultType = "TypedInspectorResult";
                }
                else if (String.Equals(operation, ScreenshotOperation, StringComparison.Ordinal))
                {
                    var value = CaptureScreenshot(payloadJson, requestId);
                    result.Artifacts = new[] { (string)value["artifact_path"] };
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = "Editor screenshot captured";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, GameViewResolutionListOperation, StringComparison.Ordinal))
                {
                    var value = GameViewResolutionList();
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = "GameView resolutions listed";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, GameViewResolutionGetOperation, StringComparison.Ordinal))
                {
                    var value = GameViewResolutionGet();
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = "GameView resolution read";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, GameViewResolutionSetOperation, StringComparison.Ordinal))
                {
                    var value = GameViewResolutionSet(payloadJson, requestId);
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = "GameView resolution set";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, TestRunOperation, StringComparison.Ordinal))
                {
                    var value = StartEditModeTestRun(payloadJson, requestId);
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = "EditMode TestRunner accepted";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, TestStatusOperation, StringComparison.Ordinal) ||
                         String.Equals(operation, TestResultOperation, StringComparison.Ordinal))
                {
                    var value = ReadTestRun(payloadJson);
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = "EditMode TestRunner result read";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, TestRecoverOperation, StringComparison.Ordinal))
                {
                    result.ResultJson = ResultJson.Serialize(RecoverTestRun(payloadJson));
                    result.ResultText = "Original TestRunner evidence recovery inspected";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, ExternalImportOperation, StringComparison.Ordinal))
                {
                    var value = ExternalAssetImportBridge.Import(requestedPaths, payloadJson, requestId);
                    result.ResultJson = ResultJson.Serialize(value); result.ResultText = "External asset imported";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, ExternalImportUndoOperation, StringComparison.Ordinal))
                {
                    var value = ExternalAssetImportBridge.Undo(payloadJson);
                    result.ResultJson = ResultJson.Serialize(value); result.ResultText = "External asset import undone";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, ExternalImportStatusOperation, StringComparison.Ordinal))
                {
                    var value = ExternalAssetImportBridge.Status(payloadJson);
                    result.ResultJson = ResultJson.Serialize(value); result.ResultText = "External asset import receipt inspected";
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else if (String.Equals(operation, OnlineProviderRegistry.ExecuteOperation, StringComparison.Ordinal))
                {
                    var execution = OnlineProviderRegistry.Execute(payloadJson, requestId);
                    result.ResultJson = execution.ResultJson;
                    result.ResultText = execution.Success
                        ? "Project online provider operation completed"
                        : "Project online provider operation " + execution.State;
                    result.ResultType = "provider-result-json";
                    result.State = execution.State;
                    result.ErrorType = execution.Success ? null : "FakeUnityCLI.OnlineProviderFailure";
                    result.ErrorMessage = execution.Success ? null : execution.Error ?? "Project provider returned failure.";
                    structuredSuccess = execution.Success;
                }
                else if (LiveBridgeUpdate.IsOperation(operation))
                {
                    var value = LiveBridgeUpdate.Execute(operation, payloadJson);
                    result.ResultJson = ResultJson.Serialize(value);
                    result.ResultText = "Bridge live update " + value["state"];
                    result.ResultType = typeof(Dictionary<string, object>).FullName;
                }
                else
                {
                    throw new NotSupportedException("Unsupported structured Editor operation: " + operation);
                }

                stopwatch.Stop();
                result.ExecuteMilliseconds = Math.Max(0, (int)stopwatch.ElapsedMilliseconds);
                result.Success = structuredSuccess ?? true;
                if (structuredSuccess == null) result.State = "success";
                return result;
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                if (exception is EditorInspectorOperations.WriteOutcomeUnknownException) result.State = "outcome_unknown";
                result.ExecuteMilliseconds = Math.Max(0, (int)stopwatch.ElapsedMilliseconds);
                result.ErrorType = exception.GetType().FullName;
                result.ErrorMessage = exception.Message;
                result.ErrorStackTrace = exception.ToString();
                return result;
            }
        }

        private static string NormalizeAssetPath(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("Asset path cannot be empty.");
            var normalized = path.Replace('\\', '/').Trim();
            if (normalized.StartsWith("/", StringComparison.Ordinal) || normalized.Contains(":"))
                throw new ArgumentException("Asset path must be project-relative: " + path);
            var segments = normalized.Split('/');
            if (segments.Any(segment => segment == ".." || segment == "." || segment.Length == 0))
                throw new ArgumentException("Asset path contains an unsafe segment: " + path);
            if (!(normalized.StartsWith("Assets/", StringComparison.Ordinal) || normalized == "Assets" ||
                  normalized.StartsWith("Packages/", StringComparison.Ordinal) || normalized == "Packages"))
                throw new ArgumentException("Asset path must be under Assets/ or Packages/: " + path);
            return normalized;
        }

        private sealed class SceneHierarchyPayload
        {
            public int max_depth = 3;
            public int max_objects = 200;
            public bool include_inactive;
        }

        private sealed class SceneFindPayload
        {
            public string name_pattern;
            public string path_pattern;
            public string tag;
            public int layer = -1;
            public string component_type;
            public int limit = 50;
            public bool include_inactive;
        }

        private sealed class ComponentPayload
        {
            public string object_path;
            public string component_type;
            public string property;
            public string value;
            public bool dry_run;
            public string execution_mode;
        }

        private sealed class VectorPayload { public float x; public float y; public float z; }

        private sealed class ScreenshotPayload { public string target; }
        internal static bool IsReadOnlyScreenshot(string payloadJson) =>
            String.Equals(JsonUtility.FromJson<ScreenshotPayload>(payloadJson ?? "{}")?.target, "game", StringComparison.OrdinalIgnoreCase);
        private sealed class ResolutionPayload { public string value; }
        private sealed class TestRunPayload
        {
            public string test_mode; public string filter; public string run_id; public string execution_mode;
            public int run_timeout_seconds = EditorTestRunContract.DefaultTimeoutSeconds;
            public bool yes;
        }

        private static Dictionary<string, object> StartEditModeTestRun(string payloadJson, string requestId)
        {
            var payload = JsonUtility.FromJson<TestRunPayload>(payloadJson ?? "{}") ?? new TestRunPayload();
            if (!String.Equals(payload.test_mode, "EditMode", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Only EditMode TestRunner is implemented; PlayMode is not entered automatically.");
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("EditMode tests require an idle Edit Mode Editor.");
            if (payload.run_timeout_seconds < 1 || payload.run_timeout_seconds > 86400)
                throw new ArgumentException("run_timeout_seconds must be in range 1..86400.");
            ObserveTestRuns();
            if (ActiveTestRunners.Count != 0 || PendingTestDisposals.Count != 0 || ReadNativeTestRunIds().Length != 0 || IsNativeTestRunActive())
                throw new InvalidOperationException("A Unity TestRunner job is already active or scheduled; no second test was started.");
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isLoaded && (scene.isDirty || String.IsNullOrEmpty(scene.path)))
                    throw new InvalidOperationException("EditMode tests require every loaded scene to be saved and clean: " + scene.name);
            }
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("EditMode tests are not started while a Prefab Stage is open.");
            var runId = Guid.NewGuid().ToString("N");
            var state = new TestRunState
            {
                run_id = runId, test_mode = "EditMode", filter = payload.filter ?? String.Empty,
                status = "running", started_at = DateTime.UtcNow.ToString("o"), request_id = requestId,
                editor_instance_id = BridgeBootstrap.EditorInstanceId, editor_session_id = LogCaptureBridge.CurrentSessionId,
                deadline_at = DateTime.UtcNow.AddSeconds(payload.run_timeout_seconds).ToString("o")
            };
            TestRuns[runId] = state;
            if (!PersistTestRun(state)) return DescribeTestRun(state);
            var nativeOperation = "create_test_api";
            try
            {
                var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                var callbacks = new TestRunCallbacks(runId);
                ActiveTestRunners[runId] = api;
                ActiveTestCallbacks[runId] = callbacks;
                nativeOperation = "register_callbacks";
                api.RegisterCallbacks(callbacks);
                nativeOperation = "execute";
                var filter = new Filter { testMode = TestMode.EditMode };
                if (!String.IsNullOrWhiteSpace(state.filter)) filter.testNames = new[] { state.filter };
                state.unity_run_id = api.Execute(new ExecutionSettings { filters = new[] { filter } });
                PersistTestRun(state);
            }
            catch (Exception exception)
            {
                state.status = "error"; state.error = exception.Message; state.finished_at = DateTime.UtcNow.ToString("o");
                state.native_error = EditorTestRunContract.DescribeFailure("native", nativeOperation, null, exception);
                PersistTestRun(state); CleanupTestRunner(runId);
            }
            return DescribeTestRun(state);
        }

        private static TestRunState LoadTestRun(string payloadJson)
        {
            var payload = JsonUtility.FromJson<TestRunPayload>(payloadJson ?? "{}") ?? new TestRunPayload();
            if (!EditorTestRunContract.IsValidRunId(payload.run_id)) throw new ArgumentException("run_id must be a 32-character GUID (N format).");
            if (!TestRuns.TryGetValue(payload.run_id, out var state))
            {
                var path = TestRunPath(payload.run_id);
                if (!File.Exists(path)) throw new InvalidOperationException("Test run was not found: " + payload.run_id);
                state = JsonUtility.FromJson<TestRunState>(File.ReadAllText(path, Encoding.UTF8));
            }
            if (state == null || state.run_id != payload.run_id) throw new InvalidDataException("Persisted test run identity does not match the requested run.");
            return state;
        }

        private static Dictionary<string, object> ReadTestRun(string payloadJson) => DescribeTestRun(LoadTestRun(payloadJson));

        private static bool HasRecoveryEvidence(TestRunState state) =>
            state.result_state != null && EditorTestRunContract.CanRecover(state.owned_final_result,
                state.native_completion_verified, state.cleanup_state == "complete", state.ownership_lost,
                EditorTestRunContract.HasFailure(state.native_error), EditorTestRunContract.HasFailure(state.cleanup_error), state.request_id, state.unity_run_id,
                state.editor_instance_id, state.total, state.passed, state.failed, state.skipped, state.inconclusive, state.other);

        private static Dictionary<string, object> RecoverTestRun(string payloadJson)
        {
            var payload = JsonUtility.FromJson<TestRunPayload>(payloadJson ?? "{}") ?? new TestRunPayload();
            if (!payload.yes) throw new ArgumentException("test-recover requires explicit yes; queries never repair evidence.");
            var state = LoadTestRun(payloadJson);
            var recovery = "not_available";
            if (HasRecoveryEvidence(state) && !ActiveTestRunners.ContainsKey(state.run_id))
            {
                // A persisted completion checkpoint is authoritative across Reload. For this Editor also
                // require a fresh negative native probe; a failed probe must not mutate the old run.
                var native = NativeTestRunActive(state);
                if (state.editor_instance_id != BridgeBootstrap.EditorInstanceId || native == false)
                {
                    if (state.persistence_state == "persisted" && state.status != "outcome_unknown") recovery = "already_complete";
                    else
                    {
                        TestRuns[state.run_id] = state;
                        ResolveCompletedTestRun(state, recovering: true);
                        recovery = PersistTestRun(state, true) ? "recovered" : "persistence_failed";
                    }
                }
            }
            var result = DescribeTestRun(state);
            result["recovery_state"] = recovery;
            return result;
        }

        private static Dictionary<string, object> DescribeTestRun(TestRunState state)
        {
            return new Dictionary<string, object>
            {
                { "run_id", state.run_id }, { "status", state.status }, { "test_mode", state.test_mode },
                { "filter", state.filter }, { "started_at", state.started_at }, { "finished_at", state.finished_at },
                { "interrupted_at", state.interrupted_at },
                { "counts", new Dictionary<string, object> { { "total", state.total }, { "passed", state.passed }, { "failed", state.failed }, { "skipped", state.skipped }, { "inconclusive", state.inconclusive }, { "other", state.other } } },
                { "failed_tests", state.failed_tests ?? new string[0] },
                { "failure_details", state.failure_details ?? new TestFailureDetail[0] }, { "error", state.error },
                { "outcome_unknown", state.status == "outcome_unknown" },
                { "owned_final_result", state.owned_final_result },
                { "native_completion_verified", state.native_completion_verified },
                { "persistence_state", state.persistence_state ?? "legacy_unverified" },
                { "cleanup_state", state.cleanup_state ?? "not_completed" },
                { "cleanup_queued_at", state.cleanup_queued_at }, { "cleanup_finished_at", state.cleanup_finished_at },
                { "cleanup_attempts", state.cleanup_attempts },
                { "persistence_error", state.persistence_error }, { "last_persistence_error", state.last_persistence_error },
                { "persistence_attempts", state.persistence_attempts }, { "ownership_error", state.ownership_error },
                { "native_error", state.native_error }, { "cleanup_error", state.cleanup_error },
                { "recovery_evidence_complete", HasRecoveryEvidence(state) },
                { "request_id", state.request_id }, { "unity_run_id", state.unity_run_id },
                { "editor_instance_id", state.editor_instance_id }, { "editor_session_id", state.editor_session_id },
                { "result_state", state.result_state }, { "interruption_reason", state.interruption_reason },
                { "deadline_at", state.deadline_at }, { "deadline_exceeded", state.deadline_exceeded },
                { "completed_count", state.completed }, { "counts_final", state.result_state != null },
                { "failure_details_truncated", state.failure_details_truncated },
                { "phase", state.status == "running" ? (state.result_state == null ? "executing" : "cleanup") : state.status },
                { "observer_attached", ActiveTestRunners.ContainsKey(state.run_id) },
                { "native_job_active", NativeTestRunActive(state) },
                { "cancellation_supported", false },
                { "automatic_retry_safe", false }
            };
        }

        private sealed class TestRunCallbacks : IErrorCallbacks
        {
            private readonly string runId;
            internal TestRunCallbacks(string id) { runId = id; }
            // UTF 1.1 RunStarted supplies the unfiltered tree; it is not a matched-test count.
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (!TryGetOwnedRun(runId, out var state)) return;
                try
                {
                    CountResults(result, state);
                    state.result_state = result == null ? "MissingResult" : result.ResultState;
                    state.completed = state.total;
                    state.owned_final_result = result != null;
                }
                catch (Exception exception)
                {
                    state.native_error = EditorTestRunContract.DescribeFailure("native", "read_final_result", null, exception);
                    state.status = "error"; state.error = exception.Message;
                    PersistTestRun(state); CleanupTestRunner(runId); return;
                }
                // Unity still has Undo, scene restoration and verification tasks after this callback.
                PersistTestRun(state);
            }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (result != null && !result.Test.IsSuite && TryGetOwnedRun(runId, out var state))
                {
                    state.completed++;
                    PersistTestRun(state);
                }
            }
            public void OnError(string message)
            {
                if (!TryGetOwnedRun(runId, out var state)) return;
                state.status = "error"; state.error = message; state.finished_at = DateTime.UtcNow.ToString("o");
                state.native_error = EditorTestRunContract.DescribeFailure("native", "runner_callback", null, new InvalidOperationException(message));
                PersistTestRun(state); CleanupTestRunner(runId);
            }
        }

        private static bool TryGetOwnedRun(string runId, out TestRunState state)
        {
            if (!TestRuns.TryGetValue(runId, out state) || !ActiveTestRunners.ContainsKey(runId) ||
                (state.status != "running" && !(state.status == "outcome_unknown" &&
                    (state.interruption_reason == "run_timeout" || state.interruption_reason == "persistence_failed")))) return false;
            string[] ids;
            try { ids = ReadNativeTestRunIds(); }
            catch (Exception exception)
            {
                state.ownership_lost = true;
                state.ownership_error = EditorTestRunContract.DescribeFailure("ownership", "read_native_jobs", null, exception);
                InterruptTestRun(state, "ownership_unavailable", exception.Message); CleanupTestRunner(runId); return false;
            }
            var ownedUnityRunId = state.unity_run_id;
            if (ids.Any(id => id != ownedUnityRunId))
            {
                state.ownership_lost = true;
                state.ownership_error = EditorTestRunContract.DescribeFailure("ownership", "read_native_jobs", null,
                    new InvalidOperationException("Another Unity test job was observed; callback ownership is unknown."));
                InterruptTestRun(state, "ownership_conflict", state.ownership_error.message);
                CleanupTestRunner(runId);
                return false;
            }
            return true;
        }

        private static string[] ReadNativeTestRunIds()
        {
            // UTF 1.1 has no public job-state API. Read-only compatibility probe, never global StopRun.
            var type = typeof(TestRunnerApi).Assembly.GetType("UnityEditor.TestTools.TestRunner.TestRun.TestJobDataHolder");
            var instance = type?.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)?.GetValue(null, null);
            var runs = type?.GetField("TestRuns")?.GetValue(instance) as System.Collections.IEnumerable;
            if (runs == null) throw new NotSupportedException("Unity Test Framework job ownership is unavailable; refusing to start tests.");
            var ids = new List<string>();
            foreach (var run in runs)
            {
                var id = run.GetType().GetField("guid")?.GetValue(run) as string;
                if (String.IsNullOrEmpty(id)) throw new InvalidDataException("Unity Test Framework returned a job without identity.");
                ids.Add(id);
            }
            return ids.ToArray();
        }

        private static bool IsNativeTestRunActive()
        {
            var method = typeof(TestRunnerApi).GetMethod("IsRunActive", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (method == null) throw new NotSupportedException("Unity Test Framework active-state probe is unavailable.");
            return (bool)method.Invoke(null, null);
        }

        private static bool? NativeTestRunActive(TestRunState state)
        {
            if (String.IsNullOrEmpty(state.unity_run_id) || state.editor_instance_id != BridgeBootstrap.EditorInstanceId) return null;
            try { return ReadNativeTestRunIds().Contains(state.unity_run_id); }
            catch { return null; }
        }

        private static void ObserveTestRuns()
        {
            // Drain only work queued before this observation. Native callbacks enqueue; they never dispose inline.
            DrainPendingTestDisposals();
            foreach (var runId in ActiveTestRunners.Keys.ToArray())
            {
                if (!TryGetOwnedRun(runId, out var state)) continue;
                if (state.status == "running" && DateTime.TryParse(state.deadline_at, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var deadline) && DateTime.UtcNow >= deadline)
                {
                    state.deadline_exceeded = true;
                    InterruptTestRun(state, "run_timeout", "Observation deadline exceeded; Unity was not cancelled. Do not retry automatically.");
                }
                if (String.IsNullOrEmpty(state.unity_run_id)) continue;
                bool active;
                try { active = ReadNativeTestRunIds().Contains(state.unity_run_id); }
                catch (Exception exception)
                {
                    state.ownership_lost = true;
                    state.ownership_error = EditorTestRunContract.DescribeFailure("ownership", "read_native_jobs", null, exception);
                    InterruptTestRun(state, "ownership_unavailable", exception.Message);
                    CleanupTestRunner(runId);
                    continue;
                }
                if (active) continue;
                state.native_completion_verified = true;
                if (state.result_state == null || !state.owned_final_result)
                    InterruptTestRun(state, "runner_disappeared", "Unity test job ended without an owned final callback; cancellation or interruption is unconfirmed.");
                // Native completion, persistence and observer cleanup have distinct failure paths.
                // Remove from the update set before scheduling API disposal; permanent IO failure cannot loop.
                CleanupTestRunner(runId);
            }
        }

        private static void InterruptTestRun(TestRunState state, string reason, string error)
        {
            state.status = "outcome_unknown"; state.interruption_reason = reason; state.error = error;
            state.interrupted_at = DateTime.UtcNow.ToString("o");
            state.finished_at = state.interrupted_at; PersistTestRun(state);
        }

        private static void CountResults(ITestResultAdaptor result, TestRunState state)
        {
            var failed = new List<string>();
            var details = new List<TestFailureDetail>();
            void Walk(ITestResultAdaptor node)
            {
                if (node == null) return;
                if (!node.Test.IsSuite)
                {
                    switch (node.TestStatus.ToString())
                    {
                        case "Passed": state.passed++; break;
                        case "Failed":
                            failed.Add(node.Test.FullName);
                            if (details.Count < 50) details.Add(new TestFailureDetail
                            {
                                test_name = node.Test.FullName,
                                message = LimitTestDetail(node.Message, state),
                                stack_trace = LimitTestDetail(node.StackTrace, state)
                            });
                            else state.failure_details_truncated = true;
                            state.failed++;
                            break;
                        case "Skipped": state.skipped++; break;
                        case "Inconclusive": state.inconclusive++; break;
                        default: state.other++; break;
                    }
                    return;
                }
                if (node.TestStatus.ToString() == "Failed" && details.Count < 50)
                    details.Add(new TestFailureDetail { test_name = node.Test.FullName,
                        message = LimitTestDetail(node.Message, state), stack_trace = LimitTestDetail(node.StackTrace, state) });
                foreach (var child in node.Children) Walk(child);
            }
            state.total = state.passed = state.failed = state.skipped = state.inconclusive = state.other = 0;
            Walk(result); state.total = state.passed + state.failed + state.skipped + state.inconclusive + state.other;
            state.failed_tests = failed.Distinct(StringComparer.Ordinal).Take(50).ToArray();
            state.failure_details = details
                .GroupBy(item => item.test_name, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
        }
        private static string LimitTestDetail(string text, TestRunState state)
        {
            if (text == null) return String.Empty;
            if (text.Length <= 4096) return text;
            state.failure_details_truncated = true;
            return text.Substring(0, 4096);
        }
        private static bool PersistTestRun(TestRunState state, bool explicitRecovery = false)
        {
            // Once IO fails, callbacks only retain memory evidence. A final checkpoint or explicit
            // test-recover may make one new attempt, never every Editor update or recursive error handling.
            if (state.persistence_state == "failed" && !explicitRecovery) return false;
            state.persistence_state = "persisted";
            state.persistence_error = null;
            state.persistence_attempts++;
            EditorTestRunContract.Failure failure;
            try { failure = EditorTestRunContract.WriteCheckpoint(TestRunPath(state.run_id), JsonUtility.ToJson(state, true)); }
            catch (Exception exception)
            {
                failure = EditorTestRunContract.DescribeFailure("persistence", "serialize_checkpoint", null, exception);
            }
            if (failure == null) return true;
            state.persistence_state = "failed"; state.persistence_error = failure; state.last_persistence_error = failure;
            state.status = "outcome_unknown";
            if (!EditorTestRunContract.HasFailure(state.ownership_error) && !EditorTestRunContract.HasFailure(state.native_error) && !EditorTestRunContract.HasFailure(state.cleanup_error))
            {
                state.interruption_reason = "persistence_failed";
                state.error = failure.message;
            }
            return false;
        }

        private static void ResolveCompletedTestRun(TestRunState state, bool recovering = false)
        {
            state.status = EditorTestRunContract.ResolveStatus(state.result_state, state.total,
                state.passed, state.failed, state.skipped, state.inconclusive, state.other);
            state.error = null; state.interruption_reason = null;
            state.finished_at = EditorTestRunContract.ResolveFinishedAt(state.finished_at, DateTime.UtcNow.ToString("o"), recovering);
        }

        private static string TestRunPath(string runId)
        {
            if (!EditorTestRunContract.IsValidRunId(runId)) throw new ArgumentException("run_id must be a 32-character GUID (N format).");
            return Path.Combine(ProjectRoot(), "Library", "FakeUnityCLI", "test-runs", runId + ".json");
        }
        private static void CleanupTestRunner(string runId)
        {
            if (!ActiveTestRunners.TryGetValue(runId, out var api)) return;
            var state = TestRuns[runId];
            state.cleanup_state = "pending";
            state.cleanup_queued_at = DateTime.UtcNow.ToString("o");
            try
            {
                if (ActiveTestCallbacks.TryGetValue(runId, out var callbacks)) api.UnregisterCallbacks(callbacks);
            }
            catch (Exception exception)
            {
                state.cleanup_error = EditorTestRunContract.DescribeFailure("cleanup", "unregister_callbacks", null, exception);
            }
            finally
            {
                ActiveTestCallbacks.Remove(runId);
                ActiveTestRunners.Remove(runId);
            }
            PendingTestDisposals.Enqueue(runId, new PendingTestDisposal { RunId = runId, State = state, Api = api });
            // Reload may lose this in-memory queue. A pending checkpoint is never evidence of completed cleanup.
            PersistTestRun(state);
        }

        private static bool IsOwnedDisposal(string runId, PendingTestDisposal pending) =>
            pending.RunId == runId && pending.State.run_id == runId &&
            TestRuns.TryGetValue(runId, out var current) && System.Object.ReferenceEquals(current, pending.State);

        private static void DrainPendingTestDisposals()
        {
            PendingTestDisposals.Drain((runId, pending) =>
            {
                if (!IsOwnedDisposal(runId, pending))
                    throw new InvalidDataException("Pending TestRunner disposal identity no longer matches the original run.");
                var state = pending.State;
                state.cleanup_attempts++;
                try { if (pending.Api != null) UnityEngine.Object.DestroyImmediate(pending.Api); }
                catch (Exception exception)
                {
                    state.cleanup_error = EditorTestRunContract.DescribeFailure("cleanup", "dispose_test_api", null, exception);
                }
                CompleteTestDisposal(state);
            }, (runId, pending, exception) =>
            {
                // Do not overwrite another run even if the queued identity was corrupted or replaced.
                if (!IsOwnedDisposal(runId, pending))
                {
                    UnityEngine.Debug.LogError("[FakeUnityCLI] TestRunner disposal identity mismatch: " + runId);
                    return;
                }
                pending.State.cleanup_error = EditorTestRunContract.DescribeFailure("cleanup", "process_disposal", null, exception);
                CompleteTestDisposal(pending.State);
            });
        }

        private static void CompleteTestDisposal(TestRunState state)
        {
            state.cleanup_finished_at = DateTime.UtcNow.ToString("o");
            state.cleanup_state = !EditorTestRunContract.HasFailure(state.cleanup_error) ? "complete" : "failed";
            if (EditorTestRunContract.HasFailure(state.cleanup_error))
            {
                state.status = "outcome_unknown"; state.interruption_reason = "cleanup_failed";
                state.error = state.cleanup_error.message;
            }
            else if (HasRecoveryEvidence(state)) ResolveCompletedTestRun(state);
            else if (state.status == "running")
            {
                state.status = "outcome_unknown"; state.interruption_reason = "completion_evidence_incomplete";
                state.error = "Final result identity or counts did not close; recovery cannot infer success.";
            }
            PersistTestRun(state, true);
        }

        private static Dictionary<string, object> CaptureScreenshot(string payloadJson, string requestId)
        {
            var payload = JsonUtility.FromJson<ScreenshotPayload>(payloadJson ?? "{}") ?? new ScreenshotPayload { target = "scene" };
            var target = String.IsNullOrWhiteSpace(payload.target) ? "scene" : payload.target.ToLowerInvariant();
            if (target != "scene" && target != "game") throw new ArgumentException("Screenshot target must be scene or game.");
            EditorWindow window;
            RenderTexture source;
            string sourceMember;
            Camera sceneCamera = null;
            if (target == "scene")
            {
                var view = SceneView.lastActiveSceneView;
                if (view == null || view.camera == null) throw new InvalidOperationException("No existing SceneView is available.");
                window = view;
                sceneCamera = view.camera;
                source = view.camera.targetTexture;
                sourceMember = "SceneView.camera.Render";
            }
            else
            {
                if (!EditorApplication.isPlaying) throw new InvalidOperationException("GameView screenshot requires Play Mode; no Editor state was changed.");
                var type = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                var windows = type == null ? new UnityEngine.Object[0] : Resources.FindObjectsOfTypeAll(type);
                if (windows.Length != 1) throw new InvalidOperationException("GameView screenshot requires exactly one existing GameView; found " + windows.Length + ". No window was created or focused.");
                window = (EditorWindow)windows[0];
                var field = FindGameViewFramebufferField(type);
                if (field == null) throw new NotSupportedException("This Unity Editor does not expose the GameView framebuffer contract.");
                source = field.GetValue(window) as RenderTexture;
                sourceMember = field.DeclaringType.FullName + "." + field.Name;
            }

            if (source == null || !source.IsCreated()) throw new InvalidOperationException("The requested view framebuffer is not ready; no render was forced.");
            var width = source.width;
            var height = source.height;
            if (width < 1 || height < 1 || width > 8192 || height > 8192 || (long)width * height > 16777216)
                throw new InvalidOperationException("View framebuffer exceeds the 8192-per-axis / 16-megapixel capture budget; no resizing was applied.");
            if (String.IsNullOrEmpty(requestId) || requestId.Length > 80 || requestId.Any(c => !Char.IsLetterOrDigit(c) && c != '-' && c != '_'))
                throw new ArgumentException("Screenshot request ID contains unsafe path characters.");
            var project = ProjectRoot();
            var directory = Path.Combine(project, "Temp", "FakeUnityCLI");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "screenshot-" + requestId + ".png");
            var previous = RenderTexture.active;
            var previousSrgbWrite = GL.sRGBWrite;
            var previousTarget = sceneCamera == null ? null : sceneCamera.targetTexture;
            Texture2D texture = null;
            RenderTexture readback = null;
            RenderTexture sceneRender = null;
            try
            {
                if (sceneCamera != null)
                {
                    // SceneView reuses its target for gizmos; preserve the camera-render contract.
                    sceneRender = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    sceneCamera.targetTexture = sceneRender;
                    sceneCamera.Render();
                    source = sceneRender;
                }
                // Adapted from AIBridge 1.3.2 ScreenshotHelper; see package THIRD-PARTY-NOTICES.md.
                readback = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                if (target == "game" && SystemInfo.graphicsUVStartsAtTop)
                    Graphics.Blit(source, readback, new Vector2(1, -1), new Vector2(0, 1));
                else Graphics.Blit(source, readback);
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                RenderTexture.active = readback;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                texture.Apply(false, false);
                var bytes = texture.EncodeToPNG();
                if (bytes == null || bytes.Length == 0) throw new InvalidOperationException("PNG encoding returned no data.");
                using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                    output.Write(bytes, 0, bytes.Length);
                using var sha = SHA256.Create();
                return new Dictionary<string, object>
                {
                    { "artifact_path", path }, { "artifact_relative_path", ToProjectRelative(path) },
                    { "sha256", BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant() },
                    { "width", width }, { "height", height }, { "frame_count", 1 }, { "target", target },
                    { "bytes", bytes.Length }, { "capture_scope", target == "game" ? "gameview_framebuffer" : "scene_camera_render" },
                    { "screen_space_overlay_included", target == "game" }, { "editor_chrome_included", false },
                    { "freshness", target == "game" ? "last_available_frame" : "rendered_during_request" }, { "source_frame_age_ms", null },
                    { "captured_at", DateTime.UtcNow.ToString("o") },
                    { "window_instance_id", EditorIdentity.InstanceId(window) }, { "source_member", sourceMember },
                    { "source_texture_instance_id", EditorIdentity.InstanceId(source) },
                    { "vertical_flip_applied", target == "game" && SystemInfo.graphicsUVStartsAtTop },
                    { "camera_render_invoked", sceneCamera != null },
                    { "is_playing", EditorApplication.isPlaying }, { "is_paused", EditorApplication.isPaused }
                };
            }
            finally
            {
                RenderTexture.active = previous;
                GL.sRGBWrite = previousSrgbWrite;
                if (sceneCamera != null) sceneCamera.targetTexture = previousTarget;
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                if (readback != null) RenderTexture.ReleaseTemporary(readback);
                if (sceneRender != null) RenderTexture.ReleaseTemporary(sceneRender);
            }
        }

        private static FieldInfo FindGameViewFramebufferField(Type type)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField("m_RenderTexture", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null && typeof(RenderTexture).IsAssignableFrom(field.FieldType)) return field;
            }
            return null;
        }

        private static Dictionary<string, object> GameViewResolutionList()
        {
            var current = TryGetGameViewSize(out var width, out var height);
            return new Dictionary<string, object>
            {
                { "supported", current }, { "current", current ? new Dictionary<string, object> { { "width", width }, { "height", height } } : null },
                { "presets", new[] { new Dictionary<string, object> { { "width", 1280 }, { "height", 720 } }, new Dictionary<string, object> { { "width", 1920 }, { "height", 1080 } } } },
                { "note", "Unity 内部 GameView 尺寸 API 由当前 Editor 版本决定；preset 仅作为受控请求候选，不代表已应用。" }
            };
        }

        private static Dictionary<string, object> GameViewResolutionGet()
        {
            if (!TryGetGameViewSize(out var width, out var height))
                throw new NotSupportedException("Current Unity Editor does not expose a GameView render size API.");
            return new Dictionary<string, object> { { "width", width }, { "height", height }, { "source", "UnityEditor.GameView.targetRenderSize" } };
        }

        private static Dictionary<string, object> GameViewResolutionSet(string payloadJson, string requestId)
        {
            var payload = JsonUtility.FromJson<ResolutionPayload>(payloadJson ?? "{}") ?? new ResolutionPayload();
            var parts = (payload.value ?? "").ToLowerInvariant().Split(new[] { 'x', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !Int32.TryParse(parts[0], out var width) || !Int32.TryParse(parts[1], out var height) || width < 64 || height < 64 || width > 8192 || height > 8192)
                throw new ArgumentException("GameView resolution requires <width>x<height> in range 64..8192.");
            var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var gameView = FindGameView(type);
            var method = type?.GetMethod("SetCustomResolution", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(Vector2), typeof(string) }, null);
            if (gameView == null || method == null)
                throw new NotSupportedException("This Unity Editor does not expose a safe GameView resolution setter.");
            method.Invoke(gameView, new object[] { new Vector2(width, height), "FakeUnityCLI" });
            return new Dictionary<string, object> { { "changed", true }, { "width", width }, { "height", height }, { "request_id", requestId }, { "setter", "UnityEditor.GameView.SetCustomResolution" } };
        }

        private static bool TryGetGameViewSize(out int width, out int height)
        {
            width = height = 0;
            var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var gameView = FindGameView(type);
            var property = type?.GetProperty("targetRenderSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? type?.GetProperty("targetSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (gameView == null || property == null) return false;
            var value = property.GetValue(gameView, null);
            if (value is Vector2 size)
            {
                width = Mathf.RoundToInt(size.x); height = Mathf.RoundToInt(size.y);
                return width > 0 && height > 0;
            }
            return false;
        }

        private static UnityEngine.Object FindGameView(Type type)
        {
            if (type == null) return null;
            return Resources.FindObjectsOfTypeAll(type).FirstOrDefault();
        }

        private static string ToProjectRelative(string path)
        {
            var root = ProjectRoot().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                ? path.Substring(root.Length).Replace(Path.DirectorySeparatorChar, '/') : path;
        }

        private static Dictionary<string, object> ReadEditorContext()
        {
            var scene = SceneManager.GetActiveScene();
            var selection = Selection.objects ?? new UnityEngine.Object[0];
            var selected = selection.Select(item => new Dictionary<string, object>
            {
                { "name", item == null ? "" : item.name },
                { "type", item == null ? "" : item.GetType().FullName },
                { "instance_id", item == null ? 0 : EditorIdentity.InstanceId(item) }
            }).ToArray();
            var focused = EditorWindow.focusedWindow;
            return new Dictionary<string, object>
            {
                { "active_scene", new Dictionary<string, object>
                    {
                        { "name", scene.name ?? "" },
                        { "path", scene.path ?? "" },
                        { "handle", EditorIdentity.SceneId(scene) },
                        { "is_loaded", scene.isLoaded },
                        { "is_dirty", scene.isDirty }
                    } },
                { "selection", selected },
                { "focused_window", focused == null ? null : new Dictionary<string, object>
                    {
                        { "type", focused.GetType().FullName },
                        { "title", focused.titleContent == null ? "" : focused.titleContent.text }
                    } },
                { "is_playing", EditorApplication.isPlaying },
                { "is_paused", EditorApplication.isPaused },
                { "execution_mode", EditorApplication.isPlaying ? "play" : "edit" }
            };
        }

        private static Dictionary<string, object> ReadSceneHierarchy(string payloadJson)
        {
            var payload = JsonUtility.FromJson<SceneHierarchyPayload>(payloadJson ?? "{}") ?? new SceneHierarchyPayload();
            payload.max_depth = Mathf.Clamp(payload.max_depth, 0, 32);
            payload.max_objects = Mathf.Clamp(payload.max_objects, 1, 5000);
            var scene = SceneManager.GetActiveScene();
            var nodes = new List<Dictionary<string, object>>();
            var roots = scene.IsValid() && scene.isLoaded ? scene.GetRootGameObjects() : new GameObject[0];
            var truncated = false;
            foreach (var root in roots)
            {
                if (nodes.Count >= payload.max_objects) { truncated = true; break; }
                AddHierarchyNode(root.transform, "", 0, payload, nodes, ref truncated);
            }
            return new Dictionary<string, object>
            {
                { "active_scene", scene.path ?? "" },
                { "max_depth", payload.max_depth },
                { "max_objects", payload.max_objects },
                { "include_inactive", payload.include_inactive },
                { "object_count", nodes.Count },
                { "truncated", truncated },
                { "objects", nodes.ToArray() }
            };
        }

        private static void AddHierarchyNode(Transform transform, string parentPath, int depth,
            SceneHierarchyPayload payload, List<Dictionary<string, object>> nodes, ref bool truncated)
        {
            var go = transform.gameObject;
            if (!payload.include_inactive && !go.activeInHierarchy) return;
            if (nodes.Count >= payload.max_objects) { truncated = true; return; }
            var path = String.IsNullOrEmpty(parentPath) ? go.name : parentPath + "/" + go.name;
            var components = go.GetComponents<Component>().Where(component => component != null)
                .Select(component => component.GetType().FullName).Distinct().OrderBy(name => name).ToArray();
            nodes.Add(new Dictionary<string, object>
            {
                { "name", go.name }, { "path", path }, { "active", go.activeSelf },
                { "active_in_hierarchy", go.activeInHierarchy }, { "layer", go.layer },
                { "tag", go.tag }, { "depth", depth }, { "instance_id", EditorIdentity.InstanceId(go) },
                { "components", components }
            });
            if (depth >= payload.max_depth) { if (transform.childCount > 0) truncated = true; return; }
            for (var index = 0; index < transform.childCount; index++)
            {
                AddHierarchyNode(transform.GetChild(index), path, depth + 1, payload, nodes, ref truncated);
                if (truncated && nodes.Count >= payload.max_objects) return;
            }
        }

        private static Dictionary<string, object> FindSceneObjects(string payloadJson)
        {
            var payload = JsonUtility.FromJson<SceneFindPayload>(payloadJson ?? "{}") ?? new SceneFindPayload();
            payload.limit = Mathf.Clamp(payload.limit, 1, 1000);
            var scene = SceneManager.GetActiveScene();
            var matches = new List<Dictionary<string, object>>();
            var visited = 0;
            var roots = scene.IsValid() && scene.isLoaded ? scene.GetRootGameObjects() : new GameObject[0];
            foreach (var root in roots)
            {
                FindSceneObjects(root.transform, "", payload, matches, ref visited);
                if (matches.Count >= payload.limit) break;
            }
            return new Dictionary<string, object>
            {
                { "active_scene", scene.path ?? "" }, { "limit", payload.limit },
                { "matched", matches.Count }, { "truncated", matches.Count >= payload.limit },
                { "objects", matches.ToArray() }
            };
        }

        private static Dictionary<string, object> ListComponents(string payloadJson)
        {
            var payload = JsonUtility.FromJson<ComponentPayload>(payloadJson ?? "{}") ?? new ComponentPayload();
            var go = FindSceneObject(payload.object_path);
            var components = go.GetComponents<Component>().Where(component => component != null)
                .Select(component => new Dictionary<string, object>
                {
                    { "type", component.GetType().FullName },
                    { "short_type", component.GetType().Name },
                    { "instance_id", EditorIdentity.InstanceId(component) },
                    { "is_behaviour", component is Behaviour },
                    { "enabled", component is Behaviour behaviour && behaviour.enabled }
                }).ToArray();
            return new Dictionary<string, object>
            {
                { "object_path", SceneHierarchyPath(go.transform) },
                { "name", go.name },
                { "component_count", components.Length },
                { "components", components }
            };
        }

        private static Dictionary<string, object> GetComponentSummary(string payloadJson)
        {
            var payload = JsonUtility.FromJson<ComponentPayload>(payloadJson ?? "{}") ?? new ComponentPayload();
            var go = FindSceneObject(payload.object_path);
            var component = go.GetComponents<Component>().FirstOrDefault(item => item != null &&
                (String.Equals(item.GetType().Name, payload.component_type, StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(item.GetType().FullName, payload.component_type, StringComparison.OrdinalIgnoreCase)));
            if (component == null) throw new InvalidOperationException("Component was not found: " + payload.component_type);
            var value = new Dictionary<string, object>
            {
                { "object_path", SceneHierarchyPath(go.transform) },
                { "name", go.name },
                { "type", component.GetType().FullName },
                { "instance_id", EditorIdentity.InstanceId(component) },
                { "is_behaviour", component is Behaviour },
                { "enabled", component is Behaviour behaviour && behaviour.enabled }
            };
            if (component is Transform transform)
            {
                value["position"] = new Dictionary<string, object> { { "x", transform.position.x }, { "y", transform.position.y }, { "z", transform.position.z } };
                value["local_position"] = new Dictionary<string, object> { { "x", transform.localPosition.x }, { "y", transform.localPosition.y }, { "z", transform.localPosition.z } };
                value["local_scale"] = new Dictionary<string, object> { { "x", transform.localScale.x }, { "y", transform.localScale.y }, { "z", transform.localScale.z } };
                value["child_count"] = transform.childCount;
            }
            else if (component is Renderer renderer)
            {
                value["enabled"] = renderer.enabled;
                value["sorting_layer"] = renderer.sortingLayerName;
                value["sorting_order"] = renderer.sortingOrder;
            }
            else if (component is Collider collider)
            {
                value["enabled"] = collider.enabled;
                value["is_trigger"] = collider.isTrigger;
            }
            return value;
        }

        private static Dictionary<string, object> SetComponentProperty(string payloadJson, string operationId)
        {
            var payload = JsonUtility.FromJson<ComponentPayload>(payloadJson ?? "{}") ?? new ComponentPayload();
            var go = FindSceneObject(payload.object_path);
            var component = go.GetComponents<Component>().FirstOrDefault(item => item != null &&
                (String.Equals(item.GetType().Name, payload.component_type, StringComparison.OrdinalIgnoreCase) ||
                 String.Equals(item.GetType().FullName, payload.component_type, StringComparison.OrdinalIgnoreCase)));
            if (component == null) throw new InvalidOperationException("Component was not found: " + payload.component_type);
            var change = ResolveTypedChange(component, payload.property, payload.value);
            var result = new Dictionary<string, object>
            {
                { "object_path", SceneHierarchyPath(go.transform) }, { "component_type", component.GetType().FullName },
                { "property", payload.property }, { "old_value", change.old_value }, { "new_value", change.new_value },
                { "changed", change.changed }, { "dry_run", payload.dry_run }, { "operation_id", payload.dry_run ? null : operationId },
                { "undo", payload.dry_run ? null : "Unity Editor Undo (Ctrl+Z)" }
            };
            if (payload.dry_run || !change.changed) return result;
            Undo.RecordObject(component, "FakeUnityCLI component set");
            change.apply();
            EditorSceneManager.MarkSceneDirty(go.scene);
            return result;
        }

        private sealed class TypedChange
        {
            internal readonly object old_value;
            internal readonly object new_value;
            internal readonly bool changed;
            internal readonly Action apply;

            internal TypedChange(object oldValue, object newValue, bool hasChanged, Action applyAction)
            {
                old_value = oldValue;
                new_value = newValue;
                changed = hasChanged;
                apply = applyAction;
            }
        }

        private static TypedChange ResolveTypedChange(Component component, string property, string rawValue)
        {
            if (component is Transform transform && property is "local_position" or "position" or "local_scale")
            {
                var vector = ParseVector(rawValue);
                var next = new Vector3(vector.x, vector.y, vector.z);
                var current = property == "position" ? transform.position : property == "local_scale" ? transform.localScale : transform.localPosition;
                var changed = current != next;
                return new TypedChange(Vector(current), Vector(next), changed, () =>
                {
                    if (property == "position") transform.position = next;
                    else if (property == "local_scale") transform.localScale = next;
                    else transform.localPosition = next;
                });
            }
            if (component is Behaviour behaviour && property == "enabled")
            {
                var next = ParseBoolean(rawValue); var changed = behaviour.enabled != next;
                return new TypedChange(behaviour.enabled, next, changed, () => behaviour.enabled = next);
            }
            if (component is Renderer renderer && property == "enabled")
            {
                var next = ParseBoolean(rawValue); var changed = renderer.enabled != next;
                return new TypedChange(renderer.enabled, next, changed, () => renderer.enabled = next);
            }
            if (component is Collider collider && property == "is_trigger")
            {
                var next = ParseBoolean(rawValue); var changed = collider.isTrigger != next;
                return new TypedChange(collider.isTrigger, next, changed, () => collider.isTrigger = next);
            }
            throw new InvalidOperationException("Unsupported typed component property: " + component.GetType().FullName + "." + property);
        }

        private static bool ParseBoolean(string rawValue)
        {
            if (Boolean.TryParse(rawValue, out var value)) return value;
            throw new ArgumentException("Typed boolean property requires true or false.");
        }

        private static VectorPayload ParseVector(string rawValue)
        {
            try
            {
                var vector = JsonUtility.FromJson<VectorPayload>(rawValue ?? "{}");
                if (vector != null) return vector;
            }
            catch (ArgumentException) { }
            var parts = (rawValue ?? "").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3 && Single.TryParse(parts[0], out var x) && Single.TryParse(parts[1], out var y) && Single.TryParse(parts[2], out var z))
                return new VectorPayload { x = x, y = y, z = z };
            throw new ArgumentException("Typed vector property requires JSON {x,y,z} or comma-separated x,y,z.");
        }

        private static Dictionary<string, object> Vector(Vector3 value) => new Dictionary<string, object>
        {
            { "x", value.x }, { "y", value.y }, { "z", value.z }
        };

        private static GameObject FindSceneObject(string objectPath)
        {
            if (String.IsNullOrWhiteSpace(objectPath)) throw new ArgumentException("object_path is required.");
            var scene = SceneManager.GetActiveScene();
            var roots = scene.IsValid() && scene.isLoaded ? scene.GetRootGameObjects() : new GameObject[0];
            foreach (var root in roots)
            {
                var found = FindSceneObject(root.transform, "", objectPath);
                if (found != null) return found;
            }
            throw new InvalidOperationException("Scene object was not found: " + objectPath);
        }

        private static GameObject FindSceneObject(Transform transform, string parentPath, string targetPath)
        {
            var path = String.IsNullOrEmpty(parentPath) ? transform.name : parentPath + "/" + transform.name;
            if (String.Equals(path, targetPath, StringComparison.Ordinal)) return transform.gameObject;
            for (var index = 0; index < transform.childCount; index++)
            {
                var found = FindSceneObject(transform.GetChild(index), path, targetPath);
                if (found != null) return found;
            }
            return null;
        }

        private static string SceneHierarchyPath(Transform transform)
        {
            var stack = new Stack<string>();
            for (var current = transform; current != null; current = current.parent) stack.Push(current.name);
            return String.Join("/", stack);
        }

        private static void FindSceneObjects(Transform transform, string parentPath, SceneFindPayload payload,
            List<Dictionary<string, object>> matches, ref int visited)
        {
            if (matches.Count >= payload.limit) return;
            var go = transform.gameObject;
            visited++;
            var path = String.IsNullOrEmpty(parentPath) ? go.name : parentPath + "/" + go.name;
            var nameMatch = String.IsNullOrEmpty(payload.name_pattern) || go.name.IndexOf(payload.name_pattern, StringComparison.OrdinalIgnoreCase) >= 0;
            var pathMatch = String.IsNullOrEmpty(payload.path_pattern) || path.IndexOf(payload.path_pattern, StringComparison.OrdinalIgnoreCase) >= 0;
            var tagMatch = String.IsNullOrEmpty(payload.tag) || go.CompareTag(payload.tag);
            var layerMatch = payload.layer < 0 || go.layer == payload.layer;
            var componentMatch = String.IsNullOrEmpty(payload.component_type) || go.GetComponents<Component>().Any(component =>
                component != null && (String.Equals(component.GetType().Name, payload.component_type, StringComparison.OrdinalIgnoreCase) ||
                                      String.Equals(component.GetType().FullName, payload.component_type, StringComparison.OrdinalIgnoreCase)));
            if ((payload.include_inactive || go.activeInHierarchy) && nameMatch && pathMatch && tagMatch && layerMatch && componentMatch)
            {
                matches.Add(new Dictionary<string, object>
                {
                    { "name", go.name }, { "path", path }, { "active", go.activeSelf },
                    { "active_in_hierarchy", go.activeInHierarchy }, { "layer", go.layer },
                    { "tag", go.tag }, { "instance_id", EditorIdentity.InstanceId(go) },
                    { "components", go.GetComponents<Component>().Where(component => component != null)
                        .Select(component => component.GetType().FullName).Distinct().OrderBy(name => name).ToArray() }
                });
            }
            for (var index = 0; index < transform.childCount && matches.Count < payload.limit; index++)
                FindSceneObjects(transform.GetChild(index), path, payload, matches, ref visited);
        }

        private static Dictionary<string, object> ExtractPrefabChild(string hostPath, string outputPath,
            PrefabExtractPayload payload, string operationId)
        {
            if (!hostPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                !outputPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Host and output paths must be Prefab assets.");
            if (String.IsNullOrWhiteSpace(payload.object_path))
                throw new ArgumentException("object_path is required.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(hostPath) == null)
                throw new FileNotFoundException("Host Prefab does not exist or is not importable: " + hostPath);
            if (AssetDatabase.LoadMainAssetAtPath(outputPath) != null || File.Exists(AbsoluteAssetPath(outputPath)))
                throw new IOException("Output Prefab already exists; overwrite is not supported: " + outputPath);
            var parentDirectory = outputPath.Substring(0, outputPath.LastIndexOf('/'));
            if (!AssetDatabase.IsValidFolder(parentDirectory))
                throw new DirectoryNotFoundException("Output asset folder does not exist: " + parentDirectory);

            GameObject hostRoot = null;
            var backupCreated = false;
            try
            {
                hostRoot = PrefabUtility.LoadPrefabContents(hostPath);
                var childTransform = FindTransform(hostRoot.transform, payload.object_path);
                if (childTransform == hostRoot.transform)
                    throw new InvalidOperationException("extract-child cannot extract the host Prefab root.");
                var originalParent = childTransform.parent;
                var hostComponent = String.IsNullOrWhiteSpace(payload.bind_component)
                    ? null : FindComponent(hostRoot.transform, payload.bind_component);
                var bindingPlan = ValidateBindings(hostComponent, payload, childTransform.gameObject, originalParent.gameObject);
                var plan = new Dictionary<string, object>
                {
                    { "host_prefab", hostPath },
                    { "object", HierarchyPath(childTransform) },
                    { "output_prefab", outputPath },
                    { "replace_source_instance", true },
                    { "connect", true },
                    { "binding_changes", bindingPlan },
                    { "affected_assets", new [] { hostPath, outputPath } }
                };
                if (payload.dry_run)
                {
                    plan["dry_run"] = true;
                    plan["changed"] = false;
                    plan["operation_id"] = null;
                    return plan;
                }

                CreateExtractBackup(operationId, hostPath, outputPath);
                backupCreated = true;
                var connected = PrefabUtility.SaveAsPrefabAssetAndConnect(
                    childTransform.gameObject, outputPath, InteractionMode.AutomatedAction);
                if (connected == null)
                    throw new InvalidOperationException("SaveAsPrefabAssetAndConnect returned null.");
                if (hostComponent != null)
                    ApplyBindings(hostComponent, payload, connected, connected.transform.parent.gameObject);
                bool hostSaved;
                PrefabUtility.SaveAsPrefabAsset(hostRoot, hostPath, out hostSaved);
                if (!hostSaved) throw new InvalidOperationException("Host Prefab could not be saved after extraction.");
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(hostPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                CompleteExtractBackup(operationId);
                plan["dry_run"] = false;
                plan["changed"] = true;
                plan["operation_id"] = operationId;
                plan["created_guid"] = AssetDatabase.AssetPathToGUID(outputPath);
                plan["connected_instance"] = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(connected) == outputPath;
                plan["semantic_readback"] = new Dictionary<string, object>
                {
                    { "host_exists", AssetDatabase.LoadAssetAtPath<GameObject>(hostPath) != null },
                    { "output_exists", AssetDatabase.LoadAssetAtPath<GameObject>(outputPath) != null },
                    { "source_instance_path", outputPath }
                };
                plan["undo_command"] = "fuc prefab extract-child-undo " + operationId + " --project \"" +
                                       ProjectRoot() + "\" --yes --json";
                return plan;
            }
            catch
            {
                if (hostRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(hostRoot);
                    hostRoot = null;
                }
                if (backupCreated) RestoreExtractBackup(operationId, markUndone: false);
                throw;
            }
            finally
            {
                if (hostRoot != null) PrefabUtility.UnloadPrefabContents(hostRoot);
            }
        }

        private static Dictionary<string, object> UndoPrefabExtract(string operationId)
        {
            if (String.IsNullOrWhiteSpace(operationId)) throw new ArgumentException("operation_id is required.");
            var manifest = ReadExtractManifest(operationId);
            if (manifest.undone)
                return new Dictionary<string, object>
                {
                    { "operation_id", operationId }, { "restored", false }, { "already_undone", true },
                    { "host_prefab", manifest.host_path }, { "removed_output", manifest.output_path }
                };
            RestoreExtractBackup(operationId, markUndone: true);
            return new Dictionary<string, object>
            {
                { "operation_id", operationId }, { "restored", true }, { "already_undone", false },
                { "host_prefab", manifest.host_path }, { "removed_output", manifest.output_path }
            };
        }

        private static List<Dictionary<string, object>> ValidateBindings(Component hostComponent,
            PrefabExtractPayload payload, GameObject extracted, GameObject container)
        {
            var result = new List<Dictionary<string, object>>();
            if (hostComponent == null) return result;
            result.Add(DescribeBinding(hostComponent, payload.bind_field, extracted, "extracted"));
            if (!String.IsNullOrWhiteSpace(payload.bind_container_field))
                result.Add(DescribeBinding(hostComponent, payload.bind_container_field, container, "container"));
            return result;
        }

        private static void ApplyBindings(Component hostComponent, PrefabExtractPayload payload,
            GameObject extracted, GameObject container)
        {
            var serialized = new SerializedObject(hostComponent);
            serialized.Update();
            SetBinding(serialized, hostComponent, payload.bind_field, extracted);
            if (!String.IsNullOrWhiteSpace(payload.bind_container_field))
                SetBinding(serialized, hostComponent, payload.bind_container_field, container);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hostComponent);
        }

        private static Dictionary<string, object> DescribeBinding(Component host, string fieldName,
            GameObject target, string targetRole)
        {
            var field = FindField(host.GetType(), fieldName);
            var value = ResolveBindingTarget(field.FieldType, target);
            var serialized = new SerializedObject(host);
            var property = serialized.FindProperty(fieldName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                throw new InvalidOperationException(host.GetType().FullName + "." + fieldName +
                    " is not a serialized Unity Object reference field.");
            return new Dictionary<string, object>
            {
                { "component", host.GetType().FullName }, { "field", fieldName },
                { "expected_type", field.FieldType.FullName }, { "target_role", targetRole },
                { "resolved_target", value.GetType().FullName }
            };
        }

        private static void SetBinding(SerializedObject serialized, Component host, string fieldName, GameObject target)
        {
            var field = FindField(host.GetType(), fieldName);
            var property = serialized.FindProperty(fieldName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                throw new InvalidOperationException(host.GetType().FullName + "." + fieldName +
                    " is not a serialized Unity Object reference field.");
            property.objectReferenceValue = ResolveBindingTarget(field.FieldType, target);
        }

        private static UnityEngine.Object ResolveBindingTarget(Type expected, GameObject target)
        {
            if (expected.IsAssignableFrom(target.GetType())) return target;
            var candidates = target.GetComponents<Component>().Where(component =>
                component != null && expected.IsAssignableFrom(component.GetType())).ToArray();
            if (candidates.Length != 1)
                throw new InvalidOperationException("Binding target " + HierarchyPath(target.transform) +
                    " has " + candidates.Length + " component(s) assignable to " + expected.FullName + ".");
            return candidates[0];
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            if (String.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException("Binding field cannot be empty.");
            for (var current = type; current != null; current = current.BaseType)
            {
                var field = current.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }
            throw new MissingFieldException(type.FullName, fieldName);
        }

        private static Component FindComponent(Transform root, string selector)
        {
            var separator = selector.LastIndexOf("::", StringComparison.Ordinal);
            if (separator <= 0 || separator == selector.Length - 2)
                throw new ArgumentException("bind_component must use /Root/Path::Type.");
            var transform = FindTransform(root, selector.Substring(0, separator));
            var typeName = selector.Substring(separator + 2);
            var matches = transform.GetComponents<Component>().Where(component => component != null &&
                (component.GetType().FullName == typeName || component.GetType().Name == typeName)).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Component selector " + selector + " matched " + matches.Length + " components.");
            return matches[0];
        }

        private static Transform FindTransform(Transform root, string path)
        {
            var segments = path.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0) throw new ArgumentException("Object path cannot be empty.");
            var index = segments[0] == root.name ? 1 : 0;
            var current = root;
            for (; index < segments.Length; index++)
            {
                var matches = Enumerable.Range(0, current.childCount).Select(current.GetChild)
                    .Where(child => child.name == segments[index]).ToArray();
                if (matches.Length != 1)
                    throw new InvalidOperationException("Object path segment '" + segments[index] +
                        "' matched " + matches.Length + " children under " + HierarchyPath(current) + ".");
                current = matches[0];
            }
            return current;
        }

        private static string HierarchyPath(Transform transform)
        {
            var names = new List<string>();
            for (var current = transform; current != null; current = current.parent) names.Add(current.name);
            names.Reverse();
            return "/" + String.Join("/", names.ToArray());
        }

        private static void CreateExtractBackup(string operationId, string hostPath, string outputPath)
        {
            var directory = ExtractBackupDirectory(operationId);
            if (Directory.Exists(directory)) throw new IOException("Extract operation backup already exists: " + operationId);
            Directory.CreateDirectory(directory);
            File.Copy(AbsoluteAssetPath(hostPath), Path.Combine(directory, "host.prefab"), false);
            var hostMeta = AbsoluteAssetPath(hostPath) + ".meta";
            if (File.Exists(hostMeta)) File.Copy(hostMeta, Path.Combine(directory, "host.prefab.meta"), false);
            WriteExtractManifest(directory, new PrefabExtractBackupManifest
            {
                operation_id = operationId, host_path = hostPath, output_path = outputPath,
                completed = false, undone = false
            });
        }

        private static void CompleteExtractBackup(string operationId)
        {
            var manifest = ReadExtractManifest(operationId);
            manifest.completed = true;
            WriteExtractManifest(ExtractBackupDirectory(operationId), manifest);
        }

        private static void RestoreExtractBackup(string operationId, bool markUndone)
        {
            var directory = ExtractBackupDirectory(operationId);
            var manifest = ReadExtractManifest(operationId);
            File.Copy(Path.Combine(directory, "host.prefab"), AbsoluteAssetPath(manifest.host_path), true);
            var backupMeta = Path.Combine(directory, "host.prefab.meta");
            if (File.Exists(backupMeta)) File.Copy(backupMeta, AbsoluteAssetPath(manifest.host_path) + ".meta", true);
            if (AssetDatabase.LoadMainAssetAtPath(manifest.output_path) != null)
                AssetDatabase.DeleteAsset(manifest.output_path);
            else
            {
                TryDelete(AbsoluteAssetPath(manifest.output_path));
                TryDelete(AbsoluteAssetPath(manifest.output_path) + ".meta");
            }
            AssetDatabase.ImportAsset(manifest.host_path,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            if (markUndone)
            {
                manifest.undone = true;
                WriteExtractManifest(directory, manifest);
            }
        }

        private static PrefabExtractBackupManifest ReadExtractManifest(string operationId)
        {
            var path = Path.Combine(ExtractBackupDirectory(operationId), "manifest.json");
            if (!File.Exists(path)) throw new FileNotFoundException("Extract operation backup was not found: " + operationId);
            var value = JsonUtility.FromJson<PrefabExtractBackupManifest>(File.ReadAllText(path));
            if (value == null || value.operation_id != operationId)
                throw new InvalidDataException("Extract backup manifest is invalid: " + operationId);
            return value;
        }

        private static void WriteExtractManifest(string directory, PrefabExtractBackupManifest manifest) =>
            File.WriteAllText(Path.Combine(directory, "manifest.json"), JsonUtility.ToJson(manifest, true));

        private static string ExtractBackupDirectory(string operationId) =>
            Path.Combine(ProjectRoot(), "Library", "FakeUnityCLI", "prefab-extract-undo", operationId);

        private static string AbsoluteAssetPath(string path) =>
            Path.Combine(ProjectRoot(), path.Replace('/', Path.DirectorySeparatorChar));

        private static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
        }

        [Serializable]
        private sealed class PrefabExtractPayload
        {
            public string object_path;
            public string bind_component;
            public string bind_field;
            public string bind_container_field;
            public bool dry_run;
        }

        [Serializable]
        private sealed class PrefabExtractUndoPayload { public string operation_id; }

        [Serializable]
        private sealed class PrefabExtractBackupManifest
        {
            public string operation_id;
            public string host_path;
            public string output_path;
            public bool completed;
            public bool undone;
        }
    }
}
#endif
