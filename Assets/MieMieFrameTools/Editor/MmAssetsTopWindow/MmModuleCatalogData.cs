using System;
using System.Collections.Generic;

namespace MieMieFrameWork.Editor.MmAssets
{
    [Serializable]
    public class MmModuleCatalogData
    {
        public List<MmModuleEntry> modules = new();
    }

    [Serializable]
    public class MmModuleEntry
    {
        public string id = string.Empty;
        public string displayName = string.Empty;
        public string category = "未分类";
        public string description = string.Empty;
        public string version = "0.1.0";
        public string gitUrl = string.Empty;
        public string packageName = string.Empty;
        public string installCheckPath = string.Empty;
        public string menuPath = string.Empty;
        public List<string> tags = new();
        public bool isBuiltIn;
    }
}
