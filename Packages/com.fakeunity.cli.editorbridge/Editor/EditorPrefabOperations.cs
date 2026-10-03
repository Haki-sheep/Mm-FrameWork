#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FakeUnityCLI.EditorBridge
{
    internal static class EditorPrefabOperations
    {
        internal const string ReadOperation = "editor-prefab-inspect";
        internal const string EditOperation = "editor-prefab-edit";
        internal const string UndoOperation = "editor-prefab-undo";
        internal const string ResultOperation = "editor-prefab-result";
        internal static bool Handles(string operation) => operation == ReadOperation || operation == EditOperation || operation == UndoOperation || operation == ResultOperation;

        internal static string Execute(string operation, string json, string requestId)
        {
            var request = EditorInspectorOperations.Parse(json);
            if (operation == ResultOperation) return Bounded(ReadReceipt((string)request["operation_id"]));
            if (operation == ReadOperation)
            {
                var path = (string)request["asset_path"];
                EditorObjectTarget.ValidateAssetPath(path);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root == null) throw new ArgumentException("Prefab not found.");
                return Bounded(Describe(root, path));
            }
            RequireCleanEditor(request);
            if ((bool?)request["dry_run"] != true && (bool?)request["confirmed"] != true)
                throw new ArgumentException("Prefab mutation requires --yes or --dry-run.");
            var result = operation == UndoOperation ? Undo(request, requestId) : Edit(request, requestId);
            try { return Bounded(result); }
            catch (Exception exception)
            {
                if ((bool?)result["saved"] == true || (bool?)result["restored"] == true)
                    throw new EditorInspectorOperations.WriteOutcomeUnknownException(requestId, exception);
                throw;
            }
        }

        private static JObject Edit(JObject request, string id)
        {
            var path = (string)request["asset_path"];
            EditorObjectTarget.ValidateAssetPath(path);
            var beforeHash = EditorObjectTarget.FileHash(path);
            if (beforeHash != (string)request["asset_sha256"]) throw new InvalidOperationException("Prefab hash changed or is missing; inspect again.");
            var operations = request["operations"] as JArray;
            if (operations == null || operations.Count == 0 || operations.Count > 32) throw new ArgumentException("operations must contain 1..32 component operations.");
            foreach (var operation in operations)
                if (!(operation is JObject op) || !new[] { "component.add", "component.remove", "component.set" }.Contains((string)op["op"]))
                    throw new ArgumentException("Supported operations: component.add/remove/set.");
            var dryRun = (bool?)request["dry_run"] == true;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var before = Describe(asset, path);
            var originalIssues = IssueKeys((JArray)before["issues"]);
            var permitted = PermittedImports(request, path, dryRun);
            var sourceGuid = AssetDatabase.AssetPathToGUID(path);
            var metaHash = EditorObjectTarget.FileHash(path + ".meta");
            GameObject root = null;
            var saved = false;
            var receipt = new JObject { ["operation_id"] = id, ["asset_path"] = path, ["before_sha256"] = beforeHash,
                ["asset_guid"] = sourceGuid, ["meta_sha256"] = metaHash, ["expected_imports"] = new JArray(permitted) };
            using (var audit = new EditorAssetChangeAudit(permitted))
            {
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    if (root == null) throw new InvalidOperationException("LoadPrefabContents returned null.");
                    audit.RequireExpected();
                    var results = new JArray();
                    foreach (JObject op in operations)
                    {
                        results.Add(Apply(root, op));
                        audit.RequireExpected();
                    }
                    var candidate = Describe(root, path);
                    if (IssueKeys((JArray)candidate["issues"]).Except(originalIssues).Any())
                        throw new InvalidOperationException("Prefab edit introduces missing scripts/references; no save performed.");
                    var response = new JObject { ["operation_id"] = id, ["dry_run"] = dryRun, ["saved"] = false,
                        ["operations"] = results, ["before"] = before, ["after"] = candidate,
                        ["temporary_contents_unloaded"] = true, ["undo_kind"] = "asset_hash_bound_byte_restore",
                        ["preview_scope"] = "owned_prefab_memory_with_unity_callbacks_no_asset_save" };
                    Bounded(response);
                    if (dryRun)
                    {
                        if (EditorObjectTarget.FileHash(path) != beforeHash) throw new InvalidOperationException("Prefab bytes changed during preview.");
                        response["import_audit"] = audit.Describe();
                        return response;
                    }
                    if (EditorObjectTarget.FileHash(path) != beforeHash || EditorObjectTarget.FileHash(path + ".meta") != metaHash)
                        throw new InvalidOperationException("Prefab or meta changed during preparation.");
                    var backup = ReceiptPath(id, "before.bin");
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    var full = FullPath(path);
                    if (new FileInfo(full).Length > 32 * 1024 * 1024) throw new ArgumentException("Prefab exceeds the 32 MiB backup budget.");
                    File.Copy(full, backup, false);
                    Persist(id, "prepared", receipt);
                    RequireCleanEditor(request);
                    saved = true;
                    PrefabUtility.SaveAsPrefabAsset(root, path, out var success);
                    if (!success) throw new IOException("Prefab save failed.");
                    audit.RequireExpected();
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    audit.RequireExpected();
                    if (AssetDatabase.AssetPathToGUID(path) != sourceGuid || EditorObjectTarget.FileHash(path + ".meta") != metaHash)
                        throw new InvalidOperationException("Prefab import changed its GUID or importer metadata.");
                    var readback = Describe(AssetDatabase.LoadAssetAtPath<GameObject>(path), path);
                    if (IssueKeys((JArray)readback["issues"]).Except(originalIssues).Any()) throw new InvalidOperationException("Saved Prefab has new missing references/scripts.");
                    receipt["after_sha256"] = EditorObjectTarget.FileHash(path);
                    receipt["import_audit"] = audit.Describe();
                    Persist(id, "applied", receipt);
                    response["saved"] = true;
                    response["after"] = readback;
                    response["import_audit"] = audit.Describe();
                    response["undo_operation_id"] = id;
                    return response;
                }
                catch (Exception exception)
                {
                    receipt["error"] = exception.Message;
                    receipt["save_started"] = saved;
                    receipt["import_audit"] = audit.Describe();
                    try { Persist(id, "failed", receipt); } catch { }
                    if (saved || audit.Unexpected.Length > 0 || audit.Truncated)
                        throw new EditorInspectorOperations.WriteOutcomeUnknownException(id, exception);
                    throw;
                }
                finally { if (root != null) PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        private static JObject Apply(GameObject root, JObject operation)
        {
            var path = (string)operation["object_path"];
            if (String.IsNullOrEmpty(path) || !path.StartsWith("/", StringComparison.Ordinal)) throw new ArgumentException("object_path must begin with /.");
            var go = EditorObjectTarget.Find(new[] { root }, path);
            var name = (string)operation["component_type"];
            var type = TypeCache.GetTypesDerivedFrom<Component>().FirstOrDefault(t => t.FullName == name && !t.IsAbstract && !t.ContainsGenericParameters);
            if (type == null) throw new ArgumentException("Use a concrete, fully qualified component_type: " + name);
            var previous = go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().FullName).ToArray();
            var kind = (string)operation["op"];
            Component component;
            if (kind == "component.add")
            {
                if (typeof(Transform).IsAssignableFrom(type)) throw new ArgumentException("Transform replacement is not a component-add operation.");
                var beforeCount = go.GetComponents(type).Length;
                component = go.AddComponent(type);
                if (component == null || go.GetComponents(type).Length != beforeCount + 1)
                    throw new ArgumentException("Unity rejected component addition or DisallowMultipleComponent applies.");
            }
            else
            {
                var candidates = go.GetComponents(type);
                var index = (int?)operation["component_index"];
                if (index == null && candidates.Length != 1 || (index ?? 0) < 0 || (index ?? 0) >= candidates.Length)
                    throw new ArgumentException("Component is missing or ambiguous; use component_index.");
                component = candidates[index ?? 0];
            }
            if (kind == "component.remove")
            {
                if (component is Transform) throw new ArgumentException("Cannot remove Transform.");
                if (PrefabUtility.IsPartOfPrefabInstance(component) && !PrefabUtility.IsAddedComponentOverride(component))
                    throw new ArgumentException("Removing inherited Variant/nested Prefab components is not supported; use explicit override tools.");
                RequireUnreferenced(root, component);
                UnityEngine.Object.DestroyImmediate(component);
                if (component != null) throw new InvalidOperationException("Unity refused removal, possibly due to RequireComponent.");
            }
            else if (operation["values"] is JObject values)
            {
                if (values.Count == 0 || values.Count > 64) throw new ArgumentException("values requires 1..64 fields.");
                using (var serialized = new SerializedObject(component))
                {
                    foreach (var property in values.Properties())
                        EditorPropertyValues.Write(EditorPropertyValues.Find(serialized, property.Name), property.Value, root);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
            else if (kind == "component.set") throw new ArgumentException("component.set requires values.");
            return new JObject { ["op"] = kind, ["object_path"] = path, ["component_type"] = name,
                ["components_before"] = new JArray(previous),
                ["components_after"] = new JArray(go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().FullName)) };
        }

        private static void RequireUnreferenced(GameObject root, Component target)
        {
            foreach (var component in root.GetComponentsInChildren<Component>(true).Where(c => c != null && c != target))
                using (var serialized = new SerializedObject(component))
                {
                    var p = serialized.GetIterator();
                    var count = 0;
                    while (p.Next(true))
                    {
                        if (++count > 10000) throw new InvalidOperationException("Reference scan budget exceeded.");
                        if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == target)
                            throw new ArgumentException("Component is referenced inside the Prefab; clear its reference explicitly before removing.");
                    }
                }
        }

        private static JObject Describe(GameObject root, string path)
        {
            if (root == null) throw new ArgumentException("Prefab root not found.");
            var transforms = root.GetComponentsInChildren<Transform>(true);
            if (transforms.Length > 2000) throw new ArgumentException("Prefab hierarchy exceeds 2000 objects; use scoped offline inspection.");
            var nodes = new JArray();
            var issues = new JArray();
            var properties = 0;
            foreach (var transform in transforms)
            {
                var objectPath = EditorObjectTarget.HierarchyPath(transform);
                var components = new JArray();
                var componentSlot = -1;
                foreach (var component in transform.GetComponents<Component>())
                {
                    componentSlot++;
                    if (component == null) { issues.Add(new JObject { ["kind"] = "missing_script", ["object_path"] = objectPath, ["component_slot"] = componentSlot }); continue; }
                    components.Add(new JObject { ["type"] = component.GetType().FullName,
                        ["index"] = Array.IndexOf(transform.GetComponents(component.GetType()), component) });
                    using (var serialized = new SerializedObject(component))
                    {
                        var p = serialized.GetIterator();
                        while (p.Next(true))
                        {
                            if (++properties > 100000) throw new ArgumentException("Prefab serialized-property scan exceeds 100000 nodes.");
                            if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null && EditorIdentity.HasReference(p))
                                issues.Add(new JObject { ["kind"] = "missing_reference", ["object_path"] = objectPath,
                                    ["component_type"] = component.GetType().FullName, ["component_slot"] = componentSlot,
                                    ["field"] = EditorPropertyValues.PublicName(p) ?? "internal_reference", ["field_index"] = properties });
                        }
                    }
                }
                nodes.Add(new JObject { ["object_path"] = objectPath, ["active"] = transform.gameObject.activeSelf,
                    ["nested_source"] = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(transform.gameObject), ["components"] = components });
            }
            return new JObject { ["asset_path"] = path, ["asset_sha256"] = EditorObjectTarget.FileHash(path),
                ["asset_guid"] = AssetDatabase.AssetPathToGUID(path), ["prefab_kind"] = PrefabUtility.GetPrefabAssetType(root).ToString(),
                ["nodes"] = nodes, ["issues"] = issues, ["properties_scanned"] = properties,
                ["dependencies"] = new JArray(AssetDatabase.GetDependencies(path, true)) };
        }

        private static HashSet<string> IssueKeys(JArray issues) => new HashSet<string>(issues.Select(i => i.ToString(Formatting.None)), StringComparer.Ordinal);

        internal static void RequireCleanEditor(JObject request, bool requireCleanScenes = true)
        {
            var state = EditorActivityBridge.Snapshot();
            var stablePlay = (string)request["development_mode"] == "free" && state.execution_mode == "play" &&
                state.is_playing && state.is_playing_or_will_change_playmode;
            if (state.is_compiling || state.is_updating ||
                !stablePlay && (state.execution_mode != "edit" || state.is_playing || state.is_playing_or_will_change_playmode))
                throw new InvalidOperationException("Asset writes require idle Edit or stable Play in free development mode; mode transitions, compilation and asset updating are excluded.");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) throw new InvalidOperationException("User Prefab Stage must be closed first.");
            // Runtime scenes commonly become dirty during Play. This workflow owns isolated Prefab contents
            // and never saves a user scene, so those dirty flags do not require leaving Play.
            if (stablePlay || !requireCleanScenes) return;
            for (var i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save dirty loaded scenes before Prefab operations.");
        }

        private static string[] PermittedImports(JObject request, string target, bool dryRun)
        {
            if (dryRun) return new string[0];
            var paths = request["expected_imports"] as JArray ?? new JArray();
            if (paths.Count > 32 || paths.Any(p => p.Type != JTokenType.String || !((string)p).StartsWith("Assets/", StringComparison.Ordinal) ||
                ((string)p).Contains("..") || ((string)p).Contains('*') || ((string)p).Contains('\\')))
                throw new ArgumentException("expected_imports requires at most 32 exact Assets paths without wildcards.");
            return paths.Select(p => (string)p).Append(target).Distinct().ToArray();
        }

        private static JObject Undo(JObject request, string id)
        {
            var originalId = (string)request["operation_id"];
            if (File.Exists(ReceiptPath(originalId, "restored.json"))) throw new InvalidOperationException("Prefab edit is already restored.");
            var receipt = EditorInspectorOperations.Parse(File.ReadAllText(ReceiptPath(originalId, "applied.json")), 1024 * 1024);
            var path = (string)receipt["asset_path"];
            EditorObjectTarget.ValidateAssetPath(path);
            if (EditorObjectTarget.FileHash(path) != (string)receipt["after_sha256"] ||
                AssetDatabase.AssetPathToGUID(path) != (string)receipt["asset_guid"] || EditorObjectTarget.FileHash(path + ".meta") != (string)receipt["meta_sha256"])
                throw new InvalidOperationException("Prefab/GUID/meta changed since receipt; refuse restore.");
            var dryRun = (bool?)request["dry_run"] == true;
            if (dryRun) return new JObject { ["operation_id"] = id, ["dry_run"] = true, ["restored"] = false, ["asset_path"] = path };
            var backup = ReceiptPath(originalId, "before.bin");
            using (var stream = File.OpenRead(backup))
            using (var sha = System.Security.Cryptography.SHA256.Create())
                if (BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != (string)receipt["before_sha256"])
                    throw new InvalidOperationException("Prefab backup hash mismatch.");
            using (var audit = new EditorAssetChangeAudit(((JArray)receipt["expected_imports"]).Values<string>()))
            {
                Persist(id, "prepared", new JObject { ["restore_of"] = originalId, ["asset_path"] = path });
                try
                {
                    RequireCleanEditor(request);
                    File.Copy(backup, FullPath(path), true);
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                    audit.RequireExpected();
                    if (EditorObjectTarget.FileHash(path) != (string)receipt["before_sha256"] || EditorObjectTarget.FileHash(path + ".meta") != (string)receipt["meta_sha256"])
                        throw new InvalidOperationException("Import did not preserve the restored Prefab/meta bytes.");
                    var result = new JObject { ["operation_id"] = id, ["restored"] = true, ["asset_path"] = path,
                        ["asset_sha256"] = EditorObjectTarget.FileHash(path), ["import_audit"] = audit.Describe(), ["restore_of"] = originalId };
                    Persist(originalId, "restored", result);
                    Persist(id, "applied", result);
                    return result;
                }
                catch (Exception exception)
                {
                    try { Persist(id, "failed", new JObject { ["error"] = exception.Message, ["import_audit"] = audit.Describe() }); } catch { }
                    throw new EditorInspectorOperations.WriteOutcomeUnknownException(id, exception);
                }
            }
        }

        private static string FullPath(string asset) => Path.Combine(Application.dataPath, "..", asset);
        private static JObject ReadReceipt(string id)
        {
            var stages = new JObject();
            foreach (var stage in new[] { "prepared", "applied", "failed", "restored" })
            {
                var path = ReceiptPath(id, stage + ".json");
                if (File.Exists(path)) stages[stage] = EditorInspectorOperations.Parse(File.ReadAllText(path), 1024 * 1024);
            }
            if (!stages.HasValues) throw new FileNotFoundException("No Prefab receipt exists for operation_id.");
            return new JObject { ["operation_id"] = id, ["stages"] = stages,
                ["state"] = stages["restored"] != null ? "restored" : stages["failed"] != null ? "requires_inspection" : stages["applied"] != null ? "applied" : "outcome_unknown",
                ["automatic_retry_safe"] = false };
        }
        private static string Bounded(JObject result)
        {
            var text = result.ToString(Formatting.None);
            if (Encoding.UTF8.GetByteCount(text) > 1024 * 1024) throw new ArgumentException("Prefab result exceeds 1 MiB; split the operation scope.");
            return text;
        }
        private static string ReceiptPath(string id, string file)
        {
            if (!Guid.TryParseExact(id, "N", out _)) throw new ArgumentException("operation_id requires a 32-character GUID.");
            return Path.Combine(Application.dataPath, "..", "Library", "FakeUnityCLI", "prefab-edits", id, file);
        }
        private static void Persist(string id, string state, JObject value)
        {
            var path = ReceiptPath(id, state + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(temporary, value.ToString(Formatting.None), new UTF8Encoding(false));
            File.Move(temporary, path);
        }
    }
}
#endif
