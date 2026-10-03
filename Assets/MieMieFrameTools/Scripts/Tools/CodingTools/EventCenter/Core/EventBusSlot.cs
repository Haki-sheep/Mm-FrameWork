namespace MiMieEventBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 单个事件槽位 持有独立订阅与不可变派发快照
    /// </summary>
    internal sealed class EventBusSlot
    {
        /// <summary>
        /// 按注册顺序保存的订阅令牌
        /// </summary>
        private readonly List<EventBusSubscription> subscriptionList = new();

        /// <summary>
        /// 最后一个订阅移除后删除所属槽位
        /// </summary>
        private readonly Action onEmptyAction;

        internal Delegate Handler { get; private set; }

        internal int ListenerCount { get; private set; }

        /// <summary>
        /// 创建槽位并绑定空槽移除动作
        /// </summary>
        internal EventBusSlot(Action onEmptyAction)
        {
            this.onEmptyAction = onEmptyAction;
        }

        /// <summary>
        /// 注册监听 返回只关联本次注册的令牌
        /// </summary>
        internal EventBusSubscription Subscribe(Delegate handler, Delegate dispatchHandler)
        {
            var Snapshot = Delegate.Combine(Handler, dispatchHandler);
            int NewListenerCount = handler.GetInvocationList().Length;
            EventBusSubscription Subscription = null;
            // 创建订阅令牌 传入移除订阅信息
            Subscription = new EventBusSubscription(handler, dispatchHandler, () => Remove(Subscription));
            subscriptionList.Add(Subscription);
            Handler = Snapshot;
            ListenerCount += NewListenerCount;
            return Subscription;
        }

        /// <summary>
        /// 取消最后一次委托相同的注册
        /// </summary>
        internal void Unsubscribe(Delegate handler)
        {
            for (int Index = subscriptionList.Count - 1; Index >= 0; Index--)
            {
                if (subscriptionList[Index].Handler.Equals(handler))
                {
                    subscriptionList[Index].Dispose();
                    return;
                }
            }
        }

        /// <summary>
        /// 解除全部令牌引用并清空监听
        /// </summary>
        internal void Clear()
        {
            foreach (var Subscription in subscriptionList)
                Subscription.Detach();

            subscriptionList.Clear();
            Handler = null;
            ListenerCount = 0;
        }

        /// <summary>
        /// 按令牌身份取消注册并更新后续派发快照
        /// </summary>
        private void Remove(EventBusSubscription subscription)
        {
            subscriptionList.RemoveAt(subscriptionList.IndexOf(subscription));
            ListenerCount -= subscription.Handler.GetInvocationList().Length;
            subscription.Detach();

            // 按注册记录重建快照 避免重复委托误删其他位置影响顺序
            Handler = null;
            foreach (var Subscription in subscriptionList)
                Handler = Delegate.Combine(Handler, Subscription.DispatchHandler);

            if (subscriptionList.Count == 0)
                onEmptyAction.Invoke();
        }
    }
}
