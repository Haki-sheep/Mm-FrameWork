using System.Collections.Generic;

namespace MieMieFrameWork.Editor.ToolsCenter
{
    /// <summary>
    /// 工具中枢内置页面目录
    /// </summary>
    public static class MieMieToolsPageCatalog
    {
        #region 页面目录

        /// <summary>
        /// 创建内置页面列表
        /// </summary>
        public static List<IMieMieToolsPage> CreatePageList()
        {
            return new List<IMieMieToolsPage>
            {
                new MieMieToolsHomePage(),

                new MieMieLogToolsPage(),
                CreateEmbedded<MieMieFrameWork.Effects.Editor.EffectMonitorWindow>(
                    "运行监控/视觉特效",
                    "视觉特效运行监控",
                    "查看特效预算与资源持有并控制当前播放"),
                CreateAction(
                    "多语言与字体/字体管理器",
                    "字体管理器",
                    "打开字体收集 烘焙 预览与覆盖诊断工作台",
                    MieMieFrameWork.Localization.FontManagement.FontManagerWindow.Open),
                CreateAction(
                    "多语言与字体/创建默认字体映射",
                    "创建默认字体映射",
                    "创建简中与英文的默认字体映射 不覆盖现有配置",
                    MieMieFrameWork.Localization.FontManagement.LocalizationProjectSetup.CreateDefaultCatalog),
                CreateAction(
                    "多语言与字体/接入选中的框架根节点",
                    "接入选中的框架根节点",
                    "为选中的场景 ModuleHub 根节点接入多语言宿主",
                    MieMieFrameWork.Localization.FontManagement.LocalizationProjectSetup.AttachToSelectedRoot),
                CreateAction(
                    "多语言与字体/绑定选中的 TMP 文本",
                    "绑定选中的 TMP 文本",
                    "绑定选中的场景 TextMeshProUGUI 文本 Key 与字体样式",
                    MieMieFrameWork.Localization.FontManagement.LocalizationProjectSetup.BindSelectedText),
                CreateAction(
                    "多语言与字体/验证字体工具迁移",
                    "验证字体工具迁移",
                    "显式运行字体收集 缺字诊断 生成与渲染验证",
                    MieMieFrameWork.Localization.FontManagement.FontManagerVerification.Run),

                CreateEmbedded<MieMieFrameWork.Editor.Animation.FbxAnimationClipRenameExtractWindow>(
                    "Animation/FBX 动画改名提取",
                    "FBX 动画改名提取",
                    "批量处理 FBX 动画片段名称与提取设置"),

                CreateEmbedded<MieMieFrameWork.DMVC.DMVCCodeGeneratorWindow>(
                    "DMVC/代码生成",
                    "DMVC 代码生成",
                    "生成 World 与 Data Message Logic 模块模板"),
                CreateEmbedded<MieMieFrameWork.DMVC.DMVCExecutionOrderWindow>(
                    "DMVC/执行顺序",
                    "DMVC 执行顺序",
                    "扫描模块并生成执行顺序实现"),

                CreateEmbedded<MieMieFrameWork.Editor.FolderBookmarkWindow>(
                    "Folder/文件夹收藏",
                    "文件夹收藏",
                    "管理常用文件夹收藏与项目窗口辅助操作"),
                CreateAction(
                    "Folder/打开 Assets",
                    "打开 Assets",
                    "快速定位项目 Assets 目录",
                    MieMieFrameWork.Editor.CheckFolder.OpenAssetsFolder),
                CreateAction(
                    "Folder/打开 Archive Data",
                    "打开 Archive Data",
                    "快速定位存档数据目录",
                    MieMieFrameWork.Editor.CheckFolder.OpenSaveDataFolder),
                CreateAction(
                    "Folder/打开 StreamingAssets",
                    "打开 StreamingAssets",
                    "快速定位 StreamingAssets 目录",
                    MieMieFrameWork.Editor.CheckFolder.OpenStreamingAssetsFolder),
                CreateAction(
                    "Folder/打开 Persistent Data",
                    "打开 Persistent Data",
                    "快速定位持久化数据目录",
                    MieMieFrameWork.Editor.CheckFolder.OpenPersistentDataFolder),
                CreateAction(
                    "Folder/打开 Temp",
                    "打开 Temp",
                    "快速定位项目临时目录",
                    MieMieFrameWork.Editor.CheckFolder.OpenTempFolder),
                CreateAction(
                    "Folder/打开 Logs",
                    "打开 Logs",
                    "快速定位项目日志目录",
                    MieMieFrameWork.Editor.CheckFolder.OpenLogsFolder),
                CreateAction(
                    "Folder/创建 StreamingAssets",
                    "创建 StreamingAssets",
                    "创建项目 StreamingAssets 目录",
                    MieMieFrameWork.Editor.CheckFolder.CreateStreamingAssetsFolder),
                CreateAction(
                    "Folder/创建 Resources",
                    "创建 Resources",
                    "创建项目 Resources 目录",
                    MieMieFrameWork.Editor.CheckFolder.CreateResourcesFolder),
                CreateAction(
                    "Folder/创建 Editor",
                    "创建 Editor",
                    "创建项目 Editor 目录",
                    MieMieFrameWork.Editor.CheckFolder.CreateEditorFolder),
                CreateAction(
                    "Folder/创建 Plugins",
                    "创建 Plugins",
                    "创建项目 Plugins 目录",
                    MieMieFrameWork.Editor.CheckFolder.CreatePluginsFolder),
                CreateAction(
                    "Folder/创建 Scripts",
                    "创建 Scripts",
                    "创建项目 Scripts 目录",
                    MieMieFrameWork.Editor.CheckFolder.CreateScriptsFolder),
                CreateAction(
                    "Folder/创建 Prefabs",
                    "创建 Prefabs",
                    "创建项目 Prefabs 目录",
                    MieMieFrameWork.Editor.CheckFolder.CreatePrefabsFolder),
                CreateAction(
                    "Folder/创建 Materials",
                    "创建 Materials",
                    "创建项目 Materials 目录",
                    MieMieFrameWork.Editor.CheckFolder.CreateMaterialsFolder),
                CreateAction(
                    "Folder/创建 Textures",
                    "创建 Textures",
                    "创建项目 Textures 目录",
                    MieMieFrameWork.Editor.CheckFolder.CreateTexturesFolder),
                CreateAction(
                    "Folder/创建 Audio",
                    "创建 Audio",
                    "创建项目 Audio 目录",
                    MieMieFrameWork.Editor.CheckFolder.CreateAudioFolder),
                CreateAction(
                    "Folder/创建 Animations",
                    "创建 Animations",
                    "创建项目 Animations 目录",
                    MieMieFrameWork.Editor.CheckFolder.CreateAnimationsFolder),
                CreateAction(
                    "Folder/创建 Common Structure",
                    "创建 Common Structure",
                    "创建项目通用目录结构",
                    MieMieFrameWork.Editor.CheckFolder.CreateCommonFolderStructure),

                CreateEmbedded<MieMieFrameWork.FSM.TickFSMWindow>(
                    "FSM/Tick FSM",
                    "Tick FSM",
                    "生成 Tick FSM 黑板枚举并配置运行时调试"),
                CreateEmbedded<MieMieFrameWork.ChainedFsm.Editor.ChinedFSMSqueueWindow>(
                    "FSM/链式 FSM 生成器",
                    "链式 FSM 生成器",
                    "按顺序生成链式状态流程代码"),

                CreateEmbedded<MieMieFrameWork.Editor.DataForEditor.LubanWorkbenchWindow>(
                    "Data/Luban 工作台",
                    "Luban 工作台",
                    "打开配置表并执行 Luban 校验与代码生成"),

                CreateAction(
                    "YooAsset/资源采集",
                    "YooAsset 资源采集",
                    "配置资源包 采集地址与资源标签",
                    YooAsset.Editor.BundleCollectorWindow.OpenWindow),
                CreateAction(
                    "YooAsset/资源构建",
                    "YooAsset 资源构建",
                    "使用 YooAsset 原生构建器生成资源包",
                    YooAsset.Editor.BundleBuilderWindow.OpenWindow),
                CreateAction(
                    "YooAsset/资源调试",
                    "YooAsset 资源调试",
                    "查看原生资源句柄和 Bundle 运行状态",
                    YooAsset.Editor.BundleDebuggerWindow.OpenWindow),

                CreateEmbedded<MieMieFrameWork.Editor.EventBusForEditor.EventBusEditorWindow>(
                    "Event Bus/事件总线",
                    "Event Bus",
                    "查看和调试编辑器事件总线"),
                CreateEmbedded<MieMieFrameWork.Editor.PoolEditor.PoolEditorWindow>(
                    "Object Pool/对象池",
                    "对象池",
                    "查看运行时对象池状态并执行预热与扫描"),

                CreateEmbedded<MieMieFrameWork.Editor.SaveForEditor.ArchiveEditorWindow>(
                    "Save System/存档系统",
                    "存档系统",
                    "管理存档槽位并诊断存档模块状态"),

                CreateEmbedded<MieMieFrameWork.Editor.AsmdefTool.AsmdefToolWindow>(
                    "AsmdefTool/程序集工具",
                    "AsmdefTool",
                    "批量分析脚本依赖并生成程序集定义")
            };
        }

        #endregion

        #region 页面工厂

        /// <summary>
        /// 创建嵌入式工具页面
        /// </summary>
        private static IMieMieToolsPage CreateEmbedded<TWindow>(
            string menuPath,
            string title,
            string description)
            where TWindow : UnityEditor.EditorWindow, IMieMieToolsEmbeddedWindow
        {
            return new MieMieToolsEmbeddedPage<TWindow>(menuPath, title, description);
        }

        /// <summary>
        /// 创建静态工具操作页面
        /// </summary>
        private static IMieMieToolsPage CreateAction(
            string menuPath,
            string title,
            string description,
            System.Action action)
        {
            return new MieMieToolsActionPage(menuPath, title, description, action);
        }

        #endregion
    }
}
