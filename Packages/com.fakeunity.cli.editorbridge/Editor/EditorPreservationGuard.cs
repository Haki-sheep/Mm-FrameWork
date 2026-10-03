// Shared source: compiled into the host Operations assembly and the Unity Bridge.
#nullable disable
using System;
using System.IO;
using System.Text;
#if UNITY_EDITOR
using UnityEngine;
#else
using System.Text.Json;
#endif

namespace FakeUnity.Preservation
{
    [Serializable]
    public sealed class PreservationRecord
    {
        public string schema_version = "";
        public string state = "";
        public string guard_id = "";
        public string owner = "";
        public string reason = "";
        public string created_at = "";
        public string editor_instance_id = "";
        public int editor_pid;
        public string released_at = "";
        public string release_reason = "";
    }

    public sealed class PreservationObservation
    {
        public string Status { get; }
        public PreservationRecord Record { get; }
        public string Reason { get; }
        public bool BlocksWrites => Status != "none" && Status != "released";
        public PreservationObservation(string status, PreservationRecord record = null, string reason = null)
        { Status = status; Record = record; Reason = reason; }
    }

    public sealed class PreservationException : IOException
    {
        public PreservationException(string message) : base(message) { }
    }

    /// <summary>Cooperative local intent, not an OS access policy. No automatic expiry or recovery.</summary>
    public static class EditorPreservationGuard
    {
        public const string ErrorCode = "FUC_EDITOR_PROTECTED";
        public const string Recovery = "Preserve the investigation. Only after explicit user approval, release the exact guard ID with a reason; do not stop Play Mode, retry writes or remove the record automatically.";
        public static string StatePath(string root) => Path.Combine(Path.GetFullPath(root), ".fuc", "editor-preservation.json");

        public static PreservationObservation Read(string root)
        {
            try
            {
                var path = StatePath(root);
                if (File.Exists(Path.GetDirectoryName(path))) throw new IOException("Preservation parent is not a directory.");
                RejectLink(Path.GetDirectoryName(path));
                RejectLink(path);
                using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    if (file.Length > 16384) throw new IOException("Preservation record exceeds 16 KiB.");
                    using (var reader = new StreamReader(file, Encoding.UTF8, true))
                    {
                        var value = Deserialize(reader.ReadToEnd());
                        Guid id;
                        DateTimeOffset created, released;
                        if (value == null || value.schema_version != "1.0" ||
                            !Guid.TryParseExact(value.guard_id, "N", out id) ||
                            String.IsNullOrWhiteSpace(value.owner) || String.IsNullOrWhiteSpace(value.reason) ||
                            !DateTimeOffset.TryParse(value.created_at, out created) ||
                            (value.state != "active" && value.state != "released") ||
                            (value.state == "released" && (String.IsNullOrWhiteSpace(value.release_reason) ||
                                !DateTimeOffset.TryParse(value.released_at, out released))))
                            return new PreservationObservation("invalid", reason: "Invalid preservation schema/state; do not delete it to bypass protection.");
                        return new PreservationObservation(value.state, value);
                    }
                }
            }
            catch (FileNotFoundException) { return new PreservationObservation("none"); }
            catch (DirectoryNotFoundException) { return new PreservationObservation("none"); }
            catch (Exception ex) { return new PreservationObservation("unavailable", reason: ex.GetType().Name + ": " + ex.Message); }
        }

        public static void RequireUnprotected(string root)
        {
            var state = Read(root);
            if (state.BlocksWrites) throw new PreservationException("Editor preservation " + state.Status + ": " +
                (state.Record == null ? state.Reason : state.Record.guard_id + " owner=" + state.Record.owner + " reason=" + state.Record.reason));
        }

        public static FileStream EnterWrite(string root)
        {
            var lease = EnterMutex(root);
            try { RequireUnprotected(root); return lease; }
            catch { lease.Dispose(); throw; }
        }

        public static PreservationRecord Begin(string root, string owner, string reason, int pid, string instance)
        {
            ValidateText(owner, "owner"); ValidateText(reason, "reason");
            using (EnterMutex(root))
            {
                RequireUnprotected(root);
                var record = new PreservationRecord { schema_version = "1.0", state = "active", guard_id = Guid.NewGuid().ToString("N"), owner = owner,
                    reason = reason, created_at = DateTimeOffset.UtcNow.ToString("o"), editor_pid = pid,
                    editor_instance_id = instance ?? "" };
                Write(root, record); return record;
            }
        }

        public static PreservationRecord Release(string root, string guardId, string reason)
        {
            ValidateText(reason, "reason");
            using (EnterMutex(root))
            {
                var state = Read(root);
                if (state.Record == null || !String.Equals(state.Record.guard_id, guardId, StringComparison.Ordinal))
                    throw new PreservationException("Release requires the exact existing guard ID; invalid/unavailable records require manual investigation.");
                if (state.Status == "released") return state.Record;
                state.Record.state = "released";
                state.Record.released_at = DateTimeOffset.UtcNow.ToString("o");
                state.Record.release_reason = reason;
                Write(root, state.Record); return state.Record;
            }
        }

        private static FileStream EnterMutex(string root)
        {
            try
            {
                var directory = Path.GetDirectoryName(StatePath(root));
                RejectLink(directory);
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, "editor-preservation.mutex");
                RejectLink(path);
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (Exception ex) { throw new PreservationException("Preservation coordination unavailable/busy: " + ex.Message); }
        }

        private static void Write(string root, PreservationRecord record)
        {
            var path = StatePath(root);
            RejectLink(path);
            var pending = path + ".pending-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllText(pending, Serialize(record), new UTF8Encoding(false));
                if (File.Exists(path)) File.Replace(pending, path, null);
                else File.Move(pending, path);
            }
            finally { if (File.Exists(pending)) File.Delete(pending); }
        }

        private static void RejectLink(string path)
        {
            try
            {
                if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new PreservationException("Preservation path cannot be a link: " + path);
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
        private static void ValidateText(string value, string name)
        {
            if (String.IsNullOrWhiteSpace(value) || value.Length > 1024 || value.IndexOf('\0') >= 0)
                throw new ArgumentException(name + " must contain 1..1024 characters.");
        }
#if UNITY_EDITOR
        private static PreservationRecord Deserialize(string json) => JsonUtility.FromJson<PreservationRecord>(json);
        private static string Serialize(PreservationRecord record) => JsonUtility.ToJson(record, true);
#else
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
        private static PreservationRecord Deserialize(string json) => JsonSerializer.Deserialize<PreservationRecord>(json, JsonOptions);
        private static string Serialize(PreservationRecord record) => JsonSerializer.Serialize(record, JsonOptions);
#endif
    }
}
