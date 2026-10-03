using UnityEngine;
using MieMieFrameWork.Diagnostics;

namespace MiMieEventBus.Unity
{
    /// <summary>
    /// Unity 日志与时间桥接
    /// </summary>
    public static class EventBusUnityBootstrap
    {
        /// <summary>
        /// 注入 Unity 依赖
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            EventBusLog.LogError = Message => FrameLog.Error(Message, "Event");
            EventBusTrace.NowFunc = () => Time.realtimeSinceStartup;
        }
    }
}
