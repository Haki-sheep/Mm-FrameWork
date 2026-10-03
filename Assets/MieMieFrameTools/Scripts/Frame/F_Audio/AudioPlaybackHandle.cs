namespace MieMieFrameWork
{
    /// <summary>
    /// 单次音效请求标识 不使用池对象 ID 以免旧句柄影响新播放
    /// </summary>
    public readonly struct AudioPlaybackHandle
    {
        internal AudioManager Owner { get; }
        public ulong Id { get; }
        public bool IsValid => Owner != null && Owner.IsEffectPlaying(this);

        /// <summary>
        /// 绑定管理器会话与单次请求编号 默认值表示请求未被接受
        /// </summary>
        internal AudioPlaybackHandle(AudioManager owner, ulong id)
        {
            Owner = owner;
            Id = id;
        }
    }
}
