namespace MieMieFrameWork.DMVC.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// DMVC 库安装检测
    /// </summary>
    public static class DmvcLibraryChecker
    {
        /// <summary>
        /// UPM 包名
        /// </summary>
        public const string PackageName = "com.hakisheep.mm-dmvc";

        /// <summary>
        /// Git 安装地址
        /// </summary>
        public const string GitInstallUrl = "git@github.com:Haki-sheep/MmCSharp-DMVC.git?path=unity";

        /// <summary>
        /// 核心程序集是否可用
        /// </summary>
        public static bool IsCoreInstalled =>
            WorldModuleType != null;

        /// <summary>
        /// WorldModule 类型
        /// </summary>
        public static Type WorldModuleType =>
            Type.GetType("MiMieDMVC.WorldModule, MiMieDMVC");

        /// <summary>
        /// Logic 接口类型
        /// </summary>
        public static Type LogicBehaviourType =>
            Type.GetType("MiMieDMVC.ILogicBehaviour, MiMieDMVC");

        /// <summary>
        /// Data 接口类型
        /// </summary>
        public static Type DataBehaviourType =>
            Type.GetType("MiMieDMVC.IDataBehaviour, MiMieDMVC");

        /// <summary>
        /// Message 接口类型
        /// </summary>
        public static Type MessageBehaviourType =>
            Type.GetType("MiMieDMVC.IMessageBehaviour, MiMieDMVC");

        /// <summary>
        /// 绘制未安装提示
        /// </summary>
        public static void DrawNotInstalledHelpBox()
        {
            EditorGUILayout.HelpBox(
                "未检测到 MiMieDMVC 运行时库\n" +
                "请先在模块中枢安装 DMVC 库 或通过 Package Manager 添加\n" +
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
                EditorGUILayout.HelpBox("MiMieDMVC 运行时库已就绪", MessageType.Info);
                return true;
            }

            DrawNotInstalledHelpBox();
            return false;
        }
    }
}
