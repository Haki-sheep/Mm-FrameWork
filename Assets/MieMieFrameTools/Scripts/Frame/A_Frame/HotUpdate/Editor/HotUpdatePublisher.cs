using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HybridCLR;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.HotUpdate.Editor
{
    public static class HotUpdatePublisher
    {
        /// <summary> 首包归档索引文件 </summary>
        private const string BaselineFile = "baseline.json";

        [Serializable]
        private sealed class Baseline
        {
            public string unityVersion;
            public bool development;
            public HotUpdateManifest manifest;
        }

        public static void CaptureBaseline(HotUpdateBuildProfile profile)
        {
            string Root = profile.GetBaselinePath();
            if (Directory.Exists(Root))
                throw new InvalidOperationException($"首包基线禁止覆盖 {Root}");
            string GeneratedFile = Path.Combine(Application.dataPath, HybridCLR.Settings.HybridCLRSettings.instance.outputAOTGenericReferenceFile);
            string Source = File.ReadAllText(GeneratedFile);
            var Block = Regex.Match(Source, @"// \{\{ AOT assemblies(?<list>[\s\S]*?)// \}\}");
            if (!Block.Success)
                throw new InvalidOperationException("未找到 HybridCLR 生成的 AOT 补充程序集列表");
            var NameList = Regex.Matches(Block.Groups["list"].Value, "\"([^\"]+)\\.dll\"")
                .Cast<Match>().Select(Item => Item.Groups[1].Value).ToArray();
            var Manifest = CreateManifest(profile);
            var AotList = new List<HotUpdateAssembly>();
            var ByteList = new List<byte[]>();
            string AotRoot = SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);
            foreach (string Name in NameList)
            {
                var Bytes = File.ReadAllBytes(Path.Combine(AotRoot, Name + ".dll"));
                ByteList.Add(Bytes);
                AotList.Add(CreateRecord(Name, "MmAot_" + Name, Bytes));
            }
            Manifest.aotAssemblyList = AotList.ToArray();
            string StagingRoot = Root + ".staging-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(StagingRoot);
            for (int Index = 0; Index < AotList.Count; Index++)
                File.WriteAllBytes(Path.Combine(StagingRoot, AotList[Index].location + ".bytes"), ByteList[Index]);
            File.WriteAllText(Path.Combine(StagingRoot, BaselineFile), JsonUtility.ToJson(new Baseline
            {
                unityVersion = Application.unityVersion,
                development = EditorUserBuildSettings.development,
                manifest = Manifest
            }, true));
            Directory.Move(StagingRoot, Root);
        }

        public static void ExportPatch(HotUpdateBuildProfile profile, string compiledDirectory)
        {
            string Root = profile.GetBaselinePath();
            var Baseline = JsonUtility.FromJson<Baseline>(File.ReadAllText(Path.Combine(Root, BaselineFile)));
            if (Baseline.unityVersion != Application.unityVersion || Baseline.development != EditorUserBuildSettings.development)
                throw new InvalidOperationException("补丁必须使用首包相同的 Unity 版本及 Development 模式");
            var Manifest = CreateManifest(profile);
            Manifest.aotAssemblyList = Baseline.manifest.aotAssemblyList;
            var OutputDict = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var Record in Manifest.aotAssemblyList)
            {
                var Bytes = File.ReadAllBytes(Path.Combine(Root, Record.location + ".bytes"));
                HotUpdateValidation.ValidateHash(Bytes, Record);
                OutputDict.Add(Record.location + ".bytes", Bytes);
            }
            var HotList = new List<HotUpdateAssembly>();
            string HotRoot = compiledDirectory;
            foreach (string Name in profile.hotAssemblyList)
            {
                var Bytes = File.ReadAllBytes(Path.Combine(HotRoot, Name + ".dll"));
                var Record = CreateRecord(Name, "MmHot_" + Name, Bytes);
                HotList.Add(Record);
                OutputDict.Add(Record.location + ".bytes", Bytes);
            }
            Manifest.hotAssemblyList = HotList.ToArray();
            HotUpdateValidation.ValidateManifest(Manifest, profile.playerBuildId, Manifest.platform);
            Directory.CreateDirectory(profile.assetOutputDirectory);
            foreach (var Output in OutputDict)
                File.WriteAllBytes(Path.Combine(profile.assetOutputDirectory, Output.Key), Output.Value);
            File.WriteAllText(Path.Combine(profile.assetOutputDirectory, "MmHotUpdateManifest.json"), JsonUtility.ToJson(Manifest, true));
            AssetDatabase.Refresh();
            Debug.Log($"代码发布已导出 {profile.codeVersion} 首包基线 {profile.playerBuildId}");
        }

        /// <summary>
        /// 真正 Player 构建后核对归档元数据 不能用临时裁剪构建代替
        /// </summary>
        public static void VerifyPlayerBaseline(HotUpdateBuildProfile profile)
        {
            var Baseline = JsonUtility.FromJson<Baseline>(File.ReadAllText(Path.Combine(profile.GetBaselinePath(), BaselineFile)));
            string Root = SettingsUtil.GetAssembliesPostIl2CppStripDir(EditorUserBuildSettings.activeBuildTarget);
            foreach (var Record in Baseline.manifest.aotAssemblyList)
                HotUpdateValidation.ValidateHash(File.ReadAllBytes(Path.Combine(Root, Record.name + ".dll")), Record);
        }

        private static HotUpdateManifest CreateManifest(HotUpdateBuildProfile profile)
        {
            string Platform;
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneWindows64: Platform = RuntimePlatform.WindowsPlayer.ToString(); break;
                case BuildTarget.StandaloneOSX: Platform = RuntimePlatform.OSXPlayer.ToString(); break;
                case BuildTarget.StandaloneLinux64: Platform = RuntimePlatform.LinuxPlayer.ToString(); break;
                case BuildTarget.Android: Platform = RuntimePlatform.Android.ToString(); break;
                case BuildTarget.iOS: Platform = RuntimePlatform.IPhonePlayer.ToString(); break;
                default: throw new PlatformNotSupportedException("该发布工具尚未配置当前平台");
            }
            return new HotUpdateManifest
            {
                playerBuildId = profile.playerBuildId,
                platform = Platform,
                codeVersion = profile.codeVersion,
                entryAssembly = profile.entryAssembly,
                entryType = profile.entryType
            };
        }

        private static HotUpdateAssembly CreateRecord(string name, string location, byte[] bytes)
        {
            return new HotUpdateAssembly { name = name, location = location, sha256 = HotUpdateValidation.ComputeHash(bytes) };
        }
    }
}
