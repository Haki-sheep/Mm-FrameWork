#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace FakeUnity.AssetDelete
{
    /// <summary> 删除范围中的文件身份 </summary>
    public sealed class AssetDeleteFile
    {
        public string Path { get; set; }
        public string Sha256 { get; set; }
    }

    /// <summary> 原生删除前的精确文件与目录范围 </summary>
    public sealed class AssetDeleteSnapshot
    {
        public string AssetPath { get; set; }
        public string FullPath { get; set; }
        public AssetDeleteFile[] FileList { get; set; }
        public string[] DirectoryList { get; set; }
    }

    public static class AssetDeleteContract
    {
        /// <summary>
        /// 输入工程与目标 校验路径和链接 返回实际删除范围及文件身份
        /// </summary>
        public static AssetDeleteSnapshot Capture(string Root, string Target)
        {
            var ProjectRoot = System.IO.Path.GetFullPath(Root);
            var FullPath = System.IO.Path.GetFullPath(System.IO.Path.IsPathRooted(Target) ? Target : System.IO.Path.Combine(ProjectRoot, Target));
            var AssetPath = System.IO.Path.GetRelativePath(ProjectRoot, FullPath).Replace('\\', '/');
            if (!AssetPath.StartsWith("Assets/", StringComparison.Ordinal) || AssetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Delete target must be strictly inside Assets and cannot be a meta file or Assets root");
            if (!File.Exists(FullPath) && !Directory.Exists(FullPath))
                throw new FileNotFoundException("Delete target does not exist", AssetPath);
            RejectLinks(FullPath);
            var FileList = new List<string>();
            var DirectoryList = new List<string>();
            Collect(FullPath, FileList, DirectoryList);
            if (File.Exists(FullPath + ".meta")) { RejectLinks(FullPath + ".meta"); FileList.Add(FullPath + ".meta"); }
            return new AssetDeleteSnapshot
            {
                AssetPath = AssetPath, FullPath = FullPath,
                FileList = FileList.OrderBy(Item => Item, StringComparer.Ordinal).Select(Item => new AssetDeleteFile
                { Path = System.IO.Path.GetRelativePath(ProjectRoot, Item).Replace('\\', '/'), Sha256 = HashFile(Item) }).ToArray(),
                DirectoryList = DirectoryList.Select(Item => System.IO.Path.GetRelativePath(ProjectRoot, Item).Replace('\\', '/'))
                    .OrderBy(Item => Item, StringComparer.Ordinal).ToArray()
            };
        }

        /// <summary>
        /// 输入计划与当前范围 比较路径和内容 返回是否保持一致
        /// </summary>
        public static bool Matches(AssetDeleteSnapshot Before, AssetDeleteSnapshot After) =>
            Before.AssetPath == After.AssetPath && Before.DirectoryList.SequenceEqual(After.DirectoryList) &&
            Before.FileList.Select(Item => Item.Path + "=" + Item.Sha256)
                .SequenceEqual(After.FileList.Select(Item => Item.Path + "=" + Item.Sha256));

        /// <summary>
        /// 输入目标路径 遍历时先拒绝链接 收集文件与目录
        /// </summary>
        private static void Collect(string Target, List<string> FileList, List<string> DirectoryList)
        {
            RejectLinks(Target);
            if (!Directory.Exists(Target)) { FileList.Add(Target); return; }
            DirectoryList.Add(Target);
            foreach (var Entry in Directory.EnumerateFileSystemEntries(Target)) Collect(Entry, FileList, DirectoryList);
        }

        /// <summary>
        /// 输入现有路径 拒绝任意父级符号链接或重解析点
        /// </summary>
        private static void RejectLinks(string Target)
        {
            for (var Current = Target; !String.IsNullOrEmpty(Current); Current = System.IO.Path.GetDirectoryName(Current))
                if ((File.GetAttributes(Current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Delete path cannot traverse a reparse point " + Current);
        }

        /// <summary>
        /// 输入文件 流式计算 SHA256 返回内容身份
        /// </summary>
        private static string HashFile(string Target)
        {
            using (var Stream = File.OpenRead(Target))
            using (var Hash = SHA256.Create())
                return BitConverter.ToString(Hash.ComputeHash(Stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
