using System.Collections.Generic;
using UnityEngine;

namespace MieMieFrameWork.Asset.DynamicAtlas
{
    /// <summary>
    /// 调试条目的只读快照
    /// </summary>
    public readonly struct DynamicAtlasEntryInfo
    {
        public string Key { get; }
        public RectInt Area { get; }
        public int ReferenceCount { get; }
        public int WaiterCount { get; }

        /// <summary>
        /// 从内部条目生成只读调试信息
        /// </summary>
        internal DynamicAtlasEntryInfo(DynamicAtlasEntry entry)
        {
            Key = entry.Key;
            Area = entry.Area;
            ReferenceCount = entry.ReferenceCount;
            WaiterCount = entry.Request?.WaiterCount ?? 0;
        }
    }

    /// <summary>
    /// 调试页的只读快照 纹理仅借用不得销毁或写入
    /// </summary>
    public readonly struct DynamicAtlasPageInfo
    {
        public int Id { get; }
        public Texture2D Texture { get; }
        public float Occupancy { get; }
        public bool CanRecycle { get; }
        public IReadOnlyList<DynamicAtlasEntryInfo> EntryList { get; }

        /// <summary>
        /// 从内部页生成用于 Editor 的只读信息
        /// </summary>
        internal DynamicAtlasPageInfo(DynamicAtlasPage page)
        {
            Id = page.Id;
            Texture = page.Texture;
            Occupancy = (float)page.AllocatedPixels / ((long)page.Texture.width * page.Texture.height);
            CanRecycle = page.CanRecycle();
            var InfoList = new List<DynamicAtlasEntryInfo>(page.entryList.Count);
            foreach (var Entry in page.entryList)
                InfoList.Add(new DynamicAtlasEntryInfo(Entry));
            EntryList = InfoList.AsReadOnly();
        }
    }
}
