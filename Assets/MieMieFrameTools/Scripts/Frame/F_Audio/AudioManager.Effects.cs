namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// 特效音预算 播放结束回收与完成回调调度
    /// </summary>
    public partial class AudioManager
    {
        /// <summary>
        /// 已接受的特效音请求 包含尚未完成的资源加载
        /// </summary>
        private readonly List<EffectPlayback> effectPlaybackList = new();

        /// <summary>
        /// 按本次请求编号定位播放状态
        /// </summary>
        private readonly Dictionary<ulong, EffectPlayback> effectPlaybackDict = new();

        /// <summary>
        /// 会话内单调递增的播放编号 不复用池实例编号
        /// </summary>
        private ulong nextEffectId;

        /// <summary>
        /// 按片段或资源地址统计的并发请求数
        /// </summary>
        private readonly Dictionary<object, int> effectCountDict = new();

        /// <summary>
        /// 播放结束后等待触发的延迟回调
        /// </summary>
        private readonly List<AudioCompletion> completionList = new();

        /// <summary>
        /// 本次每帧请求计数所属帧
        /// </summary>
        private int effectRequestFrame = -1;

        /// <summary>
        /// 本帧已接受的特效音请求数量
        /// </summary>
        private int effectRequestCount;

        /// <summary>
        /// 单次特效音播放状态
        /// </summary>
        private sealed class EffectPlayback
        {
            public object Key { get; set; }
            public ulong Id { get; set; }
            public AudioSource Source { get; set; }
            public ulong LeaseVersion { get; set; }
            public AudioClipLease Lease { get; set; }
            public CancellationTokenSource Cancellation { get; set; }
            public Component FollowTarget { get; set; }
            public bool Started { get; set; }
            public bool Paused { get; set; }
            public UnityAction Complete { get; set; }
            public float CompleteDelay { get; set; }
        }

        /// <summary>
        /// 延迟回调状态 不保留已经归还的播放器
        /// </summary>
        private sealed class AudioCompletion
        {
            public UnityAction Complete { get; set; }
            public double DueTime { get; set; }
        }

        #region 请求与播放

        /// <summary>
        /// 预留播放预算 超出配置预算的请求不加载资源也不借出播放器
        /// </summary>
        private EffectPlayback ReserveEffect(object key, UnityAction complete, float completeDelay)
        {
            if (effectRequestFrame != Time.frameCount)
            {
                effectRequestFrame = Time.frameCount;
                effectRequestCount = 0;
            }
            effectCountDict.TryGetValue(key, out int Count);
            if (effectPlaybackList.Count >= config.MaxConcurrentEffects ||
                effectRequestCount >= config.MaxEffectsPerFrame || Count >= config.MaxSameClipEffects)
                return null;

            var Playback = new EffectPlayback
            {
                Id = checked(++nextEffectId), Key = key, Complete = complete, CompleteDelay = completeDelay
            };
            effectPlaybackList.Add(Playback);
            effectPlaybackDict.Add(Playback.Id, Playback);
            effectCountDict[key] = Count + 1;
            effectRequestCount++;
            return Playback;
        }

        /// <summary>
        /// 借出播放器 绑定 Mixer 和片段并开始播放
        /// </summary>
        private bool StartEffect(EffectPlayback playback, AudioClip clip, float volumeScale, bool is3d, Component component, bool loop)
        {
            var Source = effectPoolHandle.Get<AudioSource>(config.EffectClipRoot, activate: false, configure: Audio =>
            {
                Audio.Stop();
                Audio.playOnAwake = false;
                Audio.outputAudioMixerGroup = config.EffectMixerGroup;
                Audio.mute = false;
                Audio.volume = volumeScale;
                Audio.pitch = 1f;
                Audio.loop = loop;
                Audio.spatialBlend = is3d ? 1f : 0f;
                Audio.transform.position = component != null ? component.transform.position : serviceRoot.position;
                Audio.clip = clip;
            });
            if (Source == null)
                return false;
            playback.Source = Source;
            playback.LeaseVersion = effectPoolHandle.GetLeaseVersion(Source);
            if (disposed || !effectPlaybackDict.ContainsKey(playback.Id))
            {
                try { ReleaseEffectSource(Source, playback.LeaseVersion, playback.Id); }
                finally { playback.Source = null; }
                return false;
            }
            playback.FollowTarget = component;
            // 激活前登记租借 激活回调可能停止请求或销毁管理器
            Source.gameObject.SetActive(true);
            if (disposed || !effectPlaybackDict.ContainsKey(playback.Id) ||
                !effectPoolHandle.IsLeaseValid(Source, playback.LeaseVersion))
                return false;
            Source.Play();
            playback.Started = true;
            RefreshEffectPause(playback);
            return true;
        }

        /// <summary>
        /// 全局暂停或恢复已启动的特效音 加载中请求不操作播放器
        /// </summary>
        private void RefreshEffectPause(EffectPlayback playback)
        {
            if (!playback.Started || !effectPoolHandle.IsLeaseValid(playback.Source, playback.LeaseVersion))
                return;
            if (isPause || playback.Paused)
                playback.Source.Pause();
            else
                playback.Source.UnPause();
        }

        #endregion

        #region 完成与释放

        /// <summary>
        /// 检测自然播放结束 先归还播放器和释放资源再安排完成回调
        /// </summary>
        private void UpdateEffects()
        {
            if (disposed)
                return;
            for (int Index = effectPlaybackList.Count - 1; Index >= 0; Index--)
            {
                var Playback = effectPlaybackList[Index];
                if (Playback.Started && !effectPoolHandle.IsLeaseValid(Playback.Source, Playback.LeaseVersion))
                {
                    ReleaseEffect(Playback);
                    continue;
                }
                if (Playback.Started && Playback.Source != null && Playback.FollowTarget != null)
                    Playback.Source.transform.position = Playback.FollowTarget.transform.position;
                if (!Playback.Started || isPause || Playback.Paused || Playback.Source.isPlaying)
                    continue;
                var Complete = Playback.Complete;
                float Delay = Playback.CompleteDelay;
                ReleaseEffect(Playback);
                if (Complete != null)
                    completionList.Add(new AudioCompletion { Complete = Complete, DueTime = Time.unscaledTimeAsDouble + Delay });
            }

            for (int Index = completionList.Count - 1; Index >= 0; Index--)
            {
                var Completion = completionList[Index];
                if (Time.unscaledTimeAsDouble < Completion.DueTime)
                    continue;
                completionList.RemoveAt(Index);
                Completion.Complete.Invoke();
                if (disposed)
                    return;
            }
        }

        /// <summary>
        /// 释放单次请求 停止播放器后归还对象池 最后释放资源持有
        /// </summary>
        private void ReleaseEffect(EffectPlayback playback)
        {
            if (!effectPlaybackList.Remove(playback))
                return;
            effectPlaybackDict.Remove(playback.Id);
            int Count = effectCountDict[playback.Key] - 1;
            if (Count == 0)
                effectCountDict.Remove(playback.Key);
            else
                effectCountDict[playback.Key] = Count;

            try
            {
                playback.Cancellation?.Cancel();
                ReleaseEffectSource(playback.Source, playback.LeaseVersion, playback.Id);
            }
            finally
            {
                playback.Source = null;
                playback.Lease?.Dispose();
                playback.Lease = null;
                playback.Cancellation?.Dispose();
                playback.Cancellation = null;
            }
        }

        /// <summary>
        /// 按本地持有的租借版本归还播放器 请求已移除也能完成清理
        /// </summary>
        private void ReleaseEffectSource(AudioSource source, ulong leaseVersion, ulong requestId)
        {
            if (!effectPoolHandle.IsLeaseValid(source, leaseVersion)) return;
            source.Stop();
            source.clip = null;
            try
            {
                if (!effectPoolHandle.TryRelease(source, leaseVersion))
                    throw new InvalidOperationException($"[AudioManager] 归还音效播放器失败 请求 {requestId}");
            }
            catch (Exception ReleaseFailure)
            {
                try
                {
                    effectPoolHandle.DiscardAfterFailedRelease(source.gameObject, leaseVersion);
                }
                catch (Exception CleanupFailure)
                {
                    throw new AggregateException("[AudioManager] 音效归还和清理失败", ReleaseFailure, CleanupFailure);
                }
                throw;
            }
        }

        #endregion
    }
}
