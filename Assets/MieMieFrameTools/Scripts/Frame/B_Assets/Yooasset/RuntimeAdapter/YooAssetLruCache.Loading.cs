using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;
using Object = UnityEngine.Object;

namespace MieMieFrameWork.Asset
{
    /// <summary>
    /// 缓存加载入口 每次成功返回调用方独立持有的原生句柄
    /// </summary>
    public sealed partial class YooAssetLruCache
    {
        #region 加载与持有

        /// <summary>
        /// 同步加载成功后保留缓存引用 返回必须由调用方释放的句柄
        /// </summary>
        public AssetHandle LoadAsset<T>(string location) where T : Object
        {
            RequireActive();
            var Info = ResolveAssetInfo<T>(location);
            RecordLookup(Info);
            ulong Version = cacheVersion;
            var Handle = resourcePackage.LoadAssetAsync(Info);
            pendingHandleHashList.Add(Handle);
            try
            {
                RequireActive();
                Handle.WaitForAsyncComplete();
                RequireActive();
                ValidateHandle<T>(Handle, location);
                Retain<T>(Info, Version);
                RequireActive();
                return Handle;
            }
            catch (Exception Failure)
            {
                ReleaseFailedRequest(Handle, Failure);
                throw;
            }
            finally
            {
                pendingHandleHashList.Remove(Handle);
            }
        }

        /// <summary>
        /// 异步加载成功后保留缓存引用 失败取消或退出释放未交付句柄
        /// </summary>
        public async UniTask<AssetHandle> LoadAssetAsync<T>(string location,
            CancellationToken cancellationToken = default, uint priority = 0) where T : Object
        {
            RequireActive();
            cancellationToken.ThrowIfCancellationRequested();
            var Info = ResolveAssetInfo<T>(location);
            RecordLookup(Info);
            ulong Version = cacheVersion;
            using var RequestCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, lifetimeCancellation.Token);
            var RequestToken = RequestCancellation.Token;
            var Handle = resourcePackage.LoadAssetAsync(Info, priority);
            pendingHandleHashList.Add(Handle);
            try
            {
                await UniTask.WaitUntil(() => disposed || Handle.IsDone, cancellationToken: RequestToken);
                RequestToken.ThrowIfCancellationRequested();
                ValidateHandle<T>(Handle, location);
                Retain<T>(Info, Version);
                if (disposed)
                    RequestToken.ThrowIfCancellationRequested();
                RequireActive();
                return Handle;
            }
            catch (Exception Failure)
            {
                ReleaseFailedRequest(Handle, Failure);
                throw;
            }
            finally
            {
                pendingHandleHashList.Remove(Handle);
            }
        }

        /// <summary>
        /// 通过原生清单解析路径与类型 无效地址立即暴露上下文
        /// </summary>
        private AssetInfo ResolveAssetInfo<T>(string location) where T : Object
        {
            var Info = resourcePackage.GetAssetInfo(location, typeof(T));
            if (!Info.IsValid)
                throw new InvalidOperationException($"YooAsset 缓存地址无效 包 {resourcePackage.PackageName} 地址 {location} 错误 {Info.Error}");
            return Info;
        }

        /// <summary>
        /// 校验原生加载状态与资源类型 失败不登记缓存
        /// </summary>
        private void ValidateHandle<T>(AssetHandle handle, string location) where T : Object
        {
            if (handle.Status != EOperationStatus.Succeeded || !(handle.AssetObject is T))
                throw new InvalidOperationException($"YooAsset 缓存加载失败 包 {resourcePackage.PackageName} 地址 {location} 类型 {typeof(T).FullName} 错误 {handle.Error}");
        }

        /// <summary>
        /// 为成功资源取得缓存独占句柄 清理前的旧请求不重新填充缓存
        /// </summary>
        private void Retain<T>(AssetInfo assetInfo, ulong version) where T : Object
        {
            if (disposed || version != cacheVersion || Capacity == 0)
                return;
            if (cacheEntryDict.TryGetValue((assetInfo.AssetPath, assetInfo.AssetType), out var Node))
            {
                Touch(Node);
                return;
            }

            // 原生资源已加载完成 再取得独立句柄只增加缓存自己的持有
            var Handle = resourcePackage.LoadAssetAsync(assetInfo);
            pendingHandleHashList.Add(Handle);
            try
            {
                RequireActive();
                ValidateHandle<T>(Handle, assetInfo.AssetPath);
                Node = leastRecentList.AddLast(new CacheEntry(assetInfo, Handle));
                cacheEntryDict.Add(Node.Value.Key, Node);
            }
            catch (Exception Failure)
            {
                ReleaseFailedRequest(Handle, Failure);
                throw;
            }
            finally
            {
                pendingHandleHashList.Remove(Handle);
            }
            TrimToCapacity();
        }

        /// <summary>
        /// 失败请求释放未交付句柄 清理失败同时保留原始错误
        /// </summary>
        private static void ReleaseFailedRequest(AssetHandle handle, Exception failure)
        {
            try
            {
                if (handle.IsValid)
                    handle.Release();
            }
            catch (Exception CleanupFailure)
            {
                throw new AggregateException("YooAsset 缓存请求失败且句柄清理失败", failure, CleanupFailure);
            }
        }

        #endregion
    }
}
