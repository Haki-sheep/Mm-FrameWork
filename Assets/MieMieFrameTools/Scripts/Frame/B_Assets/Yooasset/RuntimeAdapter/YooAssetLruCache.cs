using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using YooAsset;
using Object = UnityEngine.Object;

namespace MieMieFrameWork.Asset
{
    /// <summary>
    /// 按资源条目数保留近期资源 只释放缓存自己的原生句柄
    /// </summary>
    public sealed partial class YooAssetLruCache : IDisposable
    {
        /// <summary> 当前缓存绑定的原生资源包 </summary>
        private readonly ResourcePackage resourcePackage;

        /// <summary> 资源路径与加载类型对应的缓存条目 </summary>
        private readonly Dictionary<(string Path, Type AssetType), LinkedListNode<CacheEntry>> cacheEntryDict = new();

        /// <summary> 从最久未使用到最近使用排列的缓存条目 </summary>
        private readonly LinkedList<CacheEntry> leastRecentList = new();

        /// <summary> 尚未完成业务交付或缓存登记的原生句柄 </summary>
        private readonly HashSet<AssetHandle> pendingHandleHashList = new();

        /// <summary> 当前缓存实例的异步请求取消源 </summary>
        private readonly CancellationTokenSource lifetimeCancellation = new();

        /// <summary> 清空或移除后使此前请求失去缓存写入资格的版本 </summary>
        private ulong cacheVersion;

        /// <summary> 缓存实例是否已经释放 </summary>
        private bool disposed;

        public int Capacity { get; private set; }
        public int Count => cacheEntryDict.Count;
        public int PendingCount => pendingHandleHashList.Count;
        public long HitCount { get; private set; }
        public long MissCount { get; private set; }

        /// <summary>
        /// 接收资源包和条目容量 零容量只加载资源不保留缓存
        /// </summary>
        public YooAssetLruCache(ResourcePackage package, int capacity)
        {
            CheckMainThread();
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "缓存条目容量不能为负数");
            resourcePackage = package ?? throw new ArgumentNullException(nameof(package));
            Capacity = capacity;
        }

        /// <summary>
        /// 只保留缓存自己的句柄 不向外暴露该句柄
        /// </summary>
        private sealed class CacheEntry
        {
            public (string Path, Type AssetType) Key { get; }
            public AssetHandle Handle { get; }

            /// <summary>
            /// 绑定资源身份和缓存独占的句柄
            /// </summary>
            public CacheEntry(AssetInfo assetInfo, AssetHandle handle)
            {
                Key = (assetInfo.AssetPath, assetInfo.AssetType);
                Handle = handle;
            }
        }

        #region 缓存策略

        /// <summary>
        /// 调整条目容量并立即淘汰超额的缓存持有
        /// </summary>
        public void SetCapacity(int capacity)
        {
            RequireActive();
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "缓存条目容量不能为负数");
            Capacity = capacity;
            TrimToCapacity();
        }

        /// <summary>
        /// 查询指定类型的资源是否已缓存 不改变使用顺序
        /// </summary>
        public bool Contains<T>(string location) where T : Object
        {
            RequireActive();
            var Info = ResolveAssetInfo<T>(location);
            return cacheEntryDict.ContainsKey((Info.AssetPath, Info.AssetType));
        }

        /// <summary>
        /// 移除指定资源的缓存持有 不影响调用方句柄
        /// </summary>
        public bool Remove<T>(string location) where T : Object
        {
            RequireActive();
            var Info = ResolveAssetInfo<T>(location);
            cacheVersion++;
            if (!cacheEntryDict.TryGetValue((Info.AssetPath, Info.AssetType), out var Node))
                return false;
            DetachEntry(Node).Release();
            return true;
        }

        /// <summary>
        /// 记录请求进入时的命中情况 命中条目移动到最近使用位置
        /// </summary>
        private void RecordLookup(AssetInfo assetInfo)
        {
            if (cacheEntryDict.TryGetValue((assetInfo.AssetPath, assetInfo.AssetType), out var Node))
            {
                HitCount++;
                Touch(Node);
            }
            else
            {
                MissCount++;
            }
        }

        /// <summary>
        /// 将条目移到链表末尾 保持查询与更新为常数复杂度
        /// </summary>
        private void Touch(LinkedListNode<CacheEntry> node)
        {
            leastRecentList.Remove(node);
            leastRecentList.AddLast(node);
        }

        /// <summary>
        /// 解除缓存条目登记 返回仅属于缓存的句柄
        /// </summary>
        private AssetHandle DetachEntry(LinkedListNode<CacheEntry> node)
        {
            cacheEntryDict.Remove(node.Value.Key);
            leastRecentList.Remove(node);
            return node.Value.Handle;
        }

        /// <summary>
        /// 淘汰最久未使用的超额条目 所有释放错误集中抛出
        /// </summary>
        private void TrimToCapacity()
        {
            var FailureList = new List<Exception>();
            while (leastRecentList.Count > Capacity)
                TryRelease(DetachEntry(leastRecentList.First), FailureList);
            ThrowReleaseFailures(FailureList);
        }

        #endregion
    }
}
