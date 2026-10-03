using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MieMieFrameWork;
using UnityEngine;
using static MieMieFrameWork.ModuleHub;
using MieMieFrameWork.Diagnostics;

namespace MmUIFrameWork.Core
{
    /// <summary>
    /// UI 核心管理类
    /// </summary>
    [Serializable]
    [ManagerAttribute(Priority)]
    public class UIHub : SingletonMono<UIHub>, IManagerBase
    {
        /// <summary> UI 管理器顺序 位于输入管理器之后与交互管理器之前 </summary>
        public const int Priority = 10;

        /// <summary>
        /// 堆栈系统
        /// </summary>
        private UIStack uiStack;

        /// <summary>
        /// 所有窗口字典
        /// </summary>
        private Dictionary<string, UIDataBase> uiDic = new();

        [SerializeField]
        private Transform UIRoot;
        [SerializeField]
        private Transform PanelRoot;
        [SerializeField]
        private Camera UICamera;

        /// <summary>
        /// 异步预热不可见根节点
        /// </summary>
        private Transform warmUpRoot;

        protected override bool DontDestroyOnLoadEnabled => true;

        #region 初始化与查询

        /// <summary>
        /// 统一管理器初始化入口 仅由框架管理器阶段调用一次
        /// </summary>
        public void Init()
        {
            InitComponents();
        }

        /// <summary>
        /// 初始化 UI 组件引用与窗口堆栈
        /// </summary>
        private void InitComponents()
        {
            if (UIRoot == null)
                UIRoot = transform;
            if (UICamera == null)
                UICamera = UIRoot.GetComponentInChildren<Camera>();
            uiStack = new UIStack();
        }
        /// <summary>
        /// 获取窗口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>

        public T GetWindow<T>() where T : UIDataBase, new()
        {
            Type type = typeof(T);
            string uiName = type.Name;
            if (uiDic.TryGetValue(uiName, out var window))
                return window as T;
            return null;
        }

        /// <summary>
        /// 窗口是否已创建
        /// </summary>
        public bool HasWindow<T>() where T : UIDataBase, new()
        {
            return uiDic.ContainsKey(typeof(T).Name);
        }

        #endregion

        #region 窗口生命周期

        /// <summary>
        /// 显示窗口
        /// </summary>
        public T ShowWindow<T>(Action action = null) where T : UIDataBase, new()
        {
            Type type = typeof(T);
            string uiName = type.Name;

            if (uiDic.ContainsKey(uiName))
            {
                var existingWindow = uiDic[uiName];
                existingWindow.OnShow();
                action?.Invoke();
                return existingWindow as T;
            }

            T uiWindow = CreateWindowInstance<T>(uiName);
            if (uiWindow == null)
                return null;

            uiWindow.OnShow();
            action?.Invoke();
            return uiWindow;
        }

        /// <summary>
        /// 预热窗口 只创建实例 不打开显示
        /// </summary>
        public T WarmUpWindow<T>() where T : UIDataBase, new()
        {
            Type type = typeof(T);
            string uiName = type.Name;

            if (uiDic.TryGetValue(uiName, out var existingWindow))
                return existingWindow as T;

            T uiWindow = CreateWindowInstance<T>(uiName);
            if (uiWindow == null)
                return null;

            uiWindow.OnHide();
            return uiWindow;
        }

        /// <summary>
        /// 异步预热窗口 只创建实例 不打开显示
        /// </summary>
        public async UniTask<T> WarmUpWindowAsync<T>() where T : UIDataBase, new()
        {
            Type windowType = typeof(T);
            string uiName = windowType.Name;
            if (uiDic.TryGetValue(uiName, out var existingWindow))
                return existingWindow as T;

            Transform warmUpParent = GetWarmUpRoot();
            GameObject uiPrefab = await UILoad.LoadAsync(uiName, warmUpParent);
            if (uiPrefab == null)
                return null;

            // 等待期间窗口可能已由其他入口创建
            if (uiDic.TryGetValue(uiName, out existingWindow))
            {
                UILoad.Release(uiPrefab);
                return existingWindow as T;
            }

            T uiWindow = new T();
            uiWindow.BindGameObject(uiPrefab, UICamera);
            uiDic.Add(uiName, uiWindow);
            if (uiWindow.UIMask != null)
                uiWindow.UIMask.gameObject.SetActive(false);

            uiWindow.OnAwake();
            uiWindow.OnHide();
            uiPrefab.transform.SetParent(PanelRoot, false);
            return uiWindow;
        }

        /// <summary>
        /// 获取不可见的异步预热根节点
        /// </summary>
        private Transform GetWarmUpRoot()
        {
            if (warmUpRoot != null)
                return warmUpRoot;

            var rootObject = new GameObject("UIWarmUpRoot");
            warmUpRoot = rootObject.transform;
            warmUpRoot.SetParent(PanelRoot, false);
            var canvasGroup = rootObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return warmUpRoot;
        }

        /// <summary>
        /// 加载并绑定窗口 只走 Awake 不 Show
        /// </summary>
        private T CreateWindowInstance<T>(string uiName) where T : UIDataBase, new()
        {
            T uiWindow = new T();
            GameObject uiPrefab = UILoad.Load(uiName);
            if (uiPrefab == null)
            {
                FrameLog.Error($"[{uiName}] 加载失败", "UI", this);
                return null;
            }

            uiPrefab.transform.SetParent(PanelRoot, false);
            uiWindow.BindGameObject(uiPrefab, UICamera);
            uiDic.Add(uiName, uiWindow);
            if (uiWindow.UIMask != null)
                uiWindow.UIMask.gameObject.SetActive(false);
            uiWindow.OnAwake();
            return uiWindow;
        }

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        public void HideWindow<T>(Action action = null) where T : UIDataBase, new()
        {
            Type type = typeof(T);
            string uiName = type.Name;
            uiDic[uiName]?.OnHide();
            action?.Invoke();
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public void CloseWindow<T>(Action action = null) where T : UIDataBase, new()
        {
            Type type = typeof(T);
            string uiName = type.Name;

            if (uiDic.TryGetValue(uiName, out var uiWindow))
            {
                uiWindow.OnDestroy();
                UILoad.Release(uiWindow.UIGameObject);
                uiDic.Remove(uiName);
            }

            action?.Invoke();
        }

        /// <summary>
        /// 异步加载面板
        /// </summary>
        public async UniTask<T> ShowWindowAsync<T>(Action<T> onComplete = null) where T : UIDataBase, new()
        {
            Type type = typeof(T);
            string uiName = type.Name;

            if (uiDic.ContainsKey(uiName))
            {
                var existingWindow = uiDic[uiName] as T;
                existingWindow.OnShow();
                onComplete?.Invoke(existingWindow);
                return existingWindow;
            }

            GameObject uiPrefab = await UILoad.LoadAsync(uiName);
            if (uiPrefab == null)
                return null;

            T uiWindow = new T();
            
            uiPrefab.transform.SetParent(PanelRoot, false);
            uiWindow.BindGameObject(uiPrefab, UICamera);
            uiDic.Add(uiName, uiWindow);
            uiWindow.OnAwake();
            uiWindow.OnShow();
            onComplete?.Invoke(uiWindow);
            return uiWindow;
        }

        #endregion

        #region 窗口栈

        /// <summary>
        /// 显示窗口并入栈
        /// </summary>
        public T ShowWindowWithStack<T>(Action action = null) where T : UIDataBase, new()
        {
            var currentTop = uiStack.GetTopUI();
            currentTop?.OnHide();
            var newWindow = ShowWindow<T>();
            if (newWindow != null)
                uiStack.PushUI(newWindow);
            action?.Invoke();
            return newWindow;
        }

        /// <summary>
        /// 从堆栈回退
        /// </summary>
        public void PopWindowFromStack(Action action = null)
        {
            var poppedUI = uiStack.PopUI();
            poppedUI?.OnHide();
            var topUI = uiStack.GetTopUI();
            topUI?.OnShow();
            action?.Invoke();
        }

        /// <summary>
        /// 关闭窗口并从堆栈移除
        /// </summary>
        public void CloseWindowFromStack<T>(Action action = null) where T : UIDataBase, new()
        {
            uiStack.RemoveUI<T>();
            CloseWindow<T>();
            action?.Invoke();
        }

        #endregion
    }
}
