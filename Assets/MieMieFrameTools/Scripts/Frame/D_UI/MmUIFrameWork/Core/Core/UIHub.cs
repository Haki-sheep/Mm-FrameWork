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
    public class UIHub : SingletonMono<UIHub>, IManagerBase, IDisposable
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
        private readonly Dictionary<string, UIDataBase> uiDic = new();

        /// <summary> 仍可提交结果的同名窗口加载批次 </summary>
        private readonly Dictionary<string, PendingWindowLoad> pendingWindowLoadDict = new();

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

        /// <summary> 管理器释放后拒绝新的窗口加载 </summary>
        private bool isDisposed;

        protected override bool DontDestroyOnLoadEnabled => true;

        #region 初始化与查询

        /// <summary>
        /// 统一管理器初始化入口 仅由框架管理器阶段调用一次
        /// </summary>
        public void Init()
        {
            ThrowIfDisposed();
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
            ThrowIfDisposed();
            string UiName = typeof(T).Name;
            T Window = uiDic.TryGetValue(UiName, out var ExistingWindow)
                ? UseExistingWindow<T>(UiName, ExistingWindow, false)
                : LoadWindow<T>(UiName, false);
            if (Window != null)
                action?.Invoke();
            return Window;
        }

        /// <summary>
        /// 预热窗口 只创建实例 不打开显示
        /// </summary>
        public T WarmUpWindow<T>() where T : UIDataBase, new()
        {
            ThrowIfDisposed();
            string UiName = typeof(T).Name;
            return uiDic.TryGetValue(UiName, out var ExistingWindow)
                ? ExistingWindow as T
                : LoadWindow<T>(UiName, true);
        }

        /// <summary>
        /// 异步预热窗口 只创建实例 不打开显示
        /// </summary>
        public UniTask<T> WarmUpWindowAsync<T>() where T : UIDataBase, new()
        {
            return LoadWindowAsync<T>(true);
        }

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        public void HideWindow<T>(Action action = null) where T : UIDataBase, new()
        {
            string UiName = typeof(T).Name;
            if (pendingWindowLoadDict.TryGetValue(UiName, out var PendingLoad))
                PendingLoad.HideVersion++;

            if (uiDic.TryGetValue(UiName, out var Window))
                Window.OnHide();
            action?.Invoke();
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public void CloseWindow<T>(Action action = null) where T : UIDataBase, new()
        {
            string UiName = typeof(T).Name;
            pendingWindowLoadDict.Remove(UiName);
            if (uiDic.TryGetValue(UiName, out var Window))
                DestroyWindow(UiName, Window, Window.UIGameObject);

            action?.Invoke();
        }

        /// <summary>
        /// 异步加载面板
        /// </summary>
        public async UniTask<T> ShowWindowAsync<T>(Action<T> onComplete = null) where T : UIDataBase, new()
        {
            T Window = await LoadWindowAsync<T>(false);
            if (Window != null)
                onComplete?.Invoke(Window);
            return Window;
        }

        #endregion

        #region 窗口加载

        /// <summary>
        /// 同步加载并绑定窗口
        /// </summary>
        private T LoadWindow<T>(string uiName, bool isWarmUp) where T : UIDataBase, new()
        {
            var PendingLoad = BeginWindowLoad(uiName);
            int HideVersion = PendingLoad.HideVersion;
            try
            {
                var Instance = UILoad.Load(uiName);
                if (Instance == null)
                {
                    FrameLog.Error($"[{uiName}] 加载失败", "UI", this);
                    return null;
                }

                return CompleteWindowLoad<T>(uiName, Instance, PendingLoad, HideVersion, isWarmUp, false);
            }
            finally
            {
                EndWindowLoad(uiName, PendingLoad);
            }
        }

        private async UniTask<T> LoadWindowAsync<T>(bool isWarmUp) where T : UIDataBase, new()
        {
            ThrowIfDisposed();
            string UiName = typeof(T).Name;
            if (uiDic.TryGetValue(UiName, out var ExistingWindow))
                return UseExistingWindow<T>(UiName, ExistingWindow, isWarmUp);

            var PendingLoad = BeginWindowLoad(UiName);
            int HideVersion = PendingLoad.HideVersion;
            try
            {
                var Instance = await UILoad.LoadAsync(UiName, isWarmUp ? GetWarmUpRoot() : null);
                if (Instance == null)
                    return null;

                return CompleteWindowLoad<T>(UiName, Instance, PendingLoad, HideVersion, isWarmUp, isWarmUp);
            }
            finally
            {
                EndWindowLoad(UiName, PendingLoad);
            }
        }

        private T CompleteWindowLoad<T>(string uiName, GameObject instance, PendingWindowLoad pendingLoad,
            int hideVersion, bool isWarmUp, bool underWarmUpRoot) where T : UIDataBase, new()
        {
            T Window = null;
            bool OwnsInstance = true;
            Exception Failure = null;
            try
            {
                if (!CanCompleteLoad(uiName, pendingLoad, hideVersion, isWarmUp))
                    return null;

                // 等待期间窗口可能已由其他入口创建
                if (uiDic.TryGetValue(uiName, out var ExistingWindow))
                {
                    T Existing = UseExistingWindow<T>(uiName, ExistingWindow, isWarmUp);
                    return CanCompleteLoad(uiName, pendingLoad, hideVersion, isWarmUp) ? Existing : null;
                }

                if (!underWarmUpRoot)
                    instance.transform.SetParent(PanelRoot, false);

                Window = new T();
                Window.BindGameObject(instance, UICamera);
                if (!CanCompleteLoad(uiName, pendingLoad, hideVersion, isWarmUp))
                    return null;
                if (uiDic.TryGetValue(uiName, out ExistingWindow))
                {
                    T Existing = UseExistingWindow<T>(uiName, ExistingWindow, isWarmUp);
                    return CanCompleteLoad(uiName, pendingLoad, hideVersion, isWarmUp) ? Existing : null;
                }

                uiDic.Add(uiName, Window);
                OwnsInstance = false;
                if (Window.UIMask != null)
                    Window.UIMask.gameObject.SetActive(false);
                if (!IsCurrentWindow(uiName, Window))
                    return null;

                Window.OnAwake();
                if (!CanCompleteLoad(uiName, pendingLoad, hideVersion, isWarmUp) || !IsCurrentWindow(uiName, Window))
                    return null;

                if (isWarmUp)
                    Window.OnHide();
                else
                    Window.OnShow();

                if (underWarmUpRoot && IsCurrentWindow(uiName, Window))
                    instance.transform.SetParent(PanelRoot, false);

                return CanCompleteLoad(uiName, pendingLoad, hideVersion, isWarmUp) && IsCurrentWindow(uiName, Window)
                    ? Window
                    : null;
            }
            catch (Exception Exception)
            {
                Failure = Exception;
                if (!OwnsInstance && IsCurrentWindow(uiName, Window))
                {
                    try
                    {
                        DestroyWindow(uiName, Window, instance);
                    }
                    catch (Exception CleanupFailure)
                    {
                        throw new AggregateException($"[{uiName}] 创建窗口及清理失败", Exception, CleanupFailure);
                    }
                }
                throw;
            }
            finally
            {
                if (OwnsInstance)
                    ReleaseWindowInstance(instance, Failure);
            }
        }

        private T UseExistingWindow<T>(string uiName, UIDataBase window, bool isWarmUp) where T : UIDataBase
        {
            if (!isWarmUp)
                window.OnShow();
            return IsCurrentWindow(uiName, window) ? window as T : null;
        }

        private bool IsCurrentWindow(string uiName, UIDataBase window)
        {
            return uiDic.TryGetValue(uiName, out var CurrentWindow) && ReferenceEquals(CurrentWindow, window);
        }

        private void DestroyWindow(string uiName, UIDataBase window, GameObject instance)
        {
            uiDic.Remove(uiName);
            uiStack.RemoveUI(window);
            ReleaseOwnedWindow(window, instance);
        }

        private static void ReleaseOwnedWindow(UIDataBase window, GameObject instance)
        {
            Exception Failure = null;
            try
            {
                window.OnDestroy();
            }
            catch (Exception Exception)
            {
                Failure = Exception;
                throw;
            }
            finally
            {
                ReleaseWindowInstance(instance, Failure);
            }
        }

        private static void ReleaseWindowInstance(GameObject instance, Exception failure)
        {
            if (instance == null)
                return;

            try
            {
                UILoad.Release(instance);
            }
            catch (Exception CleanupFailure) when (failure != null)
            {
                throw new AggregateException("窗口操作及实例释放失败", failure, CleanupFailure);
            }
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

        #endregion

        #region 在途加载

        private sealed class PendingWindowLoad
        {
            public int Count { get; set; }
            public int HideVersion { get; set; }
        }

        private PendingWindowLoad BeginWindowLoad(string uiName)
        {
            if (!pendingWindowLoadDict.TryGetValue(uiName, out var PendingLoad))
            {
                PendingLoad = new PendingWindowLoad();
                pendingWindowLoadDict.Add(uiName, PendingLoad);
            }

            PendingLoad.Count++;
            return PendingLoad;
        }

        private void EndWindowLoad(string uiName, PendingWindowLoad pendingLoad)
        {
            pendingLoad.Count--;
            if (pendingLoad.Count == 0 && pendingWindowLoadDict.TryGetValue(uiName, out var CurrentLoad) &&
                ReferenceEquals(CurrentLoad, pendingLoad))
                pendingWindowLoadDict.Remove(uiName);
        }

        private bool CanCompleteLoad(string uiName, PendingWindowLoad pendingLoad, int hideVersion, bool isWarmUp)
        {
            return pendingWindowLoadDict.TryGetValue(uiName, out var CurrentLoad) &&
                ReferenceEquals(CurrentLoad, pendingLoad) && (isWarmUp || hideVersion == pendingLoad.HideVersion);
        }

        #endregion

        #region 释放

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            pendingWindowLoadDict.Clear();
            var WindowList = new List<KeyValuePair<string, UIDataBase>>(uiDic);
            uiDic.Clear();
            uiStack?.ClearStack();

            var FailureList = new List<Exception>();
            foreach (var Entry in WindowList)
            {
                try
                {
                    ReleaseOwnedWindow(Entry.Value, Entry.Value.UIGameObject);
                }
                catch (Exception Exception)
                {
                    FailureList.Add(new InvalidOperationException($"[{Entry.Key}] 释放窗口失败", Exception));
                }
            }

            if (warmUpRoot != null)
            {
                try
                {
                    UnityEngine.Object.Destroy(warmUpRoot.gameObject);
                }
                catch (Exception Exception)
                {
                    FailureList.Add(new InvalidOperationException("释放 UI 预热根节点失败", Exception));
                }
                finally
                {
                    warmUpRoot = null;
                }
            }

            if (FailureList.Count > 0)
                throw new AggregateException("释放 UI 管理器失败", FailureList);
        }

        protected override void OnDestroy()
        {
            try
            {
                Dispose();
            }
            finally
            {
                base.OnDestroy();
            }
        }

        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(UIHub));
        }

        #endregion

        #region 窗口栈

        /// <summary>
        /// 显示窗口并入栈
        /// </summary>
        public T ShowWindowWithStack<T>(Action action = null) where T : UIDataBase, new()
        {
            var NewWindow = ShowWindow<T>();
            if (NewWindow != null)
            {
                var CurrentTop = uiStack.GetTopUI();
                // 已在栈中的窗口移到栈顶 避免重复入栈
                uiStack.RemoveUI(NewWindow);
                uiStack.PushUI(NewWindow);

                if (CurrentTop != null && !ReferenceEquals(CurrentTop, NewWindow))
                    CurrentTop.OnHide();
                if (!IsCurrentWindow(typeof(T).Name, NewWindow))
                    NewWindow = null;
            }
            action?.Invoke();
            return NewWindow;
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
            CloseWindow<T>();
            action?.Invoke();
        }

        #endregion
    }
}
