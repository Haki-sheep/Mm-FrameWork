namespace MieMieFrameWork
{
    using System;
    using System.Threading;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// 独立背景通道持有播放资源 管理请求覆盖 暂停和渐变
    /// </summary>
    internal sealed class BackgroundAudioChannel : IDisposable
    {
        /// <summary>
        /// 绑定的音频播放器
        /// </summary>
        private readonly AudioSource source;

        /// <summary>
        /// 当前资源持有 外部片段没有持有记录
        /// </summary>
        private AudioClipLease clipLease;

        /// <summary>
        /// 当前请求尚未完成的资源持有 停止或覆盖时立即释放
        /// </summary>
        private AudioClipLease pendingLease;

        /// <summary>
        /// 当前加载请求取消源
        /// </summary>
        private CancellationTokenSource requestCancellation;

        /// <summary>
        /// 单通道暂停状态
        /// </summary>
        private bool paused;

        /// <summary>
        /// 是否已经释放
        /// </summary>
        private bool disposed;

        /// <summary>
        /// 渐变已经消耗的不缩放时间
        /// </summary>
        private float fadeElapsed;

        /// <summary>
        /// 渐变持续时间
        /// </summary>
        private float fadeDuration;

        /// <summary>
        /// 渐变起始音量
        /// </summary>
        private float fadeStartVolume;

        /// <summary>
        /// 渐变目标音量
        /// </summary>
        private float fadeTargetVolume;

        /// <summary>
        /// 渐变完成回调
        /// </summary>
        private UnityAction fadeComplete;

        #region 通道播放

        /// <summary>
        /// 接收播放器 不在构造期间开始播放
        /// </summary>
        public BackgroundAudioChannel(AudioSource source)
        {
            this.source = source;
        }

        /// <summary>
        /// 开始新请求 使此前未完成的同通道请求失效
        /// </summary>
        public CancellationToken BeginRequest(AudioClipLease lease = null)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(BackgroundAudioChannel));
            CancelRequest();
            pendingLease = lease;
            requestCancellation = new CancellationTokenSource();
            return requestCancellation.Token;
        }

        /// <summary>
        /// 替换播放内容 先停止旧片段再释放旧资源持有
        /// </summary>
        public void Play(AudioClip clip, AudioClipLease lease, bool loop, bool globalPause)
        {
            StopPlayback();
            pendingLease = null;
            clipLease = lease;
            source.volume = 1f;
            source.clip = clip;
            source.loop = loop;
            source.Play();
            RefreshPause(globalPause);
        }

        /// <summary>
        /// 设置播放器通道增益 不修改 Mixer 的基准音量
        /// </summary>
        public void SetVolume(float volume)
        {
            source.volume = volume;
        }

        /// <summary>
        /// 设置独立暂停状态 并叠加管理器全局暂停
        /// </summary>
        public void SetPause(bool pause, bool globalPause)
        {
            paused = pause;
            RefreshPause(globalPause);
        }

        /// <summary>
        /// 刷新当前播放器暂停状态 保留单通道暂停意图
        /// </summary>
        public void RefreshPause(bool globalPause)
        {
            if (paused || globalPause)
                source.Pause();
            else
                source.UnPause();
        }

        #endregion

        #region 渐变与生命周期

        /// <summary>
        /// 开始音量渐变 零持续时间立即应用目标音量并回调
        /// </summary>
        public void Fade(float volume, float duration, UnityAction complete)
        {
            fadeElapsed = 0f;
            fadeDuration = duration;
            fadeStartVolume = source.volume;
            fadeTargetVolume = volume;
            fadeComplete = complete;
            if (duration <= 0f)
            {
                source.volume = volume;
                fadeComplete = null;
                complete?.Invoke();
            }
        }

        /// <summary>
        /// 刷新渐变和自然播放结束 暂停期间冻结渐变时间
        /// </summary>
        public void Update(float deltaTime, bool globalPause)
        {
            if (disposed || paused || globalPause)
                return;
            if (source == null)
            {
                Dispose();
                return;
            }
            if (fadeDuration > 0f)
            {
                fadeElapsed += deltaTime;
                source.volume = Mathf.Lerp(fadeStartVolume, fadeTargetVolume, fadeElapsed / fadeDuration);
                if (fadeElapsed >= fadeDuration)
                {
                    fadeDuration = 0f;
                    var Complete = fadeComplete;
                    fadeComplete = null;
                    Complete?.Invoke();
                    return;
                }
            }
            if (source.clip != null && !source.loop && !source.isPlaying)
                StopPlayback();
        }

        /// <summary>
        /// 显式停止通道并使加载请求失效
        /// </summary>
        public void Stop()
        {
            CancelRequest();
            StopPlayback();
        }

        /// <summary>
        /// 停止播放器后释放当前资源 不取消后续待完成的加载请求
        /// </summary>
        private void StopPlayback()
        {
            fadeDuration = 0f;
            fadeComplete = null;
            if (source != null)
            {
                source.Stop();
                source.clip = null;
            }
            clipLease?.Dispose();
            clipLease = null;
        }

        /// <summary>
        /// 取消并释放当前请求状态 不遗留取消源
        /// </summary>
        private void CancelRequest()
        {
            if (requestCancellation == null)
                return;
            requestCancellation.Cancel();
            requestCancellation.Dispose();
            requestCancellation = null;
            pendingLease?.Dispose();
            pendingLease = null;
        }

        /// <summary>
        /// 通道退出时取消请求 停止播放并释放资源持有
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            Stop();
        }

        #endregion
    }
}
