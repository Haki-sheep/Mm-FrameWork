namespace MieMieFrameWork
{
    using MiMieEventBus;

    /// <summary>
    /// 全局 EventBusCore 单例入口
    /// </summary>
    public static class MmGlobalEventBus
    {
        /// <summary>
        /// 全局总线实例
        /// </summary>
        public static EventBusCore GlobalBus { get; } = new EventBusCore();
    }
}
