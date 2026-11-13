using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using HybridCLR;
using MieMieFrameWork.Diagnostics;
using UnityEngine;
using YooAsset;

namespace MieMieFrameWork.HotUpdate
{
    public sealed class HotUpdateRuntime : IDisposable
    {
        /// <summary> 本次启动的业务入口 先于框架管理器释放 </summary>
        private IHotUpdateEntry entry;
        /// <summary> 程序集加载不可回滚 同一实例只执行一次 </summary>
        private bool loadStarted;
        public string CodeVersion { get; private set; }

        public async UniTask LoadAsync(ResourcePackage package, HotUpdateConfig config, CancellationToken cancellationToken)
        {
            if (loadStarted)
                throw new InvalidOperationException("热更程序集不能在同一运行会话重复加载 失败后须重启 Player");
            loadStarted = true;
            var ManifestBytes = await LoadBytesAsync(package, config.manifestLocation, cancellationToken);
            var Manifest = JsonUtility.FromJson<HotUpdateManifest>(System.Text.Encoding.UTF8.GetString(ManifestBytes));
#if UNITY_EDITOR
            string Platform = Manifest.platform;
#else
            string Platform = Application.platform.ToString();
#endif
            HotUpdateValidation.ValidateManifest(Manifest, config.playerBuildId, Platform);
            var AotByteList = await LoadAssemblyBytesAsync(package, Manifest.aotAssemblyList, cancellationToken);
            var HotByteList = await LoadAssemblyBytesAsync(package, Manifest.hotAssemblyList, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

#if !UNITY_EDITOR && !ENABLE_IL2CPP
            throw new PlatformNotSupportedException("Player 代码热更新需要安装 HybridCLR 的 IL2CPP 后端");
#else
            for (int Index = 0; Index < AotByteList.Count; Index++)
            {
#if !UNITY_EDITOR
                var eResult = RuntimeApi.LoadMetadataForAOTAssembly(AotByteList[Index], HomologousImageMode.SuperSet);
                if (eResult != LoadImageErrorCode.OK)
                    throw new InvalidOperationException($"补充 AOT 元数据失败 {Manifest.aotAssemblyList[Index].name} 错误 {eResult}");
#endif
            }
            Assembly EntryAssembly = null;
            for (int Index = 0; Index < HotByteList.Count; Index++)
            {
                var Record = Manifest.hotAssemblyList[Index];
                var LoadedAssembly = FindAssembly(Record.name);
#if UNITY_EDITOR
                if (LoadedAssembly != null)
                    FrameLog.Info($"编辑器使用已编译程序集 {Record.name} 下载 DLL 仅校验 Player 才验证解释执行", "HotUpdate");
                else
                    LoadedAssembly = Assembly.Load(HotByteList[Index]);
#else
                if (LoadedAssembly != null)
                    throw new InvalidOperationException($"热更程序集已在 Player 中加载 {Record.name} 检查 AOT 引用与剔除配置");
                LoadedAssembly = Assembly.Load(HotByteList[Index]);
#endif
                if (LoadedAssembly.GetName().Name != Record.name)
                    throw new InvalidOperationException($"DLL 内部程序集名称与清单不一致 {Record.name}");
                if (Record.name == Manifest.entryAssembly)
                    EntryAssembly = LoadedAssembly;
            }
            var EntryType = EntryAssembly.GetType(Manifest.entryType, true);
            if (!typeof(IHotUpdateEntry).IsAssignableFrom(EntryType) || EntryType.IsAbstract)
                throw new InvalidOperationException($"热更入口必须实现 IHotUpdateEntry {Manifest.entryType}");
            entry = (IHotUpdateEntry)Activator.CreateInstance(EntryType);
            CodeVersion = Manifest.codeVersion;
            FrameLog.Info($"代码已加载 {CodeVersion} 基线 {Manifest.playerBuildId}", "HotUpdate");
#endif
        }

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await entry.StartAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }

        public void Dispose()
        {
            var Entry = entry;
            entry = null;
            Entry?.Dispose();
        }

        private static async UniTask<List<byte[]>> LoadAssemblyBytesAsync(ResourcePackage package,
            HotUpdateAssembly[] assemblyList, CancellationToken cancellationToken)
        {
            var ByteList = new List<byte[]>(assemblyList.Length);
            foreach (var Assembly in assemblyList)
            {
                var Bytes = await LoadBytesAsync(package, Assembly.location, cancellationToken);
                HotUpdateValidation.ValidateHash(Bytes, Assembly);
                ByteList.Add(Bytes);
            }
            return ByteList;
        }

        private static async UniTask<byte[]> LoadBytesAsync(ResourcePackage package, string location, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var Handle = package.LoadAssetAsync<TextAsset>(location);
            try
            {
                await UniTask.WaitUntil(() => Handle.IsDone);
                cancellationToken.ThrowIfCancellationRequested();
                if (Handle.Status != EOperationStatus.Succeeded)
                    throw new InvalidOperationException($"热更资源加载失败 {location} 错误 {Handle.Error}");
                return ((TextAsset)Handle.AssetObject).bytes;
            }
            finally
            {
                Handle.Release();
            }
        }

        private static Assembly FindAssembly(string name)
        {
            foreach (var Assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (Assembly.GetName().Name == name)
                    return Assembly;
            return null;
        }
    }
}
