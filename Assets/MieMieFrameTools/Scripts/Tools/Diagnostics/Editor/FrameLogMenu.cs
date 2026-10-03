using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Diagnostics.Editor
{
    /// <summary>
    /// Play 日志导出与 Editor 退出释放 不接管用户场景
    /// </summary>
    [InitializeOnLoad]
    public static class FrameLogMenu
    {
        /// <summary>
        /// 注册 Editor 会话退出与程序集重载清理
        /// </summary>
        static FrameLogMenu()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += FrameLog.Shutdown;
        }

        /// <summary>
        /// 打开保存对话框并导出当前运行会话
        /// </summary>
        public static void ExportRecent()
        {
            string FileName = $"MieMieLog-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}";
            string OutputPath = EditorUtility.SaveFilePanel("导出近期日志", Application.persistentDataPath, FileName, "log");
            if (!string.IsNullOrEmpty(OutputPath))
                FrameLog.ExportRecent(OutputPath);
        }

        /// <summary>
        /// 只允许在存在运行会话时导出
        /// </summary>
        public static bool CanExportRecent()
        {
            return FrameLog.IsReady;
        }

        /// <summary>
        /// 打开日志专属目录 不创建或删除文件
        /// </summary>
        public static void OpenLogDirectory()
        {
            EditorUtility.RevealInFinder(Path.Combine(Application.persistentDataPath, FrameLog.FileDirectoryName));
        }

        /// <summary>
        /// 场景退出完成后释放日志 保留 OnDestroy 阶段的采集
        /// </summary>
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                FrameLog.Shutdown();
        }
    }
}
