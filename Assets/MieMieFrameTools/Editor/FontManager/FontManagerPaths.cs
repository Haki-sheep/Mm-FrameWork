namespace MieMieFrameWork.Localization.FontManagement
{
    /// <summary>
    /// 字体工作台资产目录的唯一归属
    /// </summary>
    public static class FontManagerPaths
    {
        /// <summary> 字体工具输出根目录 </summary>
        public const string Root = "Assets/MieMieFrameTools/ADefaultRes/Fonts";
        /// <summary> 字体烘焙配置目录 </summary>
        public const string Profiles = Root + "/Profiles";
        /// <summary> 生成 TMP 字体目录 </summary>
        public const string Generated = Root + "/Generated";
        /// <summary> 原位更新前的字体备份目录 </summary>
        public const string Backups = Root + "/Backups";
        /// <summary> 当前工程已有的字体源目录 </summary>
        public const string Source = "Assets/MieMieFrameTools/ADefaultRes/Arts/FrontArt";
        /// <summary> 当前工程默认字体源 </summary>
        public const string DefaultSource = Source + "/ZLabsRoundPix_16px_M_CN.ttf";
    }
}
