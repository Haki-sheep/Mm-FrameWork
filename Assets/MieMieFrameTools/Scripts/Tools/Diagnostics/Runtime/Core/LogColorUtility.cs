using System;

namespace MieMieFrameWork.Diagnostics
{
    /// <summary>
    /// 日志颜色的统一映射 不依赖具体输出平台
    /// </summary>
    public static class LogColorUtility
    {
        /// <summary>
        /// 按级别选择默认颜色 普通白色 告警黄色 错误红色
        /// </summary>
        public static ELogColor GetDefault(ELogLevel level)
        {
            return level >= ELogLevel.Error ? ELogColor.Red :
                level == ELogLevel.Warning ? ELogColor.Yellow : ELogColor.White;
        }

        /// <summary>
        /// 将九种颜色转换为六位 RGB 十六进制文本
        /// </summary>
        public static string GetHex(ELogColor color)
        {
            switch (color)
            {
                case ELogColor.White: return "FFFFFF";
                case ELogColor.Red: return "FF0000";
                case ELogColor.Orange: return "FF8000";
                case ELogColor.Yellow: return "FFFF00";
                case ELogColor.Green: return "00FF00";
                case ELogColor.Blue: return "0000FF";
                case ELogColor.Indigo: return "4B0082";
                case ELogColor.Violet: return "8B00FF";
                case ELogColor.Black: return "000000";
                default: throw new ArgumentOutOfRangeException(nameof(color), color, "未知日志颜色");
            }
        }
    }
}
