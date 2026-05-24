namespace MieMieFrameWork.Pool
{
    using System;
    using UnityEngine;

    #region 对象池特性
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PoolAttribute : Attribute { }

    #endregion

    #region 对象池配置规则
    /// <summary>
    /// 对象池配置规则
    /// </summary>
    public static class PoolConfig
    {
        public static bool ShouldUsePool<T>() where T : class => ShouldUsePool(typeof(T));
        public static bool ShouldUsePool(Type type) => type.IsDefined(typeof(PoolAttribute), false);
    }
    #endregion

    #region 对象池相关的扩展方法

    public static class PoolExtensions
    {
        public static void PushGameObjectToPool(this GameObject obj)
        {
            ModuleHub.Instance.GetManager<PoolManager>().PushGameObj(obj);
        }
        public static void PushGameObjectToPool(this Component component)
        {
            ModuleHub.Instance.GetManager<PoolManager>().PushGameObj(component.gameObject);
        }
        public static void PushObjectToPool(this object obj)
        {
            ModuleHub.Instance.GetManager<PoolManager>().PushObject(obj);
        }
    }
    #endregion
}
