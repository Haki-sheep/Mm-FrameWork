namespace MieMieFrameWork
{
    using UnityEngine;

    /// <summary>
    /// Unity 侧日志与时间注入
    /// </summary>
    public static class EventBusBootstrap
    {
        /// <summary>
        /// 注入 Unity 依赖
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            EventBusLog.LogError = Debug.LogError;
            TypedEventBusTrace.NowFunc = () => Time.realtimeSinceStartup;
        }
    }
}
