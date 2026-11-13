using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace MieMieFrameWork.HotUpdate
{
    public static class HotUpdateValidation
    {
        public static void ValidateManifest(HotUpdateManifest manifest, string playerBuildId, string platform)
        {
            if (manifest == null || manifest.formatVersion != HotUpdateManifest.CurrentFormatVersion)
                throw new InvalidOperationException("热更清单格式不受支持");
            if (string.IsNullOrWhiteSpace(playerBuildId) || manifest.playerBuildId != playerBuildId || manifest.platform != platform)
                throw new InvalidOperationException($"热更基线不匹配 Player {playerBuildId} {platform} 清单 {manifest.playerBuildId} {manifest.platform}");
            if (string.IsNullOrWhiteSpace(manifest.codeVersion) || string.IsNullOrWhiteSpace(manifest.entryType) ||
                manifest.hotAssemblyList == null || manifest.hotAssemblyList.Length == 0 || manifest.aotAssemblyList == null)
                throw new InvalidOperationException("热更清单缺少版本 入口或程序集列表");
            var NameHashList = new HashSet<string>(StringComparer.Ordinal);
            var LocationHashList = new HashSet<string>(StringComparer.Ordinal);
            ValidateAssemblyList(manifest.aotAssemblyList, NameHashList, LocationHashList);
            ValidateAssemblyList(manifest.hotAssemblyList, NameHashList, LocationHashList);
            if (!Array.Exists(manifest.hotAssemblyList, Assembly => Assembly.name == manifest.entryAssembly))
                throw new InvalidOperationException("热更入口程序集不在加载列表中");
        }

        public static void ValidateHash(byte[] bytes, HotUpdateAssembly assembly)
        {
            if (!string.Equals(ComputeHash(bytes), assembly.sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"热更文件 SHA256 不匹配 {assembly.name} 地址 {assembly.location}");
        }

        public static string ComputeHash(byte[] bytes)
        {
            using (var Hash = SHA256.Create())
                return BitConverter.ToString(Hash.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void ValidateAssemblyList(HotUpdateAssembly[] assemblyList, HashSet<string> nameHashList, HashSet<string> locationHashList)
        {
            foreach (var Assembly in assemblyList)
            {
                if (Assembly == null || string.IsNullOrWhiteSpace(Assembly.name) || Assembly.name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(Assembly.location) || Assembly.sha256 == null || Assembly.sha256.Length != 64)
                    throw new InvalidOperationException("热更程序集记录须包含无扩展名名称 资源地址与 SHA256");
                foreach (char Character in Assembly.sha256)
                    if (!Uri.IsHexDigit(Character))
                        throw new InvalidOperationException($"SHA256 不是十六进制 {Assembly.name}");
                if (!nameHashList.Add(Assembly.name) || !locationHashList.Add(Assembly.location))
                    throw new InvalidOperationException($"热更程序集名称或地址重复 {Assembly.name}");
            }
        }
    }
}
