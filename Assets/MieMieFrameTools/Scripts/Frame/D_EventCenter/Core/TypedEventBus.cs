namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 结构体 EventKey 驱动的类型安全事件总线
    /// </summary>
    public class TypedEventBus
    {
        /// <summary>
        /// 事件字典
        /// </summary>
        private readonly Dictionary<(string name, Type handlerType), Delegate> eventDict =
            new ();

        /// <summary>
        /// 注册无参监听
        /// </summary>
        public void Subscribe(EventKey eventKey, Action action)
        {
            CombineDelegate((eventKey.Name, typeof(Action)), action);
        }

        /// <summary>
        /// 注册单参监听
        /// </summary>
        public void Subscribe<T>(EventKey<T> eventKey, Action<T> action)
        {
            CombineDelegate((eventKey.Name, typeof(Action<T>)), action);
        }

        /// <summary>
        /// 注册双参监听
        /// </summary>
        public void Subscribe<T0, T1>(EventKey<T0, T1> eventKey, Action<T0, T1> action)
        {
            CombineDelegate((eventKey.Name, typeof(Action<T0, T1>)), action);
        }

        /// <summary>
        /// 注册三参监听
        /// </summary>
        public void Subscribe<T0, T1, T2>(EventKey<T0, T1, T2> eventKey, Action<T0, T1, T2> action)
        {
            CombineDelegate((eventKey.Name, typeof(Action<T0, T1, T2>)), action);
        }

        /// <summary>
        /// 注册四参监听
        /// </summary>
        public void Subscribe<T0, T1, T2, T3>(EventKey<T0, T1, T2, T3> eventKey, Action<T0, T1, T2, T3> action)
        {
            CombineDelegate((eventKey.Name, typeof(Action<T0, T1, T2, T3>)), action);
        }

        /// <summary>
        /// 注册五参监听
        /// </summary>
        public void Subscribe<T0, T1, T2, T3, T4>(EventKey<T0, T1, T2, T3, T4> eventKey, Action<T0, T1, T2, T3, T4> action)
        {
            CombineDelegate((eventKey.Name, typeof(Action<T0, T1, T2, T3, T4>)), action);
        }

        /// <summary>
        /// 发布无参事件
        /// </summary>
        public void Publish(EventKey eventKey)
        {
            Invoke((eventKey.Name, typeof(Action)), eventKey.Name, eventDelegate =>
            {
                (eventDelegate as Action)?.Invoke();
            });
        }

        /// <summary>
        /// 发布单参事件
        /// </summary>
        public void Publish<T>(EventKey<T> eventKey, T arg)
        {
            Invoke((eventKey.Name, typeof(Action<T>)), eventKey.Name, eventDelegate =>
            {
                (eventDelegate as Action<T>)?.Invoke(arg);
            });
        }

        /// <summary>
        /// 发布双参事件
        /// </summary>
        public void Publish<T0, T1>(EventKey<T0, T1> eventKey, T0 arg0, T1 arg1)
        {
            Invoke((eventKey.Name, typeof(Action<T0, T1>)), eventKey.Name, eventDelegate =>
            {
                (eventDelegate as Action<T0, T1>)?.Invoke(arg0, arg1);
            });
        }

        /// <summary>
        /// 发布三参事件
        /// </summary>
        public void Publish<T0, T1, T2>(EventKey<T0, T1, T2> eventKey, T0 arg0, T1 arg1, T2 arg2)
        {
            Invoke((eventKey.Name, typeof(Action<T0, T1, T2>)), eventKey.Name, eventDelegate =>
            {
                (eventDelegate as Action<T0, T1, T2>)?.Invoke(arg0, arg1, arg2);
            });
        }

        /// <summary>
        /// 发布四参事件
        /// </summary>
        public void Publish<T0, T1, T2, T3>(EventKey<T0, T1, T2, T3> eventKey, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            Invoke((eventKey.Name, typeof(Action<T0, T1, T2, T3>)), eventKey.Name, eventDelegate =>
            {
                (eventDelegate as Action<T0, T1, T2, T3>)?.Invoke(arg0, arg1, arg2, arg3);
            });
        }

        /// <summary>
        /// 发布五参事件
        /// </summary>
        public void Publish<T0, T1, T2, T3, T4>(EventKey<T0, T1, T2, T3, T4> eventKey, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            Invoke((eventKey.Name, typeof(Action<T0, T1, T2, T3, T4>)), eventKey.Name, eventDelegate =>
            {
                (eventDelegate as Action<T0, T1, T2, T3, T4>)?.Invoke(arg0, arg1, arg2, arg3, arg4);
            });
        }

        /// <summary>
        /// 取消无参监听
        /// </summary>
        public void Unsubscribe(EventKey eventKey, Action action)
        {
            RemoveDelegate((eventKey.Name, typeof(Action)), action);
        }

        /// <summary>
        /// 取消单参监听
        /// </summary>
        public void Unsubscribe<T>(EventKey<T> eventKey, Action<T> action)
        {
            RemoveDelegate((eventKey.Name, typeof(Action<T>)), action);
        }

        /// <summary>
        /// 取消双参监听
        /// </summary>
        public void Unsubscribe<T0, T1>(EventKey<T0, T1> eventKey, Action<T0, T1> action)
        {
            RemoveDelegate((eventKey.Name, typeof(Action<T0, T1>)), action);
        }

        /// <summary>
        /// 取消三参监听
        /// </summary>
        public void Unsubscribe<T0, T1, T2>(EventKey<T0, T1, T2> eventKey, Action<T0, T1, T2> action)
        {
            RemoveDelegate((eventKey.Name, typeof(Action<T0, T1, T2>)), action);
        }

        /// <summary>
        /// 取消四参监听
        /// </summary>
        public void Unsubscribe<T0, T1, T2, T3>(EventKey<T0, T1, T2, T3> eventKey, Action<T0, T1, T2, T3> action)
        {
            RemoveDelegate((eventKey.Name, typeof(Action<T0, T1, T2, T3>)), action);
        }

        /// <summary>
        /// 取消五参监听
        /// </summary>
        public void Unsubscribe<T0, T1, T2, T3, T4>(EventKey<T0, T1, T2, T3, T4> eventKey, Action<T0, T1, T2, T3, T4> action)
        {
            RemoveDelegate((eventKey.Name, typeof(Action<T0, T1, T2, T3, T4>)), action);
        }

        /// <summary>
        /// 移除指定 Key 的全部监听
        /// </summary>
        public void RemoveAll(EventKey eventKey)
        {
            RemoveAllByName(eventKey.Name);
        }

        /// <summary>
        /// 移除指定 Key 的全部监听
        /// </summary>
        public void RemoveAll<T>(EventKey<T> eventKey)
        {
            RemoveAllByName(eventKey.Name);
        }

        /// <summary>
        /// 清空全部监听
        /// </summary>
        public void Clear()
        {
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
        public int GetListenerCount<T>(EventKey<T> eventKey)
        {
            return GetListenerCount((eventKey.Name, typeof(Action<T>)));
        }

        /// <summary>
        /// 获取指定 Key 的监听数量
        /// </summary>
        public int GetListenerCount<T0, T1>(EventKey<T0, T1> eventKey)
        {
            return GetListenerCount((eventKey.Name, typeof(Action<T0, T1>)));
        }

        /// <summary>
        /// 获取指定 Key 的监听数量
        /// </summary>
        public int GetListenerCount<T0, T1, T2>(EventKey<T0, T1, T2> eventKey)
        {
            return GetListenerCount((eventKey.Name, typeof(Action<T0, T1, T2>)));
        }

        /// <summary>
        /// 获取指定 Key 的监听数量
        /// </summary>
        public int GetListenerCount<T0, T1, T2, T3>(EventKey<T0, T1, T2, T3> eventKey)
        {
            return GetListenerCount((eventKey.Name, typeof(Action<T0, T1, T2, T3>)));
        }

        /// <summary>
        /// 获取指定 Key 的监听数量
        /// </summary>
        public int GetListenerCount<T0, T1, T2, T3, T4>(EventKey<T0, T1, T2, T3, T4> eventKey)
        {
            return GetListenerCount((eventKey.Name, typeof(Action<T0, T1, T2, T3, T4>)));
        }

        /// <summary>
        /// 获取全部已注册 Key 名称
        /// </summary>
        public List<string> GetRegisteredKeyNameList()
        {
            var nameList = new List<string>();
            foreach (var slotKey in eventDict.Keys)
            {
                if (!nameList.Contains(slotKey.name))
                    nameList.Add(slotKey.name);
            }

            nameList.Sort(StringComparer.Ordinal);
            return nameList;
        }

        /// <summary>
        /// 获取指定 Key 名称的监听总数
        /// </summary>
        public int GetListenerCountByName(string keyName)
        {
            int count = 0;
            foreach (KeyValuePair<(string name, Type handlerType), Delegate> pair in eventDict)
            {
                if (pair.Key.name != keyName)
                    continue;

                count += pair.Value.GetInvocationList().Length;
            }

            return count;
        }

        /// <summary>
        /// 合并委托
        /// </summary>
        private void CombineDelegate((string name, Type handlerType) slotKey, Delegate newDelegate)
        {
            if (eventDict.TryGetValue(slotKey, out Delegate existingEvent))
            {
                if (existingEvent.GetType() != newDelegate.GetType())
                {
                    EventBusLog.LogError(
                        $"事件 {slotKey.name} 委托类型冲突 已有 {existingEvent.GetType().Name} 尝试注册 {newDelegate.GetType().Name}");
                    return;
                }

                eventDict[slotKey] = Delegate.Combine(existingEvent, newDelegate);
            }
            else
            {
                eventDict[slotKey] = newDelegate;
            }
        }

        /// <summary>
        /// 触发事件
        /// </summary>
        private void Invoke((string name, Type handlerType) slotKey, string traceName, Action<Delegate> invoker)
        {
            if (!eventDict.TryGetValue(slotKey, out Delegate eventDelegate))
                return;

            TypedEventBusTrace.MarkTriggered(traceName);
            try
            {
                invoker(eventDelegate);
            }
            catch (Exception ex)
            {
                EventBusLog.LogError($"触发事件 {traceName} 失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 移除委托
        /// </summary>
        private void RemoveDelegate((string name, Type handlerType) slotKey, Delegate action)
        {
            if (!eventDict.TryGetValue(slotKey, out Delegate existingEvent))
                return;

            try
            {
                Delegate newEvent = Delegate.Remove(existingEvent, action);
                if (newEvent == null)
                    eventDict.Remove(slotKey);
                else
                    eventDict[slotKey] = newEvent;
            }
            catch (Exception ex)
            {
                EventBusLog.LogError($"删除事件 {slotKey.name} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取槽位监听数
        /// </summary>
        private int GetListenerCount((string name, Type handlerType) slotKey)
        {
            if (eventDict.TryGetValue(slotKey, out Delegate eventDelegate))
                return eventDelegate.GetInvocationList().Length;

            return 0;
        }

        /// <summary>
        /// 按名称移除全部槽位
        /// </summary>
        private void RemoveAllByName(string keyName)
        {
            var removeList = new List<(string name, Type handlerType)>();
            foreach (var slotKey in eventDict.Keys)
            {
                if (slotKey.name == keyName)
                    removeList.Add(slotKey);
            }

            for (int i = 0; i < removeList.Count; i++)
                eventDict.Remove(removeList[i]);
        }
    }
}
