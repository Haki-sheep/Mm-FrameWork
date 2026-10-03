namespace MieMieFrameWork.Boot
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using MieMieFrameWork.Asset;
    using MieMieFrameWork.Diagnostics;
    /// <summary>
    /// 启动编排入口 显式安排基础设施组装与运行时管理器初始化
    /// </summary>
    public sealed class BootManager
    {
        /// <summary> 框架根节点 提供管理器组装与生命周期操作 </summary>
        private readonly ModuleHub moduleHub;

        /// <summary> 启动结果 支持多个业务入口等待同一次启动 </summary>
        private readonly UniTaskCompletionSource readySource = new UniTaskCompletionSource();

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
        internal async UniTask InitComponents(string packageName, int manifestTimeout, CancellationToken cancellationToken)
        {
            try
            {
                // 启动资源包
                FrameLog.SetContext("BootStage", "Resources");
                FrameLog.Info("开始初始化默认资源包", "Boot");
                await YooAssetMgr.InitializeAsync(packageName, manifestTimeout, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                // 初始化数据配置
                FrameLog.SetContext("BootStage", "Config");
                moduleHub.InitConfigModule();
                // 注册管理器
                FrameLog.SetContext("BootStage", "RegisterManagers");
                moduleHub.RegisterAllManagers();
                // 初始化管理器
                FrameLog.SetContext("BootStage", "Managers");
                moduleHub.InitManagers();
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
                    moduleHub.ReleaseConfigModule();
                }
                catch (Exception CleanupError)
                {
                    Failure = new AggregateException("框架启动失败且配置清理失败", Error, CleanupError);
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
    }
}
