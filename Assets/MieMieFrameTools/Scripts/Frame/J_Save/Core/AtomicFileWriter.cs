using System.IO;

namespace MiMieSaver
{
    /// <summary>
    /// 存档文件原子写入 中途崩溃或断电时目标文件保持旧内容
    /// </summary>
    internal static class AtomicFileWriter
    {
        /// <summary>
        /// 先写同目录临时文件并落盘 再整体替换目标文件
        /// </summary>
        internal static void WriteAllBytes(string path, byte[] bytes)
        {
            string TempPath = path + ".tmp";
            using (var Stream = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Stream.Write(bytes, 0, bytes.Length);
                Stream.Flush(true);
            }

            // 同卷 Replace 为重命名操作 不存在旧文件的窗口期
            if (File.Exists(path))
                File.Replace(TempPath, path, null);
            else
                File.Move(TempPath, path);
        }
    }
}
