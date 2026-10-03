using System.Collections.Generic;
using UnityEngine;

namespace MieMieFrameWork.Asset.DynamicAtlas
{
    /// <summary>
    /// 持有一页 GPU 纹理与只追加分配器
    /// </summary>
    internal sealed class DynamicAtlasPage
    {
        /// <summary>
        /// 只追加矩形分配器
        /// </summary>
        private readonly DynamicAtlasAllocator allocator;
        /// <summary>
        /// 本页资源条目
        /// </summary>
        internal readonly List<DynamicAtlasEntry> entryList = new List<DynamicAtlasEntry>();

        public int Id { get; }
        public Texture2D Texture { get; }
        public long AllocatedPixels { get; private set; }

        /// <summary>
        /// 建立无 MipMap 的 RGBA32 GPU 图集并释放 CPU 副本
        /// </summary>
        public DynamicAtlasPage(int id, int width, int height, bool srgb)
        {
            Id = id;
            allocator = new DynamicAtlasAllocator(width, height);
            Texture = new Texture2D(width, height, TextureFormat.RGBA32, false, !srgb)
            {
                name = $"DynamicAtlas Page {id}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            Texture.Apply(false, true);
        }

        /// <summary>
        /// 追加分配矩形并累计包含边缘扩展的占用
        /// </summary>
        public bool TryAllocate(int width, int height, out RectInt area)
        {
            if (!allocator.TryAllocate(width, height, out area))
                return false;
            AllocatedPixels += (long)width * height;
            return true;
        }

        /// <summary>
        /// 发布事务结束且活跃句柄与等待者均为零时允许整页回收
        /// </summary>
        public bool CanRecycle()
        {
            foreach (var Entry in entryList)
            {
                if (Entry.ReferenceCount > 0 || Entry.Request != null && (!Entry.Request.IsCompleted || Entry.Request.WaiterCount > 0))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 销毁本页生成的 Sprite 与纹理
        /// </summary>
        public void Dispose()
        {
            foreach (var Entry in entryList)
                DestroyObject(Entry.Sprite);
            entryList.Clear();
            DestroyObject(Texture);
        }

        /// <summary>
        /// 按当前运行模式销毁本模块持有的 Unity 对象
        /// </summary>
        internal static void DestroyObject(Object target)
        {
            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }
}
