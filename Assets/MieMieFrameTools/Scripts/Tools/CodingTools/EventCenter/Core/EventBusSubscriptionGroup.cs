namespace MiMieEventBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 订阅容器 Clear 释放当前订阅后可复用 Dispose 后不可新增
    /// </summary>
    public sealed class EventBusSubscriptionGroup : IDisposable
    {
        /// <summary>
        /// 当前收集的订阅令牌
        /// </summary>
        private List<IDisposable> subscriptionList = new();

        /// <summary>
        /// 是否已永久释放
        /// </summary>
        private bool isDisposed;

        public int Count => subscriptionList.Count;

        /// <summary>
        /// 收集订阅令牌 由容器统一承担释放责任
        /// </summary>
        public void Add(IDisposable subscription)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(EventBusSubscriptionGroup));

            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            subscriptionList.Add(subscription);
        }

        /// <summary>
        /// 释放本轮收集的全部订阅并清空 失败时汇总异常向上传播
        /// </summary>
        public void Clear()
        {
            // 先分离本轮令牌 避免释放回调重入导致重复释放
            var ReleaseList = subscriptionList;
            subscriptionList = new List<IDisposable>();
            List<Exception> ErrorList = null;

            foreach (var Subscription in ReleaseList)
            {
                try
                {
                    Subscription.Dispose();
                }
                catch (Exception Exception)
                {
                    ErrorList ??= new List<Exception>();
                    ErrorList.Add(Exception);
                }
            }

            ReleaseList.Clear();
            if (ErrorList != null)
                throw new AggregateException("事件订阅容器释放失败", ErrorList);
        }

        /// <summary>
        /// 永久释放容器和已收集的订阅 重复释放无额外操作
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            Clear();
        }
    }
}
