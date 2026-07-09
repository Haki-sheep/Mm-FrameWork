namespace MieMieFrameWork
{
    using MiMieEventBus;

    /// <summary>
    /// 框架内置事件 Key 各模块可自建静态类扩展
    /// </summary>
    public static class MmGameEvents
    {
        /// <summary>
        /// 场景加载进度
        /// </summary>
        public static readonly EventKey<float> LoadingSceneProgress = new EventKey<float>("Scene.LoadingProgress");

        /// <summary>
        /// 切换语言
        /// </summary>
        public static readonly EventKey ChangeLanguage = new EventKey("UI.ChangeLanguage");

        /// <summary>
        /// 游戏开始
        /// </summary>
        public static readonly EventKey GameStart = new EventKey("Game.Start");
    }
}
