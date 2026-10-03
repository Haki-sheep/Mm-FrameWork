using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MieMieFrameWork.Diagnostics
{
    /// <summary>
    /// 线程安全日志会话 负责过滤 有界缓存与项目文件 不调用 Unity API
    /// </summary>
    public sealed class LogSession : IDisposable
    {
        /// <summary> 会话同步锁 </summary>
        private readonly object syncRoot = new object();

        /// <summary> 最近日志环形数组 </summary>
        private readonly LogRecord[] recentList;

        /// <summary> 渠道显式开关 未配置渠道默认启用 </summary>
        private readonly Dictionary<string, bool> channelDict = new Dictionary<string, bool>(StringComparer.Ordinal);

        /// <summary> 导出上下文 不持有业务对象 </summary>
        private readonly SortedDictionary<string, string> contextDict = new SortedDictionary<string, string>(StringComparer.Ordinal);

        /// <summary> 文本字符预算 </summary>
        private readonly int maximumTextLength;

        /// <summary> 可选轮转文件所有者 </summary>
        private readonly FileLogWriter fileWriter;

        /// <summary> 当前最低级别 </summary>
        private ELogLevel minimumLevel;

        /// <summary> 下一个写入槽位 </summary>
        private int nextIndex;

        /// <summary> 当前缓存条数 </summary>
        private int recentCount;

        /// <summary> 已接受日志序号 </summary>
        private long sequence;

        /// <summary> 是否已经释放 </summary>
        private bool disposed;

        public string SessionId { get; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// 校验并复制预算 按显式配置创建文件后端
        /// </summary>
        public LogSession(LogSettings settings, string fileDirectory = null)
        {
            settings.Validate();
            minimumLevel = settings.MinimumLevel;
            maximumTextLength = settings.MaximumTextLength;
            recentList = new LogRecord[settings.RecentCapacity];
            foreach (string Channel in settings.DisabledChannelList)
                channelDict[Channel] = false;
            if (settings.EnableFile)
                fileWriter = new FileLogWriter(fileDirectory, SessionId, settings.MaximumFileBytes, settings.MaximumFileCount);
        }

        #region 过滤与上下文

        /// <summary>
        /// 查询级别与渠道是否允许记录 可用于避免构造昂贵消息
        /// </summary>
        public bool IsEnabled(ELogLevel level, string channel)
        {
            lock (syncRoot)
            {
                CheckActive();
                return level >= minimumLevel && (!channelDict.TryGetValue(channel, out bool Enabled) || Enabled);
            }
        }

        /// <summary>
        /// 设置最低级别 影响后续所有来源的日志
        /// </summary>
        public void SetMinimumLevel(ELogLevel level)
        {
            if (!Enum.IsDefined(typeof(ELogLevel), level))
                throw new ArgumentOutOfRangeException(nameof(level));
            lock (syncRoot)
            {
                CheckActive();
                minimumLevel = level;
            }
        }

        /// <summary>
        /// 设置模块渠道开关 影响包括异常在内的所有级别
        /// </summary>
        public void SetChannelEnabled(string channel, bool enabled)
        {
            lock (syncRoot)
            {
                CheckActive();
                channelDict[channel] = enabled;
            }
        }

        /// <summary>
        /// 更新导出上下文 如启动阶段和游戏状态
        /// </summary>
        public void SetContext(string key, string value)
        {
            lock (syncRoot)
            {
                CheckActive();
                contextDict[key] = LimitText(value);
            }
        }

        #endregion

        #region 记录与导出

        /// <summary>
        /// 过滤后按序记录日志 返回接受的记录或被过滤时的空值
        /// </summary>
        public LogRecord Write(ELogLevel level, string channel, string message, string stackTrace = "",
            int frame = -1, string memberName = "", string filePath = "", int lineNumber = 0, ELogColor? color = null)
        {
            lock (syncRoot)
            {
                CheckActive();
                if (!IsEnabled(level, channel))
                    return null;
                var Record = new LogRecord(++sequence, frame, level, LimitText(channel), LimitText(message),
                    LimitText(stackTrace), LimitText(memberName), LimitText(filePath), lineNumber, color ?? LogColorUtility.GetDefault(level));
                recentList[nextIndex] = Record;
                nextIndex = (nextIndex + 1) % recentList.Length;
                recentCount = Math.Min(recentCount + 1, recentList.Length);
                fileWriter?.Write(Record);
                return Record;
            }
        }

        /// <summary>
        /// 采集回调与退出并发时允许放弃已经关闭会话的迟到消息
        /// </summary>
        internal void WriteCaptured(ELogLevel level, string message, string stackTrace, int frame)
        {
            lock (syncRoot)
            {
                if (!disposed)
                    Write(level, "Unity", message, stackTrace, frame);
            }
        }

        /// <summary>
        /// 按时间顺序复制最近记录 调用方不能修改内部缓存
        /// </summary>
        public IReadOnlyList<LogRecord> GetRecentList()
        {
            lock (syncRoot)
            {
                CheckActive();
                var RecordList = new List<LogRecord>(recentCount);
                int StartIndex = (nextIndex - recentCount + recentList.Length) % recentList.Length;
                for (int Index = 0; Index < recentCount; Index++)
                    RecordList.Add(recentList[(StartIndex + Index) % recentList.Length]);
                return RecordList.AsReadOnly();
            }
        }

        /// <summary>
        /// 导出当前上下文与近期日志到新文件 不覆盖现有文件
        /// </summary>
        public void ExportRecent(string outputPath)
        {
            lock (syncRoot)
            {
                CheckActive();
                using (var Writer = new StreamWriter(new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)))
                {
                    Writer.WriteLine($"# MieMieLog session={SessionId} accepted={sequence} retained={recentCount}");
                    foreach (var Context in contextDict)
                        Writer.WriteLine($"# {Context.Key}={Context.Value}");
                    foreach (var Record in GetRecentList())
                        Writer.WriteLine(Record.ToString());
                }
            }
        }

        #endregion

        #region 释放与预算

        /// <summary>
        /// 显式刷新可选文件后端
        /// </summary>
        public void Flush()
        {
            lock (syncRoot)
            {
                CheckActive();
                fileWriter?.Flush();
            }
        }

        /// <summary>
        /// 释放文件和缓存 重复释放无副作用
        /// </summary>
        public void Dispose()
        {
            lock (syncRoot)
            {
                if (disposed)
                    return;
                disposed = true;
                try
                {
                    fileWriter?.Dispose();
                }
                finally
                {
                    Array.Clear(recentList, 0, recentList.Length);
                    contextDict.Clear();
                    channelDict.Clear();
                }
            }
        }

        /// <summary>
        /// 限制单条文本预算并保留可见截断标记
        /// </summary>
        private string LimitText(string text)
        {
            if (text == null)
                return string.Empty;
            return text.Length <= maximumTextLength ? text : text.Substring(0, maximumTextLength) + "\n[TRUNCATED]";
        }

        /// <summary>
        /// 使用已释放会话立即暴露生命周期错误
        /// </summary>
        private void CheckActive()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(LogSession));
        }

        #endregion
    }
}
