namespace MieMieFrameWork
{
    using Sirenix.OdinInspector;
    using System;
    using UnityEngine;
    using UnityEngine.Audio;

    public partial class AudioManager
    {
        /// <summary>
        /// 音频组件与可调播放预算 保留原序列化字段名称
        /// </summary>
        [Serializable]
        public sealed class AudioManagerConfig
        {
            /// <summary>
            /// 特效声音播放器预制体
            /// </summary>
            [TitleGroup("音频组件配置")]
            [SerializeField, LabelText("特效声音播放器")]
            private GameObject efPlayerES;

            /// <summary>
            /// BGM 播放器
            /// </summary>
            [SerializeField, LabelText("BGM播放器")]
            private AudioSource bgmSource;

            /// <summary>
            /// 环境音播放器
            /// </summary>
            [SerializeField, LabelText("环境音播放器")]
            private AudioSource ambienceSource;

            /// <summary>
            /// 特效音对象池根节点
            /// </summary>
            [SerializeField, LabelText("特效音乐根节点")]
            private Transform effectClipRoot;

            /// <summary>
            /// 音频混音器
            /// </summary>
            [SerializeField, LabelText("音频混音器")]
            private AudioMixer mixer;

            /// <summary>
            /// 特效音混音组
            /// </summary>
            [SerializeField, LabelText("特效音混音组")]
            private AudioMixerGroup effectMixerGroup;

            /// <summary>
            /// 全局音量系数
            /// </summary>
            [TitleGroup("音频音量设置")]
            [SerializeField, Range(0, 1), LabelText("全局音量系数")]
            private float globalVolumeFactor = 1;

            /// <summary>
            /// BGM 基准音量
            /// </summary>
            [SerializeField, Range(0, 1), LabelText("BGM基准音量")]
            private float bgVolumeBaseNum = 1;

            /// <summary>
            /// 环境音基准音量
            /// </summary>
            [SerializeField, Range(0, 1), LabelText("环境音基准音量")]
            private float ambienceVolumeBaseNum = 0.8f;

            /// <summary>
            /// 特效音基准音量
            /// </summary>
            [SerializeField, Range(0, 1), LabelText("特效音基准音量")]
            private float effectVolumeBaseNum = 1;

            /// <summary>
            /// 是否静音
            /// </summary>
            [TitleGroup("音频全局控制")]
            [SerializeField, LabelText("静音")]
            private bool isMute;

            /// <summary>
            /// 是否循环播放
            /// </summary>
            [SerializeField, LabelText("循环播放")]
            private bool isLoop;

            /// <summary>
            /// 是否暂停所有
            /// </summary>
            [SerializeField, LabelText("暂停所有")]
            private bool isPause;

            /// <summary>
            /// 同时占用的音效数量上限 包括加载中和暂停中的请求
            /// </summary>
            [TitleGroup("音效播放预算")]
            [SerializeField, Min(1), LabelText("同时音效上限")]
            private int maxConcurrentEffects = 32;

            /// <summary>
            /// 每帧接受的音效请求数量上限
            /// </summary>
            [SerializeField, Min(1), LabelText("每帧请求上限")]
            private int maxEffectsPerFrame = 10;

            /// <summary>
            /// 同一片段或同一路径的同时音效数量上限
            /// </summary>
            [SerializeField, Min(1), LabelText("同音效并发上限")]
            private int maxSameClipEffects = 4;

            /// <summary>
            /// 初始化时预热的特效播放器数量
            /// </summary>
            [SerializeField, Min(0), LabelText("播放器预热数量")]
            private int effectPrewarmCount = 8;

            public GameObject EfPlayerES => efPlayerES;
            public AudioSource BgmSource => bgmSource;
            public AudioSource AmbienceSource => ambienceSource;
            public Transform EffectClipRoot => effectClipRoot;
            public AudioMixer Mixer => mixer;
            public AudioMixerGroup EffectMixerGroup => effectMixerGroup;
            public float GlobalVolumeFactor => globalVolumeFactor;
            public float BgVolumeBaseNum => bgVolumeBaseNum;
            public float AmbienceVolumeBaseNum => ambienceVolumeBaseNum;
            public float EffectVolumeBaseNum => effectVolumeBaseNum;
            public bool IsMute => isMute;
            public bool IsLoop => isLoop;
            public bool IsPause => isPause;
            public int MaxConcurrentEffects => maxConcurrentEffects;
            public int MaxEffectsPerFrame => maxEffectsPerFrame;
            public int MaxSameClipEffects => maxSameClipEffects;
            public int EffectPrewarmCount => effectPrewarmCount;
        }
    }
}
