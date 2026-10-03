namespace MieMieFrameWork.Effects
{
    /// <summary>
    /// 单类特效诊断快照 请求数包含尚未完成的预热和播放
    /// </summary>
    public readonly struct EffectDiagnostics
    {
        public string Id { get; }
        public string Location { get; }
        public int Requests { get; }
        public int Active { get; }
        public int Cached { get; }
        public bool Loaded { get; }

        /// <summary>
        /// 记录资源持有与对象池当前状态
        /// </summary>
        internal EffectDiagnostics(string id, string location, int requests, int active, int cached, bool loaded)
        {
            Id = id;
            Location = location;
            Requests = requests;
            Active = active;
            Cached = cached;
            Loaded = loaded;
        }
    }
}
