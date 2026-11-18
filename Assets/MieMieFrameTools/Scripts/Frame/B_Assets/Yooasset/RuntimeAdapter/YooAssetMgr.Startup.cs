using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MieMieFrameWork.Diagnostics;
using YooAsset;

namespace MieMieFrameWork.Asset
{
    public static partial class YooAssetMgr
    {
        private static StartupResources startupOwner;

        /// <summary>
        /// 持有根节点本轮建立的默认包绑定与资源包
        /// </summary>
        public sealed class StartupResources
        {
            private ResourcePackage acquiredPackage;
            private bool ownsDefaultBinding;
            private bool ownsPackage;

            internal bool IsReleasing { get; private set; }

            public async UniTask InitializeAsync(string packageName, int manifestTimeout,
                ResourceUpdateConfig config, CancellationToken cancellationToken)
            {
                ValidateStartup(manifestTimeout, config, cancellationToken);
                if (startupOwner != null)
                    throw new InvalidOperationException("YooAsset 已有框架根节点持有启动资源");

                startupOwner = this;
                ownsDefaultBinding = DefaultPackage == null;
                acquiredPackage = Initialize(packageName, out ownsPackage);
                await InitializePackageAsync(acquiredPackage, manifestTimeout, config, cancellationToken, null);
            }

            public async UniTask ReleaseAsync()
            {
                if (!ReferenceEquals(startupOwner, this) || IsReleasing)
                    return;

                IsReleasing = true;
                var Package = acquiredPackage;
                acquiredPackage = null;
                try
                {
                    if (ownsDefaultBinding && ReferenceEquals(DefaultPackage, Package))
                    {
                        DefaultPackage = null;
                        IsReady = false;
                    }

                    if (!ownsPackage || !YooAssets.IsInitialized ||
                        !YooAssets.TryGetPackage(Package.PackageName, out var RegisteredPackage) ||
                        !ReferenceEquals(RegisteredPackage, Package))
                        return;

                    var Operation = Package.DestroyPackageAsync();
                    await UniTask.WaitUntil(() => Operation.IsDone || !YooAssets.IsInitialized);
                    if (!YooAssets.IsInitialized)
                        return;

                    if (Operation.Status != EOperationStatus.Succeeded)
                        throw new InvalidOperationException($"YooAsset 释放资源包 {Package.PackageName} 失败 {Operation.Error}");

                    if (YooAssets.TryGetPackage(Package.PackageName, out RegisteredPackage) &&
                        ReferenceEquals(RegisteredPackage, Package))
                        YooAssets.RemovePackage(Package.PackageName);
                }
                finally
                {
                    if (ReferenceEquals(startupOwner, this))
                        startupOwner = null;
                }
            }
        }

        /// <summary>
        /// 启动默认包 联机模式下载清单中的全部资源后开放业务加载
        /// </summary>
        public static async UniTask InitializeAsync(string packageName, int manifestTimeout,
            ResourceUpdateConfig config, CancellationToken cancellationToken,
            Action<DownloadProgressChangedEventArgs> downloadProgress = null)
        {
            ValidateStartup(manifestTimeout, config, cancellationToken);
            var Package = Initialize(packageName);
            await InitializePackageAsync(Package, manifestTimeout, config, cancellationToken, downloadProgress);
        }

        private static void ValidateStartup(int manifestTimeout, ResourceUpdateConfig config, CancellationToken cancellationToken)
        {
            config.Validate();
            if (manifestTimeout <= 0)
                throw new ArgumentOutOfRangeException(nameof(manifestTimeout));
            cancellationToken.ThrowIfCancellationRequested();
        }

        private static async UniTask InitializePackageAsync(ResourcePackage package, int manifestTimeout,
            ResourceUpdateConfig config, CancellationToken cancellationToken, Action<DownloadProgressChangedEventArgs> downloadProgress)
        {
            string PackageName = package.PackageName;
            if (IsReady)
                return;

            FrameLog.Info($"初始化资源包 {PackageName} 模式 {config.playMode}", "Asset");
            if (package.InitializeStatus == EOperationStatus.None)
            {
                var Options = CreateStartupOptions(PackageName, config);
                await WaitStartupOperation(package.InitializePackageAsync(Options), "初始化", cancellationToken);
            }
            else if (package.InitializeStatus != EOperationStatus.Succeeded)
            {
                throw new InvalidOperationException($"资源包 {PackageName} 不能启动 状态 {package.InitializeStatus}");
            }

            bool IsRemote = config.playMode == ResourcePlayMode.Host;
#if UNITY_EDITOR
            IsRemote &= !config.simulateInEditor;
#endif
            var VersionOperation = package.RequestPackageVersionAsync(new RequestPackageVersionOptions(IsRemote, manifestTimeout));
            await WaitStartupOperation(VersionOperation, "读取版本", cancellationToken);
            var ManifestOperation = package.LoadPackageManifestAsync(new LoadPackageManifestOptions(VersionOperation.PackageVersion, manifestTimeout));
            await WaitStartupOperation(ManifestOperation, "加载清单", cancellationToken);
            if (IsRemote)
            {
                var Downloader = package.CreateResourceDownloader(new ResourceDownloaderOptions(config.downloadConcurrency, config.downloadRetryCount));
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
            FrameLog.SetContext("ResourcePackage", PackageName);
            FrameLog.SetContext("ResourceVersion", VersionOperation.PackageVersion);
            FrameLog.Info($"资源包就绪 {PackageName} 版本 {VersionOperation.PackageVersion}", "Asset");
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
