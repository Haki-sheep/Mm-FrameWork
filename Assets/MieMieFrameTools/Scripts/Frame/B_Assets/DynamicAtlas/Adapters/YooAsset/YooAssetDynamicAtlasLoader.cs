using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace MieMieFrameWork.Asset.DynamicAtlas
{
    /// <summary>
    /// 通过现有 YooAsset 门面加载源纹理 不另建资源管理器
    /// </summary>
    public sealed class YooAssetDynamicAtlasLoader : IDynamicAtlasSourceLoader
    {
        /// <summary>
        /// 加载默认包中的 Texture2D 成功后转交句柄释放责任
        /// </summary>
        public async UniTask<DynamicAtlasSource> LoadAsync(string key, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var Handle = YooAssetMgr.LoadAssetAsync<Texture2D>(key);
            bool Transferred = false;
            try
            {
                await UniTask.WaitUntil(() => Handle.IsDone, cancellationToken: cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (Handle.Status != EOperationStatus.Succeeded)
                    throw new InvalidOperationException($"动态图集 YooAsset 加载失败 键 {key} 错误 {Handle.Error}");
                var Texture = Handle.GetAssetObject<Texture2D>();
                if (Texture == null)
                    throw new InvalidOperationException($"动态图集资源不是 Texture2D 键 {key}");
                var Source = new DynamicAtlasSource(Texture, Handle.Release);
                Transferred = true;
                return Source;
            }
            finally
            {
                if (!Transferred)
                    Handle.Release();
            }
        }
    }
}
