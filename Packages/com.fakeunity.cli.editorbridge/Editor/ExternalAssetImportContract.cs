#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace FakeUnity.ExternalAssetImport
{
    public sealed class ExternalAssetImportPlan
    {
        public string SourcePath;
        public string DestinationPath;
        public string AssetPath;
        public string SourceSha256;
        public long SizeBytes;
        public string Extension;
    }

    public static class ExternalAssetImportContract
    {
        private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".tif", ".tiff", ".psd", ".exr", ".hdr", ".gif", ".svg",
            ".fbx", ".obj", ".blend", ".dae", ".3ds", ".dxf",
            ".wav", ".mp3", ".ogg", ".aif", ".aiff", ".flac", ".mp4", ".mov", ".webm",
            ".ttf", ".otf", ".bytes", ".json", ".txt", ".csv", ".xml"
        };

        public static ExternalAssetImportPlan Plan(string projectRoot, string source, string destination,
            string expectedSourceSha256 = null, Func<string, FileAttributes> attributes = null)
        {
            var root = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var sourcePath = Path.GetFullPath(source);
            var assetsRoot = Path.Combine(root, "Assets");
            var destinationPath = Path.GetFullPath(Path.IsPathRooted(destination) ? destination : Path.Combine(root, destination));
            if (!File.Exists(sourcePath)) throw new InvalidDataException("External source must be an existing file.");
            if (Inside(sourcePath, root)) throw new InvalidDataException("External source must be outside the Unity project root.");
            if (!Inside(destinationPath, assetsRoot) || destinationPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Destination must be a non-meta file strictly under this project's Assets directory.");
            if (!Directory.Exists(Path.GetDirectoryName(destinationPath)))
                throw new InvalidDataException("Destination parent directory must already exist in v1.");
            if (File.Exists(destinationPath) || Directory.Exists(destinationPath) ||
                File.Exists(destinationPath + ".meta") || Directory.Exists(destinationPath + ".meta"))
                throw new InvalidDataException("Destination asset and meta must both be absent; v1 never overwrites.");
            RejectReparse(sourcePath, null, attributes);
            RejectReparse(Path.GetDirectoryName(destinationPath), root, attributes);
            var sourceExtension = Path.GetExtension(sourcePath);
            var destinationExtension = Path.GetExtension(destinationPath);
            if (!AllowedExtensions.Contains(sourceExtension) || !sourceExtension.Equals(destinationExtension, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Source/destination require the same allow-listed non-code extension.");
            var hash = HashFile(sourcePath, out var size);
            if (expectedSourceSha256 != null && (!IsSha256(expectedSourceSha256) ||
                !hash.Equals(expectedSourceSha256, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("External source SHA-256 does not match --expected-source-sha256.");
            var assetPath = Path.GetRelativePath(root, destinationPath).Replace('\\', '/');
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidDataException("Destination must preserve Unity's canonical Assets/ casing.");
            return new ExternalAssetImportPlan { SourcePath = sourcePath, DestinationPath = destinationPath,
                AssetPath = assetPath, SourceSha256 = hash,
                SizeBytes = size, Extension = sourceExtension.ToLowerInvariant() };
        }

        public static string CopyNew(string source, string destination, string expectedSha256, out long size)
        {
            var pending = destination + ".fuc-importing-" + Guid.NewGuid().ToString("N");
            string actual; size = 0;
            try
            {
                using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.SequentialScan))
                using (var output = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.SequentialScan))
                using (var sha = SHA256.Create())
                {
                    var buffer = new byte[1024 * 1024]; int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        output.Write(buffer, 0, read); sha.TransformBlock(buffer, 0, read, null, 0); size += read;
                    }
                    sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0); output.Flush(true);
                    actual = BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
                    if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("External source changed after preflight; destination was not created.");
                }
                if (File.Exists(destination) || File.Exists(destination + ".meta"))
                    throw new IOException("Destination appeared during copy; overwrite refused.");
                File.Move(pending, destination); return actual;
            }
            finally { if (File.Exists(pending)) File.Delete(pending); }
        }

        public static string HashFile(string path, out long size)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.SequentialScan))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(stream); size = stream.Length;
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public static bool IsSha256(string value) => value != null && value.Length == 64 && value.All(c =>
            c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F');

        private static bool Inside(string path, string root)
        {
            var comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return path.StartsWith(prefix, comparison);
        }

        private static void RejectReparse(string path, string stopBefore, Func<string, FileAttributes> attributes)
        {
            var current = path; attributes = attributes ?? File.GetAttributes;
            while (!String.IsNullOrEmpty(current) && (stopBefore == null || !Path.GetFullPath(current).Equals(stopBefore,
                       Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
            {
                if ((attributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("External import paths cannot traverse reparse points: " + current);
                current = Path.GetDirectoryName(current);
            }
        }
    }
}
