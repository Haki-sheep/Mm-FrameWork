using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.MmAssets
{
    public class MmAssetsTopWindow : OdinMenuEditorWindow
    {
        private int _installedCount;
        private int _totalCount;

        [MenuItem("Tools/MieMieFrameWork/模块中枢")]
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
            _totalCount = modules.Count;
            _installedCount = modules.Count(MmModuleCatalogStore.IsInstalled);

            foreach (IGrouping<string, MmModuleEntry> group in modules.GroupBy(m => m.category).OrderBy(g => g.Key))
            {
                foreach (MmModuleEntry entry in group.OrderBy(m => m.displayName))
                    AddModuleNode(tree, group.Key, entry);
            }

            return tree;
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

        protected override void OnBeginDrawEditors()
        {
            SirenixEditorGUI.BeginHorizontalToolbar(MenuTree.Config.SearchToolbarHeight);
            {
                GUILayout.Label("MieMie 模块中枢", SirenixGUIStyles.BoldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"已安装 {_installedCount} / {_totalCount}", SirenixGUIStyles.Label);
                GUILayout.Space(8);

                if (SirenixEditorGUI.ToolbarButton("刷新"))
                    ForceMenuTreeRebuild();

                if (SirenixEditorGUI.ToolbarButton("打开清单"))
                {
                    Object json = AssetDatabase.LoadAssetAtPath<Object>(
                        "Assets/MieMieFrameTools/Editor/MmAssetsTopWindow/MmModuleCatalog.json");
                    if (json != null)
                        AssetDatabase.OpenAsset(json);
                }
            }
            SirenixEditorGUI.EndHorizontalToolbar();
        }

        protected override void OnImGUI()
        {
            SirenixEditorGUI.Title(
                "外部玩法模块 · 浏览 / 导入 / 移除",
                "左侧选择模块，右侧查看详情。清单编辑：MmModuleCatalog.json",
                TextAlignment.Left,
                true);
            GUILayout.Space(4);
            base.OnImGUI();
        }
    }
}
