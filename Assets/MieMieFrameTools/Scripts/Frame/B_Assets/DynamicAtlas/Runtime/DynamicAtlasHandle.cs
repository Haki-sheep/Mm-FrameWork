using System;
using UnityEngine;

namespace MieMieFrameWork.Asset.DynamicAtlas
{
    /// <summary>
    /// 一个消费者的一次图集引用 释放后禁止继续使用共享 Sprite
    /// </summary>
    public sealed class DynamicAtlasHandle : IDisposable
    {
        /// <summary>
        /// 所属服务 释放时清除
        /// </summary>
        private DynamicAtlasService owner;
        /// <summary>
        /// 共享资源条目
        /// </summary>
        private readonly DynamicAtlasEntry entry;

        public bool IsValid => owner != null && !owner.IsDisposed;
        public string Key => entry.Key;
        public int PageId => entry.Page.Id;
        public Sprite Sprite => IsValid ? entry.Sprite : throw new ObjectDisposedException(nameof(DynamicAtlasHandle));
        public Rect Uv => new Rect(Sprite.rect.x / entry.Page.Texture.width,
            Sprite.rect.y / entry.Page.Texture.height, Sprite.rect.width / entry.Page.Texture.width,
            Sprite.rect.height / entry.Page.Texture.height);

        /// <summary>
        /// 服务已增加引用后创建消费者句柄
        /// </summary>
        internal DynamicAtlasHandle(DynamicAtlasService owner, DynamicAtlasEntry entry)
        {
            this.owner = owner;
            this.entry = entry;
        }

        /// <summary>
        /// 归还本消费者引用 重复调用不重复扣减
        /// </summary>
        public void Dispose()
        {
            if (owner == null)
                return;
            owner.Release(entry);
            owner = null;
        }
    }
}
