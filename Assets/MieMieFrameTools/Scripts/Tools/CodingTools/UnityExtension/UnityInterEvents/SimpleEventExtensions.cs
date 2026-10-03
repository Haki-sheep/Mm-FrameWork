using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MieMieFrameWork
{
    /// <summary>
    /// 简化的事件监听器扩展方法
    /// </summary>
    public static class SimpleEventExtensions
    {
        /// <summary>
        /// 获取或添加SimpleEventListener组件，用于监听事件 
        /// </summary>
        private static SimpleEventListener GetOrAddListener(Component component)
        {
            var listener = component.GetComponent<SimpleEventListener>();
            if (listener == null)
                listener = component.gameObject.AddComponent<SimpleEventListener>();
            return listener;
        }

        #region 鼠标/指针事件

        /// <summary>
        /// 添加点击事件监听（无参数版本）
        /// </summary>
        public static void OnClick(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.Click, callback);
        }

        /// <summary>
        /// 添加点击事件监听（带事件数据版本）
        /// </summary>
        public static void OnClick(this Component component, Action<PointerEventData> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.Click, callback);
        }

        /// <summary>
        /// 添加鼠标按下事件监听
        /// </summary>
        public static void OnPointerDown(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.PointerDown, callback);
        }

        public static void OnPointerDown(this Component component, Action<PointerEventData> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.PointerDown, callback);
        }

        /// <summary>
        /// 添加鼠标抬起事件监听
        /// </summary>
        public static void OnPointerUp(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.PointerUp, callback);
        }

        public static void OnPointerUp(this Component component, Action<PointerEventData> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.PointerUp, callback);
        }

        /// <summary>
        /// 添加鼠标进入事件监听
        /// </summary>
        public static void OnMouseEnter(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.PointerEnter, callback);
        }

        public static void OnMouseEnter(this Component component, Action<PointerEventData> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.PointerEnter, callback);
        }

        /// <summary>
        /// 添加鼠标离开事件监听
        /// </summary>
        public static void OnMouseExit(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.PointerExit, callback);
        }

        public static void OnMouseExit(this Component component, Action<PointerEventData> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.PointerExit, callback);
        }

        #endregion

        #region 拖拽事件

        /// <summary>
        /// 添加开始拖拽事件监听
        /// </summary>
        public static void OnBeginDrag(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.BeginDrag, callback);
        }

        public static void OnBeginDrag(this Component component, Action<PointerEventData> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.BeginDrag, callback);
        }

        /// <summary>
        /// 添加拖拽中事件监听
        /// </summary>
        public static void OnDrag(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.Drag, callback);
        }

        public static void OnDrag(this Component component, Action<PointerEventData> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.Drag, callback);
        }

        /// <summary>
        /// 添加结束拖拽事件监听
        /// </summary>
        public static void OnEndDrag(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.EndDrag, callback);
        }

        public static void OnEndDrag(this Component component, Action<PointerEventData> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.EndDrag, callback);
        }

        #endregion

        #region 3D物理事件

        /// <summary>
        /// 添加碰撞进入事件监听
        /// </summary>
        public static void OnCollisionEnter(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.CollisionEnter, callback);
        }

        public static void OnCollisionEnter(this Component component, Action<Collision> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.CollisionEnter, callback);
        }

        /// <summary>
        /// 添加碰撞离开事件监听
        /// </summary>
        public static void OnCollisionExit(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.CollisionExit, callback);
        }

        public static void OnCollisionExit(this Component component, Action<Collision> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.CollisionExit, callback);
        }

        /// <summary>
        /// 添加碰撞持续事件监听
        /// </summary>
        public static void OnCollisionStay(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.CollisionStay, callback);
        }

        public static void OnCollisionStay(this Component component, Action<Collision> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.CollisionStay, callback);
        }

        /// <summary>
        /// 添加触发进入事件监听
        /// </summary>
        public static void OnTriggerEnter(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.TriggerEnter, callback);
        }

        public static void OnTriggerEnter(this Component component, Action<Collider> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.TriggerEnter, callback);
        }

        /// <summary>
        /// 添加触发离开事件监听
        /// </summary>
        public static void OnTriggerExit(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.TriggerExit, callback);
        }

        public static void OnTriggerExit(this Component component, Action<Collider> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.TriggerExit, callback);
        }

        /// <summary>
        /// 添加触发持续事件监听
        /// </summary>
        public static void OnTriggerStay(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.TriggerStay, callback);
        }

        public static void OnTriggerStay(this Component component, Action<Collider> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.TriggerStay, callback);
        }

        #endregion

        #region 2D物理事件

        /// <summary>
        /// 添加2D碰撞进入事件监听
        /// </summary>
        public static void OnCollisionEnter2D(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.CollisionEnter2D, callback);
        }

        public static void OnCollisionEnter2D(this Component component, Action<Collision2D> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.CollisionEnter2D, callback);
        }

        /// <summary>
        /// 添加2D碰撞离开事件监听
        /// </summary>
        public static void OnCollisionExit2D(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.CollisionExit2D, callback);
        }

        public static void OnCollisionExit2D(this Component component, Action<Collision2D> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.CollisionExit2D, callback);
        }

        /// <summary>
        /// 添加2D触发进入事件监听
        /// </summary>
        public static void OnTriggerEnter2D(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.TriggerEnter2D, callback);
        }

        public static void OnTriggerEnter2D(this Component component, Action<Collider2D> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.TriggerEnter2D, callback);
        }

        /// <summary>
        /// 添加2D触发离开事件监听
        /// </summary>
        public static void OnTriggerExit2D(this Component component, Action callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.TriggerExit2D, callback);
        }

        public static void OnTriggerExit2D(this Component component, Action<Collider2D> callback)
        {
            GetOrAddListener(component).AddListener(SimpleEventType.TriggerExit2D, callback);
        }

        #endregion

        #region 移除事件监听

        /// <summary>
        /// 移除点击事件监听
        /// </summary>
        public static void RemoveClick(this Component component, Action callback)
        {
            var listener = component.GetComponent<SimpleEventListener>();
            listener?.RemoveListener(SimpleEventType.Click, callback);
        }

        public static void RemoveClick(this Component component, Action<PointerEventData> callback)
        {
            var listener = component.GetComponent<SimpleEventListener>();
            listener?.RemoveListener(SimpleEventType.Click, callback);
        }

        /// <summary>
        /// 移除鼠标进入事件监听
        /// </summary>
        public static void RemoveMouseEnter(this Component component, Action callback)
        {
            var listener = component.GetComponent<SimpleEventListener>();
            listener?.RemoveListener(SimpleEventType.PointerEnter, callback);
        }

        /// <summary>
        /// 移除鼠标离开事件监听
        /// </summary>
        public static void RemoveMouseExit(this Component component, Action callback)
        {
            var listener = component.GetComponent<SimpleEventListener>();
            listener?.RemoveListener(SimpleEventType.PointerExit, callback);
        }

        /// <summary>
        /// 移除所有事件监听
        /// </summary>
        public static void RemoveAllListeners(this Component component)
        {
            var listener = component.GetComponent<SimpleEventListener>();
            if (listener != null)
            {
                listener.RemoveAllListeners();
                UnityEngine.Object.DestroyImmediate(listener);
            }
        }

        /// <summary>
        /// 移除指定类型的所有事件监听
        /// </summary>
        public static void RemoveAllListeners(this Component component, SimpleEventType eventType)
        {
            var listener = component.GetComponent<SimpleEventListener>();
            listener?.RemoveAllListeners(eventType);
        }

        #endregion
    }
}
