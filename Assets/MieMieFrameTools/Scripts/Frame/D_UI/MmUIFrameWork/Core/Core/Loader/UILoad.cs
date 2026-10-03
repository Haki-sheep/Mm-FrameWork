using Cysharp.Threading.Tasks;
using System;
using MieMieFrameWork.Asset;
using UnityEngine;
using YooAsset;

namespace MmUIFrameWork.Core
{
    /// <summary>
    /// UI 加载工具类 使用 YooAsset 原生句柄并由实例生命周期释放
    /// </summary>
    public static class UILoad
    {
        /// <summary>
        /// 同步加载 UI 预制体 窗口名须对应 YooAsset 采集地址
        /// </summary>
        public static GameObject Load(string uiName)
        {
            var Handle = YooAssetMgr.LoadAsset<GameObject>(uiName);
            try
            {
                return CreateInstance(Handle, uiName, null);
            }
            catch
            {
                YooAssetMgr.Release(Handle);
                throw;
            }
        }

        /// <summary>
        /// 异步加载 UI 预制体 根节点销毁时取消等待并释放本次句柄
        /// </summary>
        public static async UniTask<GameObject> LoadAsync(string uiName, Transform parent = null)
        {
            var Handle = YooAssetMgr.LoadAssetAsync<GameObject>(uiName);
            try
            {
                await UniTask.WaitUntil(() => Handle.IsDone,
                    cancellationToken: MieMieFrameWork.ModuleHub.Instance.GetCancellationTokenOnDestroy());
                return CreateInstance(Handle, uiName, parent);
            }
            catch
            {
                YooAssetMgr.Release(Handle);
                throw;
            }
        }

        /// <summary>
        /// 释放 UI 实例
        /// </summary>
        public static void Release(GameObject uiInstance)
        {
            uiInstance.GetComponent<YooAssetInstanceOwner>().DestroyInstance();
        }

        /// <summary>
        /// 验证加载结果 创建 UI 实例并移交句柄所有权
        /// </summary>
        private static GameObject CreateInstance(AssetHandle handle, string uiName, Transform parent)
        {
            if (handle.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException($"UI 加载失败 地址 {uiName} 错误 {handle.Error}");

            GameObject Instance = handle.InstantiateSync(new InstantiateOptions(true, parent, false));
            if (Instance == null)
                throw new InvalidOperationException($"UI 实例化失败 地址 {uiName}");

            Instance.AddComponent<YooAssetInstanceOwner>().InitComponents(handle);
            NormalizeRect(Instance);
            return Instance;
        }

        /// <summary>
        /// 归一化 RectTransform
        /// </summary>
        private static void NormalizeRect(GameObject uiPrefab)
        {
            RectTransform rectTransform = uiPrefab.GetComponent<RectTransform>();
            if (rectTransform == null)
                return;

            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
        }
    }
}
