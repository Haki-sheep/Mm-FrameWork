namespace MieMieFrameWork
{
    using Cysharp.Threading.Tasks;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    public static partial class AddressableMgr
    {
        public static async UniTask Init() => await state.EnsureReadyAsync();
        private static readonly AddressableState state = new();

        // 单条已加载资源
        private sealed class AssetSlot
        {
            public Object Asset;
            public AsyncOperationHandle Handle;
            public int RefCount;
        }

        // 内部状态 资源表 加载中 批量句柄 初始化
        private sealed class AddressableState
        {
            // 资源表
            readonly Dictionary<string, AssetSlot> assets = new();
            // 加载中
            readonly Dictionary<string, AsyncOperationHandle> loading = new();
            // 批量句柄
            readonly List<AsyncOperationHandle> batchHandles = new();

            // 是否初始化
            bool ready;
            // 初始化任务源
            UniTaskCompletionSource readyTcs;

            /// <summary>
            ///  异步初始化
            /// </summary>
            /// <returns></returns>
            public async UniTask EnsureReadyAsync()
            {
                if (ready) return;
                if (readyTcs == null)
                {
                    readyTcs = new UniTaskCompletionSource();
                    var init = Addressables.InitializeAsync();
                    await init;
                    if (init.Status != AsyncOperationStatus.Succeeded)
                        Debug.LogError("[AddressableMgr] Addressables 初始化失败");
                    ready = true;
                    readyTcs.TrySetResult();
                }
                else
                {
                    await readyTcs.Task;
                }
            }

            /// <summary>
            ///  同步初始化
            /// </summary>
            public void EnsureReadySync() => EnsureReadyAsync().GetAwaiter().GetResult();

            ///  尝试获取缓存
            /// </summary>
            /// <typeparam name="T">资源类型</typeparam>
            /// <param name="address">资源地址</param>
            /// <param name="retainOnHit">是否保留引用</param>
            /// <param name="asset">输出资源</param>
            /// <returns></returns>
            public bool TryGetCached<T>(string address, bool retainOnHit, out T asset) where T : Object
            {
                if (!assets.TryGetValue(address, out AssetSlot slot) || slot.Asset is not T typed)
                {
                    asset = null;
                    return false;
                }
                if (retainOnHit) slot.RefCount++;
                asset = typed;
                return true;
            }

            /// <summary>
            ///  尝试获取加载中
            /// </summary>
            /// <param name="address"></param>
            /// <param name="handle"></param>
            /// <returns></returns>
            public bool TryGetLoading(string address, out AsyncOperationHandle handle)
                => loading.TryGetValue(address, out handle);

            /// <summary>
            ///  跟踪加载中
            /// </summary>
            /// <param name="address"></param>
            /// <param name="handle"></param>
            public void TrackLoading(string address, AsyncOperationHandle handle)=> loading[address] = handle;

            /// <summary>
            ///  取消跟踪加载句柄
            /// </summary>
            /// <param name="address"></param>
            public void UntrackLoading(string address)=> loading.Remove(address);

            /// <summary>
            ///  存储资源
            /// </summary>
            /// <param name="address"></param>
            /// <param name="asset"></param>
            /// <param name="handle"></param>
            public void StoreAsset(string address, Object asset, AsyncOperationHandle handle)
            {
                assets[address] = new AssetSlot
                {
                    Asset = asset,
                    Handle = handle,
                    RefCount = 1
                };
            }

            /// <summary>
            ///  释放引用
            /// </summary>
            /// <param name="address">资源地址</param>
            public void ReleaseReference(string address)
            {
                if (!assets.TryGetValue(address, out AssetSlot slot)) return;
                if (--slot.RefCount > 0) return;
                if (slot.Handle.IsValid()) Addressables.Release(slot.Handle);
                assets.Remove(address);
            }

            /// <summary>
            ///  添加批量句柄
            /// </summary>
            /// <param name="handle">批量句柄</param>
            public void AddBatchHandle(AsyncOperationHandle handle)=> batchHandles.Add(handle);

            /// <summary>
            ///  清空所有缓存和批量句柄
            /// </summary>
            public void ClearAll()
            {
                foreach (AssetSlot slot in assets.Values)
                {
                    if (slot.Handle.IsValid()) Addressables.Release(slot.Handle);
                }
                assets.Clear();
                loading.Clear();

                for (int i = 0; i < batchHandles.Count; i++)
                {
                    if (batchHandles[i].IsValid()) Addressables.Release(batchHandles[i]);
                }
                batchHandles.Clear();
            }
        }

        // 去掉名字里的 Clone 后缀
        private static void PrepareInstance(GameObject go)
        {
            go.name = go.name.Replace("(Clone)", "");
        }
    }
}
