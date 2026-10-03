using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MieMieFrameWork.Business;
using UnityEngine;

namespace MieMieFrameWork.Localization
{
    /// <summary>
    /// 多语言服务唯一生命周期宿主 不改变 ModuleHub 的启动顺序
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public abstract class LocalizationHost : MonoBehaviour
    {
        /// <summary> 框架启动入口 由 Inspector 显式指定 </summary>
        [SerializeField]
        private ModuleHub frameworkRoot;
        /// <summary> 语言与字体的资源配置 </summary>
        [SerializeField]
        private LocalizationFontCatalog fontCatalog;
        /// <summary> 尚未保存选择时使用的默认语言 </summary>
        [SerializeField]
        private string defaultLocale = "zh-CN";
        /// <summary> 用户语言设置的存储 Key </summary>
        [SerializeField]
        private string preferenceKey = "MieMie.Localization.Locale";
        /// <summary> 宿主唯一持有的语言服务 </summary>
        private LocalizationService service;

        public ILocalizationService Service => service;

        /// <summary>
        /// 根节点 Awake 排重完成后组装唯一宿主并开始加载语言
        /// </summary>
        protected virtual void Start()
        {
            if (frameworkRoot == null)
                throw new InvalidOperationException("多语言宿主必须指定框架根节点");
            if (frameworkRoot != ModuleHub.Instance)
                return;
            InitComponents();
            StartServiceAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        /// <summary>
        /// 由唯一有效根节点的 Start 组装文本源 字体加载器和用户设置服务
        /// </summary>
        private void InitComponents()
        {
            if (frameworkRoot == null)
                throw new InvalidOperationException("多语言宿主必须指定框架根节点");
            if (GameHub.TryGet<ILocalizationService>(out var ExistingService))
                throw new InvalidOperationException("当前框架已经注册多语言服务 只能保留一个宿主");
            service = new LocalizationService(CreateTextSource(), new YooAssetLocalizationFontLoader(fontCatalog),
                new PlayerPrefsLocalizationPreferences(preferenceKey));
            GameHub.Register<ILocalizationService>(service);
        }

        /// <summary>
        /// 由具体数据适配器创建文本来源
        /// </summary>
        protected abstract ILocalizationTextSource CreateTextSource();

        /// <summary>
        /// 框架就绪后加载语言 服务失败向所有等待者传递同一错误
        /// </summary>
        private async UniTask StartServiceAsync(CancellationToken CancellationToken)
        {
            try
            {
                await frameworkRoot.ReadyTask.AttachExternalCancellation(CancellationToken);
                await service.InitAsync(defaultLocale, CancellationToken);
            }
            catch (Exception Error)
            {
                service.AbortInitialization(Error);
                throw;
            }
        }

        /// <summary>
        /// 游戏设置按钮可直接调用 业务代码可通过 GameHub 等待切换
        /// </summary>
        public void SetLocale(string Locale)
        {
            service.SetLocaleAsync(Locale, this.GetCancellationTokenOnDestroy()).Forget();
        }

        /// <summary>
        /// 注销自身服务并归还字体资源
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (service == null)
                return;
            if (ReferenceEquals(GameHub.Get<ILocalizationService>(), service))
                GameHub.Unregister<ILocalizationService>();
            service.Dispose();
        }
    }
}
