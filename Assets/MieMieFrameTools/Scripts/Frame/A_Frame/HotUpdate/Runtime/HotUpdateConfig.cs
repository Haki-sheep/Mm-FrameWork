using System;

namespace MieMieFrameWork.HotUpdate
{
    [Serializable]
    public sealed class HotUpdateConfig
    {
        /// <summary> 显式开启代码热更新 </summary>
        public bool enabled;
        /// <summary> 随 Player 固定的 AOT 基线标识 换包必须更新 </summary>
        public string playerBuildId = string.Empty;
        /// <summary> YooAsset 内热更清单地址 </summary>
        public string manifestLocation = "MmHotUpdateManifest";
    }

    [Serializable]
    public sealed class HotUpdateManifest
    {
        /// <summary> 当前发布格式 </summary>
        public const int CurrentFormatVersion = 1;
        public int formatVersion = CurrentFormatVersion;
        public string playerBuildId;
        public string platform;
        public string codeVersion;
        public string entryAssembly;
        public string entryType;
        public HotUpdateAssembly[] aotAssemblyList = Array.Empty<HotUpdateAssembly>();
        public HotUpdateAssembly[] hotAssemblyList = Array.Empty<HotUpdateAssembly>();
    }

    [Serializable]
    public sealed class HotUpdateAssembly
    {
        public string name;
        public string location;
        public string sha256;
    }
}
