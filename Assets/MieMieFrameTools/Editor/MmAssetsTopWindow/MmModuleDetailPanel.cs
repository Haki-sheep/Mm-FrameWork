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

        [HideInInspector] public bool IsInstalled;
        [HideInInspector] public bool IsFavorite;

        public string TitleText => entry?.displayName ?? "未知模块";

        private bool HasGitUrl => !string.IsNullOrWhiteSpace(GitUrl);
        private bool ShowPackageName => !string.IsNullOrWhiteSpace(PackageName);
        private bool HasInstallCheckPath => !string.IsNullOrWhiteSpace(InstallCheckPath);
        private bool SupportsUpmInstall =>
            !string.IsNullOrWhiteSpace(entry?.packageName) && !string.IsNullOrWhiteSpace(entry?.gitUrl);

        public void Refresh()
        {
            if (entry == null)
                return;

            IsInstalled = MmModuleCatalogStore.IsInstalled(entry);
            IsFavorite = MmModuleCatalogStore.IsFavorite(entry.id);
            Category = entry.category;
            Version = entry.version;
            Tags = entry.tags != null && entry.tags.Count > 0 ? string.Join(" · ", entry.tags) : "无";
            Description = entry.description;
            GitUrl = entry.gitUrl;
            PackageName = entry.packageName;
            InstallCheckPath = entry.installCheckPath;
            StatusLine = BuildStatusLine();
        }

        private string BuildStatusLine()
        {
            string install = IsInstalled ? "● 已安装" : "○ 未安装";
            string builtIn = entry.isBuiltIn ? "内置" : "外部";
            string favorite = IsFavorite ? " ★ 已收藏" : string.Empty;
            return $"{install}   |   {builtIn}{favorite}";
        }

        [PropertySpace(12)]
        [Button("安装到项目 (UPM)", ButtonSizes.Large)]
        [GUIColor(0.45f, 0.85f, 0.55f)]
        [ShowIf(nameof(SupportsUpmInstall))]
        [HideIf(nameof(IsInstalled))]
        [PropertyOrder(100)]
        private void InstallModule()
        {
            if (!MmModuleCatalogStore.TryInstallPackage(entry, out string message))
            {
                EditorUtility.DisplayDialog("安装模块", message, "确定");
                return;
            }

            EditorUtility.DisplayDialog("安装模块", message, "确定");
            Refresh();
            onChanged?.Invoke();
        }

        [PropertySpace(12)]
        [Button("定位模块路径", ButtonSizes.Large)]
        [ShowIf(nameof(IsInstalled))]
        [PropertyOrder(101)]
        private void PingModule()
        {
            MmModuleCatalogStore.PingInstallPath(entry);
        }

        [Button("打开 Git 仓库", ButtonSizes.Medium)]
        [ShowIf(nameof(HasGitUrl))]
        [PropertyOrder(102)]
        private void OpenGitUrl()
        {
            Application.OpenURL(entry.gitUrl);
        }

        [Button("@FavoriteButtonText", ButtonSizes.Medium)]
        [PropertyOrder(103)]
        private void ToggleFavorite()
        {
            MmModuleCatalogStore.ToggleFavorite(entry.id);
            Refresh();
            onChanged?.Invoke();
        }

        private string FavoriteButtonText => IsFavorite ? "取消收藏" : "加入收藏";

        [Button("编辑清单 JSON", ButtonSizes.Medium)]
        [PropertyOrder(104)]
        private void OpenCatalogJson()
        {
            string assetPath = "Assets/MieMieFrameTools/Editor/MmAssetsTopWindow/MmModuleCatalog.json";
            Object json = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (json != null)
            {
                AssetDatabase.OpenAsset(json);
                EditorGUIUtility.PingObject(json);
            }
        }
    }
}
