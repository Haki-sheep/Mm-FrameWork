using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MieMieFrameWork.Asset.DynamicAtlas
{
    /// <summary>
    /// 加载源纹理 返回包含明确释放责任的临时所有权
    /// </summary>
    public interface IDynamicAtlasSourceLoader
    {
        /// <summary>
        /// 输入资源键与取消令牌 成功后转移源资源释放责任给调用者
        /// </summary>
        public UniTask<DynamicAtlasSource> LoadAsync(string key, CancellationToken cancellationToken);
    }

    /// <summary>
    /// GPU 写入完成后释放源资源 不持有图集使用引用
    /// </summary>
    public sealed class DynamicAtlasSource : IDisposable
    {
        /// <summary>
        /// 源资源释放回调
        /// </summary>
        private Action release;

        public Texture2D Texture { get; private set; }

        /// <summary>
        /// 输入源纹理与释放回调 建立一次性源资源所有权
        /// </summary>
        public DynamicAtlasSource(Texture2D texture, Action release)
        {
            Texture = texture;
            this.release = release ?? throw new ArgumentNullException(nameof(release));
        }

        /// <summary>
        /// 只执行一次源资源释放回调
        /// </summary>
        public void Dispose()
        {
            var Release = release;
            release = null;
            Texture = null;
            Release?.Invoke();
        }
    }
}
