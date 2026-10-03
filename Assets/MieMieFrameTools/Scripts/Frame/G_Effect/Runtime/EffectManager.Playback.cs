namespace MieMieFrameWork.Effects
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public sealed partial class EffectManager
    {
        /// <summary>
        /// 一轮播放的请求 所有者 场景与租借版本
        /// </summary>
        private sealed class EffectPlayback
        {
            public ulong Id { get; set; }
            public EffectResourceEntry Entry { get; set; }
            public EffectInstance Instance { get; set; }
            public ulong LeaseVersion { get; set; }
            public EffectSpawnOptions Options { get; set; }
            public bool HasOwner { get; set; }
            public Scene Scene { get; set; }
            public bool Paused { get; set; }
            public CancellationTokenSource Cancellation { get; set; }
            public CancellationToken Token { get; set; }
        }

        #region 请求与预算

        /// <summary>
        /// 预留预算并异步播放 预算拒绝返回状态 失败和取消清理后抛出
        /// </summary>
        public async UniTask<EffectPlayResult> PlayAsync(string id, EffectSpawnOptions options,
            CancellationToken cancellationToken = default)
        {
            CheckReady();
            cancellationToken.ThrowIfCancellationRequested();
            if (!(options.Scale > 0f) || float.IsInfinity(options.Scale) ||
                Quaternion.Dot(options.Rotation, options.Rotation) <= 0f ||
                (options.FollowOwner && options.Owner == null))
                throw new ArgumentException($"[EffectManager] 特效 {id} 播放姿态或所有者无效", nameof(options));
            if (!definitionDict.TryGetValue(id, out var Definition))
                throw new InvalidOperationException($"[EffectManager] 未配置特效 {id}");
            if (requestFrame != Time.frameCount)
            {
                requestFrame = Time.frameCount;
                frameRequests = 0;
            }
            var eStatus = EEffectPlayStatus.Accepted;
            if (playbackList.Count >= maxConcurrent)
                eStatus = EEffectPlayStatus.GlobalLimit;
            else if (resourceDict.TryGetValue(id, out var Existing) && Existing.PlaybackRequests >= Definition.MaxConcurrent)
                eStatus = EEffectPlayStatus.DefinitionLimit;
            else if (frameRequests >= maxPerFrame)
                eStatus = EEffectPlayStatus.FrameLimit;
            if (eStatus != EEffectPlayStatus.Accepted)
            {
                RejectedRequests++;
                return new EffectPlayResult(eStatus);
            }

            var Entry = GetResource(id);
            var Cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetimeCancellation.Token);
            var Playback = new EffectPlayback
            {
                Id = checked(++nextId), Entry = Entry, Options = options, HasOwner = options.Owner != null,
                Scene = options.Scene.IsValid() ? options.Scene :
                    options.Owner != null ? options.Owner.gameObject.scene : SceneManager.GetActiveScene(),
                Cancellation = Cancellation, Token = Cancellation.Token
            };
            playbackList.Add(Playback);
            playbackDict.Add(Playback.Id, Playback);
            Entry.Requests++;
            Entry.PlaybackRequests++;
            frameRequests++;
            try
            {
                await EnsureLoadedAsync(Entry, Playback.Token);
                await ReserveSpawnAsync(Playback.Token);
                Playback.Token.ThrowIfCancellationRequested();
                if (Playback.HasOwner && options.Owner == null)
                    throw new OperationCanceledException(Playback.Token);
                var InstanceObject = Entry.Pool.Get(serviceRoot, activate: false);
                if (InstanceObject == null)
                    throw new InvalidOperationException($"[EffectManager] {Definition.Location} 池容量与请求预算不一致");
                Playback.Instance = Entry.GetInstance(InstanceObject);
                Playback.LeaseVersion = Entry.Pool.GetLeaseVersion(InstanceObject);
                Playback.Instance.RefreshPlayback(options, Quality, profileList[(int)Quality]);
                InstanceObject.SetActive(true);
                Playback.Token.ThrowIfCancellationRequested();
                if (!IsValid(Playback.Id))
                    throw new OperationCanceledException(Playback.Token);
                Playback.Instance.Play();
                Playback.Instance.RefreshPaused(IsPaused || Playback.Paused);
                return new EffectPlayResult(EEffectPlayStatus.Accepted, new EffectHandle(this, Playback.Id));
            }
            catch (Exception Failure)
            {
                try
                {
                    ReleasePlayback(Playback);
                    RetireIfUnused(Entry);
                }
                catch (Exception CleanupFailure)
                {
                    throw new AggregateException($"[EffectManager] 特效播放失败且清理失败 {Definition.Location}",
                        Failure, CleanupFailure);
                }
                throw;
            }
        }

        #endregion

        #region 单次与全局控制

        /// <summary>
        /// 查询本轮请求及池租借是否仍有效
        /// </summary>
        internal bool IsValid(ulong id)
        {
            return !disposed && playbackDict.TryGetValue(id, out var Playback) && !Playback.Token.IsCancellationRequested &&
                Playback.Instance != null && Playback.Entry.Pool.IsLeaseValid(Playback.Instance, Playback.LeaseVersion);
        }

        /// <summary>
        /// 停止对应请求 旧轮次不操作新租借
        /// </summary>
        internal bool Stop(ulong id)
        {
            if (!playbackDict.TryGetValue(id, out var Playback))
                return false;
            bool Valid = IsValid(id);
            ReleasePlayback(Playback);
            return Valid;
        }

        /// <summary>
        /// 设置单轮暂停 全局暂停与单轮暂停叠加
        /// </summary>
        internal bool SetPaused(ulong id, bool paused)
        {
            if (!IsValid(id))
                return false;
            var Playback = playbackDict[id];
            Playback.Paused = paused;
            Playback.Instance.RefreshPaused(IsPaused || paused);
            return true;
        }

        /// <summary>
        /// 全局暂停或恢复 不清除单轮暂停状态
        /// </summary>
        public void SetGlobalPaused(bool paused)
        {
            CheckReady();
            IsPaused = paused;
            foreach (var Playback in playbackList)
                if (IsValid(Playback.Id))
                    Playback.Instance.RefreshPaused(paused || Playback.Paused);
        }

        /// <summary>
        /// 显式切换档位 使用原始粒子参数避免累计缩减
        /// </summary>
        public void SetQuality(EEffectQuality quality)
        {
            CheckReady();
            if (!Enum.IsDefined(typeof(EEffectQuality), quality))
                throw new ArgumentOutOfRangeException(nameof(quality));
            Quality = quality;
            var PlaybackList = playbackList.ToArray();
            foreach (var Playback in PlaybackList)
            {
                if (!IsValid(Playback.Id))
                    continue;
                Playback.Instance.ApplyQuality(quality, profileList[(int)quality], true);
                if (IsValid(Playback.Id))
                    Playback.Instance.RefreshPaused(IsPaused || Playback.Paused);
            }
        }

        /// <summary>
        /// 停止全部播放和加载中的请求 保留可复用资源缓存
        /// </summary>
        public void StopAll()
        {
            CheckReady();
            foreach (var Playback in playbackList.ToArray())
                ReleasePlayback(Playback);
        }

        #endregion

        #region 帧驱动与释放

        /// <summary>
        /// 更新跟随与自然结束 同时清理取消请求和失效所有者
        /// </summary>
        private void Tick()
        {
            for (int Index = playbackList.Count - 1; Index >= 0; Index--)
            {
                if (Index >= playbackList.Count)
                    continue;
                var Playback = playbackList[Index];
                if (Playback.Token.IsCancellationRequested || (Playback.HasOwner && Playback.Options.Owner == null) ||
                    (Playback.LeaseVersion != 0 && !IsValid(Playback.Id)))
                {
                    ReleasePlayback(Playback);
                    continue;
                }
                if (Playback.Instance == null)
                    continue;
                if (Playback.Options.FollowOwner)
                    Playback.Instance.RefreshPose(Playback.Options);
                if (!IsPaused && !Playback.Paused && !Playback.Entry.Definition.Looping && !Playback.Instance.IsAlive())
                    ReleasePlayback(Playback);
                if (disposed)
                    return;
            }
            foreach (var Entry in resourceDict.Values)
                Entry.CollectDestroyed();
            for (int Index = retiredList.Count - 1; Index >= 0; Index--)
                if (retiredList[Index].Released)
                    retiredList.RemoveAt(Index);
        }

        /// <summary>
        /// 场景卸载停止所属请求 全局池实例不会随业务场景直接销毁
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            foreach (var Playback in playbackList.ToArray())
                if (!Playback.Options.Persistent && Playback.Scene == scene)
                    ReleasePlayback(Playback);
        }

        /// <summary>
        /// 先移除请求再归还本轮租借 回调失败时销毁本轮实例并保留首错
        /// </summary>
        private void ReleasePlayback(EffectPlayback playback)
        {
            if (!playbackDict.Remove(playback.Id))
                return;
            playbackList.Remove(playback);
            playback.Entry.Requests--;
            playback.Entry.PlaybackRequests--;
            try
            {
                playback.Cancellation.Cancel();
            }
            finally
            {
                try
                {
                    if (playback.Instance != null && playback.Entry.Pool.IsLeaseValid(playback.Instance, playback.LeaseVersion))
                    {
                        try { playback.Entry.Pool.TryRelease(playback.Instance, playback.LeaseVersion); }
                        catch (Exception Failure)
                        {
                            try { playback.Entry.Pool.DiscardAfterFailedRelease(playback.Instance.gameObject, playback.LeaseVersion); }
                            catch (Exception CleanupFailure) { throw new AggregateException(Failure, CleanupFailure); }
                            throw;
                        }
                    }
                }
                finally
                {
                    playback.Instance = null;
                    playback.Cancellation.Dispose();
                }
            }
        }

        #endregion
    }
}
