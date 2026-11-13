#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using FakeUnity.AssetDelete;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FakeUnityCLI.EditorBridge
{
    internal static class AssetDeleteBridge
    {
        /// <summary> 原生资产删除操作 </summary>
        internal const string Operation = "asset-delete";

        /// <summary>
        /// 输入已绑定请求 检查范围和引用 调用 Unity 删除并返回验证结果
        /// </summary>
        internal static RoslynExecutionResult Execute(OnlineRequest Envelope)
        {
            bool Started = false;
            JObject Result = null;
            try
            {
                var Payload = EditorInspectorOperations.Parse(Envelope.payload_json);
                bool DryRun = (bool?)Payload["dry_run"] == true;
                bool Force = (bool?)Payload["force"] == true;
                if (!DryRun && (bool?)Payload["confirmed"] != true) throw new ArgumentException("Delete requires explicit confirmation");
                RequireIdleEdit();
                var Root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                var Snapshot = AssetDeleteContract.Capture(Root, (string)Payload["target"]);
                if (String.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(Snapshot.AssetPath, AssetPathToGUIDOptions.OnlyExistingAssets)))
                    throw new InvalidDataException("Target is not imported by Unity " + Snapshot.AssetPath);
                var ImpactList = Impacts(Snapshot);
                Result = new JObject
                {
                    ["backend"] = "online", ["request_id"] = Envelope.request_id, ["operation_id"] = Envelope.request_id,
                    ["target"] = Snapshot.AssetPath, ["dry_run"] = DryRun, ["deleted"] = false, ["verified"] = false,
                    ["files"] = JArray.FromObject(Snapshot.FileList), ["directories"] = JArray.FromObject(Snapshot.DirectoryList),
                    ["reverse_reference_impacts"] = ImpactList, ["blocked_by_references"] = ImpactList.Count > 0 && !Force,
                    ["forced_with_impacts"] = ImpactList.Count > 0 && Force,
                    ["reference_scope"] = "Unity direct asset dependencies including Packages excluding the deletion scope",
                    ["reference_limitations"] = "Dynamic string references and unsaved in-memory references are not proven absent",
                    ["recovery"] = "No CLI backup or Unity Undo is created Restore asset and meta from version control or an independent backup",
                    ["undo_supported"] = false, ["side_effect_scope"] = "Unity import and deletion callbacks may affect other assets"
                };
                if (DryRun) return Response(Result, true, "success");
                if (ImpactList.Count > 0 && !Force) return Response(Result, false, "write_denied", "Deletion would break asset references");
                RequireIdleEdit();
                if (!AssetDeleteContract.Matches(Snapshot, AssetDeleteContract.Capture(Root, Snapshot.AssetPath)))
                    return Response(Result, false, "target_changed", "Deletion scope or content changed during planning");
                EditorActivityBridge.RecordOperation(Operation, Envelope.request_id);
                // 删除回调可能抛出异常或触发 Reload 一旦调用原生 API 禁止自动重试
                Started = true;
                bool Deleted = AssetDatabase.DeleteAsset(Snapshot.AssetPath);
                bool Absent = !File.Exists(Snapshot.FullPath) && !Directory.Exists(Snapshot.FullPath) &&
                    !File.Exists(Snapshot.FullPath + ".meta") &&
                    Snapshot.FileList.All(Item => !File.Exists(Path.Combine(Root, Item.Path))) &&
                    Snapshot.DirectoryList.All(Item => !Directory.Exists(Path.Combine(Root, Item))) &&
                    String.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(Snapshot.AssetPath, AssetPathToGUIDOptions.OnlyExistingAssets));
                Result["native_delete_returned"] = Deleted;
                Result["deleted"] = Absent;
                Result["verified"] = Deleted && Absent;
                return Response(Result, Deleted && Absent, Deleted && Absent ? "success" : "delete_failed",
                    Deleted && Absent ? null : "Unity delete did not confirm removal of the complete asset and meta scope Query the original request and inspect the target");
            }
            catch (Exception Error)
            {
                if (Result == null) Result = new JObject { ["backend"] = "online", ["request_id"] = Envelope.request_id,
                    ["operation_id"] = Envelope.request_id, ["undo_supported"] = false };
                Result["outcome_unknown"] = Started;
                return Response(Result, false, Started ? "outcome_unknown" : "runtime_error", Error.ToString());
            }
        }

        /// <summary>
        /// 输入删除范围 查找范围外的直接依赖 返回具体来源和目标
        /// </summary>
        private static JArray Impacts(AssetDeleteSnapshot Snapshot)
        {
            var TargetHashList = Snapshot.FileList.Where(Item => !Item.Path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                .Select(Item => Item.Path).Concat(Snapshot.DirectoryList).ToHashSet(StringComparer.Ordinal);
            var ImpactList = new JArray();
            foreach (var Source in AssetDatabase.GetAllAssetPaths().OrderBy(Item => Item, StringComparer.Ordinal))
            {
                if (!(Source.StartsWith("Assets/", StringComparison.Ordinal) || Source.StartsWith("Packages/", StringComparison.Ordinal)) ||
                    TargetHashList.Contains(Source) || AssetDatabase.IsValidFolder(Source)) continue;
                foreach (var Dependency in AssetDatabase.GetDependencies(Source, false).Where(Item => TargetHashList.Contains(Item)))
                    ImpactList.Add(new JObject { ["source_asset"] = Source, ["target_asset"] = Dependency,
                        ["guid"] = AssetDatabase.AssetPathToGUID(Dependency), ["kind"] = "hard_dependency" });
            }
            return ImpactList;
        }

        /// <summary>
        /// 检查空闲 Edit 与未保存现场 拒绝可能丢失内存修改的删除
        /// </summary>
        private static void RequireIdleEdit()
        {
            if (!EditorActivityBridge.ModeMatches("edit") || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("Asset deletion requires idle Edit mode");
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("Close Prefab Stage explicitly before asset deletion");
            for (int Index = 0; Index < UnityEngine.SceneManagement.SceneManager.sceneCount; Index++)
            {
                var Scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(Index);
                if (Scene.isDirty) throw new InvalidOperationException("Unsaved scene prevents accurate reference checking " + Scene.path);
            }
            foreach (var Asset in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
                if (EditorUtility.IsPersistent(Asset) && EditorUtility.IsDirty(Asset))
                {
                    var AssetPath = AssetDatabase.GetAssetPath(Asset);
                    if (AssetPath.StartsWith("Assets/", StringComparison.Ordinal) || AssetPath.StartsWith("Packages/", StringComparison.Ordinal))
                        throw new InvalidOperationException("Unsaved asset prevents accurate reference checking " + AssetPath);
                }
        }

        /// <summary>
        /// 输入计划与执行状态 生成包含失败影响的结构化响应
        /// </summary>
        private static RoslynExecutionResult Response(JObject Result, bool Success, string State, string Error = null) =>
            new RoslynExecutionResult { Success = Success, State = State, ResultJson = Result.ToString(Newtonsoft.Json.Formatting.None),
                ResultType = typeof(JObject).FullName, ErrorType = Success ? null : "AssetDeleteRejectedOrFailed", ErrorMessage = Error };
    }
}
#endif
