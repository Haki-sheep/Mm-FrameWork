namespace MieMieFrameWork
{
    /// <summary>
    /// 全局 TypedEventBus 单例入口
    /// </summary>
    public static class MmGlobalEventBus
    {
        /// <summary>
        /// 全局总线实例
        /// </summary>
        public static TypedEventBus Bus { get; } = new TypedEventBus();
    }
}
