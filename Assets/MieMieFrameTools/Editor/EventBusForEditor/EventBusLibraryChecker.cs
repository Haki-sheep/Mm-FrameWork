namespace MieMieFrameWork.Editor.EventBusForEditor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// EventBus 库安装检测
    /// </summary>
    public static class EventBusLibraryChecker
    {
        /// <summary>
        /// UPM 包名
        /// </summary>
        public const string PackageName = "com.hakisheep.mm-eventbus";

        /// <summary>
        /// Git 安装地址
        /// </summary>
        public const string GitInstallUrl = "git@github.com:Haki-sheep/MmCSharp-EventBus.git?path=unity";

        /// <summary>
        /// 核心程序集是否可用
        /// </summary>
        public static bool IsCoreInstalled =>
            Type.GetType("MiMieEventBus.EventBus`1, MiMieEventBus") != null;

        /// <summary>
        /// 绘制未安装提示
        /// </summary>
        public static void DrawNotInstalledHelpBox()
        {
            EditorGUILayout.HelpBox(
                "未检测到 MiMieEventBus 运行时库\n" +
                "请先在模块中枢安装 EventBus 库 或通过 Package Manager 添加\n" +
                PackageName + "\n" +
                GitInstallUrl,
                MessageType.Warning);
        }

        /// <summary>
        /// 绘制顶部安装状态
        /// </summary>
        public static bool DrawInstallGate()
        {
            if (IsCoreInstalled)
            {
                EditorGUILayout.HelpBox("MiMieEventBus 运行时库已就绪", MessageType.Info);
                return true;
            }

            DrawNotInstalledHelpBox();
            return false;
        }
    }
}
