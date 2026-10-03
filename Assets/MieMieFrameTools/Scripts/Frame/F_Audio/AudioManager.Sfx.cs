namespace MieMieFrameWork
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// 音频管理器 特效音请求与播放入口
    /// </summary>
    public partial class AudioManager
    {
        #region 兼容入口

        /// <summary>
        /// 刷新特效音 Mixer 基准音量 不重复叠加总音量
        /// </summary>
        private void ChangeEffectVolume() => SetMixerVolume(EffectVolumeParameter, effectVolumeBaseNum);

        /// <summary>
        /// 播放一次特效音 支持外部 AudioClip 且不释放外部资源
        /// </summary>
        public void PlayOneShot(AudioClip clip, float volumeScale = 1, bool is3d = true,
            Component component = null, UnityAction callBack = null, float callBackTime = 0)
            => PlayEffect(clip, volumeScale, is3d, component, callBack, callBackTime);

        /// <summary>
        /// 播放一次特效音 支持 YooAsset 地址同步加载
        /// </summary>
        public void PlayOneShot(string clipPath, Component component = null, float volumeScale = 1,
            bool is3d = true, UnityAction callBack = null, float callBacKTime = 0)
            => PlayEffect(clipPath, component, volumeScale, is3d, callBack, callBacKTime);

        /// <summary>
        /// 播放一次特效音 2D UI 专用且禁用 3D 传播
        /// </summary>
        public void PlayOneShotWith2DUI(string clipPath, Component component = null, float volumeScale = 1,
            UnityAction callBack = null, float callBacKTime = 0)
            => PlayOneShot(clipPath, component, volumeScale, false, callBack, callBacKTime);

        /// <summary>
        /// 启动异步音效请求 保留现有无返回值业务入口
        /// </summary>
        public void PlayOneShotAsync(string clipPath, Component component = null, float volumeScale = 1,
            bool is3d = true, UnityAction callBack = null, float callBackTime = 0)
            => RequestEffectAsync(clipPath, component, volumeScale, is3d, callBack, callBackTime);

        #endregion

        #region 可控播放

        /// <summary>
        /// 播放外部片段并返回本次句柄 超预算返回默认值 支持循环
        /// </summary>
        public AudioPlaybackHandle PlayEffect(AudioClip clip, float volumeScale = 1, bool is3d = true,
            Component component = null, UnityAction callBack = null, float callBackTime = 0, bool loop = false)
        {
            RequireActive();
            var Playback = ReserveEffect(clip, callBack, callBackTime);
            if (Playback == null) return default;
            bool Started = false;
            try
            {
                Started = StartEffect(Playback, clip, volumeScale, is3d, component, loop);
                return Started ? new AudioPlaybackHandle(this, Playback.Id) : default;
            }
            finally
            {
                if (!Started) ReleaseEffect(Playback);
            }
        }

        /// <summary>
        /// 同步加载并播放音效 返回可停止查询的本次句柄
        /// </summary>
        public AudioPlaybackHandle PlayEffect(string clipPath, Component component = null, float volumeScale = 1,
            bool is3d = true, UnityAction callBack = null, float callBackTime = 0, bool loop = false)
        {
            RequireActive();
            var Playback = ReserveEffect(clipPath, callBack, callBackTime);
            if (Playback == null) return default;
            Playback.Lease = new AudioClipLease(clipPath);
            bool Started = false;
            try
            {
                var Clip = Playback.Lease.Load();
                Started = StartEffect(Playback, Clip, volumeScale, is3d, component, loop);
                return Started ? new AudioPlaybackHandle(this, Playback.Id) : default;
            }
            finally
            {
                if (!Started) ReleaseEffect(Playback);
            }
        }

        /// <summary>
        /// 提交异步加载立即返回句柄 加载中也可停止或暂停 错误交给 UniTask 上报
        /// </summary>
        public AudioPlaybackHandle RequestEffectAsync(string clipPath, Component component = null, float volumeScale = 1,
            bool is3d = true, UnityAction callBack = null, float callBackTime = 0, bool loop = false)
        {
            RequireActive();
            var Playback = ReserveEffect(clipPath, callBack, callBackTime);
            if (Playback == null) return default;
            LoadEffectAsync(Playback, clipPath, component, volumeScale, is3d, loop, default).Forget();
            return new AudioPlaybackHandle(this, Playback.Id);
        }

        /// <summary>
        /// 等待加载并返回已启动的播放句柄 外部取消抛出取消异常
        /// </summary>
        public async UniTask<AudioPlaybackHandle> PlayEffectAsync(string clipPath, Component component = null,
            float volumeScale = 1, bool is3d = true, UnityAction callBack = null, float callBackTime = 0,
            bool loop = false, CancellationToken cancellationToken = default)
        {
            RequireActive();
            cancellationToken.ThrowIfCancellationRequested();
            var Playback = ReserveEffect(clipPath, callBack, callBackTime);
            if (Playback == null) return default;
            return await LoadEffectAsync(Playback, clipPath, component, volumeScale, is3d, loop, cancellationToken);
        }

        #endregion

        #region 异步加载

        /// <summary>
        /// 加载期间预留预算 取消停止或退出均释放预留且不继续播放
        /// </summary>
        private async UniTask<AudioPlaybackHandle> LoadEffectAsync(EffectPlayback playback, string location,
            Component component, float volumeScale, bool is3d, bool loop, CancellationToken cancellationToken)
        {
            playback.Cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCancellation.Token, cancellationToken);
            var Token = playback.Cancellation.Token;
            playback.Lease = new AudioClipLease(location);
            bool Started = false;
            try
            {
                var Clip = await playback.Lease.LoadAsync(Token);
                Token.ThrowIfCancellationRequested();
                Started = StartEffect(playback, Clip, volumeScale, is3d, component, loop);
                return Started ? new AudioPlaybackHandle(this, playback.Id) : default;
            }
            catch (OperationCanceledException) when (Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                return default;
            }
            finally
            {
                if (!Started) ReleaseEffect(playback);
            }
        }

        #endregion
    }
}
