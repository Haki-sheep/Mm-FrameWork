namespace MieMieFrameWork.Business.Quality
{
    /// <summary>
    /// 画质设置公共边界 ReadyTask 完成后通过 GameHub 获取
    /// </summary>
    public interface IQualityService : IGameService
    {
        public QualityState Current { get; }

        /// <summary>
        /// 切换画质档位 保持独立帧率策略并保存用户偏好
        /// </summary>
        public void SetLevel(EQualityLevel eLevel);

        /// <summary>
        /// 修改目标帧率与垂直同步策略 保持当前画质档位
        /// </summary>
        public void SetFramePolicy(int TargetFrameRate, int VSyncCount);

        /// <summary>
        /// 恢复平台默认设置 只删除本模块偏好
        /// </summary>
        public void ResetToDefaults();
    }
}
