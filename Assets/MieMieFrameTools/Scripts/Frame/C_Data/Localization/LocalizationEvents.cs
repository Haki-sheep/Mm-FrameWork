using MiMieEventBus;

namespace MieMieFrameWork.Localization
{
    /// <summary>
    /// 多语言公共状态通知 使用项目统一事件总线
    /// </summary>
    public static class LocalizationEvents
    {
        /// <summary> 目标语言及字体已经提交 </summary>
        public static readonly EventKey<string> LocaleChanged = new EventKey<string>("Localization.LocaleChanged");
    }
}
