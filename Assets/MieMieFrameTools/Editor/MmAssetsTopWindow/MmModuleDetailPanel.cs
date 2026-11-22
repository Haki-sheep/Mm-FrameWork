using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MieMieFrameWork.Editor.MmAssets
{
    public class MmModuleDetailPanel
    {
        /// <summary>
        /// 当前模块清单数据
        /// </summary>
        private readonly MmModuleEntry entry;

        public MmModuleDetailPanel(MmModuleEntry entry)
        {
            this.entry = entry;
            Refresh();
        }

        public string StatusLine;

        public string Category;

        public string Version;

        public string Tags;

        public string Description;

        public string TitleText => entry?.displayName ?? "未知模块";

        /// <summary>
        /// 从清单刷新当前模块详情
        /// </summary>
        public void Refresh()
        {
            if (entry == null)
                return;

            bool isInstalled = MmModuleCatalogStore.IsInstalled(entry);
            Category = entry.category;
            Version = entry.version;
            Tags = entry.tags != null && entry.tags.Count > 0 ? string.Join(" · ", entry.tags) : "无";
            Description = entry.description;
            StatusLine = BuildStatusLine(isInstalled);
        }

        private string BuildStatusLine(bool isInstalled)
        {
            string install = isInstalled ? "● 已安装" : "○ 未安装";
            string builtIn = entry.isBuiltIn ? "内置" : "外部";
            return $"{install}   |   {builtIn}";
        }

        internal void Draw(VisualElement parent)
        {
            var Title = new Label(TitleText) { name = "module-title" };
            Title.style.unityFontStyleAndWeight = FontStyle.Bold;
            Title.style.fontSize = 16f;
            Title.style.marginTop = 10f;
            parent.Add(Title);
            parent.Add(new Label(StatusLine));
            AddDetail(parent, "分类", Category);
            AddDetail(parent, "版本", Version);
            AddDetail(parent, "标签", Tags);
            AddDetail(parent, "描述", Description);
            var EditButton = new Button(OpenCatalogJson) { text = "编辑清单 JSON" };
            EditButton.style.alignSelf = Align.FlexStart;
            parent.Add(EditButton);
        }

        private static void AddDetail(VisualElement parent, string label, string value)
        {
            var Detail = new Label(label + "　" + value);
            Detail.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(Detail);
        }

        private void OpenCatalogJson()
        {
            string assetPath = MmEditorPaths.EditorRoot + "/MmAssetsTopWindow/MmModuleCatalog.json";
            UnityEngine.Object json = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (json != null)
            {
                AssetDatabase.OpenAsset(json);
                EditorGUIUtility.PingObject(json);
            }
        }
    }
}
