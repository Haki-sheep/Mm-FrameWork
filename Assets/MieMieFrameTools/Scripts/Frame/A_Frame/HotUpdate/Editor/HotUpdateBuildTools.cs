using System;
using System.IO;
using HybridCLR;
using HybridCLR.Commands;
using HybridCLR.Installer;
using HybridCLR.Settings;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace MieMieFrameWork.HotUpdate.Editor
{
    public static class HotUpdateBuildTools
    {
        public static void Configure(HotUpdateBuildProfile profile)
        {
            profile.Validate();
            // 让官方安装器验证原生版本映射 不放宽白名单
            var Installer = new InstallerController();
            var Settings = HybridCLRSettings.instance;
            Settings.enable = true;
            Settings.useGlobalIl2cpp = false;
            Settings.hybridclrRepoURL = "https://gitee.com/focus-creative-games/hybridclr";
            Settings.il2cppPlusRepoURL = "https://gitee.com/focus-creative-games/il2cpp_plus";
            Settings.hotUpdateAssemblies = profile.hotAssemblyList;
            Settings.hotUpdateAssemblyDefinitions = Array.Empty<UnityEditorInternal.AssemblyDefinitionAsset>();
            Settings.outputLinkFile = "MmHotUpdateGenerated/link.xml";
            Settings.outputAOTGenericReferenceFile = "MmHotUpdateGenerated/AOTGenericReferences.cs";
            HybridCLRSettings.Save();
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(
                BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)), ScriptingImplementation.IL2CPP);
            Debug.Log($"HybridCLR {Installer.PackageVersion} 已配置 本机运行库须安装到工程 Library");
        }

        public static void InstallRuntime()
        {
            var Installer = new InstallerController();
            Installer.InstallDefaultHybridCLR();
            if (!Installer.HasInstalledHybridCLR() || Installer.InstalledLibil2cppVersion != Installer.PackageVersion)
                throw new InvalidOperationException("HybridCLR 本机运行库安装未完成或版本不一致");
        }

        public static void PrepareBaseline(HotUpdateBuildProfile profile)
        {
            profile.Validate();
            if (Directory.Exists(profile.GetBaselinePath()))
                throw new InvalidOperationException($"首包基线已存在 禁止覆盖 {profile.GetBaselinePath()}");
            Configure(profile);
            PrebuildCommand.GenerateAll();
            HotUpdatePublisher.CaptureBaseline(profile);
            string CompileRoot = HotUpdateCompiler.Compile(profile);
            HotUpdatePublisher.ExportPatch(profile, CompileRoot);
            RegisterCollector(profile);
        }

        public static void ExportPatch(HotUpdateBuildProfile profile)
        {
            profile.Validate();
            string CompileRoot = HotUpdateCompiler.Compile(profile);
            HotUpdatePublisher.ExportPatch(profile, CompileRoot);
            RegisterCollector(profile);
        }

        public static string BuildResources(HotUpdateBuildProfile profile, string packageVersion, bool copyBuiltin)
        {
            profile.Validate();
            ValidateResourceVersion(profile, packageVersion);
            var Parameters = new LegacyBuildParameters
            {
                BuildOutputRoot = profile.bundleOutputDirectory,
                BundledFileRoot = Path.Combine(Application.streamingAssetsPath, "yoo"),
                BuildPipeline = EBuildPipeline.LegacyBuildPipeline.ToString(),
                BuildBundleType = (int)EBundleType.AssetBundle,
                BuildTarget = EditorUserBuildSettings.activeBuildTarget,
                PackageName = profile.packageName,
                PackageVersion = packageVersion,
                VerifyBuildingResult = true,
                CompressOption = ECompressOption.LZ4,
                BundledCopyOption = copyBuiltin ? EBundledCopyOption.ClearAndCopyAll : EBundledCopyOption.None
            };
            var Result = new LegacyBuildPipeline().Run(Parameters, true);
            if (!Result.Success)
                throw new InvalidOperationException($"YooAsset 构建失败 {Result.FailedTask} {Result.ErrorInfo}");
            return Result.OutputPackageDirectory;
        }

        /// <summary>
        /// 在编译或构建前拒绝已占用的资源版本 保留既有发布内容
        /// </summary>
        public static void ValidateResourceVersion(HotUpdateBuildProfile profile, string packageVersion)
        {
            if (string.IsNullOrWhiteSpace(packageVersion) || packageVersion != Path.GetFileName(packageVersion) ||
                packageVersion.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || packageVersion == "." || packageVersion == "..")
                throw new ArgumentException("资源版本不能为空或包含路径字符", nameof(packageVersion));
            var Parameters = new LegacyBuildParameters
            {
                BuildOutputRoot = profile.bundleOutputDirectory,
                BuildTarget = EditorUserBuildSettings.activeBuildTarget,
                PackageName = profile.packageName,
                PackageVersion = packageVersion
            };
            string Output = Parameters.GetPackageOutputDirectory();
            string Published = Path.Combine(profile.GetServerPath(), profile.packageName + "_" + packageVersion + ".bytes");
            if (Directory.Exists(Output) || File.Exists(Published))
                throw new InvalidOperationException($"资源版本 {packageVersion} 已用于构建或发布 请填写新的资源版本后重新发布 本次未发布 codeVersion 可以保持不变");
        }

        public static void RegisterCollector(HotUpdateBuildProfile profile)
        {
            var Package = BundleCollectorSettingData.Setting.Packages.Find(Item => Item.PackageName == profile.packageName);
            if (Package == null)
                Package = BundleCollectorSettingData.CreatePackage(profile.packageName);
            Package.EnableAddressable = true;
            var Group = Package.Groups.Find(Item => Item.GroupName == "MmHotUpdate");
            if (Group == null)
                Group = BundleCollectorSettingData.CreateGroup(Package, "MmHotUpdate");
            if (!Group.Collectors.Exists(Item => Item.CollectPath == profile.assetOutputDirectory))
                BundleCollectorSettingData.CreateCollector(Group, new BundleCollector
                {
                    CollectPath = profile.assetOutputDirectory,
                    AddressRuleName = nameof(AddressByFileName),
                    PackRuleName = nameof(PackSeparately),
                    FilterRuleName = nameof(CollectAll)
                });
            BundleCollectorSettingData.ModifyPackage(Package);
            BundleCollectorSettingData.SaveFile();
        }
    }
}
