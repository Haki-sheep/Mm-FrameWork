using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MieMieFrameWork.Business;

namespace MieMieFrameWork.Localization
{
    /// <summary>
    /// 文本查询与运行时语言切换入口
    /// </summary>
    public interface ILocalizationService : IGameService
    {
        public string CurrentLocale { get; }
        public bool IsReady { get; }
        public bool IsSwitching { get; }
        public UniTask ReadyTask { get; }
        public IReadOnlyList<string> LocaleList { get; }

        /// <summary>
        /// 根据当前语言和文本 Key 返回原始文案
        /// </summary>
        public string GetText(string Key);

        /// <summary>
        /// 使用当前语言文化规则格式化文案参数
        /// </summary>
        public string FormatText(string Key, params object[] ArgumentList);

        /// <summary>
        /// 持有当前语言字体与材质 消费者替换或销毁后必须归还
        /// </summary>
        public LocaleFontLease AcquireFontLease();

        /// <summary>
        /// 准备目标字体后提交语言并同步通知绑定组件
        /// </summary>
        public UniTask SetLocaleAsync(string Locale, CancellationToken CancellationToken = default);
    }

    /// <summary>
    /// 多语言文本来源 不依赖具体配置表格式
    /// </summary>
    public interface ILocalizationTextSource
    {
        public IReadOnlyList<string> LocaleList { get; }

        /// <summary>
        /// 加载并检查全部语言文案 仅由服务初始化调用一次
        /// </summary>
        public UniTask InitAsync(CancellationToken CancellationToken);

        /// <summary>
        /// 根据语言和文本 Key 查询文案 缺失时暴露错误
        /// </summary>
        public string GetText(string Locale, string Key);
    }

    /// <summary>
    /// 语言字体加载接口 成功返回字体租约
    /// </summary>
    public interface ILocalizationFontLoader
    {
        /// <summary>
        /// 加载指定语言的全部字体样式
        /// </summary>
        public UniTask<LocaleFontLease> LoadAsync(string Locale, CancellationToken CancellationToken);
    }

    /// <summary>
    /// 用户语言选择的持久化接口
    /// </summary>
    public interface ILocalizationPreferences
    {
        /// <summary>
        /// 读取已保存的语言 未选择时使用明确配置的默认语言
        /// </summary>
        public string GetLocale(string DefaultLocale);

        /// <summary>
        /// 保存已经准备完成的目标语言
        /// </summary>
        public void SetLocale(string Locale);
    }
}
