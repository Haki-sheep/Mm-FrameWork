using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MieMieFrameWork.Editor.MmAssets
{
    public class MmAssetsTopWindow : EditorWindow
    {
        /// <summary>
        /// 模块中心左侧分类页签顺序
        /// </summary>
        private static readonly string[] CategoryOrder = { "框架", "工具", "插件", "编辑器拓展" };

        [SerializeField]
        private string selectedModuleId;
        private readonly Dictionary<string, bool> ExpandedCategoryDict = new Dictionary<string, bool>();
        private ToolbarSearchField SearchField;
        private ScrollView Navigation;
        private ScrollView Details;

        [MenuItem("Tools/MieMieFrameWork/模块中心", priority = -1000)]
        private static void Open()
        {
            GetWindow<MmAssetsTopWindow>("MieMie 模块中心").Show();
        }

        private void CreateGUI()
        {
            var Split = new TwoPaneSplitView(0, 240, TwoPaneSplitViewOrientation.Horizontal);
            var Sidebar = new VisualElement();
            Sidebar.style.minWidth = 130f;
            SearchField = new ToolbarSearchField { name = "module-search" };
            SearchField.style.width = Length.Percent(100);
            SearchField.RegisterValueChangedCallback(_ => DrawNavigation());
            Sidebar.Add(SearchField);
            Navigation = new ScrollView { name = "module-navigation" };
            Navigation.style.flexGrow = 1f;
            Sidebar.Add(Navigation);
            Details = new ScrollView { name = "module-details" };
            Details.style.minWidth = 170f;
            Details.style.paddingLeft = 12f;
            Details.style.paddingRight = 12f;
            Split.Add(Sidebar);
            Split.Add(Details);
            Split.style.flexGrow = 1f;
            rootVisualElement.Add(Split);
            OnProjectChange();
        }

        private void OnProjectChange()
        {
            MmModuleCatalogStore.Invalidate();
            if (Navigation != null)
                DrawNavigation();
        }

        private void DrawNavigation()
        {
            Navigation.Clear();
            var Modules = MmModuleCatalogStore.Catalog.modules ?? new List<MmModuleEntry>();
            var Groups = Modules.GroupBy(Module => Module.category).ToDictionary(Group => Group.Key, Group => Group.ToList());
            bool Searching = !string.IsNullOrWhiteSpace(SearchField.value);
            string Search = SearchField.value.Trim();
            var Selected = Modules.FirstOrDefault(Module => Module.id == selectedModuleId);

            foreach (string Category in CategoryOrder.Where(Groups.ContainsKey).Concat(Groups.Keys.Except(CategoryOrder).OrderBy(Name => Name)))
            {
                var Visible = Groups[Category]
                    .Where(Module => !Searching || Matches(Module.displayName, Search) || Matches(Category, Search))
                    .Select(Module => (Entry: Module, Installed: MmModuleCatalogStore.IsInstalled(Module)))
                    .OrderByDescending(Module => Module.Installed)
                    .ThenBy(Module => Module.Entry.displayName)
                    .ToArray();
                if (Visible.Length == 0)
                    continue;

                Selected ??= Visible[0].Entry;
                bool ContainsSelection = Visible.Any(Module => Module.Entry.id == Selected.id);
                var Group = new Foldout
                {
                    text = Category,
                    value = Searching || (ExpandedCategoryDict.TryGetValue(Category, out bool Expanded) ? Expanded : ContainsSelection)
                };
                Group.RegisterValueChangedCallback(Event =>
                {
                    if (!Searching && Event.target == Group)
                        ExpandedCategoryDict[Category] = Event.newValue;
                });
                Navigation.Add(Group);
                foreach (var Module in Visible)
                {
                    var Row = new Button(() =>
                    {
                        selectedModuleId = Module.Entry.id;
                        DrawNavigation();
                    })
                    {
                        text = (Module.Installed ? "● " : "○ ") + Module.Entry.displayName,
                        tooltip = Module.Entry.displayName,
                        name = Module.Entry.id
                    };
                    Row.style.unityTextAlign = TextAnchor.MiddleLeft;
                    if (Module.Entry.id == Selected.id)
                        Row.style.unityFontStyleAndWeight = FontStyle.Bold;
                    Group.Add(Row);
                }
            }

            selectedModuleId = Selected?.id;
            Details.Clear();
            if (Selected != null)
                new MmModuleDetailPanel(Selected).Draw(Details);
        }

        private static bool Matches(string value, string search) => value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
