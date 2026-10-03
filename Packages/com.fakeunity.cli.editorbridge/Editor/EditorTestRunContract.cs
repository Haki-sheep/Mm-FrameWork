#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FakeUnityCLI.EditorBridge
{
    internal static class EditorTestRunContract
    {
        internal const int DefaultTimeoutSeconds = 1800;

        internal static bool IsValidRunId(string value) =>
            value != null && value.Length == 32 && Guid.TryParseExact(value, "N", out _);

        // Request identities are caller supplied; unlike run IDs they are not restricted to GUIDs.
        internal static bool IsValidRequestId(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value.Length > 80) return false;
            foreach (var character in value)
                if (!Char.IsLetterOrDigit(character) && character != '-' && character != '_') return false;
            return true;
        }

        // A live completion supersedes an earlier interruption time. Evidence recovery cannot
        // turn its own wall-clock time into the original run's completion time.
        internal static string ResolveFinishedAt(string recordedFinishedAt, string observedCompletedAt, bool recovering) =>
            recovering ? recordedFinishedAt : observedCompletedAt;

        // Recovery uses checkpoints written by this Bridge, never a bare legacy Passed field.
        internal static bool CanRecover(bool ownedFinalResult, bool nativeCompleted, bool cleanupComplete,
            bool ownershipLost, bool nativeError, bool cleanupError, string requestId, string unityRunId,
            string editorInstanceId, int total, int passed, int failed, int skipped, int inconclusive, int other)
        {
            return ownedFinalResult && nativeCompleted && cleanupComplete && !ownershipLost &&
                !nativeError && !cleanupError && IsValidRequestId(requestId) &&
                !String.IsNullOrWhiteSpace(unityRunId) && !String.IsNullOrWhiteSpace(editorInstanceId) &&
                total >= 0 && passed >= 0 && failed >= 0 && skipped >= 0 && inconclusive >= 0 && other >= 0 &&
                (long)passed + failed + skipped + inconclusive + other == total;
        }

        // Single-threaded lifecycle queue. Snapshot first; newly queued work belongs to the next drain.
        // Remove before invoking user/Unity code so a throwing disposal never repeats on every update.
        internal sealed class DeferredDisposals<T> where T : class
        {
            private readonly Dictionary<string, T> pending = new Dictionary<string, T>(StringComparer.Ordinal);
            internal int Count => pending.Count;
            internal bool Enqueue(string runId, T item)
            {
                if (!IsValidRunId(runId)) throw new ArgumentException("Invalid disposal run identity.", nameof(runId));
                if (item == null) throw new ArgumentNullException(nameof(item));
                if (pending.ContainsKey(runId)) return false;
                pending.Add(runId, item);
                return true;
            }
            internal void Drain(Action<string, T> dispose, Action<string, T, Exception> failed)
            {
                var batch = new List<KeyValuePair<string, T>>(pending);
                foreach (var entry in batch)
                {
                    pending.Remove(entry.Key);
                    try { dispose(entry.Key, entry.Value); }
                    catch (Exception exception) { failed(entry.Key, entry.Value, exception); }
                }
            }
        }

        [Serializable]
        internal sealed class Failure
        {
            public string phase;
            public string operation;
            public string path;
            public string exception_type;
            public int hresult;
            public string message;
        }

        internal static bool HasFailure(Failure failure) => failure != null && !String.IsNullOrEmpty(failure.operation);

        internal static Failure DescribeFailure(string phase, string operation, string path, Exception exception)
        {
            var cause = exception.GetBaseException();
            return new Failure { phase = phase, operation = operation, path = path,
                exception_type = cause.GetType().FullName, hresult = cause.HResult, message = cause.Message };
        }

        // One attempt per checkpoint/recovery request. No sleep, recursion, or unbounded Editor update retries.
        // Injectable replacement permits isolated IO failure tests without a running Unity Editor.
        internal static Failure WriteCheckpoint(string path, string json, Action<string, string> replace = null)
        {
            var operation = "create_directory";
            var target = Path.GetDirectoryName(path);
            var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(target);
                operation = "write_temporary"; target = temporary;
                File.WriteAllText(temporary, json, new UTF8Encoding(false));
                operation = "replace_checkpoint"; target = path;
                if (replace != null) replace(temporary, path);
                else if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                return null;
            }
            catch (Exception exception)
            {
                return DescribeFailure("persistence", operation, target, exception);
            }
            finally
            {
                // Never delete/overwrite the prior checkpoint as a fallback for a failed replace.
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
            }
        }

        internal static string ResolveStatus(string resultState, int total, int passed, int failed,
            int skipped, int inconclusive, int other)
        {
            if ((resultState ?? "").IndexOf("Cancel", StringComparison.OrdinalIgnoreCase) >= 0) return "cancelled";
            if ((resultState ?? "").StartsWith("Failed", StringComparison.OrdinalIgnoreCase) || failed > 0) return "failed";
            if (total == 0) return "no_tests";
            if (other > 0) return "error";
            if (inconclusive > 0) return "inconclusive";
            if (skipped > 0) return "completed_with_skips";
            return passed == total && resultState == "Passed" ? "passed" : "error";
        }
    }
}
