using MieMieFrameWork.HotUpdate.Editor;
using UnityEngine;

namespace MieMieFrameWork.Editor.ToolsCenter
{
    public sealed class HotUpdateToolsPage : MieMieToolsPage
    {
        /// <summary> 本页面持有的隐藏发布窗口 </summary>
        private HotUpdateWindow window;
        /// <summary> 切页期间保留的发布表单 </summary>
        private string windowState;

        public HotUpdateToolsPage() : base("资源/热更新", "热更新", "配置 HybridCLR 准备首包基线并导出代码与发布资源")
        {
        }

        public override void OnOpen()
        {
            if (window != null)
                return;
            window = ScriptableObject.CreateInstance<HotUpdateWindow>();
            window.hideFlags = HideFlags.HideAndDontSave;
            if (!string.IsNullOrEmpty(windowState))
                UnityEditor.EditorJsonUtility.FromJsonOverwrite(windowState, window);
        }

        public override void OnClose()
        {
            if (window == null)
                return;
            windowState = UnityEditor.EditorJsonUtility.ToJson(window);
            Object.DestroyImmediate(window);
            window = null;
        }

        public override void DrawGUI()
        {
            DrawPageTitle();
            window.DrawEmbeddedGUI();
        }
    }
}
