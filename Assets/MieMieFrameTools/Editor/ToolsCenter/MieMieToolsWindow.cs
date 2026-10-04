using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.ToolsCenter
{
    /// <summary>
    /// MieMie 编辑器工具中枢
    /// </summary>
    public sealed class MieMieToolsWindow : OdinMenuEditorWindow
    {
        #region 数据

        /// <summary>
        /// 左侧导航宽度
        /// </summary>
        private const float NavigationWidth = 248f;

        /// <summary>
        /// 当前工具页面列表
        /// </summary>
        private List<IMieMieToolsPage> PageList = new List<IMieMieToolsPage>();

        /// <summary>
        /// 当前导航分组列表
        /// </summary>
        private List<NavigationGroup> NavigationGroupList = new List<NavigationGroup>();

        /// <summary>
        /// 当前首页页面
        /// </summary>
        private IMieMieToolsPage HomePage;

        /// <summary>
        /// 当前选中页面
        /// </summary>
        private IMieMieToolsPage SelectedPage;

        /// <summary>
        /// 当前页面滚动位置
        /// </summary>
        private Vector2 PageScroll;

        /// <summary>
        /// 左侧导航滚动位置
        /// </summary>
        private Vector2 NavigationScroll;

        /// <summary>
        /// 左侧导航搜索文本
        /// </summary>
        private string NavigationSearchText = string.Empty;

        /// <summary>
        /// 当前样式纹理列表
        /// </summary>
        private List<Texture2D> TextureList = new List<Texture2D>();

        /// <summary>
        /// 顶部面板样式
        /// </summary>
        private GUIStyle HeaderPanelStyle;

        /// <summary>
        /// 顶部标题样式
        /// </summary>
        private GUIStyle HeaderTitleStyle;

        /// <summary>
        /// 顶部按钮样式
        /// </summary>
        private GUIStyle HeaderButtonStyle;

        /// <summary>
        /// 左侧面板样式
        /// </summary>
        private GUIStyle NavigationPanelStyle;

        /// <summary>
        /// 右侧面板样式
        /// </summary>
        private GUIStyle PagePanelStyle;

        /// <summary>
        /// 左侧搜索框样式
        /// </summary>
        private GUIStyle SearchFieldStyle;

        /// <summary>
        /// 左侧分组按钮样式
        /// </summary>
        private GUIStyle CategoryButtonStyle;

        /// <summary>
        /// 普通页面按钮样式
        /// </summary>
        private GUIStyle PageButtonStyle;

        /// <summary>
        /// 选中页面按钮样式
        /// </summary>
        private GUIStyle SelectedPageButtonStyle;

        /// <summary>
        /// 首页按钮样式
        /// </summary>
        private GUIStyle HomeButtonStyle;

        /// <summary>
        /// 导航分组数据
        /// </summary>
        private sealed class NavigationGroup
        {
            /// <summary>
            /// 分组名称
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 分组页面列表
            /// </summary>
            public List<IMieMieToolsPage> PageList { get; }

            /// <summary>
            /// 分组展开状态
            /// </summary>
            public bool IsExpanded { get; set; }

            public NavigationGroup(string name)
            {
                Name = name;
                PageList = new List<IMieMieToolsPage>();
            }
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 打开工具中枢
        /// </summary>
        [MenuItem("Tools/MieMieFrameWork/工具中枢", priority = -900)]
        private static void Open()
        {
            MieMieToolsWindow Window = GetWindow<MieMieToolsWindow>();
            Window.titleContent = new GUIContent("MieMie 工具中枢");
            Window.minSize = new Vector2(900f, 560f);
            Window.Show();
        }

        /// <summary>
        /// 窗口启用时初始化窗口
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            BuildMenuTree();
            titleContent = new GUIContent("MieMie 工具中枢");
            minSize = new Vector2(900f, 560f);
        }

        /// <summary>
        /// 使用 Editor 定期回调刷新宿主 保持嵌入监控与会话按钮状态更新
        /// </summary>
        private void OnInspectorUpdate()
        {
            Repaint();
        }

        /// <summary>
        /// 窗口销毁时释放当前页面
        /// </summary>
        protected override void OnDestroy()
        {
            SelectedPage?.OnClose();
            for (int i = 0; i < TextureList.Count; i++)
            {
                if (TextureList[i] != null)
                    DestroyImmediate(TextureList[i]);
            }

            TextureList.Clear();
            base.OnDestroy();
        }

        /// <summary>
        /// 绘制工具中枢界面
        /// </summary>
        protected override void OnImGUI()
        {
            EnsureStyles();
            DrawHeader();

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawNavigation();
                DrawPageContent();
            }
        }

        #endregion

        #region Odin 菜单树

        /// <summary>
        /// 构建工具中枢菜单树
        /// </summary>
        protected override OdinMenuTree BuildMenuTree()
        {
            PageList = MieMieToolsPageCatalog.CreatePageList();
            HomePage = PageList.FirstOrDefault();
            SelectedPage = HomePage;
            BuildNavigationGroups();

            return new OdinMenuTree(false);
        }

        /// <summary>
        /// 根据页面路径构建缓存导航分组
        /// </summary>
        private void BuildNavigationGroups()
        {
            NavigationGroupList.Clear();
            for (int i = 0; i < PageList.Count; i++)
            {
                IMieMieToolsPage Page = PageList[i];
                int SeparatorIndex = Page.MenuPath.IndexOf('/');
                if (SeparatorIndex <= 0)
                    continue;

                string GroupName = Page.MenuPath.Substring(0, SeparatorIndex);
                NavigationGroup Group = NavigationGroupList.FirstOrDefault(Item => Item.Name == GroupName);
                if (Group == null)
                {
                    Group = new NavigationGroup(GroupName)
                    {
                        IsExpanded = NavigationGroupList.Count == 0
                    };
                    NavigationGroupList.Add(Group);
                }

                Group.PageList.Add(Page);
            }
        }

        #endregion

        #region 界面绘制

        /// <summary>
        /// 绘制窗口顶部工具栏
        /// </summary>
        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(HeaderPanelStyle))
            {
                GUILayout.Label("MieMie 工具中枢", HeaderTitleStyle);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("模块中心", HeaderButtonStyle, GUILayout.Width(82f), GUILayout.Height(28f)))
                    EditorApplication.ExecuteMenuItem("Tools/MieMieFrameWork/模块中心");
            }
        }

        /// <summary>
        /// 绘制左侧导航区域
        /// </summary>
        private void DrawNavigation()
        {
            using (new EditorGUILayout.VerticalScope(
                       NavigationPanelStyle,
                       GUILayout.Width(NavigationWidth),
                       GUILayout.ExpandHeight(true)))
            {
                EditorGUILayout.LabelField("功能导航", HeaderTitleStyle);
                EditorGUILayout.Space(6f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    NavigationSearchText = EditorGUILayout.TextField(
                        NavigationSearchText,
                        SearchFieldStyle,
                        GUILayout.Height(28f));
                    if (!string.IsNullOrEmpty(NavigationSearchText) &&
                        GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(24f)))
                    {
                        NavigationSearchText = string.Empty;
                        GUI.FocusControl(null);
                    }
                }

                EditorGUILayout.Space(6f);
                NavigationScroll = EditorGUILayout.BeginScrollView(NavigationScroll);
                if (HomePage != null && IsPageVisible(HomePage))
                    DrawNavigationPage(HomePage, true);

                for (int i = 0; i < NavigationGroupList.Count; i++)
                    DrawNavigationGroup(NavigationGroupList[i]);

                EditorGUILayout.EndScrollView();
            }
        }

        /// <summary>
        /// 绘制导航分组
        /// </summary>
        private void DrawNavigationGroup(NavigationGroup group)
        {
            bool HasVisiblePage = false;
            for (int i = 0; i < group.PageList.Count; i++)
            {
                if (!IsPageVisible(group.PageList[i]))
                    continue;

                HasVisiblePage = true;
                break;
            }

            if (!HasVisiblePage)
                return;

            string Arrow = group.IsExpanded ? "▾" : "▸";
            if (GUILayout.Button($"{Arrow}  {group.Name}", CategoryButtonStyle, GUILayout.Height(30f)))
            {
                group.IsExpanded = !group.IsExpanded;
                Repaint();
            }

            if (!group.IsExpanded && string.IsNullOrWhiteSpace(NavigationSearchText))
                return;

            for (int i = 0; i < group.PageList.Count; i++)
            {
                IMieMieToolsPage Page = group.PageList[i];
                if (IsPageVisible(Page))
                    DrawNavigationPage(Page, false);
            }
        }

        /// <summary>
        /// 绘制导航页面
        /// </summary>
        private void DrawNavigationPage(IMieMieToolsPage page, bool isHome)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(isHome ? 0f : 12f);
                GUIStyle Style = isHome
                    ? HomeButtonStyle
                    : ReferenceEquals(page, SelectedPage)
                        ? SelectedPageButtonStyle
                        : PageButtonStyle;

                if (GUILayout.Button(page.Title, Style, GUILayout.Height(isHome ? 32f : 30f)))
                    SelectPage(page);
            }
        }

        /// <summary>
        /// 绘制右侧当前页面
        /// </summary>
        private void DrawPageContent()
        {
            using (new EditorGUILayout.VerticalScope(PagePanelStyle, GUILayout.ExpandHeight(true)))
            {
                if (SelectedPage == null)
                {
                    EditorGUILayout.HelpBox("请从左侧选择一个工具页面", MessageType.Info);
                    return;
                }

                PageScroll = EditorGUILayout.BeginScrollView(PageScroll);
                SelectedPage.DrawGUI();
                EditorGUILayout.EndScrollView();
            }
        }

        #endregion

        #region 导航逻辑

        /// <summary>
        /// 判断页面是否符合搜索条件
        /// </summary>
        private bool IsPageVisible(IMieMieToolsPage page)
        {
            if (string.IsNullOrWhiteSpace(NavigationSearchText))
                return true;

            return page.Title.IndexOf(NavigationSearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   page.MenuPath.IndexOf(NavigationSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// 选择工具页面
        /// </summary>
        private void SelectPage(IMieMieToolsPage page)
        {
            if (page == null || ReferenceEquals(page, SelectedPage))
                return;

            SelectedPage?.OnClose();
            SelectedPage = page;
            SelectedPage.OnOpen();
            PageScroll = Vector2.zero;

            for (int i = 0; i < NavigationGroupList.Count; i++)
            {
                if (NavigationGroupList[i].PageList.Contains(page))
                    NavigationGroupList[i].IsExpanded = true;
            }

            Repaint();
        }

        #endregion

        #region 样式

        /// <summary>
        /// 确保界面样式已经创建
        /// </summary>
        private void EnsureStyles()
        {
            if (HeaderPanelStyle != null)
                return;

            bool IsProSkin = EditorGUIUtility.isProSkin;
            Color PanelColor = IsProSkin ? new Color(0.19f, 0.2f, 0.22f, 1f) : new Color(0.92f, 0.92f, 0.92f, 1f);
            Color NavigationColor = IsProSkin ? new Color(0.15f, 0.16f, 0.18f, 1f) : new Color(0.86f, 0.86f, 0.86f, 1f);
            Color HeaderColor = IsProSkin ? new Color(0.24f, 0.25f, 0.27f, 1f) : new Color(0.72f, 0.72f, 0.72f, 1f);
            Color ButtonColor = IsProSkin ? new Color(0.22f, 0.23f, 0.25f, 1f) : new Color(0.82f, 0.82f, 0.82f, 1f);
            Color HoverColor = IsProSkin ? new Color(0.29f, 0.3f, 0.32f, 1f) : new Color(0.74f, 0.74f, 0.74f, 1f);
            Color SelectedColor = IsProSkin ? new Color(0.36f, 0.37f, 0.39f, 1f) : new Color(0.62f, 0.62f, 0.62f, 1f);
            Color TextColor = IsProSkin ? new Color(0.88f, 0.88f, 0.88f, 1f) : new Color(0.15f, 0.15f, 0.15f, 1f);
            Color MutedTextColor = IsProSkin ? new Color(0.68f, 0.68f, 0.68f, 1f) : new Color(0.36f, 0.36f, 0.36f, 1f);
            Color SearchColor = IsProSkin ? new Color(0.09f, 0.09f, 0.1f, 1f) : Color.white;

            Texture2D PanelTexture = CreateRoundedTexture(PanelColor);
            Texture2D NavigationTexture = CreateRoundedTexture(NavigationColor);
            Texture2D HeaderTexture = CreateRoundedTexture(HeaderColor);
            Texture2D ButtonTexture = CreateRoundedTexture(ButtonColor);
            Texture2D HoverTexture = CreateRoundedTexture(HoverColor);
            Texture2D SelectedTexture = CreateRoundedTexture(SelectedColor);
            Texture2D SearchTexture = CreateRoundedTexture(SearchColor);

            HeaderPanelStyle = CreatePanelStyle(HeaderTexture, new RectOffset(14, 10, 6, 6));
            NavigationPanelStyle = CreatePanelStyle(NavigationTexture, new RectOffset(10, 10, 10, 10));
            PagePanelStyle = CreatePanelStyle(PanelTexture, new RectOffset(14, 14, 12, 12));
            SearchFieldStyle = CreateButtonStyle(
                SearchTexture,
                SearchTexture,
                SearchTexture,
                TextColor,
                12,
                FontStyle.Normal,
                new RectOffset(10, 10, 3, 3));
            CategoryButtonStyle = CreateButtonStyle(
                ButtonTexture,
                HoverTexture,
                HoverTexture,
                TextColor,
                12,
                FontStyle.Bold,
                new RectOffset(10, 10, 3, 3));
            PageButtonStyle = CreateButtonStyle(
                ButtonTexture,
                HoverTexture,
                HoverTexture,
                MutedTextColor,
                11,
                FontStyle.Normal,
                new RectOffset(12, 10, 3, 3));
            SelectedPageButtonStyle = CreateButtonStyle(
                SelectedTexture,
                SelectedTexture,
                SelectedTexture,
                Color.white,
                11,
                FontStyle.Bold,
                new RectOffset(12, 10, 3, 3));
            HomeButtonStyle = CreateButtonStyle(
                HeaderTexture,
                SelectedTexture,
                SelectedTexture,
                Color.white,
                12,
                FontStyle.Bold,
                new RectOffset(12, 10, 4, 4));

            HeaderTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = Color.white }
            };
            HeaderButtonStyle = CreateButtonStyle(
                HoverTexture,
                SelectedTexture,
                SelectedTexture,
                Color.white,
                11,
                FontStyle.Bold,
                new RectOffset(10, 10, 3, 3));
        }

        /// <summary>
        /// 创建圆角面板样式
        /// </summary>
        private static GUIStyle CreatePanelStyle(Texture2D background, RectOffset padding)
        {
            return new GUIStyle(EditorStyles.helpBox)
            {
                normal = { background = background },
                padding = padding,
                border = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(0, 0, 0, 0)
            };
        }

        /// <summary>
        /// 创建圆角按钮样式
        /// </summary>
        private static GUIStyle CreateButtonStyle(
            Texture2D normalTexture,
            Texture2D hoverTexture,
            Texture2D activeTexture,
            Color textColor,
            int fontSize,
            FontStyle fontStyle,
            RectOffset padding)
        {
            GUIStyle Style = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = fontSize,
                fontStyle = fontStyle,
                padding = padding,
                margin = new RectOffset(0, 0, 2, 2),
                border = new RectOffset(8, 8, 8, 8)
            };
            Style.normal.background = normalTexture;
            Style.normal.textColor = textColor;
            Style.hover.background = hoverTexture;
            Style.hover.textColor = textColor;
            Style.active.background = activeTexture;
            Style.active.textColor = Color.white;
            Style.focused.background = activeTexture;
            Style.focused.textColor = Color.white;
            return Style;
        }

        /// <summary>
        /// 创建圆角纹理
        /// </summary>
        private Texture2D CreateRoundedTexture(Color backgroundColor)
        {
            const int TextureSize = 32;
            const int Radius = 8;
            Texture2D Texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                name = "MieMieToolsRoundedTexture",
                hideFlags = HideFlags.HideAndDontSave
            };
            Color[] PixelList = new Color[TextureSize * TextureSize];

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    int CornerX = Mathf.Clamp(x, Radius, TextureSize - Radius - 1);
                    int CornerY = Mathf.Clamp(y, Radius, TextureSize - Radius - 1);
                    float DistanceX = x - CornerX;
                    float DistanceY = y - CornerY;
                    float Distance = Mathf.Sqrt(DistanceX * DistanceX + DistanceY * DistanceY);
                    float Alpha = Mathf.Clamp01(Radius + 0.5f - Distance);
                    PixelList[y * TextureSize + x] = new Color(
                        backgroundColor.r,
                        backgroundColor.g,
                        backgroundColor.b,
                        backgroundColor.a * Alpha);
                }
            }

            Texture.SetPixels(PixelList);
            Texture.Apply();
            TextureList.Add(Texture);
            return Texture;
        }

        #endregion
    }
}
