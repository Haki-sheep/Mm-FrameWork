namespace MieMieFrameWork
{
    using MieMieFrameWork.Pool;
    using MieMieFrameWork.Diagnostics;
    using System;
    using System.Threading;
    using UnityEngine;
    using static MieMieFrameWork.ModuleHub;

    /// <summary>
    /// 音频管理器 支持 BGM 环境音和特效音三种通道独立播放与控制
    /// </summary>
    [ManagerAttribute(3)]
    public partial class AudioManager : IManagerBase, IDisposable
    {
        /// <summary>
        /// 背景音乐类型枚举
        /// </summary>
        public enum BgAudioType
        {
            /// <summary>主背景音乐 如主题曲</summary>
            BGM,
            /// <summary>环境音 如雨声和风声</summary>
            Ambience
        }

        /// <summary>
        /// Mixer 总音量参数
        /// </summary>
        private const string MasterVolumeParameter = "MasterVolume";

        /// <summary>
        /// Mixer 背景音乐音量参数
        /// </summary>
        private const string BgmVolumeParameter = "BgmVolume";

        /// <summary>
        /// Mixer 环境音音量参数
        /// </summary>
        private const string AmbienceVolumeParameter = "AmbienceVolume";

        /// <summary>
        /// Mixer 特效音音量参数
        /// </summary>
        private const string EffectVolumeParameter = "EffectVolume";

        /// <summary>
        /// 静音衰减分贝
        /// </summary>
        private const float SilenceDecibels = -80f;

        /// <summary>
        /// 服务根节点
        /// </summary>
        private readonly Transform serviceRoot;

        /// <summary>
        /// 音频组件与预算配置
        /// </summary>
        private readonly AudioManagerConfig config;

        /// <summary>
        /// 音频管理器生命周期取消源
        /// </summary>
        private readonly CancellationTokenSource lifetimeCancellation = new();

        /// <summary>
        /// 背景音乐通道
        /// </summary>
        private readonly BackgroundAudioChannel bgmChannel;

        /// <summary>
        /// 环境音通道
        /// </summary>
        private readonly BackgroundAudioChannel ambienceChannel;

        /// <summary>
        /// 特效音对象池句柄
        /// </summary>
        private PoolHandle effectPoolHandle;

        /// <summary>
        /// 帧生命周期代理
        /// </summary>
        private MonoManager monoManager;

        /// <summary>
        /// 是否已完成唯一初始化
        /// </summary>
        private bool initialized;

        /// <summary>
        /// 是否已到达允许设置 Mixer 的首帧
        /// </summary>
        private bool mixerReady;

        /// <summary>
        /// 是否已释放
        /// </summary>
        private bool disposed;

        /// <summary>
        /// 全局音量系数
        /// </summary>
        private float globalVolumeFactor;

        /// <summary>
        /// 主背景音乐基准音量
        /// </summary>
        private float bgVolumeBaseNum;

        /// <summary>
        /// 环境音基准音量
        /// </summary>
        private float ambienceVolumeBaseNum;

        /// <summary>
        /// 特效音基准音量
        /// </summary>
        private float effectVolumeBaseNum;

        /// <summary>
        /// 是否静音 影响所有通道
        /// </summary>
        private bool isMute;

        /// <summary>
        /// 是否循环播放 仅影响 BGM 通道
        /// </summary>
        private bool isLoop;

        /// <summary>
        /// 是否暂停 影响所有通道
        /// </summary>
        private bool isPause;

        public static AudioManager Instance { get; internal set; }

        public float GlobalVolumeFactor
        {
            get => globalVolumeFactor;
            set { globalVolumeFactor = value; ChangeGlobalVolume(); }
        }

        public float BgVolumeBaseNum
        {
            get => bgVolumeBaseNum;
            set { bgVolumeBaseNum = value; ChangeBgVolume(); }
        }

        public float AmbienceVolumeBaseNum
        {
            get => ambienceVolumeBaseNum;
            set { ambienceVolumeBaseNum = value; ChangeBgVolume(); }
        }

        public float EffectVolumeBaseNum
        {
            get => effectVolumeBaseNum;
            set { effectVolumeBaseNum = value; ChangeEffectVolume(); }
        }

        public bool IsMute
        {
            get => isMute;
            set { isMute = value; OnSelectMute(); }
        }

        public bool IsLoop
        {
            get => isLoop;
            set { isLoop = value; OnSelectLoop(); }
        }

        public bool IsPause
        {
            get => isPause;
            set { isPause = value; OnIsPause(); }
        }

        #region 生命周期

        /// <summary>
        /// 接收配置和服务根节点 创建通道状态但不启动播放
        /// </summary>
        public AudioManager(AudioManagerConfig audioManagerConfig, Transform serviceRoot)
        {
            config = audioManagerConfig;
            this.serviceRoot = serviceRoot;
            bgmChannel = new BackgroundAudioChannel(config.BgmSource);
            ambienceChannel = new BackgroundAudioChannel(config.AmbienceSource);
            globalVolumeFactor = config.GlobalVolumeFactor;
            bgVolumeBaseNum = config.BgVolumeBaseNum;
            ambienceVolumeBaseNum = config.AmbienceVolumeBaseNum;
            effectVolumeBaseNum = config.EffectVolumeBaseNum;
            isMute = config.IsMute;
            isLoop = config.IsLoop;
            isPause = config.IsPause;
            Instance = this;
        }

        /// <summary>
        /// 兼容管理器统一入口 仅由 ModuleHub 调用一次
        /// </summary>
        public void Init()
        {
            InitComponents();
            FrameLog.Info("音频管理器就绪", "Audio");
        }

        /// <summary>
        /// 校验组件配置 预热对象池并绑定唯一帧回调
        /// </summary>
        private void InitComponents()
        {
            if (initialized || disposed)
                throw new InvalidOperationException("[AudioManager] 初始化只能执行一次");
            if (config.Mixer == null || config.EffectMixerGroup == null || config.EfPlayerES == null ||
                config.BgmSource == null || config.AmbienceSource == null || config.EffectClipRoot == null)
                throw new InvalidOperationException("[AudioManager] 音频组件或 Mixer 配置缺失 请检查 FrameRoot");
            if (config.EffectMixerGroup.audioMixer != config.Mixer ||
                config.BgmSource.outputAudioMixerGroup == null || config.AmbienceSource.outputAudioMixerGroup == null ||
                config.BgmSource.outputAudioMixerGroup.audioMixer != config.Mixer ||
                config.AmbienceSource.outputAudioMixerGroup.audioMixer != config.Mixer)
                throw new InvalidOperationException("[AudioManager] 所有通道必须绑定配置的同一个 Mixer");

            ValidateMixerParameter(MasterVolumeParameter);
            ValidateMixerParameter(BgmVolumeParameter);
            ValidateMixerParameter(AmbienceVolumeParameter);
            ValidateMixerParameter(EffectVolumeParameter);

            monoManager = serviceRoot.GetComponent<MonoManager>();
            if (monoManager == null || config.EfPlayerES.GetComponent<AudioSource>() == null)
                throw new InvalidOperationException("[AudioManager] 缺少 MonoManager 或特效预制体 AudioSource");
            if (config.MaxConcurrentEffects <= 0 || config.MaxEffectsPerFrame <= 0 || config.MaxSameClipEffects <= 0 ||
                config.EffectPrewarmCount < 0 || config.EffectPrewarmCount > config.MaxConcurrentEffects)
                throw new InvalidOperationException("[AudioManager] 播放预算必须为正数且预热数量不能超过同时音效上限");

            effectPoolHandle = PoolManager.Instance.GetPool(config.EfPlayerES, config.MaxConcurrentEffects, config.MaxConcurrentEffects);
            effectPoolHandle.Prewarm(config.EffectPrewarmCount);
            bgmChannel.SetVolume(1f);
            ambienceChannel.SetVolume(1f);
            config.BgmSource.mute = false;
            config.AmbienceSource.mute = false;
            OnSelectLoop();
            OnIsPause();
            initialized = true;
            monoManager.AddUpdateListener(UpdateAudio);
        }

        /// <summary>
        /// 取消未完成加载 停止所有通道并归还借出的特效播放器
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (monoManager != null)
                monoManager.RemoveUpdateListener(UpdateAudio);
            var FailureList = new System.Collections.Generic.List<Exception>();
            try { lifetimeCancellation.Cancel(); }
            catch (Exception Failure) { FailureList.Add(Failure); }
            DisposeAudioResource(bgmChannel, FailureList);
            DisposeAudioResource(ambienceChannel, FailureList);
            while (effectPlaybackList.Count > 0)
            {
                try { ReleaseEffect(effectPlaybackList[effectPlaybackList.Count - 1]); }
                catch (Exception Failure) { FailureList.Add(Failure); }
            }
            completionList.Clear();
            ReleaseBorrowedAudioClips(FailureList);
            lifetimeCancellation.Dispose();
            if (Instance == this)
                Instance = null;
            if (FailureList.Count > 0)
                throw new AggregateException("[AudioManager] 音频清理失败", FailureList);
        }

        /// <summary>
        /// 清理单个资源并保留异常 不因首个错误跳过其他通道
        /// </summary>
        private static void DisposeAudioResource(IDisposable resource, System.Collections.Generic.List<Exception> failureList)
        {
            try { resource.Dispose(); }
            catch (Exception Failure) { failureList.Add(Failure); }
        }

        /// <summary>
        /// 在首帧应用 Mixer 随后刷新播放结束和渐变状态
        /// </summary>
        private void UpdateAudio()
        {
            if (!mixerReady)
            {
                mixerReady = true;
                ChangeGlobalVolume();
                ChangeBgVolume();
                ChangeEffectVolume();
            }
            bgmChannel.Update(Time.unscaledDeltaTime, isPause);
            if (disposed) return;
            ambienceChannel.Update(Time.unscaledDeltaTime, isPause);
            if (disposed) return;
            UpdateEffects();
        }

        /// <summary>
        /// 检查业务调用前置状态 未初始化或已释放立即暴露错误
        /// </summary>
        private void RequireActive()
        {
            if (disposed) throw new ObjectDisposedException(nameof(AudioManager));
            if (!initialized) throw new InvalidOperationException("[AudioManager] 尚未初始化");
        }

        #endregion

        #region 全局控制

        /// <summary>
        /// 初始化时确认必要参数存在 不在 Awake 写入 Mixer 音量
        /// </summary>
        private void ValidateMixerParameter(string parameter)
        {
            if (!config.Mixer.GetFloat(parameter, out float Volume))
                throw new InvalidOperationException($"[AudioManager] Mixer 未暴露参数 {parameter}");
        }

        /// <summary>
        /// 刷新总通道实际音量 基准音量由各 Mixer 分组独立控制
        /// </summary>
        private void ChangeGlobalVolume() => SetMixerVolume(MasterVolumeParameter, isMute ? 0f : globalVolumeFactor);

        /// <summary>
        /// 同步所有通道的静音状态
        /// </summary>
        private void OnSelectMute() => ChangeGlobalVolume();

        /// <summary>
        /// 同步所有通道的暂停和恢复 保留单独暂停的播放器
        /// </summary>
        private void OnIsPause()
        {
            bgmChannel.RefreshPause(isPause);
            ambienceChannel.RefreshPause(isPause);
            foreach (var Playback in effectPlaybackList)
                RefreshEffectPause(Playback);
        }

        /// <summary>
        /// 将线性音量转为分贝 配置参数缺失立即报错
        /// </summary>
        private void SetMixerVolume(string parameter, float volume)
        {
            if (!mixerReady || disposed) return;
            float Decibels = volume <= 0f ? SilenceDecibels : Mathf.Max(SilenceDecibels, 20f * Mathf.Log10(volume));
            if (!config.Mixer.SetFloat(parameter, Decibels))
                throw new InvalidOperationException($"[AudioManager] Mixer 未暴露参数 {parameter}");
        }

        #endregion
    }
}
