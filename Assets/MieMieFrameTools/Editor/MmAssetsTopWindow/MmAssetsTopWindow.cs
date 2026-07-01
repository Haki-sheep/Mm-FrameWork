using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace MieMieFrameWork.Editor.MmAssets
{
    public class MmAssetsTopWindow : OdinMenuEditorWindow
    {
        /// <summary>
        /// 模块中枢左侧分类页签顺序
        /// </summary>
        private static readonly string[] CategoryOrder = { "框架", "工具", "插件", "编辑器拓展", "玩法" };

        [MenuItem("Tools/MieMieFrameWork/模块中枢", priority = -1000)]
        private static void Open()
        {
            GetWindow<MmAssetsTopWindow>("MieMie 模块中枢").Show();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            MmModuleCatalogStore.EnsureLoaded();
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(false)
            {
                DefaultMenuStyle = { IconSize = 20f },
                Config =
                {
                    DrawSearchToolbar = true,
                    SearchToolbarHeight = 28
                }
            };

            MmModuleCatalogStore.Invalidate();
            MmModuleCatalogStore.EnsureLoaded();

            List<MmModuleEntry> modules = MmModuleCatalogStore.Catalog.modules ?? new List<MmModuleEntry>();
            Dictionary<string, List<MmModuleEntry>> grouped = modules
                .GroupBy(m => m.category)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (string category in CategoryOrder)
            {
                if (!grouped.TryGetValue(category, out List<MmModuleEntry> list))
                    continue;

                AddCategoryNodes(tree, category, list);
                grouped.Remove(category);
            }

            foreach (KeyValuePair<string, List<MmModuleEntry>> pair in grouped.OrderBy(p => p.Key))
                AddCategoryNodes(tree, pair.Key, pair.Value);

            return tree;
        }

        private void AddCategoryNodes(OdinMenuTree tree, string category, List<MmModuleEntry> list)
        {
            foreach (MmModuleEntry entry in list
                         .OrderByDescending(m => MmModuleCatalogStore.IsInstalled(m))
                         .ThenBy(m => m.displayName))
                AddModuleNode(tree, category, entry);
        }

        private void AddModuleNode(OdinMenuTree tree, string group, MmModuleEntry entry)
        {
            string status = MmModuleCatalogStore.IsInstalled(entry) ? "●" : "○";
            string path = $"{group}/{status} {entry.displayName}";
            var panel = new MmModuleDetailPanel(entry, OnModulePanelChanged);
            tree.Add(path, panel);
        }

        private void OnModulePanelChanged()
        {
            ForceMenuTreeRebuild();
            Repaint();
        }

        protected override void OnImGUI()
        {
            base.OnImGUI();
        }
    }
}
