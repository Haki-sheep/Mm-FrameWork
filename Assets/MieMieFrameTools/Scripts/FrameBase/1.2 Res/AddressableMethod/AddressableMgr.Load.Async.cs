namespace MieMieFrameWork
{
    using Cysharp.Threading.Tasks;
    using MieMieFrameWork.Pool;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    /// 异步加载
    /// </summary>
    public static partial class AddressableMgr
    {
        /// <summary>
        ///  异步加载组件 规则同同步 LoadComponent
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="usePool">是否使用对象池</param>
        /// <returns></returns>
        public static async UniTask<T> LoadComponentAsync<T>(string address, Transform parent = null, bool usePool = false) where T : Component
        {
            GameObject go = await LoadGameObjectAsync(address, parent, usePool);
            return go != null ? go.GetComponent<T>() : null;
        }

        /// <summary>
        ///  异步得到场景物体 规则同同步 LoadGameObject
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="parent">父物体</param>
        /// <param name="usePool">是否使用对象池</param>
        /// <returns></returns>
        public static async UniTask<GameObject> LoadGameObjectAsync(string address, Transform parent = null, bool usePool = false)
        {
            if (usePool)
            {
                GameObject prefab = await GetPoolPrefabAsync(address);
                return prefab != null ? ModuleHub.Instance.GetManager<PoolManager>().GetGameObj<GameObject>(prefab, parent) : null;
            }
            return await InstantiateAsyncInternal(address, parent);
        }

        /// <summary>
        ///  异步加载资源文件 加引用 用完 ReleaseAsset
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <returns></returns>
        public static async UniTask<T> LoadAssetAsync<T>(string address) where T : Object
        {
            await state.EnsureReadyAsync();
            return await LoadAssetAsyncInternal<T>(address, retainOnHit: true);
        }

        /// <summary>
        ///  按 key 或标签批量加载 句柄由 ClearAllCache 释放
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="keys">资源地址</param>
        /// <param name="mergeMode">合并模式</param>
        /// <returns></returns>
        public static async UniTask<List<T>> LoadAssetsAsync<T>(
            IList<object> keys,
            Addressables.MergeMode mergeMode = Addressables.MergeMode.Union) where T : Object
        {
            await state.EnsureReadyAsync();
            if (keys == null || keys.Count == 0) return new List<T>();

            var handle = Addressables.LoadAssetsAsync<T>(keys, null, mergeMode);
            await handle;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                state.AddBatchHandle(handle);
                return new List<T>(handle.Result);
            }
            if (handle.IsValid()) Addressables.Release(handle);
            return new List<T>();
        }

        /// <summary>
        ///  按名称列表和标签列表批量加载
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="names">资源名称列表</param>
        /// <param name="labels">资源标签列表</param>
        /// <param name="mergeMode">合并模式</param>
        /// <returns></returns>
        public static async UniTask<List<T>> LoadAssetsAsync<T>(
            IList<string> names,
            IList<string> labels,
            Addressables.MergeMode mergeMode = Addressables.MergeMode.Union) where T : Object
        {
            var keys = new List<object>();
            if (names != null)
            {
                for (int i = 0; i < names.Count; i++)
                    if (!string.IsNullOrEmpty(names[i])) keys.Add(names[i]);
            }
            if (labels != null)
            {
                for (int i = 0; i < labels.Count; i++)
                    if (!string.IsNullOrEmpty(labels[i])) keys.Add(labels[i]);
            }
            return await LoadAssetsAsync<T>(keys, mergeMode);
        }

        /// <summary>
        ///  异步实例化 不计引用 用完 DestroyObject
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="parent">父物体</param>
        /// <returns></returns>
        public static UniTask<GameObject> InstantiateAsync(string address, Transform parent = null)
            => InstantiateAsyncInternal(address, parent);

        #region 内部异步加载

        /// <summary>
        ///  获取对象池预制体
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <returns></returns>
        private static async UniTask<GameObject> GetPoolPrefabAsync(string address)
        {
            await state.EnsureReadyAsync();
            return await LoadAssetAsyncInternal<GameObject>(address, retainOnHit: false);
        }

        /// <summary>
        ///  异步加载资源文件
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="address">资源地址</param>
        /// <param name="retainOnHit">是否保留引用</param>
        /// <returns></returns>
        private static async UniTask<T> LoadAssetAsyncInternal<T>(string address, bool retainOnHit) where T : Object
        {
            /// 尝试获取缓存
            if (state.TryGetCached(address, retainOnHit, out T cached)) return cached;

            // 如果缓存没有 则尝试获取加载中的资源
            if (state.TryGetLoading(address, out var existing))
            {
                await existing;
                state.TryGetCached(address, retainOnHit, out cached);
                return cached;
            }   

            // 如果加载中的资源也没有 则确认加载资源 并存储到缓存
            var handle = Addressables.LoadAssetAsync<T>(address);
            state.TrackLoading(address, handle);
            await handle;
            state.UntrackLoading(address);

            // 如果加载失败 则释放句柄 并返回空
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                if (handle.IsValid()) Addressables.Release(handle);
                return null;
            }

            // 如果加载成功 则存储到缓存
            state.StoreAsset(address, handle.Result, handle);
            return handle.Result;
        }

        /// <summary>
        ///  异步实例化
        /// </summary>
        /// <param name="address">资源地址</param>
        /// <param name="parent">父物体</param>
        /// <returns></returns>
        private static async UniTask<GameObject> InstantiateAsyncInternal(string address, Transform parent)
        {
            await state.EnsureReadyAsync();
            var handle = Addressables.InstantiateAsync(address, parent);
            await handle;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[AddressableMgr] 异步实例化失败 {address}");
                return null;
            }
            PrepareInstance(handle.Result);
            return handle.Result;
        }

        #endregion
    }
}
