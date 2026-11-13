using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MieMieFrameWork.Diagnostics;
using YooAsset;

namespace MieMieFrameWork.Asset
{
    public static partial class YooAssetMgr
    {
        /// <summary>
        /// 启动默认包 联机模式下载清单中的全部资源后开放业务加载
        /// </summary>
        public static async UniTask InitializeAsync(string packageName, int manifestTimeout,
            ResourceUpdateConfig config, CancellationToken cancellationToken,
            Action<DownloadProgressChangedEventArgs> downloadProgress = null)
        {
            config.Validate();
            if (manifestTimeout <= 0)
                throw new ArgumentOutOfRangeException(nameof(manifestTimeout));
            cancellationToken.ThrowIfCancellationRequested();
            var Package = Initialize(packageName);
            if (IsReady)
                return;

            FrameLog.Info($"初始化资源包 {packageName} 模式 {config.playMode}", "Asset");
            if (Package.InitializeStatus == EOperationStatus.None)
            {
                var Options = CreateStartupOptions(packageName, config);
                await WaitStartupOperation(Package.InitializePackageAsync(Options), "初始化", cancellationToken);
            }
            else if (Package.InitializeStatus != EOperationStatus.Succeeded)
            {
                throw new InvalidOperationException($"资源包 {packageName} 不能启动 状态 {Package.InitializeStatus}");
            }

            bool IsRemote = config.playMode == ResourcePlayMode.Host;
#if UNITY_EDITOR
            IsRemote &= !config.simulateInEditor;
#endif
            var VersionOperation = Package.RequestPackageVersionAsync(new RequestPackageVersionOptions(IsRemote, manifestTimeout));
            await WaitStartupOperation(VersionOperation, "读取版本", cancellationToken);
            var ManifestOperation = Package.LoadPackageManifestAsync(new LoadPackageManifestOptions(VersionOperation.PackageVersion, manifestTimeout));
            await WaitStartupOperation(ManifestOperation, "加载清单", cancellationToken);
            if (IsRemote)
            {
                var Downloader = Package.CreateResourceDownloader(new ResourceDownloaderOptions(config.downloadConcurrency, config.downloadRetryCount));
                Downloader.DownloadProgressChanged += downloadProgress;
                try
                {
                    Downloader.StartDownload();
                    bool CancellationRequested = false;
                    while (!Downloader.IsDone)
                    {
                        if (cancellationToken.IsCancellationRequested && !CancellationRequested)
                        {
                            CancellationRequested = true;
                            Downloader.CancelDownload();
                        }
                        await UniTask.Yield();
                    }
                    await WaitStartupOperation(Downloader, "下载资源", cancellationToken);
                }
                finally
                {
                    Downloader.DownloadProgressChanged -= downloadProgress;
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
            IsReady = true;
            FrameLog.SetContext("ResourcePackage", packageName);
            FrameLog.SetContext("ResourceVersion", VersionOperation.PackageVersion);
            FrameLog.Info($"资源包就绪 {packageName} 版本 {VersionOperation.PackageVersion}", "Asset");
        }

        private static InitializePackageOptions CreateStartupOptions(string packageName, ResourceUpdateConfig config)
        {
#if UNITY_EDITOR
            if (config.simulateInEditor)
            {
                var BuildResult = EditorSimulateBuildInvoker.Build(packageName, (int)EBundleType.VirtualAssetBundle);
                return new EditorSimulateModeOptions
                {
                    EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(BuildResult.PackageRootDirectory),
                    AutoUnloadBundleWhenUnused = true
                };
            }
#endif
#if UNITY_WEBGL && !UNITY_EDITOR
            throw new PlatformNotSupportedException("当前启动适配器面向原生平台 WebGL 须配置 Web 文件系统");
#else
            if (config.playMode == ResourcePlayMode.Offline)
                return new OfflinePlayModeOptions
                {
                    BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                    AutoUnloadBundleWhenUnused = true
                };
            return new HostPlayModeOptions
            {
                BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(new ResourceRemoteService(config)),
                AutoUnloadBundleWhenUnused = true
            };
#endif
        }

        private static async UniTask WaitStartupOperation(AsyncOperationBase operation, string stage, CancellationToken cancellationToken)
        {
            // 原生版本与清单操作没有公共取消接口 等待收尾后禁止进入下一阶段
            await UniTask.WaitUntil(() => operation.IsDone);
            cancellationToken.ThrowIfCancellationRequested();
            if (operation.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException($"YooAsset {stage}失败 {operation.Error}");
        }
    }
}
