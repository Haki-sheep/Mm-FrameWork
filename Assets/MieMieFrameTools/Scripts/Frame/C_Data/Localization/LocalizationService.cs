using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MieMieFrameWork.Localization
{
    /// <summary>
    /// 自研多语言服务 文本与字体准备成功后才切换可见状态
    /// </summary>
    public sealed class LocalizationService : ILocalizationService, IDisposable
    {
        /// <summary> 文本数据来源 </summary>
        private readonly ILocalizationTextSource textSource;
        /// <summary> 按语言加载字体的资源入口 </summary>
        private readonly ILocalizationFontLoader fontLoader;
        /// <summary> 用户语言选择存储入口 </summary>
        private readonly ILocalizationPreferences preferences;
        /// <summary> 初始化完成通知 支持多个 UI 等待 </summary>
        private readonly UniTaskCompletionSource readySource = new UniTaskCompletionSource();
        /// <summary> 服务生命周期取消源 </summary>
        private readonly CancellationTokenSource lifetimeSource = new CancellationTokenSource();
        /// <summary> 当前语言字体的资源租约 </summary>
        private LocaleFontLease currentFontLease;
        /// <summary> 是否已经进入唯一初始化入口 </summary>
        private bool initStarted;
        /// <summary> 是否已经释放服务 </summary>
        private bool disposed;

        public string CurrentLocale { get; private set; }
        public bool IsReady { get; private set; }
        public bool IsSwitching { get; private set; }
        public UniTask ReadyTask => readySource.Task;
        public IReadOnlyList<string> LocaleList => textSource.LocaleList;

        #region 文本与初始化

        /// <summary>
        /// 注入文本 字体和用户设置 不在构造阶段加载资源
        /// </summary>
        public LocalizationService(ILocalizationTextSource TextSource, ILocalizationFontLoader FontLoader,
            ILocalizationPreferences Preferences)
        {
            textSource = TextSource;
            fontLoader = FontLoader;
            preferences = Preferences;
        }

        /// <summary>
        /// 由宿主启动阶段加载文本并提交首个语言
        /// </summary>
        public async UniTask InitAsync(string DefaultLocale, CancellationToken CancellationToken = default)
        {
            ThrowIfDisposed();
            if (initStarted)
                throw new InvalidOperationException("多语言服务只能初始化一次");
            initStarted = true;
            using (var LinkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, lifetimeSource.Token))
            {
                try
                {
                    await textSource.InitAsync(LinkedSource.Token);
                    await SwitchLocaleAsync(preferences.GetLocale(DefaultLocale), LinkedSource.Token);
                    IsReady = true;
                    readySource.TrySetResult();
                }
                catch (OperationCanceledException)
                {
                    readySource.TrySetCanceled(LinkedSource.Token);
                    throw;
                }
                catch (Exception Error)
                {
                    readySource.TrySetException(Error);
                    throw;
                }
            }
        }

        /// <summary>
        /// 根据当前语言返回原始文本 缺 Key 不返回空字符串
        /// </summary>
        public string GetText(string Key)
        {
            EnsureReady();
            return textSource.GetText(CurrentLocale, Key);
        }

        /// <summary>
        /// 按当前语言文化格式化 参数错误保留 Key 和语言上下文
        /// </summary>
        public string FormatText(string Key, params object[] ArgumentList)
        {
            string Text = GetText(Key);
            try
            {
                return string.Format(CultureInfo.GetCultureInfo(CurrentLocale), Text, ArgumentList);
            }
            catch (FormatException Error)
            {
                throw new FormatException($"语言 {CurrentLocale} 文本 {Key} 参数格式化失败", Error);
            }
        }

        /// <summary>
        /// 持有当前语言字体 消费者在不再使用时归还独立租约
        /// </summary>
        public LocaleFontLease AcquireFontLease()
        {
            EnsureReady();
            return currentFontLease.Retain();
        }

        /// <summary>
        /// 请求运行时语言切换 正在切换时拒绝重入
        /// </summary>
        public async UniTask SetLocaleAsync(string Locale, CancellationToken CancellationToken = default)
        {
            EnsureReady();
            if (IsSwitching)
                throw new InvalidOperationException("正在切换语言 请等待当前请求完成");
            using (var LinkedSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, lifetimeSource.Token))
                await SwitchLocaleAsync(Locale, LinkedSource.Token);
        }

        #endregion

        #region 切换与生命周期

        /// <summary>
        /// 先准备目标资源 再提交语言 通知后释放旧字体
        /// </summary>
        private async UniTask SwitchLocaleAsync(string Locale, CancellationToken CancellationToken)
        {
            string CanonicalLocale = CultureInfo.GetCultureInfo(Locale).Name;
            if (!LocaleList.Contains(CanonicalLocale))
                throw new ArgumentException($"语言表没有配置语言 {CanonicalLocale}", nameof(Locale));
            CancellationToken.ThrowIfCancellationRequested();
            if (CurrentLocale == CanonicalLocale)
                return;
            IsSwitching = true;
            LocaleFontLease PreparedLease = null;
            try
            {
                PreparedLease = await fontLoader.LoadAsync(CanonicalLocale, CancellationToken);
                CancellationToken.ThrowIfCancellationRequested();
                ThrowIfDisposed();
                preferences.SetLocale(CanonicalLocale);
                var PreviousLease = currentFontLease;
                currentFontLease = PreparedLease;
                PreparedLease = null;
                CurrentLocale = CanonicalLocale;
                try
                {
                    if (IsReady)
                        MmGlobalEventBus.GlobalBus.Publish(LocalizationEvents.LocaleChanged, CurrentLocale);
                }
                finally
                {
                    PreviousLease?.Dispose();
                }
            }
            finally
            {
                PreparedLease?.Dispose();
                IsSwitching = false;
            }
        }

        /// <summary>
        /// 宿主启动失败时将同一错误传递给等待中的 UI
        /// </summary>
        internal void AbortInitialization(Exception Error)
        {
            readySource.TrySetException(Error);
        }

        /// <summary>
        /// 检查初始化完成状态
        /// </summary>
        private void EnsureReady()
        {
            ThrowIfDisposed();
            if (!IsReady)
                throw new InvalidOperationException("多语言服务尚未初始化 请先等待 ReadyTask");
        }

        /// <summary>
        /// 检查服务生命周期
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(LocalizationService));
        }

        /// <summary>
        /// 取消未完成切换并释放当前语言字体与事件订阅
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            IsReady = false;
            lifetimeSource.Cancel();
            readySource.TrySetCanceled(lifetimeSource.Token);
            currentFontLease?.Dispose();
            currentFontLease = null;
            lifetimeSource.Dispose();
        }

        #endregion
    }
}
