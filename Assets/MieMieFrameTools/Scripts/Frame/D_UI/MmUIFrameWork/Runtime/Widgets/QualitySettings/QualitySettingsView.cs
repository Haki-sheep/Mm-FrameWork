namespace MieMieUIFrameWork.Runtime.Widgets
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using MieMieFrameWork.Business.Quality;
    using MiMieEventBus;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary>
    /// 设置界面局部功能组件 由面板壳一次注入服务并编排显示周期
    /// </summary>
    public sealed class QualitySettingsView : MonoBehaviour
    {
        /// <summary> 极低至 Ultra 六档画质下拉框 </summary>
        [SerializeField]
        private TMP_Dropdown levelDropdown;

        /// <summary> 目标帧率输入框 负一表示使用平台默认行为 </summary>
        [SerializeField]
        private TMP_InputField frameRateInput;

        /// <summary> 垂直同步间隔下拉框 </summary>
        [SerializeField]
        private TMP_Dropdown vSyncDropdown;

        /// <summary> 恢复平台默认设置按钮 </summary>
        [SerializeField]
        private Button resetButton;

        /// <summary> 显示期间调用的公共设置服务 </summary>
        private IQualityService service;

        /// <summary> 由壳注入的设置事件总线 </summary>
        private EventBusCore eventBus;

        /// <summary> 组件显示期间持有的单个事件订阅 </summary>
        private IDisposable subscription;

        /// <summary> 唯一组件初始化标记 </summary>
        private bool isInitialized;

        /// <summary> 当前显示周期已绑定事件 </summary>
        private bool eventsBound;

        /// <summary> 下拉框显示顺序 与持久化档位 ID 分离 </summary>
        private static readonly EQualityLevel[] levelList =
        {
            EQualityLevel.VeryLow, EQualityLevel.Low, EQualityLevel.Medium,
            EQualityLevel.High, EQualityLevel.VeryHigh, EQualityLevel.Ultra
        };

        /// <summary>
        /// 由面板壳调用一次 保存依赖并创建控件选项
        /// </summary>
        public void InitComponents(IQualityService Service, EventBusCore EventBus)
        {
            if (isInitialized)
                throw new InvalidOperationException("[QualitySettingsView] 组件只能初始化一次");
            service = Service;
            eventBus = EventBus;
            levelDropdown.ClearOptions();
            levelDropdown.AddOptions(new List<string> { "极低", "低", "中", "高", "极高", "Ultra" });
            vSyncDropdown.ClearOptions();
            vSyncDropdown.AddOptions(new List<string> { "关闭", "1", "2", "3", "4" });
            vSyncDropdown.interactable = !Application.isMobilePlatform;
            frameRateInput.contentType = TMP_InputField.ContentType.IntegerNumber;
            isInitialized = true;
            RefreshView(service.Current);
        }

        /// <summary>
        /// 由面板显示入口绑定控件与通知 不重新初始化
        /// </summary>
        public void BindEvents()
        {
            if (!isInitialized)
                throw new InvalidOperationException("[QualitySettingsView] 必须先初始化组件");
            if (eventsBound)
                return;
            subscription = eventBus.Subscribe(QualityEvents.Changed, RefreshView);
            levelDropdown.onValueChanged.AddListener(OnLevelChanged);
            frameRateInput.onEndEdit.AddListener(OnFrameRateChanged);
            vSyncDropdown.onValueChanged.AddListener(OnVSyncChanged);
            resetButton.onClick.AddListener(OnResetClicked);
            eventsBound = true;
            RefreshView(service.Current);
        }

        /// <summary>
        /// 由面板隐藏入口解除本周期订阅与控件监听
        /// </summary>
        public void UnbindEvents()
        {
            if (!eventsBound)
                return;
            subscription.Dispose();
            subscription = null;
            levelDropdown.onValueChanged.RemoveListener(OnLevelChanged);
            frameRateInput.onEndEdit.RemoveListener(OnFrameRateChanged);
            vSyncDropdown.onValueChanged.RemoveListener(OnVSyncChanged);
            resetButton.onClick.RemoveListener(OnResetClicked);
            eventsBound = false;
        }

        /// <summary>
        /// 刷新已提交状态 不触发控件反向写入
        /// </summary>
        private void RefreshView(QualityState State)
        {
            levelDropdown.SetValueWithoutNotify(Array.IndexOf(levelList, State.Level));
            frameRateInput.SetTextWithoutNotify(State.TargetFrameRate.ToString(CultureInfo.InvariantCulture));
            vSyncDropdown.SetValueWithoutNotify(State.VSyncCount);
        }

        /// <summary>
        /// 接收选项索引 请求公共服务切换档位
        /// </summary>
        private void OnLevelChanged(int Index)
        {
            try
            {
                service.SetLevel(levelList[Index]);
            }
            finally
            {
                RefreshView(service.Current);
            }
        }

        /// <summary>
        /// 接收输入文本 提交帧率并回显实际已提交值
        /// </summary>
        private void OnFrameRateChanged(string Value)
        {
            try
            {
                int FrameRate = int.Parse(Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                service.SetFramePolicy(FrameRate, service.Current.VSyncCount);
            }
            finally
            {
                RefreshView(service.Current);
            }
        }

        /// <summary>
        /// 接收同步间隔 保持独立目标帧率并提交策略
        /// </summary>
        private void OnVSyncChanged(int Count)
        {
            try
            {
                service.SetFramePolicy(service.Current.TargetFrameRate, Count);
            }
            finally
            {
                RefreshView(service.Current);
            }
        }

        /// <summary>
        /// 请求恢复平台默认设置并刷新控件
        /// </summary>
        private void OnResetClicked()
        {
            try
            {
                service.ResetToDefaults();
            }
            finally
            {
                RefreshView(service.Current);
            }
        }

        /// <summary>
        /// Unity 销毁组件时解除残留监听 不释放外部服务
        /// </summary>
        private void OnDestroy()
        {
            UnbindEvents();
            service = null;
            eventBus = null;
        }
    }
}
