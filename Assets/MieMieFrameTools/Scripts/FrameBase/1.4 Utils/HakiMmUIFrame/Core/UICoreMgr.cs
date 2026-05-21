using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MieMieFrameWork.UI
{
    // [Serializable]
    // public enum E_UILayer
    // {
    //     Bottom,
    //     Middle,
    //     Top,
    //     System,
    // }
    /// <summary>

    /// UI核心管理类
    /// </summary>
    [Serializable]
    public class UICoreMgr : MonoBehaviour, IManagerBase
    {
        //堆栈系统
        private UIStack uiStack ;

        //所有窗口字典
        private Dictionary<string, UIDataBase> uiDic = new();

        [SerializeField] private Transform UIRoot;

        [SerializeField] private Camera UICamera;

        public void Init()
        {  
            UIRoot = this.transform;
            UICamera = UIRoot.GetComponentInChildren<Camera>();
            uiStack =new();
        }
        public T ShowWindow<T>(bool isUseAnimation = false, Action action = null) where T : UIDataBase, new()
        {
            Type type = typeof(T);
            string uiName = type.Name;

            //查询字典
            if (uiDic.ContainsKey(uiName))
            {
                var existingWindow = uiDic[uiName];
                existingWindow.OnShow();
                existingWindow.ApplyAniamtion = isUseAnimation;
                action?.Invoke();
                Debug.Log($"[UICoreMgr.ShowWindow] [{uiName}] 命中缓存，OnShow t={Time.realtimeSinceStartup:F3}");
                return existingWindow as T;
            }

            //如果没有就new新的
            T uiWindow = new T();

            //加载UI
            GameObject uiPrefab = UILoad.AddressableLoad(uiName);

            uiPrefab.transform.SetParent(UIRoot, false);
            uiWindow.BindGameObject(uiPrefab, UICamera);

            uiWindow.ApplyAniamtion = isUseAnimation;

            uiDic.Add(uiName, uiWindow);

            uiWindow.OnAwake();
            uiWindow.OnShow();

            action?.Invoke();
            return uiWindow;
        }

        public void HideWindow<T>(bool isUseAnimation = false, Action action = null) where T : UIDataBase, new()
        {
            Type type = typeof(T);
            string uiName = type.Name;

            uiDic[uiName].ApplyAniamtion = isUseAnimation;
            uiDic[uiName]?.OnHide();
            action?.Invoke();
        }

        public void CloseWindow<T>(bool isUseAnimation = false, Action action = null) where T : UIDataBase, new()
        {
            Type type = typeof(T);
            string uiName = type.Name;

            if (uiDic.TryGetValue(uiName, out var uiWindow))
            {
                uiWindow.ApplyAniamtion = isUseAnimation;
                uiWindow.OnDestroy();
                AddressableMgr.ReleaseAsset(uiName);
                uiDic.Remove(uiName);
            }
            action?.Invoke();
        }

        /// <summary>
        /// 异步加载面板
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="isUseAnimation">是否使用动画</param>
        /// <param name="onComplete">加载完成回调（参数为加载的UI实例）</param>
        /// <returns>加载的UI实例</returns>
        public async UniTask<T> ShowWindowAsync<T>(bool isUseAnimation = false, Action<T> onComplete = null) where T : UIDataBase, new()
        {
            Type type = typeof(T);
            string uiName = type.Name;

            // 查询字典，如果有则直接返回
            if (uiDic.ContainsKey(uiName))
            {
                var existingWindow = uiDic[uiName] as T;
                existingWindow.ApplyAniamtion = isUseAnimation;
                existingWindow.OnShow();
                onComplete?.Invoke(existingWindow);
                return existingWindow;
            }

            // 异步加载
            GameObject uiPrefab = await UILoad.AddressableLoadAsync(uiName);
            if (uiPrefab == null)
            {
                return null;
            }

            T uiWindow = new T();
            uiPrefab = GameObject.Instantiate(uiPrefab);
            uiPrefab.transform.SetParent(UIRoot, false);
            uiWindow.BindGameObject(uiPrefab, UICamera);
            uiWindow.ApplyAniamtion = isUseAnimation;
            uiDic.Add(uiName, uiWindow);
            uiWindow.OnAwake();
            uiWindow.OnShow();

            onComplete?.Invoke(uiWindow);
            return uiWindow;
        }

        /// <summary>
        /// 显示窗口并入栈
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="action">回调</param>
        /// <returns>显示的UI</returns>
        public T ShowWindowWithStack<T>(Action action = null) where T : UIDataBase, new()
        {
            // 隐藏当前栈顶界面
            var currentTop = uiStack.GetTopUI();
            currentTop?.OnHide(); // 隐藏旧界面

            // 显示新界面
            var newWindow = ShowWindow<T>();

            // 新界面入栈
            if (newWindow != null)
                uiStack.PushUI(newWindow);

            action?.Invoke();
            return newWindow;
        }

        /// <summary>
        /// 从堆栈回退（隐藏栈顶，显示下一个）
        /// </summary>
        public void PopWindowFromStack(Action action = null)
        {
            // 弹出栈顶界面并隐藏
            var poppedUI = uiStack.PopUI();
            poppedUI?.OnHide();

            // 显示下一个界面并显示
            var topUI = uiStack.GetTopUI();
            topUI?.OnShow();

            action?.Invoke();
        }

        /// <summary>
        /// 关闭窗口并从堆栈移除
        /// </summary>
        public void CloseWindowFromStack<T>(Action action = null) where T : UIDataBase, new()
        {
            // 从堆栈移除
            uiStack.RemoveUI<T>();
            // 关闭窗口
            CloseWindow<T>();
            action?.Invoke();
        }

    }
}