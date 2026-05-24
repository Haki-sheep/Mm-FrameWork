using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MieMieFrameWork.UI
{
    /// <summary>
    /// UI 加载工具类
    /// 提供 Addressable 和 Resources 双路径加载
    /// </summary>
    public static class UILoad
    {
        /// <summary>
        /// 同步加载 UI 预制体
        /// </summary>
        /// <param name="uiName">Addressable 地址</param>
        /// <returns>加载的 GameObject</returns>
        public static GameObject AddressableLoad(string uiName)
        {
            var t0 = Time.realtimeSinceStartup;
            Debug.Log($"[UILoad.Load] [{uiName}] 开始同步加载 t={t0:F3}");

            GameObject uiPrefab = AddressableMgr.LoadGameObject(uiName);

            if (uiPrefab == null)
            {
                Debug.LogError($"[UILoad.Load] Addressable 加载失败: {uiName}");
                return null;
            }

            var tEnd = Time.realtimeSinceStartup;
            Debug.Log($"[UILoad.Load] [{uiName}] 加载完成，总耗时={(tEnd - t0) * 1000:F1}ms t={tEnd:F3}");

            // 确保 UI 预制体在正确的位置和状态
            RectTransform rectTransform = uiPrefab.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.localPosition = Vector3.zero;
                rectTransform.localRotation = Quaternion.identity;
            }

            return uiPrefab;
        }

        /// <summary>
        /// 异步加载 UI 预制体
        /// </summary>
        /// <param name="uiName">Addressable 地址</param>
        /// <returns>加载的 GameObject</returns>
        public static async UniTask<GameObject> AddressableLoadAsync(string uiName)
        {
            GameObject uiPrefab = await AddressableMgr.LoadGameObjectAsync(uiName);

            if (uiPrefab == null)
            {
                Debug.LogError($"[UILoad.LoadAsync] Addressable 加载失败: {uiName}");
                return null;
            }

            // 确保 UI 预制体在正确的位置和状态
            RectTransform rectTransform = uiPrefab.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.localPosition = Vector3.zero;
                rectTransform.localRotation = Quaternion.identity;
            }

            return uiPrefab;
        }
    }
}
