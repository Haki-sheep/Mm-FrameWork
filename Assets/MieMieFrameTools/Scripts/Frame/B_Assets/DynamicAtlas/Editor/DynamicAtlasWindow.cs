using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Asset.DynamicAtlas.Editor
{
    /// <summary>
    /// 运行时图集页预览 引用诊断与借用纹理试装工具
    /// </summary>
    public sealed class DynamicAtlasWindow : EditorWindow
    {
        /// <summary>
        /// 调试快照刷新间隔 秒
        /// </summary>
        private const double RefreshInterval = 0.5;
        /// <summary>
        /// 预览矩形边线像素
        /// </summary>
        private const float BorderWidth = 1f;
        /// <summary>
        /// 当前观察的场景组件
        /// </summary>
        [SerializeField]
        private DynamicAtlasHost host;
        /// <summary>
        /// 试装用的已导入源纹理
        /// </summary>
        [SerializeField]
        private Texture2D source;
        /// <summary>
        /// 页面滚动位置
        /// </summary>
        private Vector2 scroll;
        /// <summary>
        /// 只由窗口持有的试装句柄
        /// </summary>
        private readonly List<DynamicAtlasHandle> handleList = new List<DynamicAtlasHandle>();
        /// <summary>
        /// 低频生成的只读页快照
        /// </summary>
        private IReadOnlyList<DynamicAtlasPageInfo> pageInfoList = new List<DynamicAtlasPageInfo>();
        /// <summary>
        /// 下一次快照刷新时间
        /// </summary>
        private double nextRefresh;

        #region 窗口生命周期

        /// <summary>
        /// 打开图集调试窗口
        /// </summary>
        public static void Open()
        {
            GetWindow<DynamicAtlasWindow>("动态图集");
        }

        /// <summary>
        /// 注册运行模式切换清理 不持有静态服务状态
        /// </summary>
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>
        /// 窗口关闭或脚本重载时归还窗口自己持有的引用
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            ReleaseTestHandles();
        }

        /// <summary>
        /// 退出运行时先释放试装引用 不修改业务句柄
        /// </summary>
        private void OnPlayModeChanged(PlayModeStateChange eState)
        {
            if (eState == PlayModeStateChange.ExitingPlayMode)
                ReleaseTestHandles();
            RefreshSnapshot();
        }

        /// <summary>
        /// 按固定间隔更新诊断数据 避免每次 GUI 事件分配快照
        /// </summary>
        private void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup < nextRefresh)
                return;
            nextRefresh = EditorApplication.timeSinceStartup + RefreshInterval;
            RefreshSnapshot();
            Repaint();
        }

        #endregion

        #region 绘制与操作

        /// <summary>
        /// 绘制组件选择 试装操作 页预览与条目引用
        /// </summary>
        private void OnGUI()
        {
            DrawEmbeddedGUI();
        }

        /// <summary>
        /// 绘制中枢嵌入内容并按间隔更新快照
        /// </summary>
        public void DrawEmbeddedGUI()
        {
            if (EditorApplication.timeSinceStartup >= nextRefresh)
            {
                nextRefresh = EditorApplication.timeSinceStartup + RefreshInterval;
                RefreshSnapshot();
            }
            EditorGUI.BeginChangeCheck();
            var Selected = (DynamicAtlasHost)EditorGUILayout.ObjectField("观察图集组", host, typeof(DynamicAtlasHost), true);
            if (EditorGUI.EndChangeCheck())
            {
                ReleaseTestHandles();
                host = Selected;
                RefreshSnapshot();
            }
            if (GUILayout.Button("观察当前选中对象的图集组"))
            {
                ReleaseTestHandles();
                host = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<DynamicAtlasHost>() : null;
                RefreshSnapshot();
            }
            if (!EditorApplication.isPlaying || host == null || !host.IsInitialized)
            {
                EditorGUILayout.HelpBox("进入 Play Mode 后选择已完成 Start 的 DynamicAtlasHost\n配置资产通过 Create → MieMieFramework → 资源 → 动态图集配置创建", MessageType.Info);
                return;
            }

            var Service = host.Service;
            EditorGUILayout.LabelField($"页数 {Service.PageCount}  待加载 {Service.PendingCount}");
            EditorGUILayout.LabelField($"逻辑常驻 {EditorUtility.FormatBytes(Service.ResidentBytes)} / 预算 {EditorUtility.FormatBytes(Service.MemoryBudgetBytes)}");
            EditorGUILayout.HelpBox("预算只统计图集纹理 不含源图 临时 RT 与延迟销毁峰值\n仅回收整页零引用内容 不移动活跃 Sprite", MessageType.None);
            source = (Texture2D)EditorGUILayout.ObjectField("试装源纹理", source, typeof(Texture2D), false);
            using (new EditorGUI.DisabledScope(source == null))
            {
                if (GUILayout.Button("试装并增加窗口引用"))
                {
                    string Key = "editor:" + AssetDatabase.GetAssetPath(source);
                    handleList.Add(Service.Acquire(Key, source));
                    RefreshSnapshot();
                }
            }
            if (GUILayout.Button($"释放窗口试装引用 {handleList.Count}"))
            {
                ReleaseTestHandles();
                RefreshSnapshot();
            }
            if (GUILayout.Button("回收所有零引用页"))
            {
                Service.TrimUnusedPages();
                RefreshSnapshot();
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var Page in pageInfoList)
            {
                if (Page.Texture == null)
                    continue;
                EditorGUILayout.LabelField($"Page {Page.Id}  {Page.Texture.width}×{Page.Texture.height}  占用 {Page.Occupancy:P1}  可回收 {Page.CanRecycle}", EditorStyles.boldLabel);
                var Preview = GUILayoutUtility.GetAspectRect((float)Page.Texture.width / Page.Texture.height, GUILayout.MaxHeight(360));
                EditorGUI.DrawTextureTransparent(Preview, Page.Texture, ScaleMode.StretchToFill);
                foreach (var Entry in Page.EntryList)
                {
                    DrawArea(Preview, Page.Texture, Entry);
                    EditorGUILayout.LabelField(Entry.Key, $"引用 {Entry.ReferenceCount}  等待 {Entry.WaiterCount}  区域 {Entry.Area}");
                }
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 将纹理底部原点矩形转换到 GUI 顶部原点并绘制边线
        /// </summary>
        private static void DrawArea(Rect preview, Texture2D texture, DynamicAtlasEntryInfo entry)
        {
            var Area = new Rect(preview.x + (float)entry.Area.x / texture.width * preview.width,
                preview.y + (1f - (float)entry.Area.yMax / texture.height) * preview.height,
                (float)entry.Area.width / texture.width * preview.width,
                (float)entry.Area.height / texture.height * preview.height);
            var Color = entry.ReferenceCount > 0 ? UnityEngine.Color.green : UnityEngine.Color.yellow;
            EditorGUI.DrawRect(new Rect(Area.x, Area.y, Area.width, BorderWidth), Color);
            EditorGUI.DrawRect(new Rect(Area.x, Area.yMax - BorderWidth, Area.width, BorderWidth), Color);
            EditorGUI.DrawRect(new Rect(Area.x, Area.y, BorderWidth, Area.height), Color);
            EditorGUI.DrawRect(new Rect(Area.xMax - BorderWidth, Area.y, BorderWidth, Area.height), Color);
        }

        /// <summary>
        /// 从有效服务获取快照 不自动创建或初始化服务
        /// </summary>
        private void RefreshSnapshot()
        {
            pageInfoList = EditorApplication.isPlaying && host != null && host.IsInitialized
                ? host.Service.GetPageInfoList() : new List<DynamicAtlasPageInfo>();
        }

        /// <summary>
        /// 只释放窗口创建的试装句柄
        /// </summary>
        private void ReleaseTestHandles()
        {
            foreach (var Handle in handleList)
                Handle.Dispose();
            handleList.Clear();
        }

        #endregion
    }
}
