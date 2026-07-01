namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 事件总线实例 支持局部总线与全局总线复用
    /// </summary>
    public class EventBus
    {
        /// <summary>
        /// 事件字典
        /// </summary>
        private Dictionary<int, Delegate> eventDict = new();

#if UNITY_EDITOR
        /// <summary>
        /// 最近一次触发的事件 Key
        /// </summary>
        public static E_EventConstKey LastTriggeredKey { get; private set; }

        /// <summary>
        /// 最近一次触发时间
        /// </summary>
        public static float LastTriggeredTime { get; private set; }
#endif

        /// <summary>
        /// 从枚举获取哈希值
        /// </summary>
        private static int GetHash(E_EventConstKey eventKey)
        {
            return eventKey.GetHashCode();
        }

        /// <summary>
        /// 注册委托时检测类型冲突
        /// </summary>
        private bool TryCombineDelegate(E_EventConstKey eventKey, int hash, Delegate newDelegate)
        {
            if (eventDict.TryGetValue(hash, out var existingEvent))
            {
                if (existingEvent.GetType() != newDelegate.GetType())
                {
                    Debug.LogError($"事件 {eventKey} 委托类型冲突 已有 {existingEvent.GetType().Name} 尝试注册 {newDelegate.GetType().Name}");
                    return false;
                }

                eventDict[hash] = Delegate.Combine(existingEvent, newDelegate);
            }
            else
            {
                eventDict[hash] = newDelegate;
            }

            return true;
        }

        /// <summary>
        /// 记录编辑器下最近一次触发
        /// </summary>
        private static void MarkTriggered(E_EventConstKey eventKey)
        {
#if UNITY_EDITOR
            LastTriggeredKey = eventKey;
            LastTriggeredTime = Time.realtimeSinceStartup;
#endif
        }

        #region 添加事件监听

        /// <summary>
        /// 添加无参数事件监听
        /// </summary>
        public void AddEventListener(E_EventConstKey eventKey, Action action)
        {
            int hash = GetHash(eventKey);
            TryCombineDelegate(eventKey, hash, action);
        }

        /// <summary>
        /// 添加单参数事件监听
        /// </summary>
        public void AddEventListener<T>(E_EventConstKey eventKey, Action<T> action)
        {
            int hash = GetHash(eventKey);
            TryCombineDelegate(eventKey, hash, action);
        }

        /// <summary>
        /// 添加双参数事件监听
        /// </summary>
        public void AddEventListener<T0, T1>(E_EventConstKey eventKey, Action<T0, T1> action)
        {
            int hash = GetHash(eventKey);
            TryCombineDelegate(eventKey, hash, action);
        }

        /// <summary>
        /// 添加三参数事件监听
        /// </summary>
        public void AddEventListener<T0, T1, T2>(E_EventConstKey eventKey, Action<T0, T1, T2> action)
        {
            int hash = GetHash(eventKey);
            TryCombineDelegate(eventKey, hash, action);
        }

        /// <summary>
        /// 添加四参数事件监听
        /// </summary>
        public void AddEventListener<T0, T1, T2, T3>(E_EventConstKey eventKey, Action<T0, T1, T2, T3> action)
        {
            int hash = GetHash(eventKey);
            TryCombineDelegate(eventKey, hash, action);
        }

        /// <summary>
        /// 添加五参数事件监听
        /// </summary>
        public void AddEventListener<T0, T1, T2, T3, T4>(E_EventConstKey eventKey, Action<T0, T1, T2, T3, T4> action)
        {
            int hash = GetHash(eventKey);
            TryCombineDelegate(eventKey, hash, action);
        }

        #endregion

        #region 触发事件

        /// <summary>
        /// 触发无参数事件
        /// </summary>
        public void TriggerEvent(E_EventConstKey eventKey)
        {
            int hash = GetHash(eventKey);
            if (!eventDict.TryGetValue(hash, out var eventDelegate))
                return;

            MarkTriggered(eventKey);
            try
            {
                (eventDelegate as Action)?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"触发事件 {eventKey} 失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 触发单参数事件
        /// </summary>
        public void TriggerEvent<T>(E_EventConstKey eventKey, T arg)
        {
            int hash = GetHash(eventKey);
            if (!eventDict.TryGetValue(hash, out var eventDelegate))
                return;

            MarkTriggered(eventKey);
            try
            {
                (eventDelegate as Action<T>)?.Invoke(arg);
            }
            catch (Exception ex)
            {
                Debug.LogError($"触发事件 {eventKey} 失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 触发双参数事件
        /// </summary>
        public void TriggerEvent<T0, T1>(E_EventConstKey eventKey, T0 arg0, T1 arg1)
        {
            int hash = GetHash(eventKey);
            if (!eventDict.TryGetValue(hash, out var eventDelegate))
                return;

            MarkTriggered(eventKey);
            try
            {
                (eventDelegate as Action<T0, T1>)?.Invoke(arg0, arg1);
            }
            catch (Exception ex)
            {
                Debug.LogError($"触发事件 {eventKey} 失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 触发三参数事件
        /// </summary>
        public void TriggerEvent<T0, T1, T2>(E_EventConstKey eventKey, T0 arg0, T1 arg1, T2 arg2)
        {
            int hash = GetHash(eventKey);
            if (!eventDict.TryGetValue(hash, out var eventDelegate))
                return;

            MarkTriggered(eventKey);
            try
            {
                (eventDelegate as Action<T0, T1, T2>)?.Invoke(arg0, arg1, arg2);
            }
            catch (Exception ex)
            {
                Debug.LogError($"触发事件 {eventKey} 失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 触发四参数事件
        /// </summary>
        public void TriggerEvent<T0, T1, T2, T3>(E_EventConstKey eventKey, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            int hash = GetHash(eventKey);
            if (!eventDict.TryGetValue(hash, out var eventDelegate))
                return;

            MarkTriggered(eventKey);
            try
            {
                (eventDelegate as Action<T0, T1, T2, T3>)?.Invoke(arg0, arg1, arg2, arg3);
            }
            catch (Exception ex)
            {
                Debug.LogError($"触发事件 {eventKey} 失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 触发五参数事件
        /// </summary>
        public void TriggerEvent<T0, T1, T2, T3, T4>(E_EventConstKey eventKey, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            int hash = GetHash(eventKey);
            if (!eventDict.TryGetValue(hash, out var eventDelegate))
                return;

            MarkTriggered(eventKey);
            try
            {
                (eventDelegate as Action<T0, T1, T2, T3, T4>)?.Invoke(arg0, arg1, arg2, arg3, arg4);
            }
            catch (Exception ex)
            {
                Debug.LogError($"触发事件 {eventKey} 失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        #endregion

        #region 删除事件监听

        /// <summary>
        /// 删除无参数事件的指定回调
        /// </summary>
        public void RemoveListener(E_EventConstKey eventKey, Action action)
        {
            int hash = GetHash(eventKey);
            if (!eventDict.TryGetValue(hash, out var existingEvent))
                return;

            try
            {
                var newEvent = Delegate.Remove(existingEvent, action);
                if (newEvent == null)
                    eventDict.Remove(hash);
                else
                    eventDict[hash] = newEvent;
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除事件 {eventKey} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除单参数事件的指定回调
        /// </summary>
        public void RemoveListener<T>(E_EventConstKey eventKey, Action<T> action)
        {
            int hash = GetHash(eventKey);
            if (!eventDict.TryGetValue(hash, out var existingEvent))
                return;

            try
            {
                var newEvent = Delegate.Remove(existingEvent, action);
                if (newEvent == null)
                    eventDict.Remove(hash);
                else
                    eventDict[hash] = newEvent;
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除事件 {eventKey} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除双参数事件的指定回调
        /// </summary>
        public void RemoveListener<T0, T1>(E_EventConstKey eventKey, Action<T0, T1> action)
        {
            int hash = GetHash(eventKey);
            if (!eventDict.TryGetValue(hash, out var existingEvent))
                return;

            try
            {
                var newEvent = Delegate.Remove(existingEvent, action);
                if (newEvent == null)
                    eventDict.Remove(hash);
                else
                    eventDict[hash] = newEvent;
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除事件 {eventKey} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除三参数事件的指定回调
        /// </summary>
        public void RemoveListener<T0, T1, T2>(E_EventConstKey eventKey, Action<T0, T1, T2> action)
        {
            int hash = GetHash(eventKey);
            if (!eventDict.TryGetValue(hash, out var existingEvent))
                return;

            try
            {
                var newEvent = Delegate.Remove(existingEvent, action);
                if (newEvent == null)
                    eventDict.Remove(hash);
                else
                    eventDict[hash] = newEvent;
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除事件 {eventKey} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除四参数事件的指定回调
        /// </summary>
        public void RemoveListener<T0, T1, T2, T3>(E_EventConstKey eventKey, Action<T0, T1, T2, T3> action)
        {
            int hash = GetHash(eventKey);
            if (!eventDict.TryGetValue(hash, out var existingEvent))
                return;

            try
            {
                var newEvent = Delegate.Remove(existingEvent, action);
                if (newEvent == null)
                    eventDict.Remove(hash);
                else
                    eventDict[hash] = newEvent;
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除事件 {eventKey} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除五参数事件的指定回调
        /// </summary>
        public void RemoveListener<T0, T1, T2, T3, T4>(E_EventConstKey eventKey, Action<T0, T1, T2, T3, T4> action)
        {
            int hash = GetHash(eventKey);
            if (!eventDict.TryGetValue(hash, out var existingEvent))
                return;

            try
            {
                var newEvent = Delegate.Remove(existingEvent, action);
                if (newEvent == null)
                    eventDict.Remove(hash);
                else
                    eventDict[hash] = newEvent;
            }
            catch (Exception ex)
            {
                Debug.LogError($"删除事件 {eventKey} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 移除指定事件的所有回调
        /// </summary>
        public void RemoveListener(E_EventConstKey eventKey)
        {
            int hash = GetHash(eventKey);
            eventDict.Remove(hash);
        }

        /// <summary>
        /// 清空所有事件回调
        /// </summary>
        public void Clear()
        {
            eventDict.Clear();
        }

        #endregion

        #region 调试工具

        /// <summary>
        /// 获取当前注册的事件数量
        /// </summary>
        public int GetEventCount()
        {
            return eventDict.Count;
        }

        /// <summary>
        /// 检查事件是否已注册
        /// </summary>
        public bool HasEvent(E_EventConstKey eventKey)
        {
            int hash = GetHash(eventKey);
            return eventDict.ContainsKey(hash);
        }

        /// <summary>
        /// 获取指定事件的监听者数量
        /// </summary>
        public int GetListenerCount(E_EventConstKey eventKey)
        {
            int hash = GetHash(eventKey);
            if (eventDict.TryGetValue(hash, out var eventDelegate))
                return eventDelegate.GetInvocationList().Length;

            return 0;
        }

        #endregion
    }
}
