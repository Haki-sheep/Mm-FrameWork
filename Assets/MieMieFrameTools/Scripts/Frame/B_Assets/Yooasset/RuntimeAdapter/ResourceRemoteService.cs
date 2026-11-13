using System;
using System.Collections.Generic;
using YooAsset;

namespace MieMieFrameWork.Asset
{
    public sealed class ResourceRemoteService : IRemoteService
    {
        /// <summary> 已校验的发布目录 </summary>
        private readonly string[] rootUrlList;

        public ResourceRemoteService(ResourceUpdateConfig config)
        {
            config.Validate();
            rootUrlList = string.IsNullOrEmpty(config.fallbackUrl)
                ? new[] { config.remoteUrl.TrimEnd('/') }
                : new[] { config.remoteUrl.TrimEnd('/'), config.fallbackUrl.TrimEnd('/') };
        }

        public IReadOnlyList<string> GetRemoteUrls(string fileName)
        {
            var UrlList = new string[rootUrlList.Length];
            for (int Index = 0; Index < rootUrlList.Length; Index++)
                UrlList[Index] = rootUrlList[Index] + "/" + Uri.EscapeDataString(fileName);
            return UrlList;
        }
    }
}
