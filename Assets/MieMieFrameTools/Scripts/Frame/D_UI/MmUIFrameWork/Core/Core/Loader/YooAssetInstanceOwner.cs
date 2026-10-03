using UnityEngine;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace MmUIFrameWork.Core
{
    /// <summary>
    /// 持有 UI 实例对应的原生资源句柄 实例销毁后释放
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class YooAssetInstanceOwner : MonoBehaviour
    {
        /// <summary> 当前实例独占的资源句柄 </summary>
        private AssetHandle assetHandle;

        /// <summary>
        /// 实例创建后由 UI 加载入口移交资源句柄
        /// </summary>
        public void InitComponents(AssetHandle handle)
        {
            assetHandle = handle;
        }

        /// <summary>
        /// 销毁实例并等待实际销毁后释放 兼容从未激活的预热实例
        /// </summary>
        public void DestroyInstance()
        {
            GameObject Instance = gameObject;
            var Handle = assetHandle;
            Object.Destroy(Instance);
            ReleaseAfterDestroy(Instance, Handle).Forget();
        }

        /// <summary>
        /// 等待实例销毁后释放句柄 OnDestroy 已释放时不重复处理
        /// </summary>
        private static async UniTask ReleaseAfterDestroy(GameObject instance, AssetHandle handle)
        {
            await UniTask.WaitUntil(() => instance == null);
            if (handle.IsValid)
                handle.Release();
        }

        /// <summary>
        /// 实例销毁时释放资源句柄 不提前卸载仍被实例使用的资源
        /// </summary>
        private void OnDestroy()
        {
            if (assetHandle != null && assetHandle.IsValid)
                assetHandle.Release();
            assetHandle = null;
        }
    }
}
