namespace MieMieFrameWork
{
    using MieMieFrameWork.Pool;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    /// 同步加载 会卡主线程 适合启动或工具
    /// </summary>
    public static partial class AddressableMgr
    {
        /// <summary>
        ///  同步加载组件 usePool 为真从对象池取
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="usePool">是否使用对象池</param>
        /// <returns></returns>
        public static T LoadComponent<T>(string address, Transform parent = null, bool usePool = false) where T : Component
        {
            GameObject go = LoadGameObject(address, parent, usePool);
            return go != null ? go.GetComponent<T>() : null;
        }

        /// <summary>
        ///  同步得到场景物体 不用池则 DestroyObject 回收 用池则 ReturnToPool
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="usePool">是否使用对象池</param>
        /// <returns></returns>
        public static GameObject LoadGameObject(string address, Transform parent = null, bool usePool = false)
        {
            if (usePool)
            {
                GameObject prefab = GetPoolPrefab(address);
                return prefab != null ? ModuleHub.Instance.GetManager<PoolManager>().GetGameObj<GameObject>(prefab, parent) : null;
            }
            return Instantiate(address, parent);
        }

        /// <summary>
        ///  同步加载资源文件 进缓存并加引用 用完 ReleaseAsset
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <returns></returns>
        public static T LoadAsset<T>(string address) where T : Object
        {
            state.EnsureReadySync();
            return LoadAssetSync<T>(address, retainOnCacheHit: true);
        }

        /// <summary>
        ///  同步实例化 不计引用 用完 DestroyObject
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="parent">父物体</param>
        /// <returns></returns>
        public static GameObject Instantiate(string address, Transform parent = null)
        {
            state.EnsureReadySync();
            var handle = Addressables.InstantiateAsync(address, parent);
            GameObject result = handle.WaitForCompletion();
            if (result != null) PrepareInstance(result);
            else Debug.LogError($"[AddressableMgr] 无法实例化 {address}");
            return result;
        }

        #region 内部同步加载

        // 对象池用预制体 同一地址只在首次加载时加一次引用
        private static GameObject GetPoolPrefab(string address)
        {
            state.EnsureReadySync();
            return LoadAssetSync<GameObject>(address, retainOnCacheHit: false);
        }

        private static T LoadAssetSync<T>(string address, bool retainOnCacheHit) where T : Object
        {
            if (state.TryGetCached(address, retainOnCacheHit, out T cached)) return cached;

            if (state.TryGetLoading(address, out var existing))
            {
                existing.WaitForCompletion();
                state.TryGetCached(address, retainOnCacheHit, out cached);
                return cached;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            state.TrackLoading(address, handle);
            T result = handle.WaitForCompletion();
            state.UntrackLoading(address);

            if (result == null)
            {
                Debug.LogError($"[AddressableMgr] 加载失败 {address}");
                if (handle.IsValid()) Addressables.Release(handle);
                return null;
            }

            state.StoreAsset(address, result, handle);
            return result;
        }

        #endregion
    }
}
