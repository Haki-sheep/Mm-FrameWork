using System;
using System.Globalization;

namespace MieMieFrameWork.Diagnostics
{
    /// <summary>
    /// 不持有 Unity 对象的不可变日志记录
    /// </summary>
    public sealed class LogRecord
    {
        public long Sequence { get; }
        public DateTime TimeUtc { get; }
        public int Frame { get; }
        public int ThreadId { get; }
        public ELogLevel Level { get; }
        public string Channel { get; }
        public string Message { get; }
        public string StackTrace { get; }
        public string MemberName { get; }
        public string FilePath { get; }
        public int LineNumber { get; }

        /// <summary>
        /// 接收采集信息生成只读记录
        /// </summary>
        internal LogRecord(long sequence, int frame, ELogLevel level, string channel,
            string message, string stackTrace, string memberName, string filePath, int lineNumber)
        {
            Sequence = sequence;
            TimeUtc = DateTime.UtcNow;
            Frame = frame;
            ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            Level = level;
            Channel = channel;
            Message = message;
            StackTrace = stackTrace;
            MemberName = memberName;
            FilePath = filePath;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// 输出包含原始堆栈与调用位置的可读文本
        /// </summary>
        public override string ToString()
        {
            string Text = $"[{TimeUtc.ToString("O", CultureInfo.InvariantCulture)}] #{Sequence} F{Frame} T{ThreadId} [{Level}][{Channel}] {Message}";
            if (!string.IsNullOrEmpty(FilePath))
                Text += $"\n(at {FilePath}:{LineNumber}) {MemberName}";
            if (!string.IsNullOrEmpty(StackTrace))
                Text += "\n" + StackTrace;
            return Text;
        }
    }
}
