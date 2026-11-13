using System;
using UnityEngine;

namespace MieMieFrameWork.Asset
{
    public enum ResourcePlayMode
    {
        Offline,
        Host
    }

    [Serializable]
    public sealed class ResourceUpdateConfig
    {
        /// <summary> Player 资源运行模式 </summary>
        public ResourcePlayMode playMode = ResourcePlayMode.Offline;
        /// <summary> 编辑器使用模拟资源 开启联机测试时关闭 </summary>
        public bool simulateInEditor = true;
        /// <summary> 当前平台资源发布目录 </summary>
        public string remoteUrl = string.Empty;
        /// <summary> 显式配置的备用发布目录 为空时只请求主地址 </summary>
        public string fallbackUrl = string.Empty;
        /// <summary> 并发下载文件数 </summary>
        public int downloadConcurrency = 8;
        /// <summary> 每个文件下载失败后的重试次数 </summary>
        public int downloadRetryCount = 2;

        /// <summary>
        /// 校验启动配置 不替换错误值
        /// </summary>
        public void Validate()
        {
            if (downloadConcurrency <= 0 || downloadRetryCount < 0)
                throw new ArgumentException("资源下载并发数须大于零 重试次数不能为负数");
            if (playMode != ResourcePlayMode.Offline && playMode != ResourcePlayMode.Host)
                throw new ArgumentOutOfRangeException(nameof(playMode));
            if (playMode == ResourcePlayMode.Host)
            {
                ValidateUrl(remoteUrl);
                if (!string.IsNullOrEmpty(fallbackUrl))
                    ValidateUrl(fallbackUrl);
            }
        }

        private static void ValidateUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var Url) ||
                (Url.Scheme != Uri.UriSchemeHttp && Url.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(Url.Query) || !string.IsNullOrEmpty(Url.Fragment))
                throw new ArgumentException($"资源服务器须为无查询参数的 HTTP 或 HTTPS 目录 {url}");
        }
    }
}
