namespace MieMieFrameWork
{
    using Sirenix.OdinInspector;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 音频管理器（分部类主文件：变量、属性、全局控制）
    /// 支持 BGM、环境音（Ambience）、特效音三种通道独立播放与控制
    /// </summary>
    [ManagerAttribute(3)]
    public partial class AudioManager : MonoBehaviour, IManagerBase
    {
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

        public void Init()
        {
            ChangeGlobalVolume();
        }

        #region 组件

        /// <summary>特效声音预制体（对象池用）</summary>
        [TitleGroup("组件配置")]
        [SerializeField, LabelText("特效声音播放器")]
        private GameObject efPlayerES;

        /// <summary>主背景音乐播放器（对应 BGM 混音组）</summary>
        [SerializeField, LabelText("BGM播放器")]
        private AudioSource BgmSource;

        /// <summary>环境音播放器（对应 Ambience 混音组）</summary>
        [SerializeField, LabelText("环境音播放器")]
        private AudioSource AmbienceSource;

        /// <summary>特效音对象池根节点</summary>
        [Header("特效音配置")]
        [SerializeField, LabelText("特效音乐根节点")]
        private Transform EffectClipRoot;

        #endregion

        #region 全局音量

        /// <summary>全局音量乘法系数（影响所有通道：BGM、环境音、特效音）</summary>
        [TitleGroup("音量设置")]
        [SerializeField, Range(0, 1), OnValueChanged("ChangeGlobalVolume"), LabelText("全局音量系数")]
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
        [Header("背景音乐音量")]
        [SerializeField, Range(0, 1), OnValueChanged("ChangeBgVolume"), LabelText("BGM基准音量")]
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
        [SerializeField, Range(0, 1), OnValueChanged("ChangeBgVolume"), LabelText("环境音基准音量")]
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
        [Header("特效音量")]
        [SerializeField, Range(0, 1), OnValueChanged("ChangeEffectVolume"), LabelText("特效音基准音量")]
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
        [Header("全局控制")]
        [SerializeField, OnValueChanged("OnSelectMute"), LabelText("静音")]
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
        [SerializeField, OnValueChanged("OnSelectLoop"), LabelText("循环播放")]
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
        [SerializeField, OnValueChanged("OnIsPause"), LabelText("暂停所有")]
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
