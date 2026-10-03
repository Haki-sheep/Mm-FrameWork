namespace MieMieFrameWork.Effects
{
    /// <summary>
    /// 特效档位 不持有全局画质状态
    /// </summary>
    public enum EEffectQuality { Low, Medium, High }

    /// <summary>
    /// 请求接受或预算拒绝结果 加载与配置错误直接抛出
    /// </summary>
    public enum EEffectPlayStatus { Accepted, GlobalLimit, DefinitionLimit, FrameLimit }

    /// <summary>
    /// 播放结果 拒绝结果不包含有效播放句柄
    /// </summary>
    public readonly struct EffectPlayResult
    {
        public EEffectPlayStatus Status { get; }
        public EffectHandle Handle { get; }
        public bool Accepted => Status == EEffectPlayStatus.Accepted && Handle.IsValid;

        /// <summary>
        /// 组装预算状态与本轮播放句柄
        /// </summary>
        internal EffectPlayResult(EEffectPlayStatus status, EffectHandle handle = default)
        {
            Status = status;
            Handle = handle;
        }
    }

    /// <summary>
    /// 单轮播放句柄 旧句柄不操作复用后的实例
    /// </summary>
    public readonly struct EffectHandle
    {
        /// <summary> 本轮播放所属管理器 </summary>
        private readonly EffectManager manager;

        /// <summary> 管理器内单调递增的播放标识 </summary>
        private readonly ulong id;

        public bool IsValid => manager != null && manager.IsValid(id);

        /// <summary>
        /// 绑定管理器与本轮请求标识
        /// </summary>
        internal EffectHandle(EffectManager manager, ulong id)
        {
            this.manager = manager;
            this.id = id;
        }

        /// <summary>
        /// 立即停止并回收本轮播放 过期句柄返回 false
        /// </summary>
        public bool Stop() => manager != null && manager.Stop(id);

        /// <summary>
        /// 暂停本轮粒子播放 过期句柄返回 false
        /// </summary>
        public bool Pause() => manager != null && manager.SetPaused(id, true);

        /// <summary>
        /// 恢复本轮播放 全局暂停仍然生效
        /// </summary>
        public bool Resume() => manager != null && manager.SetPaused(id, false);
    }
}
