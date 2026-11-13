namespace MieMieFrameWork.Boot
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using MieMieFrameWork.Asset;
    using MieMieFrameWork.Diagnostics;
    using MieMieFrameWork.HotUpdate;
    /// <summary>
    /// 启动编排入口 显式安排基础设施组装与运行时管理器初始化
    /// </summary>
    public sealed class BootManager : IDisposable
    {
        /// <summary> 框架根节点 提供管理器组装与生命周期操作 </summary>
        private readonly ModuleHub moduleHub;

        /// <summary> 启动结果 支持多个业务入口等待同一次启动 </summary>
        private readonly UniTaskCompletionSource readySource = new UniTaskCompletionSource();

        /// <summary> 本次启动的热更入口所有者 </summary>
        private HotUpdateRuntime hotUpdateRuntime;

        public bool IsReady { get; private set; }

        public UniTask ReadyTask => readySource.Task;

        /// <summary>
        /// 接收框架根节点 不在构造阶段启动管理器
        /// </summary>
        public BootManager(ModuleHub moduleHub)
        {
            this.moduleHub = moduleHub;
        }

        /// <summary>
        /// 由 ModuleHub Awake 调用一次 任一阶段失败立即中断
        /// </summary>
        internal async UniTask InitComponents(string packageName, int manifestTimeout, ResourceUpdateConfig resourceConfig,
            HotUpdateConfig codeConfig, CancellationToken cancellationToken)
        {
            try
            {
                // 启动资源包
                FrameLog.SetContext("BootStage", "Resources");
                FrameLog.Info("开始初始化默认资源包", "Boot");
                await YooAssetMgr.InitializeAsync(packageName, manifestTimeout, resourceConfig, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (codeConfig.enabled)
                {
                    FrameLog.SetContext("BootStage", "HotUpdate");
                    hotUpdateRuntime = new HotUpdateRuntime();
                    await hotUpdateRuntime.LoadAsync(YooAssetMgr.DefaultPackage, codeConfig, cancellationToken);
                }
                // 初始化数据配置
                FrameLog.SetContext("BootStage", "Config");
                moduleHub.InitConfigModule();
                // 注册管理器
                FrameLog.SetContext("BootStage", "RegisterManagers");
                moduleHub.RegisterAllManagers();
                // 初始化管理器
                FrameLog.SetContext("BootStage", "Managers");
                moduleHub.InitManagers();
                if (hotUpdateRuntime != null)
                {
                    FrameLog.SetContext("BootStage", "HotUpdateEntry");
                    await hotUpdateRuntime.StartAsync(cancellationToken);
                }
                cancellationToken.ThrowIfCancellationRequested();
                // 设置启动完成
                FrameLog.SetContext("BootStage", "Ready");
                FrameLog.Info("框架启动完成", "Boot");
                IsReady = true;
                // 设置启动完成
                readySource.TrySetResult();
            }
            catch (Exception Error)
            {
                Exception Failure = Error;
                try
                {
                    Dispose();
                }
                catch (Exception CleanupError)
                {
                    Failure = new AggregateException("框架启动失败且热更入口清理失败", Failure, CleanupError);
                }
                try
                {
                    moduleHub.ReleaseConfigModule();
                }
                catch (Exception CleanupError)
                {
                    Failure = new AggregateException("框架启动失败且配置清理失败", Failure, CleanupError);
                }

                if (Failure is OperationCanceledException)
                    readySource.TrySetCanceled(cancellationToken);
                else
                    readySource.TrySetException(Failure);

                if (ReferenceEquals(Failure, Error))
                    throw;

                throw Failure;
            }
        }

        /// <summary>
        /// 释放热更业务持有 程序集由运行会话持有直到 Player 退出
        /// </summary>
        public void Dispose()
        {
            var Runtime = hotUpdateRuntime;
            hotUpdateRuntime = null;
            Runtime?.Dispose();
        }
    }
}
