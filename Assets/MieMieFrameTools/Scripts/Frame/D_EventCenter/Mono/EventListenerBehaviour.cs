namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 事件监听基类 OnDisable 时自动反注册
    /// 支持全局总线、局部总线、使用子类 Bus 的 Listen
    /// 外部开发者的总线系统可以继承此基类
    /// </summary>
    public abstract class EventListenerBehaviour : MonoBehaviour
    {
        /// <summary>
        /// 反注册动作列表
        /// </summary>
        private List<Action> unregisterActionList = new();

        /// <summary>
        /// 局部总线 子类可重写以绑定局部 TypedEventBus
        /// </summary>
        protected virtual TypedEventBus Bus => null;

        #region 全局总线 Listen

        /// <summary>
        /// 监听无参数事件
        /// </summary>
        protected void Listen(EventKey eventKey, Action action)
        {
            MmGlobalEventBus.Bus.Subscribe(eventKey, action);
            unregisterActionList.Add(() => MmGlobalEventBus.Bus.Unsubscribe(eventKey, action));
        }

        /// <summary>
        /// 监听单参数事件
        /// </summary>
        protected void Listen<T>(EventKey<T> eventKey, Action<T> action)
        {
            MmGlobalEventBus.Bus.Subscribe(eventKey, action);
            unregisterActionList.Add(() => MmGlobalEventBus.Bus.Unsubscribe(eventKey, action));
        }

        /// <summary>
        /// 监听双参数事件
        /// </summary>
        protected void Listen<T0, T1>(EventKey<T0, T1> eventKey, Action<T0, T1> action)
        {
            MmGlobalEventBus.Bus.Subscribe(eventKey, action);
            unregisterActionList.Add(() => MmGlobalEventBus.Bus.Unsubscribe(eventKey, action));
        }

        /// <summary>
        /// 监听三参数事件
        /// </summary>
        protected void Listen<T0, T1, T2>(EventKey<T0, T1, T2> eventKey, Action<T0, T1, T2> action)
        {
            MmGlobalEventBus.Bus.Subscribe(eventKey, action);
            unregisterActionList.Add(() => MmGlobalEventBus.Bus.Unsubscribe(eventKey, action));
        }

        /// <summary>
        /// 监听四参数事件
        /// </summary>
        protected void Listen<T0, T1, T2, T3>(EventKey<T0, T1, T2, T3> eventKey, Action<T0, T1, T2, T3> action)
        {
            MmGlobalEventBus.Bus.Subscribe(eventKey, action);
            unregisterActionList.Add(() => MmGlobalEventBus.Bus.Unsubscribe(eventKey, action));
        }

        /// <summary>
        /// 监听五参数事件
        /// </summary>
        protected void Listen<T0, T1, T2, T3, T4>(EventKey<T0, T1, T2, T3, T4> eventKey, Action<T0, T1, T2, T3, T4> action)
        {
            MmGlobalEventBus.Bus.Subscribe(eventKey, action);
            unregisterActionList.Add(() => MmGlobalEventBus.Bus.Unsubscribe(eventKey, action));
        }

        #endregion

        #region 局部总线 Listen

        /// <summary>
        /// 监听无参数事件
        /// </summary>
        protected void Listen(TypedEventBus bus, EventKey eventKey, Action action)
        {
            bus.Subscribe(eventKey, action);
            unregisterActionList.Add(() => bus.Unsubscribe(eventKey, action));
        }

        /// <summary>
        /// 监听单参数事件
        /// </summary>
        protected void Listen<T>(TypedEventBus bus, EventKey<T> eventKey, Action<T> action)
        {
            bus.Subscribe(eventKey, action);
            unregisterActionList.Add(() => bus.Unsubscribe(eventKey, action));
        }

        /// <summary>
        /// 监听双参数事件
        /// </summary>
        protected void Listen<T0, T1>(TypedEventBus bus, EventKey<T0, T1> eventKey, Action<T0, T1> action)
        {
            bus.Subscribe(eventKey, action);
            unregisterActionList.Add(() => bus.Unsubscribe(eventKey, action));
        }

        /// <summary>
        /// 监听三参数事件
        /// </summary>
        protected void Listen<T0, T1, T2>(TypedEventBus bus, EventKey<T0, T1, T2> eventKey, Action<T0, T1, T2> action)
        {
            bus.Subscribe(eventKey, action);
            unregisterActionList.Add(() => bus.Unsubscribe(eventKey, action));
        }

        /// <summary>
        /// 监听四参数事件
        /// </summary>
        protected void Listen<T0, T1, T2, T3>(TypedEventBus bus, EventKey<T0, T1, T2, T3> eventKey, Action<T0, T1, T2, T3> action)
        {
            bus.Subscribe(eventKey, action);
            unregisterActionList.Add(() => bus.Unsubscribe(eventKey, action));
        }

        /// <summary>
        /// 监听五参数事件
        /// </summary>
        protected void Listen<T0, T1, T2, T3, T4>(TypedEventBus bus, EventKey<T0, T1, T2, T3, T4> eventKey, Action<T0, T1, T2, T3, T4> action)
        {
            bus.Subscribe(eventKey, action);
            unregisterActionList.Add(() => bus.Unsubscribe(eventKey, action));
        }

        #endregion

        #region 使用子类 Bus 的 Listen

        /// <summary>
        /// 监听无参数事件
        /// </summary>
        protected void ListenLocal(EventKey eventKey, Action action)
        {
            Listen(Bus, eventKey, action);
        }

        /// <summary>
        /// 监听单参数事件
        /// </summary>
        protected void ListenLocal<T>(EventKey<T> eventKey, Action<T> action)
        {
            Listen(Bus, eventKey, action);
        }

        /// <summary>
        /// 监听双参数事件
        /// </summary>
        protected void ListenLocal<T0, T1>(EventKey<T0, T1> eventKey, Action<T0, T1> action)
        {
            Listen(Bus, eventKey, action);
        }

        /// <summary>
        /// 监听三参数事件
        /// </summary>
        protected void ListenLocal<T0, T1, T2>(EventKey<T0, T1, T2> eventKey, Action<T0, T1, T2> action)
        {
            Listen(Bus, eventKey, action);
        }

        /// <summary>
        /// 监听四参数事件
        /// </summary>
        protected void ListenLocal<T0, T1, T2, T3>(EventKey<T0, T1, T2, T3> eventKey, Action<T0, T1, T2, T3> action)
        {
            Listen(Bus, eventKey, action);
        }

        /// <summary>
        /// 监听五参数事件
        /// </summary>
        protected void ListenLocal<T0, T1, T2, T3, T4>(EventKey<T0, T1, T2, T3, T4> eventKey, Action<T0, T1, T2, T3, T4> action)
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
