using System;
using UnityEngine;

namespace MieMieFrameWork
{
    /// <summary>
    /// Unity 对象与组件的通用扩展方法
    /// </summary>
    public static class MmUnityExtension
    {
        /// <summary>
        /// 获取同物体的指定组件 不存在时添加并返回 不调用组件业务初始化
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var ExistingComponent = gameObject.GetComponent<T>();
            return ExistingComponent != null ? ExistingComponent : gameObject.AddComponent<T>();
        }

        /// <summary>
        /// 获取当前组件同物体的指定组件 不存在时添加并返回
        /// </summary>
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            return component.gameObject.GetOrAddComponent<T>();
        }

        /// <summary>
        /// 按接口或组件基类复用同物体组件 缺失时添加兼容的具体组件类型
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject, Type componentType) where T : class
        {
            if (!typeof(Component).IsAssignableFrom(componentType) || !typeof(T).IsAssignableFrom(componentType))
                throw new ArgumentException($"组件类型 {componentType} 必须继承 Component 并实现或继承 {typeof(T).FullName}", nameof(componentType));

            var ExistingComponent = gameObject.GetComponent<T>();
            if (ExistingComponent != null)
                return ExistingComponent;

            var AddedComponent = gameObject.AddComponent(componentType);
            if (AddedComponent == null)
                throw new InvalidOperationException($"添加组件 {componentType.FullName} 失败 目标 {gameObject.name}");

            return (T)(object)AddedComponent;
        }

        /// <summary>
        /// 按接口或组件基类获取当前物体组件 缺失时添加兼容的具体组件类型
        /// </summary>
        public static T GetOrAddComponent<T>(this Component component, Type componentType) where T : class
        {
            return component.gameObject.GetOrAddComponent<T>(componentType);
        }
    }
}
