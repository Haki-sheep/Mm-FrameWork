using System;
using MieMieFrameWork.HotUpdate;
using UnityEngine;

namespace MieMieFrameWork.HotUpdate.Editor
{
    public static class HotUpdateVerification
    {
        public static object RunDataChecks()
        {
            int Passed = 0;
            var Bytes = new byte[] { 1, 3, 5, 7 };
            var Manifest = CreateManifest(Bytes);
            HotUpdateValidation.ValidateManifest(Manifest, "test-player-v1", "WindowsPlayer");
            HotUpdateValidation.ValidateHash(Bytes, Manifest.hotAssemblyList[0]);
            Passed += 2;
            RequireRejected(() => HotUpdateValidation.ValidateManifest(Manifest, "another-player", "WindowsPlayer")); Passed++;
            RequireRejected(() => HotUpdateValidation.ValidateManifest(Manifest, "test-player-v1", "Android")); Passed++;
            RequireRejected(() => HotUpdateValidation.ValidateHash(new byte[] { 2, 4, 6, 8 }, Manifest.hotAssemblyList[0])); Passed++;
            Manifest.entryAssembly = "Missing";
            RequireRejected(() => HotUpdateValidation.ValidateManifest(Manifest, "test-player-v1", "WindowsPlayer")); Passed++;
            Manifest = CreateManifest(Bytes);
            Manifest.aotAssemblyList = Manifest.hotAssemblyList;
            RequireRejected(() => HotUpdateValidation.ValidateManifest(Manifest, "test-player-v1", "WindowsPlayer")); Passed++;
            Manifest = CreateManifest(Bytes);
            Manifest.formatVersion++;
            RequireRejected(() => HotUpdateValidation.ValidateManifest(Manifest, "test-player-v1", "WindowsPlayer")); Passed++;
            Manifest = CreateManifest(Bytes);
            Manifest.hotAssemblyList[0].sha256 = new string('g', 64);
            RequireRejected(() => HotUpdateValidation.ValidateManifest(Manifest, "test-player-v1", "WindowsPlayer")); Passed++;
            Manifest = CreateManifest(Bytes);
            var RoundTrip = JsonUtility.FromJson<HotUpdateManifest>(JsonUtility.ToJson(Manifest));
            HotUpdateValidation.ValidateManifest(RoundTrip, "test-player-v1", "WindowsPlayer");
            HotUpdateValidation.ValidateHash(Bytes, RoundTrip.hotAssemblyList[0]); Passed++;
            return new { passed = Passed, failed = 0, scope = "manifest-baseline-platform-duplicates-integrity-json" };
        }

        private static HotUpdateManifest CreateManifest(byte[] bytes)
        {
            return new HotUpdateManifest
            {
                playerBuildId = "test-player-v1", platform = "WindowsPlayer", codeVersion = "1",
                entryAssembly = "Demo", entryType = "Demo.Entry",
                hotAssemblyList = new[] { new HotUpdateAssembly
                {
                    name = "Demo", location = "MmHot_Demo", sha256 = HotUpdateValidation.ComputeHash(bytes)
                }}
            };
        }

        private static void RequireRejected(Action operation)
        {
            try
            {
                operation();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new InvalidOperationException("错误的热更数据未被拒绝");
        }
    }
}
