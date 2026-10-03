using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MieMieFrameWork.Diagnostics
{
    /// <summary>
    /// 游戏日志门面 会话仅由启动钩子创建 不触发业务或资源系统初始化
    /// </summary>
    public static partial class FrameLog
    {
        /// <summary> 当前运行会话 退出或重新进入 Play 时释放 </summary>
        private static LogSession session;

        /// <summary> Unity 输出开关 </summary>
        private static bool outputToUnity;

        /// <summary> 防止门面输出被 Unity 回调重复采集 </summary>
        [ThreadStatic]
        private static bool handlingUnity;

        /// <summary> 主线程采集的帧号 后台线程只读取缓存 </summary>
        private static int currentFrame = -1;

        public static bool IsReady => session != null;

        /// <summary>
        /// 获取活动会话 不在日志调用中隐式初始化
        /// </summary>
        private static LogSession GetSession()
        {
            return Volatile.Read(ref session) ?? throw new InvalidOperationException("日志会话尚未启动或已经退出");
        }

        /// <summary>
        /// 查询过滤结果 在构造高开销消息前调用
        /// </summary>
        public static bool IsEnabled(ELogLevel level, string channel)
        {
            return GetSession().IsEnabled(level, channel);
        }

        /// <summary>
        /// 设置所有来源统一使用的最低级别
        /// </summary>
        public static void SetMinimumLevel(ELogLevel level)
        {
            GetSession().SetMinimumLevel(level);
        }

        /// <summary>
        /// 开关模块渠道 Unity 原始日志使用 Unity 渠道
        /// </summary>
        public static void SetChannelEnabled(string channel, bool enabled)
        {
            GetSession().SetChannelEnabled(channel, enabled);
        }

        /// <summary>
        /// 更新导出上下文 不存储业务对象
        /// </summary>
        public static void SetContext(string key, string value)
        {
            GetSession().SetContext(key, value);
        }

        /// <summary>
        /// 获取近期日志的只读快照
        /// </summary>
        public static IReadOnlyList<LogRecord> GetRecentList()
        {
            return GetSession().GetRecentList();
        }

        /// <summary>
        /// 将上下文与近期日志导出到新文件
        /// </summary>
        public static void ExportRecent(string outputPath)
        {
            GetSession().ExportRecent(outputPath);
        }

        /// <summary>
        /// 记录调试日志 正式构建裁剪调用与参数求值
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Trace(string message, string channel = "Default", Object context = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Write(ELogLevel.Debug, message, channel, context, null, memberName, filePath, lineNumber);
        }

        /// <summary>
        /// 记录普通业务阶段日志
        /// </summary>
        public static void Info(string message, string channel = "Default", Object context = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Write(ELogLevel.Info, message, channel, context, null, memberName, filePath, lineNumber);
        }

        /// <summary>
        /// 记录告警
        /// </summary>
        public static void Warning(string message, string channel = "Default", Object context = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Write(ELogLevel.Warning, message, channel, context, null, memberName, filePath, lineNumber);
        }

        /// <summary>
        /// 记录错误 不替代调用方原有异常传播
        /// </summary>
        public static void Error(string message, string channel = "Default", Object context = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Write(ELogLevel.Error, message, channel, context, null, memberName, filePath, lineNumber);
        }

        /// <summary>
        /// 记录原始异常 Unity 输出仍使用原异常对象
        /// </summary>
        public static void Exception(Exception exception, string channel = "Default", Object context = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Write(ELogLevel.Exception, exception.Message, channel, context, exception, memberName, filePath, lineNumber);
        }

        /// <summary>
        /// 先存储再向 Unity 输出 回调只采集第三方日志
        /// </summary>
        private static void Write(ELogLevel level, string message, string channel, Object context,
            Exception exception, string memberName, string filePath, int lineNumber)
        {
            var Record = GetSession().Write(level, channel, message, exception?.ToString() ?? string.Empty,
                Volatile.Read(ref currentFrame), memberName, filePath, lineNumber);
            if (Record == null || !outputToUnity)
                return;
            bool PreviousHandling = handlingUnity;
            handlingUnity = true;
            try
            {
                if (exception != null)
                    Debug.LogException(exception, context);
                else if (level >= ELogLevel.Error)
                    Debug.LogError($"[{channel}] {message}", context);
                else if (level == ELogLevel.Warning)
                    Debug.LogWarning($"[{channel}] {message}", context);
                else
                    Debug.Log($"[{channel}] {message}", context);
            }
            finally
            {
                handlingUnity = PreviousHandling;
            }
        }
    }
}
