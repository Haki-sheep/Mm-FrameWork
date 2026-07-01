namespace MieMieFrameWork
{
    using System;
    using MiMieEventBus;

    /// <summary>
    /// 全局事件中心 仅支持 E_EventConstKey 枚举 Key
    /// </summary>
    public static class EventCenter
    {
        /// <summary>
        /// 全局总线实例
        /// </summary>
        private static readonly EventBus<E_EventConstKey> globalBus = new EventBus<E_EventConstKey>();

#if UNITY_EDITOR
        /// <summary>
        /// 最近触发的事件 Key
        /// </summary>
        public static E_EventConstKey LastTriggeredKey => EventBusTrace<E_EventConstKey>.LastKey;

        /// <summary>
        /// 最近触发时间
        /// </summary>
        public static float LastTriggeredTime => EventBusTrace<E_EventConstKey>.LastTime;
#endif

        #region 添加事件监听

        /// <summary>
        /// 添加无参数事件监听
        /// </summary>
        public static void AddEventListener(E_EventConstKey eventKey, Action action)
        {
            globalBus.AddEventListener(eventKey, action);
        }

        /// <summary>
        /// 添加单参数事件监听
        /// </summary>
        public static void AddEventListener<T>(E_EventConstKey eventKey, Action<T> action)
        {
            globalBus.AddEventListener(eventKey, action);
        }

        /// <summary>
        /// 添加双参数事件监听
        /// </summary>
        public static void AddEventListener<T0, T1>(E_EventConstKey eventKey, Action<T0, T1> action)
        {
            globalBus.AddEventListener(eventKey, action);
        }

        /// <summary>
        /// 添加三参数事件监听
        /// </summary>
        public static void AddEventListener<T0, T1, T2>(E_EventConstKey eventKey, Action<T0, T1, T2> action)
        {
            globalBus.AddEventListener(eventKey, action);
        }

        /// <summary>
        /// 添加四参数事件监听
        /// </summary>
        public static void AddEventListener<T0, T1, T2, T3>(E_EventConstKey eventKey, Action<T0, T1, T2, T3> action)
        {
            globalBus.AddEventListener(eventKey, action);
        }

        /// <summary>
        /// 添加五参数事件监听
        /// </summary>
        public static void AddEventListener<T0, T1, T2, T3, T4>(E_EventConstKey eventKey, Action<T0, T1, T2, T3, T4> action)
        {
            globalBus.AddEventListener(eventKey, action);
        }

        #endregion

        #region 触发事件

        /// <summary>
        /// 触发无参数事件
        /// </summary>
        public static void TriggerEvent(E_EventConstKey eventKey)
        {
            globalBus.TriggerEvent(eventKey);
        }

        /// <summary>
        /// 触发单参数事件
        /// </summary>
        public static void TriggerEvent<T>(E_EventConstKey eventKey, T arg)
        {
            globalBus.TriggerEvent(eventKey, arg);
        }

        /// <summary>
        /// 触发双参数事件
        /// </summary>
        public static void TriggerEvent<T0, T1>(E_EventConstKey eventKey, T0 arg0, T1 arg1)
        {
            globalBus.TriggerEvent(eventKey, arg0, arg1);
        }

        /// <summary>
        /// 触发三参数事件
        /// </summary>
        public static void TriggerEvent<T0, T1, T2>(E_EventConstKey eventKey, T0 arg0, T1 arg1, T2 arg2)
        {
            globalBus.TriggerEvent(eventKey, arg0, arg1, arg2);
        }

        /// <summary>
        /// 触发四参数事件
        /// </summary>
        public static void TriggerEvent<T0, T1, T2, T3>(E_EventConstKey eventKey, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            globalBus.TriggerEvent(eventKey, arg0, arg1, arg2, arg3);
        }

        /// <summary>
        /// 触发五参数事件
        /// </summary>
        public static void TriggerEvent<T0, T1, T2, T3, T4>(E_EventConstKey eventKey, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            globalBus.TriggerEvent(eventKey, arg0, arg1, arg2, arg3, arg4);
        }

        #endregion

        #region 删除事件监听

        /// <summary>
        /// 删除无参数事件的指定回调
        /// </summary>
        public static void RemoveListener(E_EventConstKey eventKey, Action action)
        {
            globalBus.RemoveListener(eventKey, action);
        }

        /// <summary>
        /// 删除单参数事件的指定回调
        /// </summary>
        public static void RemoveListener<T>(E_EventConstKey eventKey, Action<T> action)
        {
            globalBus.RemoveListener(eventKey, action);
        }

        /// <summary>
        /// 删除双参数事件的指定回调
        /// </summary>
        public static void RemoveListener<T0, T1>(E_EventConstKey eventKey, Action<T0, T1> action)
        {
            globalBus.RemoveListener(eventKey, action);
        }

        /// <summary>
        /// 删除三参数事件的指定回调
        /// </summary>
        public static void RemoveListener<T0, T1, T2>(E_EventConstKey eventKey, Action<T0, T1, T2> action)
        {
            globalBus.RemoveListener(eventKey, action);
        }

        /// <summary>
        /// 删除四参数事件的指定回调
        /// </summary>
        public static void RemoveListener<T0, T1, T2, T3>(E_EventConstKey eventKey, Action<T0, T1, T2, T3> action)
        {
            globalBus.RemoveListener(eventKey, action);
        }

        /// <summary>
        /// 删除五参数事件的指定回调
        /// </summary>
        public static void RemoveListener<T0, T1, T2, T3, T4>(E_EventConstKey eventKey, Action<T0, T1, T2, T3, T4> action)
        {
            globalBus.RemoveListener(eventKey, action);
        }

        /// <summary>
        /// 移除指定事件的所有回调
        /// </summary>
        public static void RemoveListener(E_EventConstKey eventKey)
        {
            globalBus.RemoveListener(eventKey);
        }

        /// <summary>
        /// 移除所有事件的所有回调
        /// </summary>
        public static void ClearAllListeners()
        {
            globalBus.Clear();
        }

        #endregion

        #region 调试工具

        /// <summary>
        /// 获取当前注册的事件数量
        /// </summary>
        public static int GetEventCount()
        {
            return globalBus.GetEventCount();
        }

        /// <summary>
        /// 检查事件是否已注册
        /// </summary>
        public static bool HasEvent(E_EventConstKey eventKey)
        {
            return globalBus.HasEvent(eventKey);
        }

        /// <summary>
        /// 获取指定事件的监听者数量
        /// </summary>
        public static int GetListenerCount(E_EventConstKey eventKey)
        {
            return globalBus.GetListenerCount(eventKey);
        }

        #endregion
    }
}
