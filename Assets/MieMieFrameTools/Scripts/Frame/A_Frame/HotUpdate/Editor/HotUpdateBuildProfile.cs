using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.HotUpdate.Editor
{
    [CreateAssetMenu(menuName = "MieMieFramework/资源/热更新发布配置", fileName = "HotUpdateBuildProfile")]
    public sealed class HotUpdateBuildProfile : ScriptableObject
    {
        /// <summary> 首包身份 同一基线的补丁保持不变 </summary>
        public string playerBuildId = string.Empty;
        /// <summary> 当前代码发布标记 不决定业务代码中的显示文本 </summary>
        [Tooltip("代码发布标记 可以与资源版本同时修改 实际执行内容由本次编译的代码决定")]
        public string codeVersion = "1";
        /// <summary> YooAsset 默认包名称 </summary>
        public string packageName = "DefaultPackage";
        /// <summary> 先依赖后入口的程序集加载顺序 不带扩展名 </summary>
        public string[] hotAssemblyList = Array.Empty<string>();
        /// <summary> 实现 IHotUpdateEntry 的入口程序集 </summary>
        public string entryAssembly = string.Empty;
        /// <summary> 入口完整类型名 </summary>
        public string entryType = string.Empty;
        /// <summary> 热更二进制采集目录 </summary>
        public string assetOutputDirectory = "Assets/MmHotUpdateData";
        /// <summary> 不可覆盖的首包基线归档目录 </summary>
        public string baselineDirectory = "Builds/HotUpdateBaselines";
        /// <summary> YooAsset 构建输出目录 </summary>
        public string bundleOutputDirectory = "Bundles";
        /// <summary> 本机 HTTP 服务的资源发布根目录 </summary>
        public string serverOutputDirectory = "Builds/HotUpdateServer";

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(playerBuildId) || playerBuildId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                playerBuildId == "." || playerBuildId == "..")
                throw new ArgumentException("首包身份不能为空或包含路径字符");
            if (string.IsNullOrWhiteSpace(codeVersion) || string.IsNullOrWhiteSpace(packageName) ||
                string.IsNullOrWhiteSpace(entryType) || hotAssemblyList == null || hotAssemblyList.Length == 0 ||
                !Array.Exists(hotAssemblyList, Name => Name == entryAssembly))
                throw new ArgumentException("发布配置缺少版本 包名 程序集或入口");
            if (!assetOutputDirectory.StartsWith("Assets/", StringComparison.Ordinal) || assetOutputDirectory.Contains(".."))
                throw new ArgumentException("热更采集目录必须位于 Assets 下");
            if (packageName != Path.GetFileName(packageName) || packageName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                packageName == "." || packageName == "..")
                throw new ArgumentException("资源包名称不能包含路径字符");
            var NameHashList = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (string Name in hotAssemblyList)
                if (string.IsNullOrWhiteSpace(Name) || Path.GetFileName(Name) != Name ||
                    Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || !NameHashList.Add(Name))
                    throw new ArgumentException($"热更程序集名称不合法或重复 {Name}");
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.NoTarget)
                throw new InvalidOperationException("未选择构建平台");
        }

        public string GetBaselinePath()
        {
            return Path.GetFullPath(Path.Combine(baselineDirectory, EditorUserBuildSettings.activeBuildTarget.ToString(), playerBuildId));
        }

        public string GetServerPath()
        {
            return Path.GetFullPath(Path.Combine(serverOutputDirectory, EditorUserBuildSettings.activeBuildTarget.ToString(), packageName));
        }
    }
}
