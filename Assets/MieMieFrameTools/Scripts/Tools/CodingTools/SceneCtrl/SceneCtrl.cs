namespace MieMieFrameWork
{
    using System;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Cysharp.Threading.Tasks;

    public static class Mm_SceneCtrl
    {
        // 当前是否正在加载场景
        public static bool IsLoading { get; private set; } = false;

        /// <summary>
        /// 添加场景加载进度监听器
        /// </summary>
        /// <param name="progressCallback">进度回调 (0-1)</param>
        public static void AddSceneManagerListener(Action<float> progressCallback)
        {
            MmGlobalEventBus.GlobalBus.Subscribe(MmGameEvents.LoadingSceneProgress, progressCallback);
        }

        /// <summary>
        /// 移除场景加载进度监听器
        /// </summary>
        /// <param name="progressCallback">进度回调</param>
        public static void RemoveSceneManagerListener(Action<float> progressCallback)
        {
            MmGlobalEventBus.GlobalBus.Unsubscribe(MmGameEvents.LoadingSceneProgress, progressCallback);
        }
    
        /// <summary>
        /// 同步加载场景
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="callback">完成回调</param>
        public static void LoadScene(string sceneName, Action callback = null,bool needRepeatLoad = false)
        {
            if (IsLoading)
            {
                Debug.LogWarning("[SceneManager] 正在加载场景中，请等待完成");
                return;
            }

            if (!ValidateScene(sceneName,needRepeatLoad))
                return;

            // LoadScene 延迟到下一帧才真正切换 完成工作挂到 sceneLoaded 上执行
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> OnLoaded = null;
            OnLoaded = (scene, mode) =>
            {
                SceneManager.sceneLoaded -= OnLoaded;
                IsLoading = false;
                OptimizeMemoryAfterLoad();
                callback?.Invoke();
                Debug.Log($"[SceneManager] 场景 {sceneName} 同步加载完成");
            };

            try
            {
                IsLoading = true;
                SceneManager.sceneLoaded += OnLoaded;
                SceneManager.LoadScene(sceneName);
            }
            catch (System.Exception ex)
            {
                SceneManager.sceneLoaded -= OnLoaded;
                IsLoading = false;
                Debug.LogError($"[SceneManager] 加载场景失败: {sceneName}, 错误: {ex.Message}");
            }
        }


        /// <summary>
        /// 异步加载场景（UniTask版本）
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static async UniTask LoadSceneAsync(string sceneName, System.Threading.CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsLoading)
                throw new InvalidOperationException($"已有场景正在加载 无法开始加载 {sceneName}");
            if (string.IsNullOrEmpty(sceneName))
                throw new ArgumentException("场景名称不能为空", nameof(sceneName));
            if (sceneName == SceneManager.GetActiveScene().name)
                throw new InvalidOperationException($"场景已经激活 {sceneName}");
            await DoLoadSceneAsyncUniTask(sceneName, cancellationToken);
        }

        /// <summary>
        /// 异步加载场景（UniTask版本）
        /// </summary>
        private static async UniTask DoLoadSceneAsyncUniTask(string sceneName, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IsLoading = true;

            try
            {
                // 异步加载场景
                var asyncOperation = SceneManager.LoadSceneAsync(sceneName);

                if (asyncOperation == null)
                    throw new InvalidOperationException($"无法创建场景 {sceneName} 的异步加载操作");

                float lastProgress = 0f;

                // 等待场景加载完成
                await asyncOperation.ToUniTask(Progress.Create<float>(progress =>
                {
                    // Unity的progress通常最高到0.9，真正完成要看isDone
                    float normalizedProgress = Mathf.Clamp01(progress / 0.9f);

                    // 只在进度变化时触发事件
                    if (normalizedProgress != lastProgress)
                    {
                        MmGlobalEventBus.GlobalBus.Publish(MmGameEvents.LoadingSceneProgress, normalizedProgress);
                        lastProgress = normalizedProgress;
                    }
                })); // Unity 场景加载开始后无法取消 等待真正完成以保持 IsLoading 准确

                cancellationToken.ThrowIfCancellationRequested();

                // 确保最终进度为100%
                MmGlobalEventBus.GlobalBus.Publish(MmGameEvents.LoadingSceneProgress, 1.0f);

                // 场景加载完成后进行内存优化
                OptimizeMemoryAfterLoad();

                Debug.Log($"[SceneManager] 场景 {sceneName} 异步加载完成（UniTask）");
            }
            finally
            {
                IsLoading = false;
            }
        }


        #region 私有辅助方法

        /// <summary>
        /// 验证场景名称
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        /// <returns>是否有效</returns>
        private static bool ValidateScene(string sceneName,bool needRepeatLoad = false)
        {   
            //如果场景名称不存在，则返回false
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneManager] 场景名称不能为空");
                return false;
            }

            //如果要加载的场景与当前场景相同，则返回false
            if (sceneName == SceneManager.GetActiveScene().name && !needRepeatLoad)
            {
                Debug.LogError("[SceneManager] 要加载的场景与当前场景相同");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 场景加载完成后的内存优化
        /// </summary>
        private static void OptimizeMemoryAfterLoad()
        {
            // 强制垃圾回收
            System.GC.Collect();
            
            // 卸载未使用的资源
            Resources.UnloadUnusedAssets();
            
            Debug.Log("[SceneManager] 内存优化完成");
        }

        #endregion
    }
}
