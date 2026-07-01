namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 事件监听基类 OnDisable 时自动反注册
    /// </summary>
    public abstract class EventListenerBehaviour : MonoBehaviour
    {
        /// <summary>
        /// 反注册动作列表
        /// </summary>
        private List<Action> unregisterActionList = new();

        /// <summary>
        /// 局部总线 子类可重写以绑定局部 EventBus
        /// </summary>
        protected virtual EventBus Bus => null;

        #region 全局总线 Listen

        /// <summary>
        /// 监听无参数事件
        /// </summary>
        protected void Listen(E_EventConstKey eventKey, Action action)
        {
            EventCenter.AddEventListener(eventKey, action);
            unregisterActionList.Add(() => EventCenter.RemoveListener(eventKey, action));
        }

        /// <summary>
        /// 监听单参数事件
        /// </summary>
        protected void Listen<T>(E_EventConstKey eventKey, Action<T> action)
        {
            EventCenter.AddEventListener(eventKey, action);
            unregisterActionList.Add(() => EventCenter.RemoveListener(eventKey, action));
        }

        /// <summary>
        /// 监听双参数事件
        /// </summary>
        protected void Listen<T0, T1>(E_EventConstKey eventKey, Action<T0, T1> action)
        {
            EventCenter.AddEventListener(eventKey, action);
            unregisterActionList.Add(() => EventCenter.RemoveListener(eventKey, action));
        }

        /// <summary>
        /// 监听三参数事件
        /// </summary>
        protected void Listen<T0, T1, T2>(E_EventConstKey eventKey, Action<T0, T1, T2> action)
        {
            EventCenter.AddEventListener(eventKey, action);
            unregisterActionList.Add(() => EventCenter.RemoveListener(eventKey, action));
        }

        /// <summary>
        /// 监听四参数事件
        /// </summary>
        protected void Listen<T0, T1, T2, T3>(E_EventConstKey eventKey, Action<T0, T1, T2, T3> action)
        {
            EventCenter.AddEventListener(eventKey, action);
            unregisterActionList.Add(() => EventCenter.RemoveListener(eventKey, action));
        }

        /// <summary>
        /// 监听五参数事件
        /// </summary>
        protected void Listen<T0, T1, T2, T3, T4>(E_EventConstKey eventKey, Action<T0, T1, T2, T3, T4> action)
        {
            EventCenter.AddEventListener(eventKey, action);
            unregisterActionList.Add(() => EventCenter.RemoveListener(eventKey, action));
        }

        #endregion

        #region 局部总线 Listen

        /// <summary>
        /// 监听无参数事件
        /// </summary>
        protected void Listen(EventBus bus, E_EventConstKey eventKey, Action action)
        {
            bus.AddEventListener(eventKey, action);
            unregisterActionList.Add(() => bus.RemoveListener(eventKey, action));
        }

        /// <summary>
        /// 监听单参数事件
        /// </summary>
        protected void Listen<T>(EventBus bus, E_EventConstKey eventKey, Action<T> action)
        {
            bus.AddEventListener(eventKey, action);
            unregisterActionList.Add(() => bus.RemoveListener(eventKey, action));
        }

        /// <summary>
        /// 监听双参数事件
        /// </summary>
        protected void Listen<T0, T1>(EventBus bus, E_EventConstKey eventKey, Action<T0, T1> action)
        {
            bus.AddEventListener(eventKey, action);
            unregisterActionList.Add(() => bus.RemoveListener(eventKey, action));
        }

        /// <summary>
        /// 监听三参数事件
        /// </summary>
        protected void Listen<T0, T1, T2>(EventBus bus, E_EventConstKey eventKey, Action<T0, T1, T2> action)
        {
            bus.AddEventListener(eventKey, action);
            unregisterActionList.Add(() => bus.RemoveListener(eventKey, action));
        }

        /// <summary>
        /// 监听四参数事件
        /// </summary>
        protected void Listen<T0, T1, T2, T3>(EventBus bus, E_EventConstKey eventKey, Action<T0, T1, T2, T3> action)
        {
            bus.AddEventListener(eventKey, action);
            unregisterActionList.Add(() => bus.RemoveListener(eventKey, action));
        }

        /// <summary>
        /// 监听五参数事件
        /// </summary>
        protected void Listen<T0, T1, T2, T3, T4>(EventBus bus, E_EventConstKey eventKey, Action<T0, T1, T2, T3, T4> action)
        {
            bus.AddEventListener(eventKey, action);
            unregisterActionList.Add(() => bus.RemoveListener(eventKey, action));
        }

        #endregion

        #region 使用子类 Bus 的 Listen

        /// <summary>
        /// 监听无参数事件
        /// </summary>
        protected void ListenLocal(E_EventConstKey eventKey, Action action)
        {
            Listen(Bus, eventKey, action);
        }

        /// <summary>
        /// 监听单参数事件
        /// </summary>
        protected void ListenLocal<T>(E_EventConstKey eventKey, Action<T> action)
        {
            Listen(Bus, eventKey, action);
        }

        /// <summary>
        /// 监听双参数事件
        /// </summary>
        protected void ListenLocal<T0, T1>(E_EventConstKey eventKey, Action<T0, T1> action)
        {
            Listen(Bus, eventKey, action);
        }

        /// <summary>
        /// 监听三参数事件
        /// </summary>
        protected void ListenLocal<T0, T1, T2>(E_EventConstKey eventKey, Action<T0, T1, T2> action)
        {
            Listen(Bus, eventKey, action);
        }

        /// <summary>
        /// 监听四参数事件
        /// </summary>
        protected void ListenLocal<T0, T1, T2, T3>(E_EventConstKey eventKey, Action<T0, T1, T2, T3> action)
        {
            Listen(Bus, eventKey, action);
        }

        /// <summary>
        /// 监听五参数事件
        /// </summary>
        protected void ListenLocal<T0, T1, T2, T3, T4>(E_EventConstKey eventKey, Action<T0, T1, T2, T3, T4> action)
        {
            Listen(Bus, eventKey, action);
        }

        #endregion

        /// <summary>
        /// OnDisable 时执行所有反注册
        /// </summary>
        private void OnDisable()
        {
            for (int i = unregisterActionList.Count - 1; i >= 0; i--)
                unregisterActionList[i]?.Invoke();

            unregisterActionList.Clear();
        }
    }
}
