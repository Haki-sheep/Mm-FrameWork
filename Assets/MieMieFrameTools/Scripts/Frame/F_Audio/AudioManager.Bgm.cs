namespace MieMieFrameWork
{
    using Cysharp.Threading.Tasks;
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// 音频管理器 背景音乐和环境音控制
    /// </summary>
    public partial class AudioManager
    {
        /// <summary>
        /// 刷新背景音乐和环境音的 Mixer 基准音量
        /// </summary>
        private void ChangeBgVolume()
        {
            SetMixerVolume(BgmVolumeParameter, bgVolumeBaseNum);
            SetMixerVolume(AmbienceVolumeParameter, ambienceVolumeBaseNum);
        }

        /// <summary>
        /// 同步 BGM 通道的循环设置
        /// </summary>
        private void OnSelectLoop() => config.BgmSource.loop = isLoop;

        /// <summary>
        /// 根据类型获取对应的背景音乐通道
        /// </summary>
        private BackgroundAudioChannel GetBgChannel(BgAudioType eType)
            => eType == BgAudioType.BGM ? bgmChannel : ambienceChannel;

        /// <summary>
        /// 播放外部提供的片段 外部资源不由音频管理器释放
        /// </summary>
        public void PlayerBgAudio(AudioClip audioClip, BgAudioType type = BgAudioType.BGM, bool needLoop = true, float volume = 1)
        {
            RequireActive();
            var Channel = GetBgChannel(type);
            Channel.BeginRequest();
            ApplyBackground(Channel, audioClip, null, type, needLoop, volume);
        }

        /// <summary>
        /// 播放背景音乐 支持 YooAsset 地址同步加载
        /// </summary>
        public void PlayerBgAudio(string path, BgAudioType type = BgAudioType.BGM, bool needLoop = true, float volume = -1)
        {
            RequireActive();
            var Channel = GetBgChannel(type);
            var Lease = new AudioClipLease(path);
            Channel.BeginRequest(Lease);
            try
            {
                var Clip = Lease.Load();
                ApplyBackground(Channel, Clip, Lease, type, needLoop, volume);
            }
            catch
            {
                Lease.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 异步加载并播放背景音乐 后发请求覆盖先发请求
        /// </summary>
        public async UniTask PlayerBgAudioAsync(string path, BgAudioType type = BgAudioType.BGM, bool needLoop = true, float volume = 1)
        {
            RequireActive();
            var Channel = GetBgChannel(type);
            var Lease = new AudioClipLease(path);
            var RequestToken = Channel.BeginRequest(Lease);
            bool Transferred = false;
            try
            {
                var Clip = await Lease.LoadAsync(RequestToken);
                if (disposed || RequestToken.IsCancellationRequested) return;
                ApplyBackground(Channel, Clip, Lease, type, needLoop, volume);
                Transferred = true;
            }
            catch (OperationCanceledException) when (RequestToken.IsCancellationRequested)
            {
            }
            finally
            {
                if (!Transferred) Lease.Dispose();
            }
        }

        /// <summary>
        /// 异步加载背景音乐片段 调用者使用后必须调用 ReleaseBgClip
        /// </summary>
        public async UniTask<AudioClip> LoadBgClipAsync(string path)
        {
            RequireActive();
            var Lease = new AudioClipLease(path);
            pendingClipLeaseHashList.Add(Lease);
            bool Transferred = false;
            try
            {
                var Clip = await Lease.LoadAsync(lifetimeCancellation.Token);
                TrackBgClip(Clip, Lease);
                Transferred = true;
                return Clip;
            }
            finally
            {
                pendingClipLeaseHashList.Remove(Lease);
                if (!Transferred) Lease.Dispose();
            }
        }

        /// <summary>
        /// 停止指定类型的背景音乐 同时使未完成请求失效
        /// </summary>
        public void StopBgAudio(BgAudioType type = BgAudioType.BGM) => GetBgChannel(type).Stop();

        /// <summary>
        /// 暂停指定类型的背景音乐
        /// </summary>
        public void PauseBgAudio(BgAudioType type = BgAudioType.BGM) => GetBgChannel(type).SetPause(true, isPause);

        /// <summary>
        /// 恢复指定类型的背景音乐 全局暂停仍然生效
        /// </summary>
        public void UnPauseBgAudio(BgAudioType type = BgAudioType.BGM) => GetBgChannel(type).SetPause(false, isPause);

        /// <summary>
        /// 渐变指定通道的播放音量 使用不缩放时间且音频暂停时冻结
        /// </summary>
        public void FadeBgAudio(float targetVolume, float duration = 1f, BgAudioType type = BgAudioType.BGM, UnityAction complete = null)
        {
            RequireActive();
            GetBgChannel(type).Fade(targetVolume, duration, complete);
        }

        /// <summary>
        /// 应用片段与循环参数 保留通道基准音量的原有接口语义
        /// </summary>
        private void ApplyBackground(BackgroundAudioChannel channel, AudioClip clip, AudioClipLease lease,
            BgAudioType eType, bool needLoop, float volume)
        {
            if (volume != -1f)
            {
                if (eType == BgAudioType.BGM) BgVolumeBaseNum = volume;
                else AmbienceVolumeBaseNum = volume;
            }
            channel.Play(clip, lease, needLoop, isPause);
        }
    }
}
