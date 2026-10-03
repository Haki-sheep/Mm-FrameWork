namespace MiMieEventBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 结构体 EventKey 驱动的类型安全事件总线
    /// 约定仅在主线程调用 Subscribe Publish Unsubscribe
    /// Unsubscribe 必须传入与 Subscribe 相同的委托实例 禁止用新 Lambda 取消
    /// 推荐持有 Subscribe 返回的 IDisposable 令牌并 Dispose
    /// 同步按订阅顺序派发 监听变更从下一轮生效 异常原样传播
    /// </summary>
    public class EventBusCore
    {
        /// <summary>
        /// 事件字典
        /// </summary>
        private readonly Dictionary<(string name, Type handlerType), EventBusSlot> eventDict = new();

        #region 订阅与派发

        /// <summary>
        /// 注册无参监听 返回只取消本次订阅的令牌
        /// </summary>
        public IDisposable Subscribe(EventKey eventKey, Action action)
        {
            return Subscribe((eventKey.Name, typeof(Action)), action, action);
        }

        /// <summary>
        /// 注册消息监听 返回只取消本次订阅的令牌
        /// </summary>
        public IDisposable Subscribe<TEvent>(EventKey<TEvent> eventKey, Action<TEvent> action)
        {
            // 逆变委托转换为槽位消息类型 原委托仍用于准确注销
            var DispatchHandler = action == null || action.GetType() == typeof(Action<TEvent>)
                ? action
                : new Action<TEvent>(action);
            return Subscribe((eventKey.Name, typeof(Action<TEvent>)), action, DispatchHandler);
        }

        /// <summary>
        /// 发布无参事件 监听异常立即中断并向上传播
        /// </summary>
        public void Publish(EventKey eventKey)
        {
            if (!eventDict.TryGetValue((eventKey.Name, typeof(Action)), out var Slot))
                return;

            // 多播委托快照保持本轮监听顺序且不为派发创建数组
            var Handler = (Action)Slot.Handler;
            EventBusTrace.MarkTriggered(eventKey.Name);
            Handler.Invoke();
        }

        /// <summary>
        /// 发布消息事件 监听异常立即中断并向上传播
        /// </summary>
        public void Publish<TEvent>(EventKey<TEvent> eventKey, TEvent message)
        {
            if (!eventDict.TryGetValue((eventKey.Name, typeof(Action<TEvent>)), out var Slot))
                return;

            var Handler = (Action<TEvent>)Slot.Handler;
            EventBusTrace.MarkTriggered(eventKey.Name);
            Handler.Invoke(message);
        }

        /// <summary>
        /// 取消最后一次匹配的无参订阅 必须传入 Subscribe 时同一委托实例
        /// </summary>
        public void Unsubscribe(EventKey eventKey, Action action)
        {
            Unsubscribe((eventKey.Name, typeof(Action)), action);
        }

        /// <summary>
        /// 取消最后一次匹配的消息订阅 必须传入 Subscribe 时同一委托实例
        /// </summary>
        public void Unsubscribe<TEvent>(EventKey<TEvent> eventKey, Action<TEvent> action)
        {
            Unsubscribe((eventKey.Name, typeof(Action<TEvent>)), action);
        }

        #endregion

        #region 清理与诊断

        /// <summary>
        /// 按事件名移除全部类型槽位 含同名带参监听
        /// </summary>
        public void RemoveAllByName(string keyName)
        {
            if (string.IsNullOrEmpty(keyName))
                return;

            var RemoveList = new List<(string name, Type handlerType)>();
            foreach (var SlotKey in eventDict.Keys)
            {
                if (SlotKey.name == keyName)
                    RemoveList.Add(SlotKey);
            }

            for (int Index = 0; Index < RemoveList.Count; Index++)
            {
                eventDict[RemoveList[Index]].Clear();
                eventDict.Remove(RemoveList[Index]);
            }
        }

        /// <summary>
        /// 按事件名移除全部类型槽位 含同名带参监听
        /// </summary>
        public void RemoveAll(EventKey eventKey)
        {
            RemoveAllByName(eventKey.Name);
        }

        /// <summary>
        /// 按事件名移除全部类型槽位 含同名带参监听
        /// </summary>
        public void RemoveAll<TEvent>(EventKey<TEvent> eventKey)
        {
            RemoveAllByName(eventKey.Name);
        }

        /// <summary>
        /// 清空全部监听并解除旧订阅令牌持有的引用
        /// </summary>
        public void Clear()
        {
            foreach (var Slot in eventDict.Values)
                Slot.Clear();

            eventDict.Clear();
        }

        /// <summary>
        /// 获取已注册事件槽数量
        /// </summary>
        public int GetEventCount()
        {
            return eventDict.Count;
        }

        /// <summary>
        /// 获取指定 Key 的监听数量
        /// </summary>
        public int GetListenerCount(EventKey eventKey)
        {
            return GetListenerCount((eventKey.Name, typeof(Action)));
        }

        /// <summary>
        /// 获取指定 Key 的监听数量
        /// </summary>
        public int GetListenerCount<TEvent>(EventKey<TEvent> eventKey)
        {
            return GetListenerCount((eventKey.Name, typeof(Action<TEvent>)));
        }

        /// <summary>
        /// 获取全部已注册 Key 名称
        /// </summary>
        public List<string> GetRegisteredKeyNameList()
        {
            var NameList = new List<string>();
            foreach (var SlotKey in eventDict.Keys)
            {
                if (!NameList.Contains(SlotKey.name))
                    NameList.Add(SlotKey.name);
            }

            NameList.Sort(StringComparer.Ordinal);
            return NameList;
        }

        /// <summary>
        /// 获取指定 Key 名称的监听总数
        /// </summary>
        public int GetListenerCountByName(string keyName)
        {
            int Count = 0;
            foreach (var Pair in eventDict)
            {
                if (Pair.Key.name == keyName)
                    Count += Pair.Value.ListenerCount;
            }

            return Count;
        }

        #endregion

        #region 槽位操作

        /// <summary>
        /// 按名称与委托类型注册监听并返回独立订阅令牌
        /// </summary>
        private IDisposable Subscribe((string name, Type handlerType) slotKey, Delegate action, Delegate dispatchHandler)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (!eventDict.TryGetValue(slotKey, out var Slot))
            {
                Slot = new EventBusSlot(() => eventDict.Remove(slotKey));
                eventDict.Add(slotKey, Slot);
            }

            return Slot.Subscribe(action, dispatchHandler);
        }

        /// <summary>
        /// 按槽位取消最后一次匹配的订阅
        /// </summary>
        private void Unsubscribe((string name, Type handlerType) slotKey, Delegate action)
        {
            if (eventDict.TryGetValue(slotKey, out var Slot))
                Slot.Unsubscribe(action);
        }

        /// <summary>
        /// 获取槽位监听数
        /// </summary>
        private int GetListenerCount((string name, Type handlerType) slotKey)
        {
            if (eventDict.TryGetValue(slotKey, out var Slot))
                return Slot.ListenerCount;

            return 0;
        }

        #endregion
    }
}

