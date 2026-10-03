namespace MieMieFrameWork.Effects
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 特效资源地址 池容量与播放规则
    /// </summary>
    [Serializable]
    public sealed class EffectDefinition
    {
        /// <summary> 业务特效标识 </summary>
        [SerializeField]
        private string id;

        /// <summary> 默认资源包中的预制体地址 </summary>
        [SerializeField]
        private string location;

        /// <summary> 循环特效必须显式停止 </summary>
        [SerializeField]
        private bool looping;

        /// <summary> 同类请求上限 包括加载中请求 </summary>
        [SerializeField]
        private int maxConcurrent = 8;

        /// <summary> 闲置实例缓存上限 </summary>
        [SerializeField]
        private int maxInactive = 8;

        /// <summary> 显式预热的闲置实例目标数量 </summary>
        [SerializeField]
        private int prewarmCount;

        public string Id => id;
        public string Location => location;
        public bool Looping => looping;
        public int MaxConcurrent => maxConcurrent;
        public int MaxInactive => maxInactive;
        public int PrewarmCount => prewarmCount;

        /// <summary>
        /// 创建特效配置 后续由管理器快照持有
        /// </summary>
        public EffectDefinition(string id, string location, bool looping = false, int maxConcurrent = 8,
            int maxInactive = 8, int prewarmCount = 0)
        {
            this.id = id;
            this.location = location;
            this.looping = looping;
            this.maxConcurrent = maxConcurrent;
            this.maxInactive = maxInactive;
            this.prewarmCount = prewarmCount;
        }

        /// <summary>
        /// 验证地址和容量并生成独立快照
        /// </summary>
        internal EffectDefinition Snapshot()
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(location))
                throw new InvalidOperationException("[EffectManager] 特效标识和资源地址不能为空");
            if (maxConcurrent <= 0 || maxInactive < 0 || maxInactive > maxConcurrent ||
                prewarmCount < 0 || prewarmCount > maxInactive)
                throw new InvalidOperationException($"[EffectManager] 特效 {id} 池容量或预热数量无效");
            return new EffectDefinition(id, location, looping, maxConcurrent, maxInactive, prewarmCount);
        }
    }
}
