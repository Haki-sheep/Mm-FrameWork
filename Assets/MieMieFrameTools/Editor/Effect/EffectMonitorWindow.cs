namespace MieMieFrameWork.Effects.Editor
{
    using System.Collections.Generic;
    using MieMieFrameWork.Editor.ToolsCenter;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 视觉特效诊断窗口 只查询运行时状态 不持有资源或管理器
    /// </summary>
    public sealed class EffectMonitorWindow : EditorWindow, IMieMieToolsEmbeddedWindow
    {
        /// <summary> 复用的资源诊断快照 </summary>
        private readonly List<EffectDiagnostics> diagnosticList = new();

        /// <summary> 资源列表滚动位置 </summary>
        private Vector2 scrollPosition;

        /// <summary>
        /// 绘制独立窗口视图
        /// </summary>
        private void OnGUI() => DrawEmbeddedGUI();

        /// <summary>
        /// 查询当前会话管理器 显示预算和单类资源持有
        /// </summary>
        public void DrawEmbeddedGUI()
        {
            if (!EditorApplication.isPlaying || ModuleHub.Instance == null || !ModuleHub.Instance.IsReady)
            {
                EditorGUILayout.HelpBox("进入 Play 并等待框架就绪后查看视觉特效", MessageType.Info);
                return;
            }
            var Manager = ModuleHub.Instance.GetManager<EffectManager>();
            EditorGUILayout.LabelField($"请求 {Manager.RequestCount} 资源 {Manager.ResourceCount} 拒绝 {Manager.RejectedRequests}");
            var eQuality = (EEffectQuality)EditorGUILayout.EnumPopup("特效档位", Manager.Quality);
            if (eQuality != Manager.Quality)
                Manager.SetQuality(eQuality);
            bool Paused = EditorGUILayout.Toggle("全局暂停", Manager.IsPaused);
            if (Paused != Manager.IsPaused)
                Manager.SetGlobalPaused(Paused);
            if (GUILayout.Button("停止全部播放"))
                Manager.StopAll();
            if (GUILayout.Button("清理闲置资源"))
                Manager.ClearUnused();
            Manager.CopyDiagnostics(diagnosticList);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (var Info in diagnosticList)
            {
                EditorGUILayout.LabelField(Info.Id, Info.Location);
                EditorGUILayout.LabelField($"请求 {Info.Requests} 借出 {Info.Active} 缓存 {Info.Cached} 已加载 {Info.Loaded}");
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 编辑器更新时刷新窗口 不执行运行时业务更新
        /// </summary>
        private void OnInspectorUpdate() => Repaint();
    }
}
