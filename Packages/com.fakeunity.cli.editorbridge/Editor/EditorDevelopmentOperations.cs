#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace FakeUnityCLI.EditorBridge
{
    // These operations never participate in the Bridge's automatic reload replay list.
    internal static class EditorDevelopmentOperations
    {
        internal const string StopPlayOperation = "editor-play-stop";
        internal const string RuntimeSetOperation = "editor-runtime-inspector-set";
        private static string _pendingStop;
        internal static bool Handles(string operation) => operation == StopPlayOperation || operation == RuntimeSetOperation;

        internal static RoslynExecutionResult Execute(OnlineRequest envelope)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(envelope.request_id) || envelope.request_id.Length > 80 ||
                    envelope.request_id.Any(character => !Char.IsLetterOrDigit(character) && character != '-' && character != '_'))
                    throw new ArgumentException("request_id must contain only letters, digits, hyphens or underscores, at most 80 characters.");
                if (envelope.operation == StopPlayOperation && !Guid.TryParseExact(envelope.request_id, "N", out _))
                    throw new ArgumentException("Play stop request_id must be a 32-character GUID.");
                var request = EditorInspectorOperations.Parse(envelope.payload_json);
                EditorActivityBridge.RecordOperation(envelope.operation, envelope.request_id);
                var response = envelope.operation == StopPlayOperation ? StopPlay(envelope, request) : RuntimeSet(envelope, request);
                var verified = (string)response["state"] != "applied_readback_mismatch";
                return new RoslynExecutionResult { Success = verified, State = verified ? "success" : "runtime_verification_failed", ResultJson = Serialize(response),
                    ResultType = typeof(JObject).FullName, ResultText = (string)response["state"] };
            }
            catch (Exception exception)
            {
                return new RoslynExecutionResult { Success = false,
                    State = exception is EditorInspectorOperations.WriteOutcomeUnknownException ? "outcome_unknown" : "runtime_error",
                    ErrorType = exception.GetType().FullName, ErrorMessage = exception.Message, ErrorStackTrace = exception.ToString() };
            }
        }

        private static JObject StopPlay(OnlineRequest envelope, JObject request)
        {
            if ((string)request["development_mode"] != "free")
                throw new ArgumentException("Stopping Play automatically requires development_mode=free.");
            var state = EditorActivityBridge.Snapshot();
            var result = new JObject { ["operation_id"] = envelope.request_id, ["development_mode"] = "free",
                ["state"] = "already_edit", ["stop_requested"] = false, ["play_restored"] = false,
                ["requires_idle_edit_observation"] = true };
            if (state.execution_mode == "edit" && !state.is_playing && !state.is_playing_or_will_change_playmode) return result;
            RequireStablePlay();
            if (_pendingStop != null) throw new InvalidOperationException("A Play stop is already pending: " + _pendingStop);
            var identity = envelope.editor_instance_id;
            var session = envelope.submitted_session_id;
            var modeSince = state.mode_observed_since;
            result["state"] = "stop_requested";
            result["stop_requested"] = true;
            result["mode_observed_since"] = modeSince;
            // Persist and return the scheduling receipt before Unity can reload the domain.
            Persist(envelope.request_id, "stop_requested", result);
            _pendingStop = envelope.request_id;
            EditorApplication.delayCall += () =>
            {
                try
                {
                    var responsePath = Path.Combine(ProjectRoot(), "Library", "FakeUnityCLI", "responses", envelope.request_id + ".json");
                    if (!File.Exists(responsePath) || (bool?)JObject.Parse(File.ReadAllText(responsePath))["success"] != true)
                        throw new InvalidOperationException("Stop scheduling response was not persisted successfully; no stop was performed.");
                    if (identity != BridgeBootstrap.EditorInstanceId || session != LogCaptureBridge.CurrentSessionId ||
                        modeSince != EditorActivityBridge.Snapshot().mode_observed_since)
                        throw new InvalidOperationException("Editor identity or Play observation changed before stopping.");
                    DateTime expires;
                    if (!String.IsNullOrEmpty(envelope.expires_at) && DateTime.TryParse(envelope.expires_at, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out expires) && DateTime.UtcNow > expires)
                        throw new InvalidOperationException("Play stop request expired before execution.");
                    using (FakeUnity.Preservation.EditorPreservationGuard.EnterWrite(ProjectRoot()))
                    {
                        RequireStablePlay();
                        EditorApplication.isPlaying = false;
                    }
                }
                catch (Exception exception)
                {
                    try { Persist(envelope.request_id, "stop_not_completed", new JObject { ["operation_id"] = envelope.request_id,
                        ["state"] = "stop_not_completed", ["reason"] = exception.Message }); }
                    catch (Exception persistenceError) { Debug.LogWarning("[FakeUnityCLI] Stop receipt unavailable: " + persistenceError.Message); }
                    Debug.LogWarning("[FakeUnityCLI] Play stop was not completed; inspect the original request and Editor state: " + exception.Message);
                }
                finally { _pendingStop = null; }
            };
            return result;
        }

        private static JObject RuntimeSet(OnlineRequest envelope, JObject request)
        {
            RequireStablePlay();
            var mode = (string)request["development_mode"];
            if (mode != "auto" && mode != "free") throw new ArgumentException("Runtime writes require development_mode=auto or free.");
            var selector = request["target"] as JObject;
            var observation = (string)selector?["play_mode_observed_since"];
            if (String.IsNullOrEmpty(observation) || observation != EditorActivityBridge.Snapshot().mode_observed_since)
                throw new InvalidOperationException("Runtime writes require target.play_mode_observed_since from inspection during the current Play.");
            if (selector == null || !String.IsNullOrEmpty((string)selector["asset_path"]))
                throw new ArgumentException("Runtime writes require a live scene component, never an asset target.");
            var source = (string)selector["source"];
            if (source != "play_scene" && source != "play_persistent_scene")
                throw new ArgumentException("Read the target during Play before submitting a runtime write.");
            var dryRun = (bool?)request["dry_run"] ?? false;
            if (!dryRun && (bool?)request["confirmed"] != true) throw new ArgumentException("Explicit confirmation is required.");
            var values = request["values"] as JObject;
            if (values == null || values.Count < 1 || values.Count > 64) throw new ArgumentException("values requires 1..64 semantic properties.");
            var names = values.Properties().Select(p => p.Name).ToArray();
            if (names.Any(a => names.Any(b => a != b && b.StartsWith(a + ".", StringComparison.Ordinal))))
                throw new ArgumentException("Overlapping parent/child property writes are not supported.");
            using (var target = EditorObjectTarget.Resolve(selector, true))
            {
                if (target.Component == null || EditorUtility.IsPersistent(target.Component) || EditorUtility.IsPersistent(target.GameObject))
                    throw new ArgumentException("Runtime writes require a nonpersistent scene component.");
                using (var serialized = new SerializedObject(target.Component))
                {
                    serialized.Update();
                    var before = new JObject();
                    var expectedAfter = new JObject();
                    var expected = request["expected_values"] as JObject;
                    foreach (var item in values.Properties())
                    {
                        var property = EditorPropertyValues.Find(serialized, item.Name);
                        RequireSimpleProperty(serialized, property);
                        var current = EditorPropertyValues.Read(property);
                        if (expected != null && (!expected.ContainsKey(item.Name) || !EditorPropertyValues.Equivalent(expected[item.Name], current)))
                            throw new InvalidOperationException("Property changed since receipt: " + item.Name);
                        before[item.Name] = current;
                        EditorPropertyValues.Write(property, item.Value);
                        expectedAfter[item.Name] = EditorPropertyValues.Read(property);
                    }
                    var changed = !EditorPropertyValues.Equivalent(before, expectedAfter);
                    var result = new JObject { ["operation_id"] = envelope.request_id, ["target"] = target.Describe(),
                        ["state"] = dryRun ? "preview" : changed ? "prepared" : "unchanged", ["dry_run"] = dryRun,
                        ["before"] = before, ["expected_after"] = expectedAfter, ["after"] = before.DeepClone(),
                        ["changed"] = changed, ["applied"] = false, ["asset_saved"] = false, ["scene_saved"] = false,
                        ["prefab_applied"] = false, ["persistence"] = "runtime_only", ["play_mode_observed_since"] = observation,
                        ["side_effect_scope"] = "component_callbacks_may_affect_runtime", ["verified"] = !changed };
                    Serialize(result);
                    if (dryRun || !changed) return result;
                    RequireStablePlay();
                    using (var rechecked = EditorObjectTarget.Resolve(selector, true))
                        if (rechecked.Component != target.Component) throw new InvalidOperationException("Component changed before applying.");
                    Persist(envelope.request_id, "prepared", result);
                    try
                    {
                        // No Undo, SetDirty, scene save or Prefab persistence: only the live instance is modified.
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                        serialized.Update();
                        var after = new JObject();
                        foreach (var name in names) after[name] = EditorPropertyValues.Read(EditorPropertyValues.Find(serialized, name));
                        result["after"] = after;
                        result["target"] = target.Describe();
                        result["applied"] = true;
                        result["verified"] = EditorPropertyValues.Equivalent(expectedAfter, after);
                        result["state"] = (bool)result["verified"] ? "applied" : "applied_readback_mismatch";
                        Persist(envelope.request_id, "applied", result);
                        return result;
                    }
                    catch (Exception exception)
                    {
                        try { Persist(envelope.request_id, "outcome_unknown", new JObject { ["operation_id"] = envelope.request_id,
                            ["state"] = "outcome_unknown", ["before"] = before.DeepClone(), ["expected_after"] = expectedAfter.DeepClone() }); }
                        catch (Exception persistenceError) { Debug.LogWarning("[FakeUnityCLI] Runtime receipt unavailable: " + persistenceError.Message); }
                        throw new EditorInspectorOperations.WriteOutcomeUnknownException(envelope.request_id, exception);
                    }
                }
            }
        }

        private static void RequireStablePlay()
        {
            var state = EditorActivityBridge.Snapshot();
            if (state.execution_mode != "play" || !state.is_playing || !state.is_playing_or_will_change_playmode ||
                state.is_compiling || state.is_updating)
                throw new InvalidOperationException("Operation requires stable Play without compilation or asset updating; paused Play is supported.");
        }

        private static void RequireSimpleProperty(SerializedObject owner, SerializedProperty property)
        {
            if (!property.editable || !EditorPropertyValues.Supported(property)) throw new ArgumentException("Unsupported runtime property.");
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: case SerializedPropertyType.Boolean: case SerializedPropertyType.Float:
                case SerializedPropertyType.String: case SerializedPropertyType.Color: case SerializedPropertyType.Enum:
                case SerializedPropertyType.Vector2: case SerializedPropertyType.Vector3: case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Vector2Int: case SerializedPropertyType.Vector3Int: case SerializedPropertyType.Rect:
                case SerializedPropertyType.Bounds: break;
                default: throw new ArgumentException("Runtime writes support simple values only; object references and shared assets are excluded.");
            }
            var path = property.propertyPath;
            for (var index = path.IndexOf('.'); index >= 0; index = path.IndexOf('.', index + 1))
            {
                var parent = owner.FindProperty(path.Substring(0, index));
                if (parent != null && parent.propertyType == SerializedPropertyType.ManagedReference)
                    throw new ArgumentException("Runtime writes through managed references are not supported.");
            }
        }

        private static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string Serialize(JObject value)
        {
            var json = value.ToString(Formatting.None);
            if (Encoding.UTF8.GetByteCount(json) > 1024 * 1024) throw new ArgumentException("Development result exceeds 1 MiB.");
            return json;
        }
        private static void Persist(string id, string state, JObject receipt)
        {
            var directory = Path.Combine(ProjectRoot(), "Library", "FakeUnityCLI", "development-receipts");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, id + "." + state + ".json");
            var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(temporary, Serialize(receipt), new UTF8Encoding(false));
            File.Move(temporary, path);
        }
    }
}
#endif
