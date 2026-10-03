using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MieMieFrameWork
{
    /// <summary>
    /// 事件类型枚举
    /// </summary>
    public enum SimpleEventType
    {
        // 鼠标/指针事件
        Click, PointerDown, PointerUp, PointerEnter, PointerExit,
        Drag, BeginDrag, EndDrag,
        
        // 物理事件
        CollisionEnter, CollisionExit, CollisionStay,
        TriggerEnter, TriggerExit, TriggerStay,
        
        // 2D物理事件
        CollisionEnter2D, CollisionExit2D, TriggerEnter2D, TriggerExit2D
    }

    /// <summary>
    /// 简化的Unity事件监听器
    /// 移除了复杂的参数系统和对象池，使用闭包和委托链
    /// </summary>
    public class SimpleEventListener : MonoBehaviour, 
        IPointerClickHandler, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler,
        IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        // 简单事件（无参数）
        private Dictionary<SimpleEventType, Action> simpleEvents = new();
        
        // 指针事件（带PointerEventData参数）
        private Dictionary<SimpleEventType, Action<PointerEventData>> pointerEvents = new();
        
        // 碰撞事件（带Collision参数）
        private Dictionary<SimpleEventType, Action<Collision>> collisionEvents = new();
        
        // 触发事件（带Collider参数）
        private Dictionary<SimpleEventType, Action<Collider>> triggerEvents = new();
        
        // 2D碰撞事件
        private Dictionary<SimpleEventType, Action<Collision2D>> collision2DEvents = new();
        
        // 2D触发事件
        private Dictionary<SimpleEventType, Action<Collider2D>> trigger2DEvents = new();

        #region 添加事件监听
        
        public void AddListener(SimpleEventType eventType, Action callback)
        {
            if (simpleEvents.ContainsKey(eventType))
                simpleEvents[eventType] += callback;
            else
                simpleEvents[eventType] = callback;
        }

        public void AddListener(SimpleEventType eventType, Action<PointerEventData> callback)
        {
            if (pointerEvents.ContainsKey(eventType))
                pointerEvents[eventType] += callback;
            else
                pointerEvents[eventType] = callback;
        }

        public void AddListener(SimpleEventType eventType, Action<Collision> callback)
        {
            if (collisionEvents.ContainsKey(eventType))
                collisionEvents[eventType] += callback;
            else
                collisionEvents[eventType] = callback;
        }

        public void AddListener(SimpleEventType eventType, Action<Collider> callback)
        {
            if (triggerEvents.ContainsKey(eventType))
                triggerEvents[eventType] += callback;
            else
                triggerEvents[eventType] = callback;
        }

        public void AddListener(SimpleEventType eventType, Action<Collision2D> callback)
        {
            if (collision2DEvents.ContainsKey(eventType))
                collision2DEvents[eventType] += callback;
            else
                collision2DEvents[eventType] = callback;
        }

        public void AddListener(SimpleEventType eventType, Action<Collider2D> callback)
        {
            if (trigger2DEvents.ContainsKey(eventType))
                trigger2DEvents[eventType] += callback;
            else
                trigger2DEvents[eventType] = callback;
        }

        #endregion

        #region 移除事件监听

        public void RemoveListener(SimpleEventType eventType, Action callback)
        {
            if (simpleEvents.ContainsKey(eventType))
                simpleEvents[eventType] -= callback;
        }

        public void RemoveListener(SimpleEventType eventType, Action<PointerEventData> callback)
        {
            if (pointerEvents.ContainsKey(eventType))
                pointerEvents[eventType] -= callback;
        }

        public void RemoveListener(SimpleEventType eventType, Action<Collision> callback)
        {
            if (collisionEvents.ContainsKey(eventType))
                collisionEvents[eventType] -= callback;
        }

        public void RemoveListener(SimpleEventType eventType, Action<Collider> callback)
        {
            if (triggerEvents.ContainsKey(eventType))
                triggerEvents[eventType] -= callback;
        }

        public void RemoveAllListeners()
        {
            simpleEvents.Clear();
            pointerEvents.Clear();
            collisionEvents.Clear();
            triggerEvents.Clear();
            collision2DEvents.Clear();
            trigger2DEvents.Clear();
        }

        public void RemoveAllListeners(SimpleEventType eventType)
        {
            simpleEvents.Remove(eventType);
            pointerEvents.Remove(eventType);
            collisionEvents.Remove(eventType);
            triggerEvents.Remove(eventType);
            collision2DEvents.Remove(eventType);
            trigger2DEvents.Remove(eventType);
        }

        #endregion

        #region Unity事件接口实现

        //无参通过simpleEvents管理 有参通过pointerEvents管理 根据使用情况分类
        public void OnPointerClick(PointerEventData eventData)
        {
            simpleEvents.TryGetValue(SimpleEventType.Click, out var simpleAction);
            simpleAction?.Invoke();
            
            pointerEvents.TryGetValue(SimpleEventType.Click, out var pointerAction);
            pointerAction?.Invoke(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            simpleEvents.TryGetValue(SimpleEventType.PointerDown, out var simpleAction);
            simpleAction?.Invoke();
            
            pointerEvents.TryGetValue(SimpleEventType.PointerDown, out var pointerAction);
            pointerAction?.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            simpleEvents.TryGetValue(SimpleEventType.PointerUp, out var simpleAction);
            simpleAction?.Invoke();
            
            pointerEvents.TryGetValue(SimpleEventType.PointerUp, out var pointerAction);
            pointerAction?.Invoke(eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            simpleEvents.TryGetValue(SimpleEventType.PointerEnter, out var simpleAction);
            simpleAction?.Invoke();
            
            pointerEvents.TryGetValue(SimpleEventType.PointerEnter, out var pointerAction);
            pointerAction?.Invoke(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            simpleEvents.TryGetValue(SimpleEventType.PointerExit, out var simpleAction);
            simpleAction?.Invoke();
            
            pointerEvents.TryGetValue(SimpleEventType.PointerExit, out var pointerAction);
            pointerAction?.Invoke(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            simpleEvents.TryGetValue(SimpleEventType.BeginDrag, out var simpleAction);
            simpleAction?.Invoke();
            
            pointerEvents.TryGetValue(SimpleEventType.BeginDrag, out var pointerAction);
            pointerAction?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            simpleEvents.TryGetValue(SimpleEventType.Drag, out var simpleAction);
            simpleAction?.Invoke();
            
            pointerEvents.TryGetValue(SimpleEventType.Drag, out var pointerAction);
            pointerAction?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            simpleEvents.TryGetValue(SimpleEventType.EndDrag, out var simpleAction);
            simpleAction?.Invoke();
            
            pointerEvents.TryGetValue(SimpleEventType.EndDrag, out var pointerAction);
            pointerAction?.Invoke(eventData);
        }

        #endregion

        #region Unity物理事件

        private void OnCollisionEnter(Collision collision)
        {
            simpleEvents.TryGetValue(SimpleEventType.CollisionEnter, out var simpleAction);
            simpleAction?.Invoke();
            
            collisionEvents.TryGetValue(SimpleEventType.CollisionEnter, out var collisionAction);
            collisionAction?.Invoke(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            simpleEvents.TryGetValue(SimpleEventType.CollisionExit, out var simpleAction);
            simpleAction?.Invoke();
            
            collisionEvents.TryGetValue(SimpleEventType.CollisionExit, out var collisionAction);
            collisionAction?.Invoke(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            simpleEvents.TryGetValue(SimpleEventType.CollisionStay, out var simpleAction);
            simpleAction?.Invoke();
            
            collisionEvents.TryGetValue(SimpleEventType.CollisionStay, out var collisionAction);
            collisionAction?.Invoke(collision);
        }

        private void OnTriggerEnter(Collider other)
        {
            simpleEvents.TryGetValue(SimpleEventType.TriggerEnter, out var simpleAction);
            simpleAction?.Invoke();
            
            triggerEvents.TryGetValue(SimpleEventType.TriggerEnter, out var triggerAction);
            triggerAction?.Invoke(other);
        }

        private void OnTriggerExit(Collider other)
        {
            simpleEvents.TryGetValue(SimpleEventType.TriggerExit, out var simpleAction);
            simpleAction?.Invoke();
            
            triggerEvents.TryGetValue(SimpleEventType.TriggerExit, out var triggerAction);
            triggerAction?.Invoke(other);
        }

        private void OnTriggerStay(Collider other)
        {
            simpleEvents.TryGetValue(SimpleEventType.TriggerStay, out var simpleAction);
            simpleAction?.Invoke();
            
            triggerEvents.TryGetValue(SimpleEventType.TriggerStay, out var triggerAction);
            triggerAction?.Invoke(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            simpleEvents.TryGetValue(SimpleEventType.CollisionEnter2D, out var simpleAction);
            simpleAction?.Invoke();
            
            collision2DEvents.TryGetValue(SimpleEventType.CollisionEnter2D, out var collisionAction);
            collisionAction?.Invoke(collision);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            simpleEvents.TryGetValue(SimpleEventType.CollisionExit2D, out var simpleAction);
            simpleAction?.Invoke();
            
            collision2DEvents.TryGetValue(SimpleEventType.CollisionExit2D, out var collisionAction);
            collisionAction?.Invoke(collision);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            simpleEvents.TryGetValue(SimpleEventType.TriggerEnter2D, out var simpleAction);
            simpleAction?.Invoke();
            
            trigger2DEvents.TryGetValue(SimpleEventType.TriggerEnter2D, out var triggerAction);
            triggerAction?.Invoke(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            simpleEvents.TryGetValue(SimpleEventType.TriggerExit2D, out var simpleAction);
            simpleAction?.Invoke();
            
            trigger2DEvents.TryGetValue(SimpleEventType.TriggerExit2D, out var triggerAction);
            triggerAction?.Invoke(other);
        }

        #endregion
    }
}
