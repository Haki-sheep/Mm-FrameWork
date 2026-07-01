namespace MieMieFrameWork
{
    /// <summary>
    /// Addressable 资源地址常量
    /// 所有资源地址的单一来源，避免裸字符串散落在各处
    /// 改名时只改这里，编译器会报错提醒所有引用点
    /// </summary>
    public static class AddressDef
    {
        // ==================== UI 地址 ====================
        public static class UI
        {
            public const string StoryWindow = "StoryWindow";
            public const string StartWindow = "StartWindow";
            public const string SettingWindow = "SettingWindow";
            public const string ProgrammerWindow = "ProgrammerWindow";
            public const string MainWindow = "MainWindow";
        }

        // ==================== 音频地址 ====================
        public static class Audio
        {
            // BGM
            public const string 消光1主题曲 = "消光1主题曲";
            public const string 打雷闪电 = "打雷闪电";
            public const string 密集小雨 = "密集小雨";

            // UI音效
            public const string 进入游戏可以放 = "进入游戏可以放";
            public const string 初始进入可以放 = "初始进入可以放";
            public const string ui按下音效 = "ui按下音效";
            public const string ui切换音效 = "ui切换音效";
        }
    }
}