namespace MieMieFrameWork.Effects
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;

    public sealed partial class EffectManager
    {
        #region 加载与预热

        /// <summary>
        /// 查找配置并按地址持有单份原生资源句柄
        /// </summary>
        private EffectResourceEntry GetResource(string id)
        {
            if (!definitionDict.TryGetValue(id, out var Definition))
                throw new InvalidOperationException($"[EffectManager] 未配置特效 {id}");
            if (resourceDict.TryGetValue(id, out var Entry))
                return Entry;
            Entry = new EffectResourceEntry(Definition);
            resourceDict.Add(id, Entry);
            return Entry;
        }

        /// <summary>
        /// 共享加载不共享取消 单个请求取消不会影响其他请求
        /// </summary>
        private async UniTask EnsureLoadedAsync(EffectResourceEntry entry, CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (entry.Retired)
                    throw new OperationCanceledException(token);
                if (entry.Asset.IsDone)
                    break;
                await UniTask.NextFrame(cancellationToken: token);
            }
            entry.CreatePool(poolManager);
        }

        /// <summary>
        /// 预热配置中的闲置数量 不占用播放预算 与生命周期一起取消
        /// </summary>
        public async UniTask PrewarmAsync(string id, CancellationToken cancellationToken = default)
        {
            CheckReady();
            cancellationToken.ThrowIfCancellationRequested();
            var Entry = GetResource(id);
            Entry.Requests++;
            using var Cancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, lifetimeCancellation.Token);
            try
            {
                await EnsureLoadedAsync(Entry, Cancellation.Token);
                while (Entry.Pool.PooledCount < Entry.Definition.PrewarmCount &&
                    Entry.Pool.TotalCreated < Entry.Definition.MaxConcurrent)
                {
                    await ReserveSpawnAsync(Cancellation.Token);
                    if (Entry.Pool.PooledCount < Entry.Definition.PrewarmCount &&
                        Entry.Pool.TotalCreated < Entry.Definition.MaxConcurrent)
                        Entry.Pool.Prewarm(1);
                }
            }
            catch
            {
                Entry.Requests--;
                RetireIfUnused(Entry);
                throw;
            }
            Entry.Requests--;
        }

        #endregion

        /// <summary>
        /// 限制加载集中完成后的实际实例启动和显式预热峰值
        /// </summary>
        private async UniTask ReserveSpawnAsync(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (spawnFrame != UnityEngine.Time.frameCount)
                {
                    spawnFrame = UnityEngine.Time.frameCount;
                    frameSpawns = 0;
                }
                if (frameSpawns < maxPerFrame)
                {
                    frameSpawns++;
                    return;
                }
                await UniTask.NextFrame(cancellationToken: token);
            }
        }

        #region 缓存与诊断

        /// <summary>
        /// 仅清理没有播放或预热请求的资源 活跃实例不受影响
        /// </summary>
        public void ClearUnused()
        {
            CheckReady();
            var EntryList = new List<EffectResourceEntry>(resourceDict.Values);
            foreach (var Entry in EntryList)
                RetireIfUnused(Entry);
        }

        /// <summary>
        /// 空闲条目注销池 旧资源在实例真正销毁前继续持有
        /// </summary>
        private void RetireIfUnused(EffectResourceEntry entry)
        {
            if (entry.Requests != 0 || entry.Retired)
                return;
            resourceDict.Remove(entry.Definition.Id);
            retiredList.Add(entry);
            entry.Retire(poolManager);
        }

        /// <summary>
        /// 填充调用方复用的诊断列表 不在每帧创建快照
        /// </summary>
        public void CopyDiagnostics(List<EffectDiagnostics> targetList)
        {
            targetList.Clear();
            foreach (var Entry in resourceDict.Values)
                targetList.Add(new EffectDiagnostics(Entry.Definition.Id, Entry.Definition.Location, Entry.Requests,
                    Entry.Pool?.ActiveCount ?? 0, Entry.Pool?.PooledCount ?? 0, Entry.Pool != null));
        }

        #endregion
    }
}
