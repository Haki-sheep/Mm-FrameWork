using System;

namespace MieMieFrameWork.Diagnostics
{
    /// <summary>
    /// C# 后端控制台输出 真彩终端使用 ANSI 重定向时只输出纯文本
    /// </summary>
    public static class ConsoleLogOutput
    {
        /// <summary> 保证多线程输出的颜色序列与文本不交错 </summary>
        private static readonly object syncRoot = new object();

        /// <summary>
        /// 输出已过滤的记录 不持有当次上下文与异常对象
        /// </summary>
        public static void Write(LogRecord record, object context, Exception exception)
        {
            lock (syncRoot)
            {
                if (Console.IsOutputRedirected)
                {
                    Console.WriteLine(record.ToString());
                    return;
                }
                int Rgb = Convert.ToInt32(LogColorUtility.GetHex(record.Color), 16);
                int Red = (Rgb >> 16) & 255;
                int Green = (Rgb >> 8) & 255;
                int Blue = Rgb & 255;
                Console.WriteLine($"\u001b[38;2;{Red};{Green};{Blue}m{record}\u001b[0m");
            }
        }
    }
}
