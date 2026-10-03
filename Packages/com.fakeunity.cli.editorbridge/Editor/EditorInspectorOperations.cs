#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FakeUnityCLI.EditorBridge
{
    internal static class EditorInspectorOperations
    {
        internal sealed class WriteOutcomeUnknownException : Exception
        {
            internal WriteOutcomeUnknownException(string id, Exception inner)
                : base("Editor write started; query the original request and inspect the target before any retry. operation_id=" + id, inner) { }
        }
        internal const string GetOperation = "editor-inspector-get";
        internal const string SetOperation = "editor-inspector-set";
        internal const string UndoOperation = "editor-inspector-undo";
        internal static bool Handles(string operation) => operation == GetOperation || operation == SetOperation || operation == UndoOperation;

        internal static string Execute(string operation, string json, string requestId)
        {
            var request = Parse(json);
            if (operation == GetOperation) return SerializeBounded(Read(request));
            if (operation == UndoOperation) return SerializeBounded(Undo(request, requestId));
            return SerializeBounded(Set(request, requestId));
        }

        private static JObject Read(JObject request)
        {
            using (var target = EditorObjectTarget.Resolve(request["target"] as JObject, false))
            {
                var result = new JObject { ["target"] = target.Describe(), ["schema_version"] = "1.0" };
                var components = new JArray();
                foreach (var component in target.GameObject.GetComponents<Component>())
                {
                    if (component == null) { components.Add(new JObject { ["missing_script"] = true }); continue; }
                    var saved = target.Component;
                    target.Component = component;
                    components.Add(target.Describe());
                    target.Component = saved;
                }
                result["components"] = components;
                if (target.Component == null) return result;
                using (var serialized = new SerializedObject(target.Component))
                {
                    var properties = new JArray();
                    var limit = (int?)request["limit"] ?? 200;
                    if (limit < 1 || limit > 1000) throw new ArgumentException("limit must be 1..1000.");
                    var filter = (string)request["property"];
                    var iterator = filter == null ? serialized.GetIterator() : EditorPropertyValues.Find(serialized, filter);
                    var truncated = false;
                    var scanned = 0;
                    while (filter != null || iterator.Next(true))
                    {
                        if (++scanned > 10000) { truncated = true; break; }
                        var name = EditorPropertyValues.PublicName(iterator);
                        if (name != null)
                        {
                            if (properties.Count == limit) { truncated = true; break; }
                            properties.Add(new JObject { ["name"] = name, ["type"] = iterator.propertyType.ToString(),
                                ["value"] = EditorPropertyValues.Read(iterator, target.Root), ["writable"] = iterator.editable && EditorPropertyValues.Supported(iterator),
                                ["supported"] = EditorPropertyValues.Supported(iterator), ["has_children"] = iterator.hasChildren });
                        }
                        if (filter != null) break;
                    }
                    result["properties"] = properties;
                    result["truncated"] = truncated;
                    result["scanned"] = scanned;
                }
                return result;
            }
        }

        private static JObject Set(JObject request, string requestId)
        {
            RequireWriteMode(request);
            var dryRun = (bool?)request["dry_run"] ?? false;
            if (!dryRun && (bool?)request["confirmed"] != true) throw new ArgumentException("Explicit confirmation is required.");
            var values = request["values"] as JObject;
            if (values == null || values.Count == 0 || values.Count > 64) throw new ArgumentException("values requires 1..64 semantic properties.");
            var names = values.Properties().Select(p => p.Name).ToArray();
            if (names.Any(a => names.Any(b => a != b && b.StartsWith(a + ".", StringComparison.Ordinal))))
                throw new ArgumentException("Overlapping parent/child property writes are not supported.");
            // Preview uses persistent Prefab objects; no LoadPrefabContents callbacks on dry-run.
            using (var target = EditorObjectTarget.Resolve(request["target"] as JObject, !dryRun))
            {
                if (target.Component == null) throw new ArgumentException("target.component_type is required.");
                using (var serialized = new SerializedObject(target.Component))
                {
                    var before = new JObject();
                    var after = new JObject();
                    var expected = request["expected_values"] as JObject;
                    foreach (var property in values.Properties())
                    {
                        var field = EditorPropertyValues.Find(serialized, property.Name);
                        var old = EditorPropertyValues.Read(field, target.Root);
                        before[property.Name] = old;
                        if (expected != null && (!expected.ContainsKey(property.Name) || !EditorPropertyValues.Equivalent(expected[property.Name], old)))
                            throw new InvalidOperationException("Property changed since receipt: " + property.Name);
                        // Reject a forward change when its previous value cannot be restored by this contract.
                        EditorPropertyValues.Write(field, old, target.Root);
                        EditorPropertyValues.Write(field, property.Value, target.Root);
                        after[property.Name] = EditorPropertyValues.Read(field, target.Root);
                    }
                    var changed = !JToken.DeepEquals(before, after);
                    var response = new JObject { ["operation_id"] = requestId, ["target"] = target.Describe(), ["dry_run"] = dryRun,
                        ["changed"] = changed, ["before"] = before, ["after"] = after, ["applied"] = false,
                        ["undo_kind"] = "receipt_bound_property_restore", ["asset_saved"] = false };
                    SerializeBounded(response);
                    if (dryRun || !changed) return response;
                    RequireWriteMode(request);
                    // Persist intent before the first native mutation. A crash never fabricates a successful receipt.
                    var receipt = new JObject { ["state"] = "prepared", ["operation_id"] = requestId,
                        ["target"] = target.Describe(), ["before"] = before.DeepClone(), ["after"] = after.DeepClone() };
                    Persist(requestId, receipt);
                    UnityEditor.Undo.RecordObject(target.Component, "FakeUnityCLI Inspector " + requestId);
                    try
                    {
                        serialized.ApplyModifiedProperties();
                        if (!String.IsNullOrEmpty(target.AssetPath))
                        {
                            if (EditorObjectTarget.FileHash(target.AssetPath) != target.AssetHash)
                                throw new InvalidOperationException("Prefab changed during preparation; no asset save performed.");
                            PrefabUtility.SaveAsPrefabAsset(target.Root, target.AssetPath, out var success);
                            if (!success) throw new IOException("Prefab save failed.");
                            target.AssetHash = EditorObjectTarget.FileHash(target.AssetPath);
                            response["asset_saved"] = true;
                        }
                        else
                        {
                            PrefabUtility.RecordPrefabInstancePropertyModifications(target.Component);
                            EditorSceneManager.MarkSceneDirty(target.GameObject.scene);
                        }
                        serialized.Update();
                        foreach (var name in names) after[name] = EditorPropertyValues.Read(EditorPropertyValues.Find(serialized, name), target.Root);
                        response["target"] = target.Describe();
                        response["after"] = after;
                        response["applied"] = true;
                        response["changed_assets"] = new JArray(String.IsNullOrEmpty(target.AssetPath) ? target.GameObject.scene.path : target.AssetPath);
                        response["side_effect_scope"] = "target_only_not_project_wide_import_audit";
                        receipt["state"] = "applied";
                        receipt["target"] = target.Describe();
                        receipt["after"] = after.DeepClone();
                        Persist(requestId, receipt);
                        response["undo_operation_id"] = requestId;
                        return response;
                    }
                    catch (Exception exception)
                    {
                        receipt["state"] = "outcome_unknown";
                        try { Persist(requestId, receipt); }
                        catch (Exception persistenceError) { UnityEngine.Debug.LogWarning("[FakeUnityCLI] Inspector failure receipt unavailable: " + persistenceError.Message); }
                        throw new WriteOutcomeUnknownException(requestId, exception);
                    }
                }
            }
        }

        private static JObject Undo(JObject request, string requestId)
        {
            var id = (string)request["operation_id"];
            if (File.Exists(ReceiptPath(id, "restored"))) throw new InvalidOperationException("Receipt has already been restored.");
            var receipt = Parse(File.ReadAllText(ReceiptPath(id), Encoding.UTF8));
            if ((string)receipt["state"] != "applied") throw new InvalidOperationException("Only a completed applied receipt can be restored.");
            var inverse = new JObject { ["target"] = receipt["target"].DeepClone(), ["values"] = receipt["before"].DeepClone(),
                ["expected_values"] = receipt["after"].DeepClone(), ["confirmed"] = request["confirmed"], ["dry_run"] = request["dry_run"],
                ["development_mode"] = request["development_mode"] };
            var result = Set(inverse, requestId);
            if ((bool?)result["applied"] == true)
            {
                receipt["state"] = "restored";
                receipt["restore_operation_id"] = requestId;
                try { Persist(id, receipt); }
                catch (Exception exception) { throw new WriteOutcomeUnknownException(requestId, exception); }
            }
            return result;
        }

        private static void RequireWriteMode(JObject request)
        {
            var target = request["target"] as JObject;
            if (!String.IsNullOrEmpty((string)target?["asset_path"]))
            {
                EditorObjectTarget.ValidateAssetPath((string)target["asset_path"]);
                EditorPrefabOperations.RequireCleanEditor(request, false);
                return;
            }
            var state = EditorActivityBridge.Snapshot();
            if (state.execution_mode != "edit" || state.is_playing || state.is_playing_or_will_change_playmode ||
                state.is_compiling || state.is_updating)
                throw new InvalidOperationException("Scene Inspector writes require idle Edit Mode; use runtime-inspector-set with a current Play target for runtime instance changes.");
        }

        // The request's optional target is never trusted for undo: only the immutable applied receipt
        // establishes whether the operation restores an asset or a scene instance.
        internal static bool IsAssetWrite(string operation, string json)
        {
            var request = Parse(json);
            var target = request["target"] as JObject;
            if (operation == UndoOperation)
            {
                var receipt = Parse(File.ReadAllText(ReceiptPath((string)request["operation_id"]), Encoding.UTF8));
                if ((string)receipt["state"] != "applied") return false;
                target = receipt["target"] as JObject;
            }
            var path = (string)target?["asset_path"];
            if (String.IsNullOrEmpty(path)) return false;
            EditorObjectTarget.ValidateAssetPath(path);
            return true;
        }

        internal static JObject Parse(string json, int maximumCharacters = 256 * 1024)
        {
            if (String.IsNullOrWhiteSpace(json) || json.Length > maximumCharacters) throw new ArgumentException("JSON is empty or exceeds its character budget.");
            using (var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 24, DateParseHandling = DateParseHandling.None })
            {
                var value = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new ArgumentException("Trailing JSON content.");
                return value;
            }
        }

        private static string SerializeBounded(JObject value)
        {
            var json = value.ToString(Formatting.None);
            if (Encoding.UTF8.GetByteCount(json) > 1024 * 1024) throw new ArgumentException("Inspector result exceeds 1 MiB; narrow the property query.");
            return json;
        }

        private static string ReceiptPath(string id, string state = "applied")
        {
            if (!Guid.TryParseExact(id, "N", out _)) throw new ArgumentException("operation_id must be a 32-character GUID.");
            if (state != "prepared" && state != "applied" && state != "restored" && state != "outcome_unknown") throw new ArgumentException("Invalid receipt state.");
            return Path.Combine(Application.dataPath, "..", "Library", "FakeUnityCLI", "inspector-receipts", id + "." + state + ".json");
        }

        private static void Persist(string id, JObject receipt)
        {
            var path = ReceiptPath(id, (string)receipt["state"]);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temp = path + ".tmp-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(temp, receipt.ToString(Formatting.None), new UTF8Encoding(false));
            // Each state is immutable; imports and file watchers need no delete-share on an existing receipt.
            File.Move(temp, path);
        }
    }
}
#endif
