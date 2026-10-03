namespace MieMieFrameWork.Business.Quality
{
    /// <summary>
    /// 已提交的画质与帧率策略快照 不表示设备实际输出帧率
    /// </summary>
    public readonly struct QualityState
    {
        public EQualityLevel Level { get; }
        public string UnityQualityName { get; }
        public int TargetFrameRate { get; }
        public int VSyncCount { get; }

        /// <summary>
        /// 接收已应用参数 保存不可变状态快照
        /// </summary>
        public QualityState(EQualityLevel eLevel, string UnityQualityName, int TargetFrameRate, int VSyncCount)
        {
            Level = eLevel;
            this.UnityQualityName = UnityQualityName;
            this.TargetFrameRate = TargetFrameRate;
            this.VSyncCount = VSyncCount;
        }
    }
}
