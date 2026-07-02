namespace MieMieFrameWork
{
    using Sirenix.OdinInspector;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using static MieMieFrameWork.ModuleHub;
    /// <summary>
    /// 音频管理器（分部类主文件：变量、属性、全局控制）
    /// 支持 BGM、环境音（Ambience）、特效音三种通道独立播放与控制
    /// </summary>
    [ManagerAttribute(3)]
    public partial class AudioManager : IManagerBase, IDisposable
    {
        [Serializable]
        public sealed class AudioManagerConfig
        {
            /// <summary>特效声音播放器预制体</summary>
            [TitleGroup("音频组件配置")]
            [SerializeField, LabelText("特效声音播放器")]
            private GameObject efPlayerES;

            /// <summary>BGM 播放器</summary>
            [SerializeField, LabelText("BGM播放器")]
            private AudioSource bgmSource;

            /// <summary>环境音播放器</summary>
            [SerializeField, LabelText("环境音播放器")]
            private AudioSource ambienceSource;

            /// <summary>特效音对象池根节点</summary>
            [SerializeField, LabelText("特效音乐根节点")]
            private Transform effectClipRoot;

            /// <summary>全局音量系数</summary>
            [TitleGroup("音频音量设置")]
            [SerializeField, Range(0, 1), LabelText("全局音量系数")]
            private float globalVolumeFactor = 1;

            /// <summary>BGM 基准音量</summary>
            [SerializeField, Range(0, 1), LabelText("BGM基准音量")]
            private float bgVolumeBaseNum = 1;

            /// <summary>环境音基准音量</summary>
            [SerializeField, Range(0, 1), LabelText("环境音基准音量")]
            private float ambienceVolumeBaseNum = 0.8f;

            /// <summary>特效音基准音量</summary>
            [SerializeField, Range(0, 1), LabelText("特效音基准音量")]
            private float effectVolumeBaseNum = 1;

            /// <summary>是否静音</summary>
            [TitleGroup("音频全局控制")]
            [SerializeField, LabelText("静音")]
            private bool isMute;

            /// <summary>是否循环播放</summary>
            [SerializeField, LabelText("循环播放")]
            private bool isLoop;

            /// <summary>是否暂停所有</summary>
            [SerializeField, LabelText("暂停所有")]
            private bool isPause;

            public GameObject EfPlayerES => efPlayerES;
            public AudioSource BgmSource => bgmSource;
            public AudioSource AmbienceSource => ambienceSource;
            public Transform EffectClipRoot => effectClipRoot;
            public float GlobalVolumeFactor => globalVolumeFactor;
            public float BgVolumeBaseNum => bgVolumeBaseNum;
            public float AmbienceVolumeBaseNum => ambienceVolumeBaseNum;
            public float EffectVolumeBaseNum => effectVolumeBaseNum;
            public bool IsMute => isMute;
            public bool IsLoop => isLoop;
            public bool IsPause => isPause;
        }

        /// <summary>
        /// 背景音乐类型枚举
        /// </summary>
        public enum BgAudioType
        {
            /// <summary>主背景音乐（如主题曲）</summary>
            BGM,
            /// <summary>环境音（如雨声、风声）</summary>
            Ambience
        }

        /// <summary>服务根节点</summary>
        private readonly Transform serviceRoot;

        public AudioManager(AudioManagerConfig audioManagerConfig, Transform serviceRoot)
        {
            this.serviceRoot = serviceRoot;
            efPlayerES = audioManagerConfig.EfPlayerES;
            BgmSource = audioManagerConfig.BgmSource;
            AmbienceSource = audioManagerConfig.AmbienceSource;
            EffectClipRoot = audioManagerConfig.EffectClipRoot;
            globalVolumeFactor = audioManagerConfig.GlobalVolumeFactor;
            bgVolumeBaseNum = audioManagerConfig.BgVolumeBaseNum;
            ambienceVolumeBaseNum = audioManagerConfig.AmbienceVolumeBaseNum;
            effectVolumeBaseNum = audioManagerConfig.EffectVolumeBaseNum;
            isMute = audioManagerConfig.IsMute;
            isLoop = audioManagerConfig.IsLoop;
            isPause = audioManagerConfig.IsPause;
        }

        public void Init()
        {
            ChangeGlobalVolume();
            OnSelectMute();
            OnSelectLoop();
            OnIsPause();
        }

        public void Dispose()
        {
            StopBgAudio(BgAudioType.BGM);
            StopBgAudio(BgAudioType.Ambience);
            efAudioList.Clear();
        }

        #region 组件

        /// <summary>特效声音预制体（对象池用）</summary>
        private GameObject efPlayerES;

        /// <summary>主背景音乐播放器（对应 BGM 混音组）</summary>
        private AudioSource BgmSource;

        /// <summary>环境音播放器（对应 Ambience 混音组）</summary>
        private AudioSource AmbienceSource;

        /// <summary>特效音对象池根节点</summary>
        private Transform EffectClipRoot;

        #endregion

        #region 全局音量

        /// <summary>全局音量乘法系数（影响所有通道：BGM、环境音、特效音）</summary>
        private float globalVolumeFactor;

        public float GlobalVolumeFactor
        {
            get => globalVolumeFactor;
            set
            {
                if (globalVolumeFactor == value) return;
                globalVolumeFactor = value;
                ChangeGlobalVolume();
            }
        }

        /// <summary>
        /// 刷新所有通道的实际音量
        /// 计算公式：实际音量 = 通道基准音量 * 全局系数
        /// </summary>
        private void ChangeGlobalVolume()
        {
            ChangeBgVolume();
            ChangeEffectVolume();
        }

        #endregion

        #region 背景音乐音量

        /// <summary>主背景音乐（BGM）基准音量（0-1）</summary>
        private float bgVolumeBaseNum;

        public float BgVolumeBaseNum
        {
            get => bgVolumeBaseNum;
            set
            {
                if (bgVolumeBaseNum == value) return;
                bgVolumeBaseNum = value;
                ChangeBgVolume();
            }
        }

        /// <summary>环境音（Ambience）基准音量（0-1）</summary>
        private float ambienceVolumeBaseNum;

        public float AmbienceVolumeBaseNum
        {
            get => ambienceVolumeBaseNum;
            set
            {
                if (ambienceVolumeBaseNum == value) return;
                ambienceVolumeBaseNum = value;
                ChangeBgVolume();
            }
        }

        #endregion

        #region 特效音量

        /// <summary>特效音（Effect）基准音量（0-1）</summary>
        private float effectVolumeBaseNum;

        public float EffectVolumeBaseNum
        {
            get => effectVolumeBaseNum;
            set
            {
                if (effectVolumeBaseNum == value) return;
                effectVolumeBaseNum = value;
                ChangeEffectVolume();
            }
        }

        /// <summary>当前正在播放的特效音 AudioSource 列表（用于统一刷新音量）</summary>
        private List<AudioSource> efAudioList = new();

        #endregion

        #region 全局控制

        /// <summary>是否静音（影响所有通道）</summary>
        private bool isMute;

        public bool IsMute
        {
            get => isMute;
            set
            {
                if (isMute == value) return;
                isMute = value;
                OnSelectMute();
            }
        }

        /// <summary>
        /// 同步所有通道的静音状态
        /// </summary>
        private void OnSelectMute()
        {
            if (BgmSource != null) BgmSource.mute = IsMute;
            if (AmbienceSource != null) AmbienceSource.mute = IsMute;
            ChangeEffectVolume();
        }

        /// <summary>是否循环播放（仅影响 BGM 通道）</summary>
        private bool isLoop;

        public bool IsLoop
        {
            get => isLoop;
            set
            {
                if (isLoop == value) return;
                isLoop = value;
                OnSelectLoop();
            }
        }

        /// <summary>是否暂停（影响所有通道）</summary>
        private bool isPause;

        public bool IsPause
        {
            get => isPause;
            set
            {
                if (isPause == value) return;
                isPause = value;
                OnIsPause();
            }
        }

        /// <summary>
        /// 同步所有通道的暂停/恢复状态
        /// </summary>
        private void OnIsPause()
        {
            if (BgmSource != null)
            {
                if (isPause == true) BgmSource.Pause();
                else BgmSource.UnPause();
            }
            if (AmbienceSource != null)
            {
                if (isPause == true) AmbienceSource.Pause();
                else AmbienceSource.UnPause();
            }
            ChangeEffectVolume();
        }

        #endregion
    }
}
