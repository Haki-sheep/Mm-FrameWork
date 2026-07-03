namespace MieMieFrameWork
{
    using System;

    /// <summary>
    /// TypedEventBus 触发追踪
    /// </summary>
    public static class TypedEventBusTrace
    {
        /// <summary>
        /// 最近触发的 Key 名称
        /// </summary>
        public static string LastKeyName { get; private set; } = string.Empty;

        /// <summary>
        /// 最近触发时间
        /// </summary>
        public static float LastTime { get; private set; }

        /// <summary>
        /// 时间读取 由 EventBusBootstrap 注入
        /// </summary>
        internal static Func<float> NowFunc { get; set; } = () => 0f;

        /// <summary>
        /// 记录触发
        /// </summary>
        internal static void MarkTriggered(string keyName)
        {
            LastKeyName = keyName;
            LastTime = NowFunc();
        }
    }
}
