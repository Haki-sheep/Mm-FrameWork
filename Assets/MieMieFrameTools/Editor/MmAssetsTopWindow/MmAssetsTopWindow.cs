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

        /// <summary>
        /// 玩法下子页签顺序
        /// </summary>
        private static readonly string[] GameplaySubCategoryOrder =
        {
            "库存", "3C", "叙事", "经济", "世界", "交互"
        };

        /// <summary>
        /// 框架下子页签顺序
        /// </summary>
        private static readonly string[] FrameworkSubCategoryOrder =
        {
            "可选模块"
        };

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
            List<MmModuleEntry> flatList = list
                .Where(m => string.IsNullOrWhiteSpace(m.subCategory))
                .OrderByDescending(m => MmModuleCatalogStore.IsInstalled(m))
                .ThenBy(m => m.displayName)
                .ToList();

            foreach (MmModuleEntry entry in flatList)
                AddModuleNode(tree, category, entry);

            Dictionary<string, List<MmModuleEntry>> subGrouped = list
                .Where(m => !string.IsNullOrWhiteSpace(m.subCategory))
                .GroupBy(m => m.subCategory)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (string subCategory in GetSubCategoryOrder(category, subGrouped.Keys))
            {
                if (!subGrouped.TryGetValue(subCategory, out List<MmModuleEntry> subList))
                    continue;

                string groupPath = $"{category}/{subCategory}";
                foreach (MmModuleEntry entry in subList
                             .OrderByDescending(m => MmModuleCatalogStore.IsInstalled(m))
                             .ThenBy(m => m.displayName))
                    AddModuleNode(tree, groupPath, entry);

                subGrouped.Remove(subCategory);
            }

            foreach (KeyValuePair<string, List<MmModuleEntry>> pair in subGrouped.OrderBy(p => p.Key))
            {
                string groupPath = $"{category}/{pair.Key}";
                foreach (MmModuleEntry entry in pair.Value
                             .OrderByDescending(m => MmModuleCatalogStore.IsInstalled(m))
                             .ThenBy(m => m.displayName))
                    AddModuleNode(tree, groupPath, entry);
            }
        }

        private static IEnumerable<string> GetSubCategoryOrder(string category, IEnumerable<string> keys)
        {
            string[] order = null;
            if (category == "玩法")
                order = GameplaySubCategoryOrder;
            else if (category == "框架")
                order = FrameworkSubCategoryOrder;

            if (order == null)
            {
                foreach (string sub in keys.OrderBy(k => k))
                    yield return sub;
                yield break;
            }

            HashSet<string> keySet = new HashSet<string>(keys);
            for (int i = 0; i < order.Length; i++)
            {
                string sub = order[i];
                if (keySet.Contains(sub))
                    yield return sub;
            }

            foreach (string sub in keys.OrderBy(k => k))
            {
                if (!order.Contains(sub))
                    yield return sub;
            }
        }

        private void AddModuleNode(OdinMenuTree tree, string groupPath, MmModuleEntry entry)
        {
            string status = MmModuleCatalogStore.IsInstalled(entry) ? "●" : "○";
            string path = $"{groupPath}/{status} {entry.displayName}";
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
