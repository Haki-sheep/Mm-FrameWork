namespace MieMieFrameWork.Business.Quality
{
    using MiMieEventBus;

    /// <summary>
    /// 画质与偏好提交后的通知 消费者自行持有和释放订阅
    /// </summary>
    public static class QualityEvents
    {
        /// <summary> 已提交的画质与帧率策略 不承担必需业务步骤 </summary>
        public static readonly EventKey<QualityState> Changed = new EventKey<QualityState>("Quality.Changed");
    }
}
