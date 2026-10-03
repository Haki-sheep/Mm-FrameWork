namespace MiMieEventBus
{
    using System;

    /// <summary>
    /// 事件订阅令牌 Dispose 时取消订阅
    /// </summary>
    public sealed class EventBusSubscription : IDisposable
    {
        /// <summary>
        /// 取消订阅动作
        /// </summary>
        private Action unsubscribeAction;

        /// <summary>
        /// 是否已释放
        /// </summary>
        private bool isDisposed;

        internal Delegate Handler { get; private set; }

        internal Delegate DispatchHandler { get; private set; }

        /// <summary>
        /// 创建订阅令牌
        /// </summary>
        public EventBusSubscription(Action unsubscribeAction)
        {
            this.unsubscribeAction = unsubscribeAction;
        }

        /// <summary>
        /// 创建持有监听委托的订阅令牌
        /// </summary>
        internal EventBusSubscription(Delegate handler, Delegate dispatchHandler, Action unsubscribeAction)
            : this(unsubscribeAction)
        {
            Handler = handler;
            DispatchHandler = dispatchHandler;
        }

        /// <summary>
        /// 取消本次订阅 重复释放不产生额外操作
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            try
            {
                unsubscribeAction?.Invoke();
            }
            finally
            {
                Detach();
            }
        }

        /// <summary>
        /// 解除令牌引用 总线清理时不再触发注销动作
        /// </summary>
        internal void Detach()
        {
            isDisposed = true;
            unsubscribeAction = null;
            Handler = null;
            DispatchHandler = null;
        }
    }
}

