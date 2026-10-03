#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FakeUnityCLI.EditorBridge
{
    /// <summary>
    /// Coordinates a coherent external update of this embedded Package while Unity remains open.
    /// The currently loaded Bridge only pauses refresh/reload; the CLI owns transactional file IO.
    /// </summary>
    internal static class LiveBridgeUpdate
    {
        internal const string PrepareOperation = "bridge-update-prepare";
        internal const string CommitOperation = "bridge-update-commit";
        internal const string AbortOperation = "bridge-update-abort";
        private const int MaximumLockSeconds = 60;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        private static string _token;
        private static string _targetVersion;
        private static double _deadline;
        private static bool _refreshLocked;
        private static bool _reloadLocked;

        internal static void Initialize()
        {
            EditorApplication.update -= Watchdog;
            EditorApplication.update += Watchdog;
        }

        internal static void Stop()
        {
            EditorApplication.update -= Watchdog;
            ReleaseLocks(false);
        }

        internal static bool IsOperation(string operation)
        {
            return operation == PrepareOperation || operation == CommitOperation || operation == AbortOperation;
        }

        internal static Dictionary<string, object> Execute(string operation, string payloadJson)
        {
            var payload = JsonUtility.FromJson<UpdatePayload>(payloadJson ?? "{}");
            if (payload == null || !ValidToken(payload.token))
                throw new ArgumentException("Bridge live update requires a stable token.");
            if (operation == PrepareOperation) return Prepare(payload);
            if (operation == CommitOperation) return Commit(payload);
            if (operation == AbortOperation) return Abort(payload, "cli_abort");
            throw new NotSupportedException("Unsupported Bridge update operation: " + operation);
        }

        private static Dictionary<string, object> Prepare(UpdatePayload payload)
        {
            if (!String.IsNullOrEmpty(_token))
            {
                if (_token == payload.token)
                    return Status("prepared", false, "prepare_replayed");
                throw new InvalidOperationException("Another Bridge live update already holds the refresh lock.");
            }
            if (String.IsNullOrWhiteSpace(payload.target_version))
                throw new ArgumentException("target_version is required.");
            var lockSeconds = Math.Max(10, Math.Min(MaximumLockSeconds,
                payload.lock_timeout_seconds <= 0 ? 45 : payload.lock_timeout_seconds));
            AssetDatabase.DisallowAutoRefresh();
            _refreshLocked = true;
            try
            {
                EditorApplication.LockReloadAssemblies();
                _reloadLocked = true;
            }
            catch
            {
                ReleaseLocks(false);
                throw;
            }
            _token = payload.token;
            _targetVersion = payload.target_version;
            _deadline = EditorApplication.timeSinceStartup + lockSeconds;
            return Status("prepared", true, "refresh_and_reload_locked");
        }

        private static Dictionary<string, object> Commit(UpdatePayload payload)
        {
            RequireOwner(payload);
            if (payload.files == null || payload.files.Length == 0)
                throw new ArgumentException("Bridge update commit requires the target file digest list.");
            var packageRoot = PackageRoot();
            var declared = ReadPackageFiles(packageRoot);
            var submitted = payload.files.Select(file => file == null ? null : file.path)
                .Where(path => path != null).ToArray();
            if (declared.Length != submitted.Length ||
                declared.Except(submitted, StringComparer.Ordinal).Any() ||
                submitted.Distinct(StringComparer.Ordinal).Count() != submitted.Length)
                throw new InvalidDataException("Bridge update digest list does not match package-files.json.");
            foreach (var file in payload.files)
            {
                if (file == null || !ValidRelativePath(file.path) || !ValidHash(file.sha256))
                    throw new InvalidDataException("Bridge update contains an invalid file digest entry.");
                var path = Path.GetFullPath(Path.Combine(packageRoot, file.path.Replace('/', Path.DirectorySeparatorChar)));
                if (!IsWithin(packageRoot, path) || !File.Exists(path) || IsReparse(path))
                    throw new InvalidDataException("Bridge update target file is missing or unsafe: " + file.path);
                var actual = Hash(File.ReadAllBytes(path));
                if (!FixedHashEquals(actual, file.sha256))
                    throw new InvalidDataException("Bridge update target hash mismatch: " + file.path);
            }
            var installedVersion = ReadInstalledVersion(packageRoot);
            if (!String.Equals(installedVersion, payload.target_version, StringComparison.Ordinal))
                throw new InvalidDataException("Installed package version does not match target_version.");
            var result = Status("committed", true, "reload_scheduled");
            EditorApplication.delayCall += CompleteCommit;
            return result;
        }

        private static Dictionary<string, object> Abort(UpdatePayload payload, string reason)
        {
            RequireOwner(payload);
            var result = Status("aborted", true, reason);
            EditorApplication.delayCall += delegate { ReleaseLocks(false); };
            return result;
        }

        private static void CompleteCommit()
        {
            ReleaseLocks(true);
        }

        private static void Watchdog()
        {
            if (String.IsNullOrEmpty(_token) || EditorApplication.timeSinceStartup <= _deadline) return;
            Debug.LogError("[FakeUnityCLI] Bridge live update lock expired; auto-unlocking without commit.");
            ReleaseLocks(false);
        }

        private static void ReleaseLocks(bool refresh)
        {
            var hadLocks = _refreshLocked || _reloadLocked;
            if (_refreshLocked)
            {
                try { AssetDatabase.AllowAutoRefresh(); }
                catch (Exception exception) { Debug.LogError("[FakeUnityCLI] AllowAutoRefresh failed: " + exception.Message); }
                _refreshLocked = false;
            }
            if (_reloadLocked)
            {
                try { EditorApplication.UnlockReloadAssemblies(); }
                catch (Exception exception) { Debug.LogError("[FakeUnityCLI] UnlockReloadAssemblies failed: " + exception.Message); }
                _reloadLocked = false;
            }
            _token = null;
            _targetVersion = null;
            _deadline = 0;
            if (refresh && hadLocks)
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void RequireOwner(UpdatePayload payload)
        {
            if (String.IsNullOrEmpty(_token) || !String.Equals(_token, payload.token, StringComparison.Ordinal) ||
                !String.Equals(_targetVersion, payload.target_version, StringComparison.Ordinal))
                throw new InvalidOperationException("Bridge live update token or target version does not own the active lock.");
        }

        private static Dictionary<string, object> Status(string state, bool changed, string detail)
        {
            return new Dictionary<string, object>
            {
                { "operation", "bridge-live-update" },
                { "state", state },
                { "changed", changed },
                { "detail", detail },
                { "token", _token },
                { "current_bridge_version", BridgeBootstrap.BridgeVersion },
                { "target_bridge_version", _targetVersion },
                { "refresh_locked", _refreshLocked },
                { "reload_locked", _reloadLocked }
            };
        }

        private static string PackageRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "com.fakeunity.cli.editorbridge"));
        }

        private static string ReadInstalledVersion(string packageRoot)
        {
            var packagePath = Path.Combine(packageRoot, "package.json");
            var manifest = JsonUtility.FromJson<PackageVersion>(File.ReadAllText(packagePath, Utf8));
            return manifest == null ? null : manifest.version;
        }

        private static string[] ReadPackageFiles(string packageRoot)
        {
            var path = Path.Combine(packageRoot, "package-files.json");
            var manifest = JsonUtility.FromJson<PackageFiles>(File.ReadAllText(path, Utf8));
            if (manifest == null || manifest.manifest_version != "1.0" || manifest.files == null ||
                manifest.files.Any(file => !ValidRelativePath(file)) ||
                manifest.files.Distinct(StringComparer.Ordinal).Count() != manifest.files.Length)
                throw new InvalidDataException("Installed package-files.json is invalid.");
            return manifest.files;
        }

        private static bool ValidToken(string value)
        {
            return !String.IsNullOrWhiteSpace(value) && value.Length <= 100 && value.All(character =>
                Char.IsLetterOrDigit(character) || character == '-' || character == '_');
        }

        private static bool ValidHash(string value)
        {
            return value != null && value.Length == 64 && value.All(character =>
                character >= '0' && character <= '9' || character >= 'a' && character <= 'f' ||
                character >= 'A' && character <= 'F');
        }

        private static bool ValidRelativePath(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value.StartsWith("/", StringComparison.Ordinal) || value.Contains(":"))
                return false;
            var segments = value.Replace('\\', '/').Split('/');
            return segments.All(segment => segment.Length > 0 && segment != "." && segment != "..");
        }

        private static bool IsWithin(string root, string path)
        {
            var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedPath = Path.GetFullPath(path);
            return String.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsReparse(string path)
        {
            return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
        }

        private static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", String.Empty).ToLowerInvariant();
        }

        private static bool FixedHashEquals(string left, string right)
        {
            if (!ValidHash(left) || !ValidHash(right)) return false;
            var a = Utf8.GetBytes(left.ToLowerInvariant());
            var b = Utf8.GetBytes(right.ToLowerInvariant());
            var difference = 0;
            for (var index = 0; index < a.Length; index++) difference |= a[index] ^ b[index];
            return difference == 0;
        }

        [Serializable] private sealed class PackageVersion { public string version; }
        [Serializable] private sealed class PackageFiles { public string manifest_version; public string[] files; }
        [Serializable] private sealed class UpdatePayload
        {
            public string token; public string target_version; public int lock_timeout_seconds;
            public FileDigest[] files;
        }
        [Serializable] private sealed class FileDigest { public string path; public string sha256; }
    }
}
#endif
