using System;
using System.IO;
using System.Linq;

namespace MieMieFrameWork.HotUpdate.Editor
{
    public static class HotUpdateServerPublisher
    {
        /// <summary>
        /// 先发布不可变文件 再原子切换版本指针 保留旧客户端仍需下载的资源
        /// </summary>
        public static string Publish(HotUpdateBuildProfile profile, string packageDirectory, string packageVersion)
        {
            profile.Validate();
            string VersionName = profile.packageName + ".version";
            string VersionFile = Path.Combine(packageDirectory, VersionName);
            if (File.ReadAllText(VersionFile).Trim() != packageVersion)
                throw new InvalidOperationException("资源输出目录与发布版本不一致");
            string ManifestName = profile.packageName + "_" + packageVersion;
            if (!File.Exists(Path.Combine(packageDirectory, ManifestName + ".bytes")) ||
                !File.Exists(Path.Combine(packageDirectory, ManifestName + ".hash")))
                throw new InvalidOperationException("资源清单或哈希文件缺失");
            string Root = profile.GetServerPath();
            Directory.CreateDirectory(Root);
            var FileList = Directory.GetFiles(packageDirectory).Where(PathValue =>
                PathValue.EndsWith(".bundle", StringComparison.Ordinal) ||
                Path.GetFileName(PathValue) == ManifestName + ".bytes" ||
                Path.GetFileName(PathValue) == ManifestName + ".hash").ToArray();
            foreach (string Source in FileList)
            {
                string Destination = Path.Combine(Root, Path.GetFileName(Source));
                if (File.Exists(Destination) &&
                    HotUpdateValidation.ComputeHash(File.ReadAllBytes(Source)) != HotUpdateValidation.ComputeHash(File.ReadAllBytes(Destination)))
                    throw new InvalidOperationException($"已发布文件禁止覆盖 请增加资源版本 {Destination}");
            }
            foreach (string Source in FileList)
            {
                string Destination = Path.Combine(Root, Path.GetFileName(Source));
                if (File.Exists(Destination)) continue;
                string Staging = Destination + ".staging-" + Guid.NewGuid().ToString("N");
                File.Copy(Source, Staging);
                File.Move(Staging, Destination);
            }
            string VersionDestination = Path.Combine(Root, VersionName);
            string VersionStaging = VersionDestination + ".staging-" + Guid.NewGuid().ToString("N");
            File.Copy(VersionFile, VersionStaging);
            if (File.Exists(VersionDestination)) File.Replace(VersionStaging, VersionDestination, null);
            else File.Move(VersionStaging, VersionDestination);
            return Root;
        }
    }
}
