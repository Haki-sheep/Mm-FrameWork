using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace MieMieFrameWork.Diagnostics
{
    public static partial class FrameLog
    {
        /// <summary> 可选配置的唯一 Resources 路径 </summary>
        public const string ProfileResourcePath = "MieMieLogProfile";

        /// <summary> 独立文件后端的专属子目录 </summary>
        public const string FileDirectoryName = "MieMieLogs";

        /// <summary> 由会话独占的主线程采样组件 </summary>
        private static FrameLogPump pump;

        /// <summary>
        /// 新会话前释放旧状态 支持关闭 Domain Reload 后重复进入 Play
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            Shutdown();
        }

        /// <summary>
        /// 在场景 Awake 与业务资源初始化前启动唯一日志会话
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init()
        {
            var Profile = Resources.Load<LogProfile>(ProfileResourcePath);
            var Settings = Profile != null ? Profile.Settings : new LogSettings();
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            if (Profile == null)
                Settings.MinimumLevel = ELogLevel.Warning;
#endif
            var Session = new LogSession(Settings, Path.Combine(Application.persistentDataPath, FileDirectoryName));
            Session.SetContext("Application", Application.productName);
            Session.SetContext("Version", Application.version);
            Session.SetContext("Unity", Application.unityVersion);
            Session.SetContext("Platform", Application.platform.ToString());
            Session.SetContext("BootStage", "BeforeSceneLoad");
            session = Session;
            outputToUnity = Settings.OutputToUnity;
            currentFrame = Time.frameCount;
            if (Settings.CaptureUnity)
                Application.logMessageReceivedThreaded += CaptureUnity;
            Application.quitting += Shutdown;
            var PumpObject = new GameObject("MieMieLogSession");
            PumpObject.hideFlags = HideFlags.HideInHierarchy;
            UnityEngine.Object.DontDestroyOnLoad(PumpObject);
            pump = PumpObject.AddComponent<FrameLogPump>();
        }

        /// <summary>
        /// 主线程刷新帧缓存 记录中的帧号为最近一次主线程采样
        /// </summary>
        internal static void RefreshFrame()
        {
            Volatile.Write(ref currentFrame, Time.frameCount);
        }

        /// <summary>
        /// 应用暂停时刷新文件 降低移动端被挂起后的缓冲丢失
        /// </summary>
        internal static void FlushSession()
        {
            session?.Flush();
        }

        /// <summary>
        /// 收集各线程 Unity 日志 只写存储 不再次调用 Unity 日志
        /// </summary>
        private static void CaptureUnity(string message, string stackTrace, LogType type)
        {
            if (handlingUnity)
                return;
            var Session = Volatile.Read(ref session);
            if (Session == null)
                return;
            handlingUnity = true;
            try
            {
                var eLevel = type == LogType.Exception ? ELogLevel.Exception :
                    type == LogType.Error || type == LogType.Assert ? ELogLevel.Error :
                    type == LogType.Warning ? ELogLevel.Warning : ELogLevel.Info;
                Session.WriteCaptured(eLevel, message, stackTrace, Volatile.Read(ref currentFrame));
            }
            finally
            {
                handlingUnity = false;
            }
        }

        /// <summary>
        /// 主线程取消应用订阅并释放会话 不释放业务模块
        /// </summary>
        public static void Shutdown()
        {
            Application.logMessageReceivedThreaded -= CaptureUnity;
            Application.quitting -= Shutdown;
            var Session = Interlocked.Exchange(ref session, null);
            var Pump = pump;
            pump = null;
            try
            {
                Session?.Dispose();
            }
            finally
            {
                outputToUnity = false;
                currentFrame = -1;
                if (Pump != null)
                {
#if UNITY_EDITOR
                    UnityEngine.Object.DestroyImmediate(Pump.gameObject);
#else
                    UnityEngine.Object.Destroy(Pump.gameObject);
#endif
                }
            }
        }
    }
}
