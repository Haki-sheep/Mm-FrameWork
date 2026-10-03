namespace MieMieFrameWork.Business.Quality
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 档位映射与平台默认策略 渲染参数由 Unity Quality 预设唯一持有
    /// </summary>
    [Serializable]
    public sealed class QualityConfig
    {
        /// <summary> 稳定档位到 Unity 预设名称的映射 </summary>
        [SerializeField]
        private List<QualityPreset> presetList = new List<QualityPreset>
        {
            new QualityPreset(EQualityLevel.VeryLow, "Haki Very Low"),
            new QualityPreset(EQualityLevel.Low, "Haki Low"),
            new QualityPreset(EQualityLevel.Medium, "Haki Medium"),
            new QualityPreset(EQualityLevel.High, "Haki High"),
            new QualityPreset(EQualityLevel.VeryHigh, "Haki Very High"),
            new QualityPreset(EQualityLevel.Ultra, "Haki Ultra")
        };

        /// <summary> 移动平台首次启动默认档位 </summary>
        [SerializeField]
        private EQualityLevel mobileDefault = EQualityLevel.Medium;

        /// <summary> 非移动平台首次启动默认档位 </summary>
        [SerializeField]
        private EQualityLevel desktopDefault = EQualityLevel.High;

        /// <summary> 独立于画质档位的默认目标帧率 </summary>
        [SerializeField]
        private int targetFrameRate = 60;

        /// <summary> 默认垂直同步间隔 零表示由目标帧率控制 </summary>
        [SerializeField]
        private int vSyncCount;

        /// <summary> 本模块独占的偏好键 与游戏存档槽位分离 </summary>
        [SerializeField]
        private string preferenceKey = "HakiSheep.Graphics";

        public IReadOnlyList<QualityPreset> PresetList => presetList;
        public EQualityLevel MobileDefault => mobileDefault;
        public EQualityLevel DesktopDefault => desktopDefault;
        public int TargetFrameRate => targetFrameRate;
        public int VSyncCount => vSyncCount;
        public string PreferenceKey => preferenceKey;
    }

    /// <summary>
    /// 一个稳定档位的 Unity 预设名称 不保存构建相关的索引
    /// </summary>
    [Serializable]
    public sealed class QualityPreset
    {
        /// <summary> 对外及持久化使用的稳定档位 </summary>
        [SerializeField]
        private EQualityLevel level;

        /// <summary> 当前构建必须保留的 Unity Quality 预设名称 </summary>
        [SerializeField]
        private string unityQualityName;

        public EQualityLevel Level => level;
        public string UnityQualityName => unityQualityName;

        /// <summary>
        /// 接收稳定档位与预设名称 创建映射配置
        /// </summary>
        public QualityPreset(EQualityLevel eLevel, string UnityQualityName)
        {
            level = eLevel;
            unityQualityName = UnityQualityName;
        }
    }
}
