namespace MieMieFrameWork.Business.Quality
{
    using System;
    using System.Collections.Generic;
    using MiMieEventBus;
    using UnityEngine;

    /// <summary>
    /// 画质模块所有者 统一管理设置提交 服务注册与偏好持有
    /// </summary>
    [ModuleHub.ManagerAttribute(1)]
    public sealed partial class QualityManager : ModuleHub.IManagerBase, IQualityService, IDisposable
    {
        /// <summary> 启动配置 初始化时复制必要参数后解除持有 </summary>
        private QualityConfig config;

        /// <summary> 设置完成通知总线 </summary>
        private readonly EventBusCore eventBus;

        /// <summary> 稳定档位到预设名称的运行时映射 </summary>
        private readonly Dictionary<EQualityLevel, string> presetDict = new Dictionary<EQualityLevel, string>();

        /// <summary> 创建线程 Unity API 与事件只允许此线程操作 </summary>
        private readonly int ownerThreadId;

        /// <summary> 本模块独占偏好键 </summary>
        private string preferenceKey;

        /// <summary> 首次启动及手动恢复使用的平台默认状态 </summary>
        private QualityState defaultState;

        /// <summary> 运行期间唯一的已提交状态 </summary>
        private QualityState current;

        /// <summary> 唯一初始化入口调用标记 </summary>
        private bool initStarted;

        /// <summary> 完成初始化并可接受业务调用 </summary>
        private bool isInitialized;

        /// <summary> 当前提交与同步通知期间拒绝重入 </summary>
        private bool isApplying;

        /// <summary> 模块已释放 不再接受查询与修改 </summary>
        private bool isDisposed;

        public QualityState Current
        {
            get
            {
                VerifyReady();
                return current;
            }
        }

        /// <summary>
        /// 接收配置与事件总线 不在构造期间修改全局画质
        /// </summary>
        public QualityManager(QualityConfig Config, EventBusCore EventBus)
        {
            config = Config;
            eventBus = EventBus;
            ownerThreadId = Environment.CurrentManagedThreadId;
        }

        /// <summary>
        /// 由 ModuleHub 调用一次 校验配置恢复偏好并注册公共服务
        /// </summary>
        public void Init()
        {
            VerifyThread();
            if (isDisposed)
                throw new ObjectDisposedException(nameof(QualityManager));
            if (initStarted)
                throw new InvalidOperationException("[Quality] 初始化只能调用一次");
            initStarted = true;
            if (GameHub.TryGet<IQualityService>(out var ExistingService))
                throw new InvalidOperationException("[Quality] 公共服务已注册");

            PrepareConfig();
            var State = ReadPreference();
            ApplyUnitySettings(State);
            current = State;
            isInitialized = true;
            config = null;
            GameHub.Register<IQualityService>(this);
        }

        /// <summary>
        /// 切换稳定档位 保持当前目标帧率与垂直同步策略
        /// </summary>
        public void SetLevel(EQualityLevel eLevel)
        {
            VerifyReady();
            Commit(CreateState(eLevel, current.TargetFrameRate, current.VSyncCount), false);
        }

        /// <summary>
        /// 修改独立帧率策略 不隐式改变画质档位
        /// </summary>
        public void SetFramePolicy(int TargetFrameRate, int VSyncCount)
        {
            VerifyReady();
            Commit(CreateState(current.Level, TargetFrameRate, VSyncCount), false);
        }

        /// <summary>
        /// 恢复平台默认设置并删除本模块单个偏好键
        /// </summary>
        public void ResetToDefaults()
        {
            VerifyReady();
            Commit(defaultState, true);
        }

        /// <summary>
        /// 释放映射与服务持有 不删除偏好或替其他模块恢复全局设置
        /// </summary>
        public void Dispose()
        {
            VerifyThread();
            if (isDisposed)
                return;
            if (isApplying)
                throw new InvalidOperationException("[Quality] 设置通知期间不能释放管理器");
            isDisposed = true;
            isInitialized = false;
            config = null;
            presetDict.Clear();
            if (GameHub.TryGet<IQualityService>(out var Service) && ReferenceEquals(Service, this))
                GameHub.Unregister<IQualityService>();
        }
    }
}
