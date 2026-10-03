using System;

namespace MieMieFrameWork.Diagnostics
{
    /// <summary>
    /// 会话启动配置 修改原对象不影响已经创建的会话
    /// </summary>
    [Serializable]
    public sealed class LogSettings
    {
        /// <summary> 最低记录级别 </summary>
        public ELogLevel MinimumLevel = ELogLevel.Info;

        /// <summary> 会话启动时禁用的模块渠道 </summary>
        public string[] DisabledChannelList = Array.Empty<string>();

        /// <summary> 最近日志条数上限 </summary>
        public int RecentCapacity = 512;

        /// <summary> 每条消息与堆栈各自的字符上限 超出会明确标记截断 </summary>
        public int MaximumTextLength = 32768;

        /// <summary> 是否启用项目独立日志文件 </summary>
        public bool EnableFile = false;

        /// <summary> 单个轮转文件字节预算 </summary>
        public int MaximumFileBytes = 4194304;

        /// <summary> 固定轮转文件槽位数 仅覆盖日志专属目录中的对应槽位 </summary>
        public int MaximumFileCount = 4;

        /// <summary> 是否向 Unity 控制台输出门面日志 </summary>
        public bool OutputToUnity = true;

        /// <summary> 是否收集 Unity 与第三方原始日志 </summary>
        public bool CaptureUnity = true;

        /// <summary> 单条文本允许的最大字符数 </summary>
        private const int TextLengthLimit = 32768;

        /// <summary> 保证完整记录可写入的最小文件预算 </summary>
        private const int MinimumFileBytes = 1048576;

        /// <summary>
        /// 校验显式配置 非法预算立即抛出错误
        /// </summary>
        internal void Validate()
        {
            if (!Enum.IsDefined(typeof(ELogLevel), MinimumLevel))
                throw new ArgumentOutOfRangeException(nameof(MinimumLevel));
            if (RecentCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(RecentCapacity));
            if (MaximumTextLength <= 0 || MaximumTextLength > TextLengthLimit)
                throw new ArgumentOutOfRangeException(nameof(MaximumTextLength));
            if (EnableFile && MaximumFileBytes < MinimumFileBytes)
                throw new ArgumentOutOfRangeException(nameof(MaximumFileBytes), "文件预算至少为 1 MiB 以容纳单条日志");
            if (EnableFile && MaximumFileCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(MaximumFileCount));
        }
    }
}
