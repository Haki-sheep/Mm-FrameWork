using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MieMieFrameWork.Business;
using TMPro;
using UnityEngine;

namespace MieMieFrameWork.Localization
{
    /// <summary>
    /// 绑定文本 Key 与字体样式 场景与动态 UI Prefab 共用公共服务
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class LocalizedTMPText : MonoBehaviour
    {
        /// <summary> 文本表查询 Key </summary>
        [SerializeField]
        private string textKey;
        /// <summary> 当前语言主字体的样式标识 </summary>
        [SerializeField]
        private string fontStyle = "Body";
        /// <summary> 缓存的目标 TMP 组件 </summary>
        private TMP_Text targetText;
        /// <summary> 初始主字体 移除绑定组件时恢复 </summary>
        private TMP_FontAsset originalFont;
        /// <summary> 初始材质 移除绑定组件时恢复 </summary>
        private Material originalMaterial;
        /// <summary> 框架发布的语言服务 </summary>
        private ILocalizationService service;
        /// <summary> 当前文本持有的字体资源租约 </summary>
        private LocaleFontLease fontLease;
        /// <summary> 动态文本格式化参数 </summary>
        private object[] argumentList = Array.Empty<object>();
        /// <summary> 当前启用周期的取消源 </summary>
        private CancellationTokenSource enableSource;
        /// <summary> 当前启用周期的事件订阅令牌 </summary>
        private IDisposable subscription;
        /// <summary> 是否已经完成唯一服务绑定入口 </summary>
        private bool started;

        #region 生命周期与订阅

        /// <summary>
        /// 缓存组件引用 仅由 Awake 调用一次
        /// </summary>
        private void Awake()
        {
            InitComponents();
        }

        /// <summary>
        /// 缓存文本与原始显示资源 不依赖场景宿主引用
        /// </summary>
        private void InitComponents()
        {
            targetText = GetComponent<TMP_Text>();
            originalFont = targetText.font;
            originalMaterial = targetText.fontSharedMaterial;
        }

        /// <summary>
        /// 宿主 Start 发布服务后绑定一次 同时支持运行时加载的 Prefab
        /// </summary>
        private void Start()
        {
            service = GameHub.Get<ILocalizationService>();
            if (service == null)
                throw new InvalidOperationException($"文本 {name} 找不到多语言服务 请先接入有效框架根节点");
            started = true;
            BindEvents();
        }

        /// <summary>
        /// 再次启用时重新订阅并刷新 当前字体租约一直保留至替换
        /// </summary>
        private void OnEnable()
        {
            if (started)
                BindEvents();
        }

        /// <summary>
        /// 订阅统一语言事件并等待同一次服务初始化
        /// </summary>
        private void BindEvents()
        {
            enableSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            subscription = MmGlobalEventBus.GlobalBus.Subscribe(LocalizationEvents.LocaleChanged, OnLocaleChanged);
            RefreshWhenReadyAsync(enableSource.Token).Forget();
        }

        /// <summary>
        /// 初始化完成后刷新 取消时不继续访问停用的文本
        /// </summary>
        private async UniTask RefreshWhenReadyAsync(CancellationToken CancellationToken)
        {
            bool Canceled = await service.ReadyTask.AttachExternalCancellation(CancellationToken).SuppressCancellationThrow();
            if (!Canceled)
                RefreshText();
        }

        /// <summary>
        /// 解除事件与等待 不释放仍被 TMP 引用的字体资源
        /// </summary>
        private void OnDisable()
        {
            subscription?.Dispose();
            subscription = null;
            enableSource?.Cancel();
            enableSource?.Dispose();
            enableSource = null;
        }

        /// <summary>
        /// 先移除 TMP 对租约字体的引用 再归还消费者资源
        /// </summary>
        private void OnDestroy()
        {
            if (fontLease == null)
                return;
            if (targetText != null)
            {
                targetText.font = originalFont;
                targetText.fontSharedMaterial = originalMaterial;
                targetText.ForceMeshUpdate(true);
            }
            fontLease.Dispose();
            fontLease = null;
        }

        #endregion

        #region 文本与字体刷新

        /// <summary>
        /// 语言事实通知到达后同步刷新文字与主字体
        /// </summary>
        private void OnLocaleChanged(string Locale)
        {
            if (service.IsReady)
                RefreshText();
        }

        /// <summary>
        /// 更新业务参数 不重新初始化组件
        /// </summary>
        public void SetArguments(params object[] ArgumentList)
        {
            argumentList = ArgumentList ?? Array.Empty<object>();
            if (started && isActiveAndEnabled && service.IsReady)
                RefreshText();
        }

        /// <summary>
        /// 准备新显示资源 完成 TMP 重建后归还旧字体租约
        /// </summary>
        public void RefreshText()
        {
            string Text = argumentList.Length == 0 ? service.GetText(textKey) : service.FormatText(textKey, argumentList);
            var PreparedLease = service.AcquireFontLease();
            var PreviousFont = targetText.font;
            var PreviousMaterial = targetText.fontSharedMaterial;
            string PreviousText = targetText.text;
            try
            {
                var Font = PreparedLease.GetFont(fontStyle);
                var Material = PreparedLease.GetMaterial(fontStyle);
                targetText.font = Font;
                targetText.fontSharedMaterial = Material;
                targetText.text = Text;
                targetText.ForceMeshUpdate(true);
                var PreviousLease = fontLease;
                fontLease = PreparedLease;
                PreparedLease = null;
                PreviousLease?.Dispose();
            }
            catch
            {
                targetText.font = PreviousFont;
                targetText.fontSharedMaterial = PreviousMaterial;
                targetText.text = PreviousText;
                throw;
            }
            finally
            {
                PreparedLease?.Dispose();
            }
        }

        #endregion
    }
}
