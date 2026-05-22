namespace MieMieFrameWork
{
    using MieMieFrameWork.Pool;
    using UnityEngine;
    using UnityEngine.AddressableAssets;

    /// <summary>
    /// 生命周期管理
    /// </summary>
    public static partial class AddressableMgr
    {
        /// <summary>
        ///  释放 LoadAsset 产生的引用 减到零会卸载资源
        /// </summary>
        /// <param name="address">资源地址</param>
        public static void ReleaseAsset(string address) => state.ReleaseReference(address);

        /// <summary>
        ///  销毁物体 先尝试 ReleaseInstance 失败则 Destroy
        /// </summary>
        /// <param name="obj">物体</param>
        public static void DestroyObject(GameObject obj)
        {
            if (obj == null) return;
            if (Addressables.ReleaseInstance(obj)) return;
            Object.Destroy(obj);
        }

        /// <summary>
        ///  回收物体 usePool 为真进池 否则等同 DestroyObject
        /// </summary>
        /// <param name="obj">物体</param>
        /// <param name="usePool">是否使用对象池</param>
        public static void ReturnToPool(GameObject obj, bool usePool = false)
        {
            if (obj == null) return;
            if (usePool) ModuleHub.Instance.GetManager<PoolManager>().PushGameObj(obj);
            else DestroyObject(obj);
        }

        /// <summary>
        ///  回收普通对象到对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="obj"></param>
        /// <param name="usePool">是否使用对象池</param>
        public static void ReturnToPool<T>(T obj, bool usePool = false) where T : class
        {
            if (usePool && obj != null) ModuleHub.Instance.GetManager<PoolManager>().PushObject(obj);
        }

        /// <summary>
        ///  切换场景时调用 释放资源缓存和批量句柄并清空物体池 场景里物体请先 DestroyObject
        /// </summary>
        public static void ClearAllCache()
        {
            state.ClearAll();
            if (ModuleHub.Instance.GetManager<PoolManager>() != null)
                ModuleHub.Instance.GetManager<PoolManager>().ClearAllGameObject();
        }
    }
}
