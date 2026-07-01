namespace MieMieFrameWork.Pool
{
    using UnityEngine;

    /// <summary>
    /// 对象池扩展方法
    /// </summary>
    public static class PoolExtensions
    {
        #region 公共方法 GameObject池

        /// <summary>
        /// 将 GameObject 放回对象池
        /// </summary>
        public static void PushGameObjectToPool(this GameObject obj)
        {
            ModuleHub.Instance.GetManager<PoolManager>().PushGameObj(obj);
        }

        /// <summary>
        /// 将 Component 对应 GameObject 放回对象池
        /// </summary>
        public static void PushGameObjectToPool(this Component component)
        {
            ModuleHub.Instance.GetManager<PoolManager>().PushGameObj(component.gameObject);
        }

        #endregion

        #region 公共方法 Object池

        /// <summary>
        /// 将普通对象放回对象池
        /// </summary>
        public static void PushObjectToPool(this object obj)
        {
            ModuleHub.Instance.GetManager<PoolManager>().PushObject(obj);
        }

        #endregion
    }
}
