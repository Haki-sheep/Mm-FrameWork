#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace FakeUnityCLI.EditorBridge
{
    internal sealed class EditorAssetChangeAudit : IDisposable
    {
        private static EditorAssetChangeAudit current;
        private readonly HashSet<string> expected;
        private readonly HashSet<string> changes = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<JObject> events = new List<JObject>();
        private int recordedCharacters;
        internal bool Truncated { get; private set; }
        internal string[] Unexpected => changes.Where(p => !expected.Contains(p)).OrderBy(p => p, StringComparer.Ordinal).ToArray();

        internal EditorAssetChangeAudit(IEnumerable<string> allowed)
        {
            if (current != null) throw new InvalidOperationException("An asset change audit is already active.");
            expected = new HashSet<string>(allowed, StringComparer.Ordinal);
            current = this;
        }

        internal static void Record(string kind, IEnumerable<string> paths)
        {
            if (current == null) return;
            foreach (var path in paths)
            {
                if (current.events.Count >= 512 || current.recordedCharacters + path.Length + kind.Length + 64 > 65536)
                { current.Truncated = true; return; }
                current.recordedCharacters += path.Length + kind.Length + 64;
                current.changes.Add(path);
                current.events.Add(new JObject { ["kind"] = kind, ["path"] = path });
            }
        }

        internal JObject Describe() => new JObject
        {
            ["scope"] = "synchronous_unity_save_import_callbacks", ["events"] = new JArray(events),
            ["changed_assets"] = new JArray(changes.OrderBy(p => p, StringComparer.Ordinal)),
            ["unexpected_assets"] = new JArray(Unexpected), ["truncated"] = Truncated,
            ["deferred_callbacks_covered"] = false, ["raw_filesystem_writes_covered"] = false
        };

        internal void RequireExpected()
        {
            if (Truncated || Unexpected.Length > 0)
                throw new InvalidOperationException("Unexpected or truncated asset side effects; stop further writes and inspect the operation receipt.");
        }

        public void Dispose() { if (current == this) current = null; }
    }

    internal sealed class FakeUnityAssetSaveAudit : AssetModificationProcessor
    {
        private static string[] OnWillSaveAssets(string[] paths)
        {
            EditorAssetChangeAudit.Record("save_requested", paths);
            return paths;
        }
    }

    internal sealed class FakeUnityAssetImportAudit : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            EditorAssetChangeAudit.Record("imported", imported);
            EditorAssetChangeAudit.Record("deleted", deleted);
            EditorAssetChangeAudit.Record("moved_to", moved);
            EditorAssetChangeAudit.Record("moved_from", movedFrom);
        }
    }
}
#endif
