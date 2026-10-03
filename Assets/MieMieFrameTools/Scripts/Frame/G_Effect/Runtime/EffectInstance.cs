namespace MieMieFrameWork.Effects
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 特效预制体根组件 缓存粒子并刷新单轮播放状态
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EffectInstance : MonoBehaviour
    {
        /// <summary> 可选特效节点与最低启用档位 </summary>
        [SerializeField]
        private OptionalNode[] optionalNodeList = Array.Empty<OptionalNode>();

        /// <summary> 全部子粒子系统的原始状态 </summary>
        private ParticleState[] particleStateList;

        /// <summary> 需要显式启动的非子发射器粒子系统 </summary>
        private ParticleSystem[] playbackParticleList;

        /// <summary> 可复用拖尾组件 </summary>
        private TrailRenderer[] trailList;

        /// <summary> 预制体原始局部缩放 </summary>
        private Vector3 initialScale;

        public bool HasLoop { get; private set; }

        /// <summary>
        /// 可选节点仅允许控制当前预制体的粒子分支
        /// </summary>
        [Serializable]
        private sealed class OptionalNode
        {
            /// <summary> 特效内的可选子节点 </summary>
            [SerializeField]
            private GameObject node;

            /// <summary> 启用节点所需的最低档位 </summary>
            [SerializeField]
            private EEffectQuality minimumQuality = EEffectQuality.Medium;

            public GameObject Node => node;
            public EEffectQuality MinimumQuality => minimumQuality;
            public bool InitiallyActive { get; set; }
        }

        /// <summary>
        /// 单个粒子系统的原始配置与可复用 Burst 缓冲
        /// </summary>
        private sealed class ParticleState
        {
            public ParticleSystem System { get; set; }
            public int MaxParticles { get; set; }
            public float RateOverTime { get; set; }
            public float RateOverDistance { get; set; }
            public ParticleSystem.Burst[] OriginalBurstList { get; set; }
            public ParticleSystem.Burst[] WorkingBurstList { get; set; }
        }

        #region 唯一组件初始化

        /// <summary>
        /// 由资源条目在实例第一次取出时调用一次 不依赖 Awake
        /// </summary>
        internal void InitComponents()
        {
            var SystemList = GetComponentsInChildren<ParticleSystem>(true);
            if (SystemList.Length == 0)
                throw new InvalidOperationException($"[EffectInstance] {name} 没有 ParticleSystem");
            particleStateList = new ParticleState[SystemList.Length];
            var PlaybackList = new List<ParticleSystem>();
            var SubEmitterHashList = new HashSet<ParticleSystem>();
            foreach (var System in SystemList)
            {
                var SubEmitters = System.subEmitters;
                for (int Index = 0; Index < SubEmitters.subEmittersCount; Index++)
                {
                    var SubEmitter = SubEmitters.GetSubEmitterSystem(Index);
                    if (SubEmitter == null || !SubEmitter.transform.IsChildOf(transform))
                        throw new InvalidOperationException($"[EffectInstance] {name} 的子发射器不在特效层级内");
                    SubEmitterHashList.Add(SubEmitter);
                }
            }
            for (int Index = 0; Index < SystemList.Length; Index++)
            {
                var System = SystemList[Index];
                var Main = System.main;
                var Emission = System.emission;
                var BurstList = new ParticleSystem.Burst[Emission.burstCount];
                Emission.GetBursts(BurstList);
                particleStateList[Index] = new ParticleState
                {
                    System = System,
                    MaxParticles = Main.maxParticles,
                    RateOverTime = Emission.rateOverTimeMultiplier,
                    RateOverDistance = Emission.rateOverDistanceMultiplier,
                    OriginalBurstList = BurstList,
                    WorkingBurstList = new ParticleSystem.Burst[BurstList.Length]
                };
                Main.playOnAwake = false;
                Main.stopAction = ParticleSystemStopAction.None;
                HasLoop |= Main.loop;
                if (!SubEmitterHashList.Contains(System))
                    PlaybackList.Add(System);
            }
            playbackParticleList = PlaybackList.ToArray();
            trailList = GetComponentsInChildren<TrailRenderer>(true);
            initialScale = transform.localScale;
            foreach (var Node in optionalNodeList)
            {
                if (Node == null || Node.Node == null || Node.Node == gameObject ||
                    !Node.Node.transform.IsChildOf(transform) ||
                    !Enum.IsDefined(typeof(EEffectQuality), Node.MinimumQuality))
                    throw new InvalidOperationException($"[EffectInstance] {name} 可选节点或最低档位无效");
                Node.InitiallyActive = Node.Node.activeSelf;
            }
            RefreshStopped();
        }

        #endregion

        #region 播放与复用

        /// <summary>
        /// 激活前清理上轮粒子并设置本轮姿态 时间模式与档位
        /// </summary>
        internal void RefreshPlayback(EffectSpawnOptions options, EEffectQuality quality,
            EffectManager.EffectQualityProfile profile)
        {
            RefreshStopped();
            RefreshPose(options);
            transform.localScale = initialScale * options.Scale;
            foreach (var State in particleStateList)
            {
                var Main = State.System.main;
                Main.useUnscaledTime = options.UseUnscaledTime;
            }
            ApplyQuality(quality, profile, false);
        }

        /// <summary>
        /// 有所有者时计算局部偏移 否则直接使用世界姿态
        /// </summary>
        internal void RefreshPose(EffectSpawnOptions options)
        {
            var Owner = options.Owner;
            transform.SetPositionAndRotation(Owner != null ? Owner.TransformPoint(options.Position) : options.Position,
                Owner != null ? Owner.rotation * options.Rotation : options.Rotation);
        }

        /// <summary>
        /// 启动可见的非子发射器系统 子发射器由原配置触发
        /// </summary>
        internal void Play()
        {
            foreach (var System in playbackParticleList)
                if (System.gameObject.activeInHierarchy)
                    System.Play(false);
        }

        /// <summary>
        /// 对整组粒子应用暂停或继续 不重启已经完成的系统
        /// </summary>
        internal void RefreshPaused(bool paused)
        {
            foreach (var State in particleStateList)
            {
                if (paused && State.System.isPlaying)
                    State.System.Pause(false);
                else if (!paused && State.System.isPaused && State.System.gameObject.activeInHierarchy)
                    State.System.Play(false);
            }
        }

        /// <summary>
        /// 停止并清空粒子与拖尾 不直接销毁池实例
        /// </summary>
        internal void RefreshStopped()
        {
            foreach (var State in particleStateList)
                State.System.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (var Trail in trailList)
                Trail.Clear();
        }

        /// <summary>
        /// 全部启用系统和子发射器均结束后允许自然回收
        /// </summary>
        internal bool IsAlive()
        {
            foreach (var State in particleStateList)
                if (State.System.gameObject.activeInHierarchy && State.System.IsAlive(false))
                    return true;
            return false;
        }

        #endregion

        #region 档位刷新

        /// <summary>
        /// 从原始发射曲线计算档位 可选节点升档后继续本轮播放
        /// </summary>
        internal void ApplyQuality(EEffectQuality quality, EffectManager.EffectQualityProfile profile, bool playing)
        {
            foreach (var State in particleStateList)
            {
                var Main = State.System.main;
                var Emission = State.System.emission;
                Main.maxParticles = Mathf.Max(1, Mathf.CeilToInt(State.MaxParticles * profile.ParticleFactor));
                Emission.rateOverTimeMultiplier = State.RateOverTime * profile.EmissionFactor;
                Emission.rateOverDistanceMultiplier = State.RateOverDistance * profile.EmissionFactor;
                for (int Index = 0; Index < State.OriginalBurstList.Length; Index++)
                {
                    var Burst = State.OriginalBurstList[Index];
                    Burst.count = ScaleCurve(Burst.count, profile.EmissionFactor);
                    State.WorkingBurstList[Index] = Burst;
                }
                Emission.SetBursts(State.WorkingBurstList);
            }
            foreach (var Node in optionalNodeList)
            {
                bool Active = Node.InitiallyActive && quality >= Node.MinimumQuality;
                if (Node.Node.activeSelf == Active)
                    continue;
                Node.Node.SetActive(Active);
                foreach (var State in particleStateList)
                {
                    if (!State.System.transform.IsChildOf(Node.Node.transform))
                        continue;
                    if (!Active)
                        State.System.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                    else if (playing && Array.IndexOf(playbackParticleList, State.System) >= 0 &&
                        State.System.gameObject.activeInHierarchy)
                        State.System.Play(false);
                }
            }
        }

        /// <summary>
        /// 保留曲线模式和曲线引用 只缩放常量或曲线倍率
        /// </summary>
        private static ParticleSystem.MinMaxCurve ScaleCurve(ParticleSystem.MinMaxCurve curve, float factor)
        {
            if (curve.mode == ParticleSystemCurveMode.Constant || curve.mode == ParticleSystemCurveMode.TwoConstants)
            {
                curve.constantMin *= factor;
                curve.constantMax *= factor;
            }
            else
                curve.curveMultiplier *= factor;
            return curve;
        }

        #endregion
    }
}
