using System.Text.RegularExpressions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.MmAssets
{
    public class MmModuleDetailPanel
    {
        private readonly MmModuleEntry entry;
        private readonly System.Action onChanged;

        public MmModuleDetailPanel(MmModuleEntry entry, System.Action onChanged)
        {
            this.entry = entry;
            this.onChanged = onChanged;
            Refresh();
        }

        [Title("@TitleText", bold: true)]
        [HideLabel, DisplayAsString]
        [PropertyOrder(-10)]
        public string StatusLine;

        [LabelText("分类")]
        [ReadOnly, PropertyOrder(0)]
        public string Category;

        [LabelText("版本")]
        [ReadOnly, PropertyOrder(1)]
        public string Version;

        [LabelText("标签")]
        [ReadOnly, PropertyOrder(2)]
        public string Tags;

        [LabelText("描述"), MultiLineProperty(4)]
        [ReadOnly, PropertyOrder(3)]
        public string Description;

        [LabelText("Git 仓库")]
        [ReadOnly, PropertyOrder(4)]
        [ShowIf(nameof(HasGitUrl))]
        public string GitUrl;

        [LabelText("UPM 包名")]
        [ReadOnly, PropertyOrder(5)]
        [ShowIf(nameof(ShowPackageName))]
        public string PackageName;

        [LabelText("安装检测路径")]
        [ReadOnly, PropertyOrder(6)]
        [ShowIf(nameof(HasInstallCheckPath))]
        public string InstallCheckPath;

        [LabelText("菜单路径")]
        [ReadOnly, PropertyOrder(7)]
        [ShowIf(nameof(HasMenuPath))]
        public string MenuPath;

        [HideInInspector] public bool IsInstalled;

        public string TitleText => entry?.displayName ?? "未知模块";

        private bool HasGitUrl => !string.IsNullOrWhiteSpace(GitUrl);
        private bool ShowPackageName => !string.IsNullOrWhiteSpace(PackageName);
        private bool HasInstallCheckPath => !string.IsNullOrWhiteSpace(InstallCheckPath);
        private bool HasMenuPath => !string.IsNullOrWhiteSpace(MenuPath);
        private bool SupportsUpmInstall =>
            !string.IsNullOrWhiteSpace(entry?.packageName) && !string.IsNullOrWhiteSpace(entry?.gitUrl);

        public void Refresh()
        {
            if (entry == null)
                return;

            IsInstalled = MmModuleCatalogStore.IsInstalled(entry);
            Category = entry.category;
            Version = entry.version;
            Tags = entry.tags != null && entry.tags.Count > 0 ? string.Join(" · ", entry.tags) : "无";
            Description = entry.description;
            GitUrl = entry.gitUrl;
            PackageName = entry.packageName;
            InstallCheckPath = entry.installCheckPath;
            MenuPath = entry.menuPath;
            StatusLine = BuildStatusLine();
        }

        private string BuildStatusLine()
        {
            string install = IsInstalled ? "● 已安装" : "○ 未安装";
            string builtIn = entry.isBuiltIn ? "内置" : "外部";
            return $"{install}   |   {builtIn}";
        }

        [Button("打开编辑器菜单", ButtonSizes.Large)]
        [ShowIf(nameof(HasMenuPath))]
        [PropertyOrder(99)]
        private void OpenEditorMenu()
        {
            if (!EditorApplication.ExecuteMenuItem(entry.menuPath))
                EditorUtility.DisplayDialog("打开失败", $"未找到菜单\n{entry.menuPath}", "确定");
        }

        [PropertySpace(12)]
        [Button("导入", ButtonSizes.Large)]
        [GUIColor(0.45f, 0.85f, 0.55f)]
        [ShowIf(nameof(SupportsUpmInstall))]
        [HideIf(nameof(IsInstalled))]
        [PropertyOrder(100)]
        private void ImportModule()
        {
            if (!MmModuleCatalogStore.TryInstallPackage(entry, out string message))
            {
                EditorUtility.DisplayDialog("导入模块", message, "确定");
                return;
            }

            EditorUtility.DisplayDialog("导入模块", message, "确定");
            Refresh();
            onChanged?.Invoke();
        }

        [PropertySpace(12)]
        [Button("移除", ButtonSizes.Large)]
        [GUIColor(0.95f, 0.55f, 0.45f)]
        [ShowIf("@SupportsUpmInstall && IsInstalled")]
        [PropertyOrder(101)]
        private void RemoveModule()
        {
            if (!EditorUtility.DisplayDialog(
                    "移除模块",
                    $"将从 Packages/manifest.json 移除 {entry.packageName}\n确认继续",
                    "移除",
                    "取消"))
                return;

            if (!MmModuleCatalogStore.TryRemovePackage(entry, out string message))
            {
                EditorUtility.DisplayDialog("移除模块", message, "确定");
                return;
            }

            EditorUtility.DisplayDialog("移除模块", message, "确定");
            Refresh();
            onChanged?.Invoke();
        }

        [Button("定位模块路径", ButtonSizes.Large)]
        [ShowIf(nameof(IsInstalled))]
        [PropertyOrder(102)]
        private void PingModule()
        {
            MmModuleCatalogStore.PingInstallPath(entry);
        }

        [Button("打开 Git 仓库", ButtonSizes.Medium)]
        [ShowIf(nameof(HasGitUrl))]
        [PropertyOrder(103)]
        private void OpenGitUrl()
        {
            Application.OpenURL(ConvertGitUrlToHttps(entry.gitUrl));
        }

        [Button("编辑清单 JSON", ButtonSizes.Medium)]
        [PropertyOrder(104)]
        private void OpenCatalogJson()
        {
            string assetPath = MmEditorPaths.EditorRoot + "/MmAssetsTopWindow/MmModuleCatalog.json";
            Object json = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (json != null)
            {
                AssetDatabase.OpenAsset(json);
                EditorGUIUtility.PingObject(json);
            }
        }

        private static string ConvertGitUrlToHttps(string gitUrl)
        {
            if (string.IsNullOrWhiteSpace(gitUrl))
                return gitUrl;

            int pathIdx = gitUrl.IndexOf('?');
            string baseUrl = pathIdx >= 0 ? gitUrl.Substring(0, pathIdx) : gitUrl;

            var match = Regex.Match(baseUrl, @"^git@([^:]+):(.+?)(\.git)?$");
            if (match.Success)
                return $"https://{match.Groups[1].Value}/{match.Groups[2].Value}";

            return baseUrl;
        }
    }
}
