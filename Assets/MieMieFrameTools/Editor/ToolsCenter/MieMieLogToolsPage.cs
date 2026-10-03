using MieMieFrameWork.Diagnostics.Editor;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.ToolsCenter
{
    /// <summary>
    /// 日志导出与目录操作页面 不创建或释放日志会话
    /// </summary>
    public sealed class MieMieLogToolsPage : MieMieToolsPage
    {
        /// <summary>
        /// 定义日志页面的中枢目录位置
        /// </summary>
        public MieMieLogToolsPage()
            : base("日志与诊断/日志导出", "日志与诊断", "导出当前运行会话的近期日志与上下文")
        {
        }

        /// <summary>
        /// 按会话状态控制导出入口并保留独立目录操作
        /// </summary>
        public override void DrawGUI()
        {
            DrawPageTitle();
            bool CanExport = FrameLogMenu.CanExportRecent();
            if (!CanExport)
                EditorGUILayout.HelpBox("进入 Play 后才有日志会话可导出", MessageType.Info);
            using (new EditorGUI.DisabledScope(!CanExport))
            {
                if (GUILayout.Button("导出近期日志", GUILayout.Height(32f)))
                    FrameLogMenu.ExportRecent();
            }
            if (GUILayout.Button("打开日志目录", GUILayout.Height(32f)))
                FrameLogMenu.OpenLogDirectory();
        }
    }
}
