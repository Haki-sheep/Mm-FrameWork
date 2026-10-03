using MieMieFrameWork.Localization;

namespace MieMieFrameWork.Data.Luban.Localization
{
    /// <summary>
    /// 基于 Luban 语言表的多语言宿主 挂在框架根节点上
    /// </summary>
    public sealed class LubanLocalizationHost : LocalizationHost
    {
        /// <summary>
        /// 创建 Luban 文本适配器 不在 Awake 加载数据
        /// </summary>
        protected override ILocalizationTextSource CreateTextSource()
        {
            return new LubanLocalizationTextSource();
        }
    }
}
