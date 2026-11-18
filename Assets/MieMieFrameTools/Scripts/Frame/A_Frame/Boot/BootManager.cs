namespace MieMieFrameWork.Boot
{
    using System;
    using System.Collections.Generic;
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

        private readonly CancellationTokenSource lifetimeCancellation = new CancellationTokenSource();
        private readonly YooAssetMgr.StartupResources startupResources = new YooAssetMgr.StartupResources();
        /// <summary> 取消回调可能重入启动路径 回调全部退出后才释放管理器 </summary>
        private UniTaskCompletionSource requestingCancellation;
        private Exception cancellationFailure;
        private bool startupActive;
        private bool shutdownRequested;
        private bool cleanupStarted;

        /// <summary> 本次启动的热更入口所有者 </summary>
        private HotUpdateRuntime hotUpdateRuntime;

        public bool IsReady { get; private set; }

        public UniTask ReadyTask => readySource.Task;

        #region 启动

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
            HotUpdateConfig codeConfig)
        {
            startupActive = true;
            var CancellationToken = lifetimeCancellation.Token;
            try
            {
                // 启动资源包
                FrameLog.SetContext("BootStage", "Resources");
                FrameLog.Info("开始初始化默认资源包", "Boot");
                await startupResources.InitializeAsync(packageName, manifestTimeout, resourceConfig, CancellationToken);
                CancellationToken.ThrowIfCancellationRequested();
                if (codeConfig.enabled)
                {
                    FrameLog.SetContext("BootStage", "HotUpdate");
                    hotUpdateRuntime = new HotUpdateRuntime();
                    await hotUpdateRuntime.LoadAsync(YooAssetMgr.DefaultPackage, codeConfig, CancellationToken);
                    CancellationToken.ThrowIfCancellationRequested();
                }
                // 初始化数据配置
                FrameLog.SetContext("BootStage", "Config");
                moduleHub.InitConfigModule();
                CancellationToken.ThrowIfCancellationRequested();
                // 注册管理器
                FrameLog.SetContext("BootStage", "RegisterManagers");
                moduleHub.RegisterAllManagers(CancellationToken);
                CancellationToken.ThrowIfCancellationRequested();
                // 初始化管理器
                FrameLog.SetContext("BootStage", "Managers");
                moduleHub.InitManagers(CancellationToken);
                if (hotUpdateRuntime != null)
                {
                    FrameLog.SetContext("BootStage", "HotUpdateEntry");
                    await hotUpdateRuntime.StartAsync(CancellationToken);
                    await UniTask.SwitchToMainThread();
                }
                CancellationToken.ThrowIfCancellationRequested();
                // 设置启动完成
                FrameLog.SetContext("BootStage", "Ready");
                FrameLog.Info("框架启动完成", "Boot");
                CancellationToken.ThrowIfCancellationRequested();
                startupActive = false;
                IsReady = true;
                // 设置启动完成
                readySource.TrySetResult();
            }
            catch (Exception Error)
            {
                await UniTask.SwitchToMainThread();
                RequestShutdown();
                if (requestingCancellation != null)
                    await requestingCancellation.Task;

                try
                {
                    await CleanupAsync();
                }
                catch (Exception CleanupError)
                {
                    var Failure = new AggregateException("框架启动失败且清理失败", Error, CleanupError);
                    readySource.TrySetException(Failure);
                    throw Failure;
                }
                finally
                {
                    startupActive = false;
                }

                if (Error is OperationCanceledException)
                    readySource.TrySetCanceled(CancellationToken);
                else
                    readySource.TrySetException(Error);

                throw;
            }
        }

        #endregion

        #region 取消与释放

        /// <summary>
        /// 请求停止启动 进行中的阶段收尾后由启动路径统一清理
        /// </summary>
        public void Dispose()
        {
            if (shutdownRequested)
                return;

            bool WasStarting = startupActive;
            RequestShutdown();
            if (!WasStarting)
                CleanupAsync().Forget();
        }

        private void RequestShutdown()
        {
            if (shutdownRequested)
                return;

            shutdownRequested = true;
            IsReady = false;
            var Request = new UniTaskCompletionSource();
            requestingCancellation = Request;
            try
            {
                lifetimeCancellation.Cancel();
            }
            catch (Exception Error)
            {
                cancellationFailure = Error;
            }
            finally
            {
                requestingCancellation = null;
                Request.TrySetResult();
            }
        }

        private async UniTask CleanupAsync()
        {
            if (cleanupStarted)
                return;

            cleanupStarted = true;
            var FailureList = new List<Exception>();
            if (cancellationFailure != null)
                FailureList.Add(cancellationFailure);

            var Runtime = hotUpdateRuntime;
            hotUpdateRuntime = null;
            try
            {
                Runtime?.Dispose();
            }
            catch (Exception Error)
            {
                FailureList.Add(Error);
            }

            moduleHub.ReleaseManagers(FailureList);
            try
            {
                moduleHub.ReleaseConfigModule();
            }
            catch (Exception Error)
            {
                FailureList.Add(Error);
            }

            try
            {
                await startupResources.ReleaseAsync();
            }
            catch (Exception Error)
            {
                FailureList.Add(Error);
            }
            finally
            {
                lifetimeCancellation.Dispose();
            }

            if (FailureList.Count > 0)
                throw new AggregateException("[BootManager] 框架清理失败", FailureList);
        }

        #endregion
    }
}
