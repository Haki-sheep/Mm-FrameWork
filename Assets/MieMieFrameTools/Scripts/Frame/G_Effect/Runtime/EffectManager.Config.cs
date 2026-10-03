namespace MieMieFrameWork.Effects
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public sealed partial class EffectManager
    {
        /// <summary>
        /// 特效管理器启动配置
        /// </summary>
        [Serializable]
        public sealed class EffectManagerConfig
        {
            /// <summary> 特效定义列表 同一资源地址只能定义一次 </summary>
            [SerializeField]
            private List<EffectDefinition> definitionList = new();

            /// <summary> 全局同时接受的播放请求上限 </summary>
            [SerializeField]
            private int maxConcurrent = 64;

            /// <summary> 每帧同时接受的新播放请求上限 </summary>
            [SerializeField]
            private int maxPerFrame = 16;

            /// <summary> 初始特效档位 </summary>
            [SerializeField]
            private EEffectQuality quality = EEffectQuality.High;

            /// <summary> 低档位粒子和发射预算 </summary>
            [SerializeField]
            private EffectQualityProfile low = new(0.35f, 0.4f);

            /// <summary> 中档位粒子和发射预算 </summary>
            [SerializeField]
            private EffectQualityProfile medium = new(0.65f, 0.7f);

            /// <summary> 高档位粒子和发射预算 </summary>
            [SerializeField]
            private EffectQualityProfile high = new(1f, 1f);

            public IReadOnlyList<EffectDefinition> DefinitionList => definitionList;
            public int MaxConcurrent => maxConcurrent;
            public int MaxPerFrame => maxPerFrame;
            public EEffectQuality Quality => quality;

            /// <summary>
            /// 创建 Inspector 默认配置
            /// </summary>
            public EffectManagerConfig() { }

            /// <summary>
            /// 显式创建特效配置 供游戏 Bootstrap 或验证场景组装
            /// </summary>
            public EffectManagerConfig(List<EffectDefinition> definitionList, int maxConcurrent = 64,
                int maxPerFrame = 16, EEffectQuality quality = EEffectQuality.High)
            {
                this.definitionList = definitionList;
                this.maxConcurrent = maxConcurrent;
                this.maxPerFrame = maxPerFrame;
                this.quality = quality;
            }

            /// <summary>
            /// 检查预算和档位配置 并生成独立档位快照
            /// </summary>
            internal EffectQualityProfile[] SnapshotProfiles()
            {
                if (definitionList == null || maxConcurrent <= 0 || maxPerFrame <= 0 ||
                    !Enum.IsDefined(typeof(EEffectQuality), quality) || low == null || medium == null || high == null)
                    throw new InvalidOperationException("[EffectManager] 特效列表 全局预算或档位无效");
                return new[] { low.Snapshot(), medium.Snapshot(), high.Snapshot() };
            }
        }

        /// <summary>
        /// 档位缩放参数 每次从原始粒子配置计算
        /// </summary>
        [Serializable]
        public sealed class EffectQualityProfile
        {
            /// <summary> 原始最大粒子数缩放比例 </summary>
            [SerializeField]
            [Range(0.01f, 1f)]
            private float particleFactor = 1f;

            /// <summary> 原始发射曲线和 Burst 数量缩放比例 </summary>
            [SerializeField]
            [Range(0f, 1f)]
            private float emissionFactor = 1f;

            public float ParticleFactor => particleFactor;
            public float EmissionFactor => emissionFactor;

            /// <summary>
            /// 配置粒子数量与发射量缩放
            /// </summary>
            public EffectQualityProfile(float particleFactor, float emissionFactor)
            {
                this.particleFactor = particleFactor;
                this.emissionFactor = emissionFactor;
            }

            /// <summary>
            /// 拒绝非法缩放并保留配置快照
            /// </summary>
            internal EffectQualityProfile Snapshot()
            {
                if (!(particleFactor > 0f && particleFactor <= 1f) ||
                    !(emissionFactor >= 0f && emissionFactor <= 1f))
                    throw new InvalidOperationException("[EffectManager] 特效档位缩放比例无效");
                return new EffectQualityProfile(particleFactor, emissionFactor);
            }
        }
    }
}
