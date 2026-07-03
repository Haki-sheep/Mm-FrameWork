namespace MieMieFrameWork
{
    using System;

    /// <summary>
    /// 事件总线日志桥接
    /// </summary>
    public static class EventBusLog
    {
        /// <summary>
        /// 错误日志委托
        /// </summary>
        public static Action<string> LogError { get; set; } = _ => { };
    }
}
