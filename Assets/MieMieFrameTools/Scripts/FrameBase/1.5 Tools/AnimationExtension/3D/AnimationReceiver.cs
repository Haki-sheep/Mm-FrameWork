using System;
using System.Collections.Generic;
using UnityEngine;

namespace MieMieFrameWork.MMAnimation
{
    public enum E_AniamtionParamType
    {
        None,
        Int,
        Float,
        String,
        Object
    }

    public class AnimationReceiver : MonoBehaviour
    {
        /// <summary>
        /// 动画事件名列表
        /// </summary>
        private List<string> animationEventList = new();

        public List<string> AnimationEventList => animationEventList;

        /// <summary>
        /// 内部事件字典
        /// </summary>
        private Dictionary<string, Delegate> eventDict = new();

        /// <summary>
        /// 销毁时清空
        /// </summary>
        private void OnDestroy()
        {
            eventDict.Clear();
            animationEventList.Clear();
        }

        #region 加减事件

        /// <summary>
        /// 添加无参数动画事件
        /// </summary>
        public void AddAnimationEvent(string eventName, Action action)
        {
            if (animationEventList.Contains(eventName))
            {
                Debug.LogError($"AnimationEvent {eventName} already added");
                return;
            }

            animationEventList.Add(eventName);
            eventDict[eventName] = action;
        }

        /// <summary>
        /// 移除无参数动画事件
        /// </summary>
        public void RemoveAnimationEvent(string eventName, Action action)
        {
            if (!animationEventList.Contains(eventName))
            {
                Debug.LogError($"AnimationEvent {eventName} not found");
                return;
            }

            animationEventList.Remove(eventName);
            if (eventDict.TryGetValue(eventName, out var existing))
            {
                var newDelegate = Delegate.Remove(existing, action);
                if (newDelegate == null)
                    eventDict.Remove(eventName);
                else
                    eventDict[eventName] = newDelegate;
            }
        }

        /// <summary>
        /// 添加单参数动画事件
        /// </summary>
        public void AddAnimationEvent<T>(string eventName, Action<T> action)
        {
            if (animationEventList.Contains(eventName))
            {
                Debug.LogError($"AnimationEvent {eventName} already added");
                return;
            }

            animationEventList.Add(eventName);
            eventDict[eventName] = action;
        }

        /// <summary>
        /// 移除单参数动画事件
        /// </summary>
        public void RemoveAnimationEvent<T>(string eventName, Action<T> action)
        {
            if (!animationEventList.Contains(eventName))
            {
                Debug.LogError($"AnimationEvent {eventName} not found");
                return;
            }

            animationEventList.Remove(eventName);
            if (eventDict.TryGetValue(eventName, out var existing))
            {
                var newDelegate = Delegate.Remove(existing, action);
                if (newDelegate == null)
                    eventDict.Remove(eventName);
                else
                    eventDict[eventName] = newDelegate;
            }
        }

        #endregion

        #region 触发事件

        /// <summary>
        /// 触发无参数动画事件
        /// </summary>
        public void OnAnimationEventTriggered(string eventName)
        {
            if (!eventDict.TryGetValue(eventName, out var eventDelegate))
                return;

            (eventDelegate as Action)?.Invoke();
        }

        /// <summary>
        /// 触发 Int 参数动画事件
        /// </summary>
        public void OnIntAnimationEventTriggered(string eventName, int value)
        {
            if (!eventDict.TryGetValue(eventName, out var eventDelegate))
                return;

            (eventDelegate as Action<int>)?.Invoke(value);
        }

        /// <summary>
        /// 触发 Float 参数动画事件
        /// </summary>
        public void OnFloatAnimationEventTriggered(string eventName, float value)
        {
            if (!eventDict.TryGetValue(eventName, out var eventDelegate))
                return;

            (eventDelegate as Action<float>)?.Invoke(value);
        }

        /// <summary>
        /// 触发 String 参数动画事件
        /// </summary>
        public void OnStringAnimationEventTriggered(string eventName, string value)
        {
            if (!eventDict.TryGetValue(eventName, out var eventDelegate))
                return;

            (eventDelegate as Action<string>)?.Invoke(value);
        }

        /// <summary>
        /// 触发 Object 参数动画事件
        /// </summary>
        public void OnObjectAnimationEventTriggered(string eventName, object value)
        {
            if (!eventDict.TryGetValue(eventName, out var eventDelegate))
                return;

            (eventDelegate as Action<object>)?.Invoke(value);
        }

        #endregion
    }
}
