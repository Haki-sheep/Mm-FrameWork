using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace MieMieFrameWork.Asset
{
    /// <summary>
    /// 缓存生命周期 不释放已经交付业务的句柄或全局资源包
    /// </summary>
    public sealed partial class YooAssetLruCache
    {
        #region 生命周期

        /// <summary>
        /// 清空缓存持有 既有加载仍可交付业务但不会重新填充缓存
        /// </summary>
        public void Clear()
        {
            RequireActive();
            cacheVersion++;
            var FailureList = new List<Exception>();
            ReleaseCachedHandles(FailureList);
            ThrowReleaseFailures(FailureList);
        }

        /// <summary>
        /// 取消未完成请求并释放自身持有 已交付业务的句柄继续有效
        /// </summary>
        public void Dispose()
        {
            CheckMainThread();
            if (disposed)
                return;
            disposed = true;
            cacheVersion++;
            var FailureList = new List<Exception>();
            try
            {
                lifetimeCancellation.Cancel();
            }
            catch (Exception Failure)
            {
                FailureList.Add(Failure);
            }
            ReleaseCachedHandles(FailureList);
            foreach (var Handle in pendingHandleHashList)
                TryRelease(Handle, FailureList);
            pendingHandleHashList.Clear();
            lifetimeCancellation.Dispose();
            ThrowReleaseFailures(FailureList);
        }

        /// <summary>
        /// 解除并释放全部缓存条目 不操作业务持有
        /// </summary>
        private void ReleaseCachedHandles(List<Exception> failureList)
        {
            while (leastRecentList.Count > 0)
                TryRelease(DetachEntry(leastRecentList.First), failureList);
        }

        /// <summary>
        /// 尝试释放本层持有 记录错误以继续清理其他资源
        /// </summary>
        private static void TryRelease(AssetHandle handle, List<Exception> failureList)
        {
            try
            {
                if (handle.IsValid)
                    handle.Release();
            }
            catch (Exception Failure)
            {
                failureList.Add(Failure);
            }
        }

        /// <summary>
        /// 完成必要清理后集中暴露全部释放错误
        /// </summary>
        private static void ThrowReleaseFailures(List<Exception> failureList)
        {
            if (failureList.Count > 0)
                throw new AggregateException("YooAsset 缓存持有清理失败", failureList);
        }

        /// <summary>
        /// 检查实例生命周期与原生资源接口的线程前置条件
        /// </summary>
        private void RequireActive()
        {
            CheckMainThread();
            if (disposed)
                throw new ObjectDisposedException(nameof(YooAssetLruCache));
        }

        /// <summary>
        /// 禁止在工作线程操作原生资源包和缓存状态
        /// </summary>
        private static void CheckMainThread()
        {
            if (!PlayerLoopHelper.IsMainThread)
                throw new InvalidOperationException("YooAsset LRU 缓存只能在 Unity 主线程使用");
        }

        #endregion
    }
}
