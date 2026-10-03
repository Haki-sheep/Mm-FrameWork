namespace MieMieFrameWork
{
    /// <summary>
    /// 卡顿后遗漏回调的处理方式
    /// </summary>
    public enum TimerCatchUpMode
    {
        /// <summary> 每次驱动最多执行一次并跳过遗漏周期 不扣除遗漏次数 </summary>
        SkipMissed,

        /// <summary> 按上限补执行到期回调 未处理积压保留到后续驱动 </summary>
        CatchUp
    }
}
