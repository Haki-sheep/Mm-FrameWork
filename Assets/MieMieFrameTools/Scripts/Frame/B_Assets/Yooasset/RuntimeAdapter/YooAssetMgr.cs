using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using MieMieFrameWork.Diagnostics;
using Object = UnityEngine.Object;

namespace MieMieFrameWork.Asset
{
    /// <summary>
    /// YooAsset 运行时门面
    /// 负责初始化全局系统和访问资源包
    /// </summary>
    public static class YooAssetMgr
    {
        public static ResourcePackage DefaultPackage { get; private set; }

        public static bool IsReady { get; private set; }

        /// <summary>
        /// 进入运行会话时清除门面状态 与 YooAsset 全局生命周期一致
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            DefaultPackage = null;
            IsReady = false;
        }

        /// <summary>
        /// 初始化 YooAsset 并创建默认资源包
        /// </summary>
        public static ResourcePackage Initialize(string packageName)
        {
            if (DefaultPackage != null && DefaultPackage.PackageName != packageName)
                throw new InvalidOperationException($"YooAsset 默认包已绑定 {DefaultPackage.PackageName} 不能切换为 {packageName}");

            if (!YooAssets.IsInitialized)
                YooAssets.Initialize();

            if (!YooAssets.TryGetPackage(packageName, out ResourcePackage package))
                package = YooAssets.CreatePackage(packageName);

            DefaultPackage = package;
            return package;
        }

        /// <summary>
        /// 启动入口配置文件系统 读取版本和清单 成功后开放业务加载
        /// </summary>
        public static async UniTask InitializeAsync(string packageName, int manifestTimeout, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var Package = Initialize(packageName);
            if (IsReady)
                return;

            FrameLog.Info($"初始化资源包 {packageName}", "Asset");

            if (Package.InitializeStatus == EOperationStatus.None)
            {
                InitializePackageOptions Options;
#if UNITY_EDITOR
                var BuildResult = EditorSimulateBuildInvoker.Build(packageName, (int)EBundleType.VirtualAssetBundle);
                Options = new EditorSimulateModeOptions
                {
                    EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(BuildResult.PackageRootDirectory),
                    AutoUnloadBundleWhenUnused = true
                };
#else
                Options = new OfflinePlayModeOptions
                {
                    BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                    AutoUnloadBundleWhenUnused = true
                };
#endif
                var InitializeOperation = Package.InitializePackageAsync(Options);
                await UniTask.WaitUntil(() => InitializeOperation.IsDone);
                cancellationToken.ThrowIfCancellationRequested();
                if (InitializeOperation.Status != EOperationStatus.Succeeded)
                    throw new InvalidOperationException($"YooAsset 初始化失败 包 {packageName} 错误 {InitializeOperation.Error}");
            }
            else if (Package.InitializeStatus != EOperationStatus.Succeeded)
            {
                throw new InvalidOperationException($"YooAsset 包 {packageName} 当前不能启动 状态 {Package.InitializeStatus}");
            }

            var VersionOperation = Package.RequestPackageVersionAsync(new RequestPackageVersionOptions(false, manifestTimeout));
            await UniTask.WaitUntil(() => VersionOperation.IsDone);
            cancellationToken.ThrowIfCancellationRequested();
            if (VersionOperation.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException($"YooAsset 版本读取失败 包 {packageName} 错误 {VersionOperation.Error}");

            var ManifestOperation = Package.LoadPackageManifestAsync(new LoadPackageManifestOptions(VersionOperation.PackageVersion, manifestTimeout));
            await UniTask.WaitUntil(() => ManifestOperation.IsDone);
            cancellationToken.ThrowIfCancellationRequested();
            if (ManifestOperation.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException($"YooAsset 清单加载失败 包 {packageName} 版本 {VersionOperation.PackageVersion} 错误 {ManifestOperation.Error}");

            IsReady = true;
            FrameLog.SetContext("ResourcePackage", packageName);
            FrameLog.SetContext("ResourceVersion", VersionOperation.PackageVersion);
            FrameLog.Info($"资源包就绪 {packageName} 版本 {VersionOperation.PackageVersion}", "Asset");
        }

        /// <summary>
        /// 获取默认资源包
        /// </summary>
        public static ResourcePackage GetDefaultPackage()
        {
            return DefaultPackage;
        }

        /// <summary>
        /// 默认包就绪后创建独立 LRU 缓存 生命周期由调用方管理
        /// </summary>
        public static YooAssetLruCache CreateCache(int capacity)
        {
            CheckReady();
            return new YooAssetLruCache(DefaultPackage, capacity);
        }

        /// <summary>
        /// 异步加载资源并返回资源句柄
        /// </summary>
        public static AssetHandle LoadAssetAsync<T>(
            string location,
            uint priority = 0) where T : Object
        {
            CheckReady();
            return DefaultPackage.LoadAssetAsync<T>(location, priority);
        }

        /// <summary>
        /// 同步加载资源并返回资源句柄
        /// </summary>
        public static AssetHandle LoadAsset<T>(string location) where T : Object
        {
            CheckReady();
            return DefaultPackage.LoadAssetSync<T>(location);
        }

        /// <summary>
        /// 检查业务加载时机 未完成启动时立即暴露调用错误
        /// </summary>
        private static void CheckReady()
        {
            if (!IsReady)
                throw new InvalidOperationException("YooAsset 默认包尚未就绪 业务入口须先等待 ModuleHub.ReadyTask");
        }

        /// <summary>
        /// 释放资源句柄
        /// </summary>
        public static void Release(AssetHandle assetHandle)
        {
            assetHandle?.Release();
        }
    }
}
