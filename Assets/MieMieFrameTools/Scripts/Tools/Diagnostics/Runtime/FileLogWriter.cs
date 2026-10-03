using System;
using System.IO;
using System.Text;

namespace MieMieFrameWork.Diagnostics
{
    /// <summary>
    /// 固定槽位轮转文件 仅覆盖专属目录 runtime 编号文件 不批量删除文件
    /// </summary>
    internal sealed class FileLogWriter : IDisposable
    {
        /// <summary> 日志专属目录 </summary>
        private readonly string directory;

        /// <summary> 文件头会话信息 </summary>
        private readonly string header;

        /// <summary> 单文件字节上限 </summary>
        private readonly int maximumBytes;

        /// <summary> 固定槽位总数 </summary>
        private readonly int maximumCount;

        /// <summary> 无 BOM UTF8 编码 </summary>
        private readonly Encoding encoding = new UTF8Encoding(false);

        /// <summary> 当前文件流 </summary>
        private FileStream stream;

        /// <summary> 当前槽位 </summary>
        private int slot;

        /// <summary>
        /// 在专属目录创建首个槽位 文件错误直接传播
        /// </summary>
        internal FileLogWriter(string directory, string sessionId, int maximumBytes, int maximumCount)
        {
            this.directory = Path.GetFullPath(directory);
            header = $"# MieMieLog session={sessionId}\n";
            this.maximumBytes = maximumBytes;
            this.maximumCount = maximumCount;
            Directory.CreateDirectory(this.directory);
            OpenSlot();
        }

        /// <summary>
        /// 写入一条完整记录 达到预算时覆盖下一个专属槽位
        /// </summary>
        internal void Write(LogRecord record)
        {
            byte[] ByteList = encoding.GetBytes(record + "\n");
            if (ByteList.Length + encoding.GetByteCount(header) > maximumBytes)
                throw new InvalidOperationException("日志调用位置或渠道过长 单条记录超过文件预算");
            if (stream.Length + ByteList.Length > maximumBytes)
            {
                stream.Dispose();
                slot = (slot + 1) % maximumCount;
                OpenSlot();
            }
            stream.Write(ByteList, 0, ByteList.Length);
            if (record.Level >= ELogLevel.Error)
                stream.Flush();
        }

        /// <summary>
        /// 将缓冲数据刷新到文件流
        /// </summary>
        internal void Flush()
        {
            stream.Flush();
        }

        /// <summary>
        /// 关闭文件句柄
        /// </summary>
        public void Dispose()
        {
            stream.Dispose();
        }

        /// <summary>
        /// 创建当前固定槽位并写入会话头
        /// </summary>
        private void OpenSlot()
        {
            string FilePath = Path.Combine(directory, $"runtime-{slot}.log");
            var Stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            try
            {
                byte[] HeaderList = encoding.GetBytes(header);
                Stream.Write(HeaderList, 0, HeaderList.Length);
                stream = Stream;
            }
            catch
            {
                Stream.Dispose();
                throw;
            }
        }
    }
}
