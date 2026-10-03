using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MieMieFrameWork.Diagnostics
{
    /// <summary>
    /// 双端日志门面 会话仅由显式入口创建 不触发业务或资源系统初始化
    /// </summary>
    public static partial class FrameLog
    {
        /// <summary> 当前运行会话 由门面独占并在退出时释放 </summary>
        private static LogSession session;

        /// <summary> 同步输出委托 上下文与异常仅在当次调用传递 </summary>
        private static Action<LogRecord, object, Exception> output;

        /// <summary> 输出适配器退出时解除订阅与释放自身资源 </summary>
        private static Action disconnectOutput;

        /// <summary> 主线程采集的帧号 后台线程只读取缓存 </summary>
        private static int currentFrame = -1;

        public static bool IsReady => session != null;

        #region 会话生命周期

        /// <summary>
        /// 显式创建唯一会话并注入输出委托 空委托只保存记录
        /// Init 与 Shutdown 由宿主同一线程串行调用 完成初始化后再启动日志生产
        /// </summary>
        public static void Init(LogSettings settings, Action<LogRecord, object, Exception> logOutput = null,
            string fileDirectory = null)
        {
            if (IsReady)
                throw new InvalidOperationException("日志会话已经启动 请先结束旧会话");
            var Session = new LogSession(settings, fileDirectory);
            output = logOutput;
            Volatile.Write(ref session, Session);
        }

        /// <summary>
        /// 解除输出订阅并释放唯一会话 业务线程须先停止日志生产
        /// 与 Init 在宿主同一线程串行调用 不与新会话初始化并发
        /// </summary>
        public static void Shutdown()
        {
            var Session = Interlocked.Exchange(ref session, null);
            var DisconnectOutput = disconnectOutput;
            disconnectOutput = null;
            output = null;
            Volatile.Write(ref currentFrame, -1);
            try
            {
                DisconnectOutput?.Invoke();
            }
            finally
            {
                Session?.Dispose();
            }
        }

        /// <summary>
        /// 获取活动会话 不在日志调用中隐式初始化
        /// </summary>
        private static LogSession GetSession()
        {
            return Volatile.Read(ref session) ?? throw new InvalidOperationException("日志会话尚未启动或已经退出");
        }

        #endregion

        #region 过滤与诊断

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

        #endregion

        #region 日志入口

        /// <summary>
        /// 记录普通日志 默认白色 可指定七彩颜色或黑白
        /// </summary>
        public static void Log(string message, ELogColor color = ELogColor.White, string channel = "Default", object context = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Write(ELogLevel.Info, message, channel, context, null, memberName, filePath, lineNumber, color);
        }

        /// <summary>
        /// 记录调试日志 正式构建裁剪调用与参数求值
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("FRAME_LOG_TRACE")]
        public static void Trace(string message, string channel = "Default", object context = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Write(ELogLevel.Debug, message, channel, context, null, memberName, filePath, lineNumber, ELogColor.White);
        }

        /// <summary>
        /// 记录普通业务阶段日志
        /// </summary>
        public static void Info(string message, string channel = "Default", object context = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,
            ELogColor color = ELogColor.White)
        {
            Write(ELogLevel.Info, message, channel, context, null, memberName, filePath, lineNumber, color);
        }

        /// <summary>
        /// 记录告警
        /// </summary>
        public static void Warning(string message, string channel = "Default", object context = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,
            ELogColor color = ELogColor.Yellow)
        {
            Write(ELogLevel.Warning, message, channel, context, null, memberName, filePath, lineNumber, color);
        }

        /// <summary>
        /// 记录错误 不替代调用方原有异常传播
        /// </summary>
        public static void Error(string message, string channel = "Default", object context = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0,
            ELogColor color = ELogColor.Red)
        {
            Write(ELogLevel.Error, message, channel, context, null, memberName, filePath, lineNumber, color);
        }

        /// <summary>
        /// 记录原始异常 Unity 输出仍使用原异常对象
        /// </summary>
        public static void Exception(Exception exception, string channel = "Default", object context = null,
            [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
        {
            Write(ELogLevel.Exception, exception.Message, channel, context, exception, memberName, filePath, lineNumber, ELogColor.Red);
        }

        /// <summary>
        /// 先过滤并存储再调用输出委托 核心消息不带显示标签
        /// </summary>
        private static void Write(ELogLevel level, string message, string channel, object context,
            Exception exception, string memberName, string filePath, int lineNumber, ELogColor color)
        {
            var Record = GetSession().Write(level, channel, message, exception?.ToString() ?? string.Empty,
                Volatile.Read(ref currentFrame), memberName, filePath, lineNumber, color);
            if (Record != null)
                output?.Invoke(Record, context, exception);
        }

        #endregion
    }
}
