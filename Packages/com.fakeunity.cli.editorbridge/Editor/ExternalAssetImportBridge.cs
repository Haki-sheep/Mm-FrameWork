#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FakeUnity.ExternalAssetImport;

namespace FakeUnityCLI.EditorBridge
{
    internal static class ExternalAssetImportBridge
    {
        internal const string ImportOperation = "external-asset-import";
        internal const string UndoOperation = "external-asset-import-undo";
        internal const string StatusOperation = "external-asset-import-status";
        private static readonly System.Text.UTF8Encoding Utf8 = new System.Text.UTF8Encoding(false);

        internal static Dictionary<string, object> Import(string[] requestedPaths, string payloadJson, string requestId)
        {
            if (requestedPaths == null || requestedPaths.Length != 1) throw new ArgumentException("external-asset-import requires one destination path.");
            var payload = JsonUtility.FromJson<ImportPayload>(payloadJson ?? "{}");
            if (payload == null || String.IsNullOrWhiteSpace(payload.source_path) ||
                !ExternalAssetImportContract.IsSha256(payload.expected_source_sha256))
                throw new ArgumentException("external-asset-import requires source_path and expected_source_sha256.");
            var root = ProjectRoot(); var receipt = ReadReceipt(requestId);
            if (receipt != null)
            {
                if (Path.GetFullPath(payload.source_path) != receipt.source_path ||
                    payload.expected_source_sha256 != receipt.source_sha256 || requestedPaths[0].Replace('\\', '/') != receipt.destination)
                    throw new InvalidDataException("Import request no longer matches its durable receipt.");
                if (receipt.status == "imported") { ValidateImported(root, receipt); return Result(receipt, true); }
                if (receipt.status.StartsWith("failed_", StringComparison.Ordinal))
                    throw new InvalidOperationException("Previous import attempt is terminal: " + receipt.status + "; " + receipt.error);
            }
            var destination = Path.Combine(root, requestedPaths[0].Replace('/', Path.DirectorySeparatorChar));
            var plan = ExternalAssetImportContract.Plan(root, payload.source_path, destination, payload.expected_source_sha256);
            if (plan.AssetPath != requestedPaths[0].Replace('\\', '/')) throw new InvalidDataException("Destination path normalization changed after submission.");
            receipt = receipt ?? new Receipt { schema_version = "1.0", operation_id = requestId, status = "copying",
                source_path = plan.SourcePath, source_sha256 = plan.SourceSha256, destination = plan.AssetPath,
                size_bytes = plan.SizeBytes, created_at = DateTime.UtcNow.ToString("o") };
            WriteReceipt(receipt);
            try
            {
                if (receipt.status == "copying")
                {
                    long copiedSize;
                    receipt.target_sha256 = ExternalAssetImportContract.CopyNew(plan.SourcePath, plan.DestinationPath,
                        plan.SourceSha256, out copiedSize);
                    receipt.size_bytes = copiedSize; receipt.status = "copied"; WriteReceipt(receipt);
                }
                var beforeAssets = new HashSet<string>(AssetDatabase.GetAllAssetPaths(), StringComparer.Ordinal);
                AssetDatabase.ImportAsset(plan.AssetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                receipt.additional_created_assets = new List<string>(AssetDatabase.GetAllAssetPaths()).FindAll(path =>
                    path.StartsWith("Assets/", StringComparison.Ordinal) && path != plan.AssetPath && !beforeAssets.Contains(path)).ToArray();
                if (receipt.additional_created_assets.Length > 0)
                    throw new InvalidOperationException("Asset postprocessors created additional assets: " + String.Join(", ", receipt.additional_created_assets));
                var meta = plan.DestinationPath + ".meta";
                if (!File.Exists(plan.DestinationPath) || !File.Exists(meta)) throw new IOException("Unity import did not produce both asset and meta.");
                var guid = AssetDatabase.AssetPathToGUID(plan.AssetPath); var importer = AssetImporter.GetAtPath(plan.AssetPath);
                if (String.IsNullOrWhiteSpace(guid) || importer == null) throw new IOException("Unity did not expose GUID/importer after synchronous import.");
                long ignored;
                if (ExternalAssetImportContract.HashFile(plan.DestinationPath, out ignored) != receipt.target_sha256)
                    throw new IOException("Imported target bytes differ from the copied source.");
                receipt.meta_sha256 = ExternalAssetImportContract.HashFile(meta, out ignored);
                receipt.guid = guid; receipt.importer_type = importer.GetType().FullName;
                receipt.status = "imported"; receipt.completed_at = DateTime.UtcNow.ToString("o"); WriteReceipt(receipt);
                return Result(receipt, false);
            }
            catch (Exception exception)
            {
                receipt.error = exception.GetType().Name + ": " + exception.Message;
                var cleaned = CleanupCreated(plan, receipt); receipt.status = cleaned ? "failed_cleanup_complete" : "failed_residual";
                receipt.completed_at = DateTime.UtcNow.ToString("o");
                try { WriteReceipt(receipt); } catch (Exception write) { receipt.error += "; receipt_write=" + write.Message; }
                throw new InvalidOperationException("External import " + requestId + " failed; cleanup=" +
                    (cleaned ? "complete" : "residual_requires_inspection") + "; " + receipt.error, exception);
            }
        }

        internal static Dictionary<string, object> Undo(string payloadJson)
        {
            var payload = JsonUtility.FromJson<UndoPayload>(payloadJson ?? "{}");
            if (payload == null || !payload.confirmed || !SafeId(payload.import_operation_id)) throw new ArgumentException("A confirmed valid import_operation_id is required.");
            var receipt = ReadReceipt(payload.import_operation_id);
            if (receipt == null) throw new FileNotFoundException("External import receipt not found.");
            var root = ProjectRoot(); var target = Path.Combine(root, receipt.destination.Replace('/', Path.DirectorySeparatorChar));
            var meta = target + ".meta";
            if (receipt.status == "undone") return Result(receipt, true);
            if (receipt.status == "undoing" && !File.Exists(target) && !File.Exists(meta))
            { receipt.status = "undone"; receipt.undone_at = DateTime.UtcNow.ToString("o"); WriteReceipt(receipt); return Result(receipt, true); }
            if (receipt.status != "imported" && receipt.status != "undoing")
                throw new InvalidOperationException("Only a completed imported receipt can be undone: " + receipt.status);
            ValidateImported(root, receipt); receipt.status = "undoing"; WriteReceipt(receipt);
            if (!AssetDatabase.DeleteAsset(receipt.destination) || File.Exists(target) || File.Exists(meta))
            { receipt.status = "imported"; WriteReceipt(receipt); throw new IOException("AssetDatabase.DeleteAsset did not remove the bound asset and meta."); }
            receipt.status = "undone"; receipt.undone_at = DateTime.UtcNow.ToString("o"); WriteReceipt(receipt);
            return Result(receipt, false);
        }

        internal static Dictionary<string, object> Status(string payloadJson)
        {
            var payload = JsonUtility.FromJson<UndoPayload>(payloadJson ?? "{}");
            if (payload == null || !SafeId(payload.import_operation_id)) throw new ArgumentException("A valid import_operation_id is required.");
            var receipt = ReadReceipt(payload.import_operation_id);
            if (receipt == null) throw new FileNotFoundException("External import receipt not found.");
            var result = Result(receipt, true); object valid = null;
            if (receipt.status == "imported")
            {
                try { ValidateImported(ProjectRoot(), receipt); valid = true; }
                catch { valid = false; }
            }
            result["currentBindingValid"] = valid;
            result["error"] = receipt.error;
            return result;
        }

        private static void ValidateImported(string root, Receipt receipt)
        {
            var target = Path.Combine(root, receipt.destination.Replace('/', Path.DirectorySeparatorChar)); var meta = target + ".meta"; long ignored;
            if (!File.Exists(target) || !File.Exists(meta) ||
                ExternalAssetImportContract.HashFile(target, out ignored) != receipt.target_sha256 ||
                ExternalAssetImportContract.HashFile(meta, out ignored) != receipt.meta_sha256 ||
                AssetDatabase.AssetPathToGUID(receipt.destination) != receipt.guid)
                throw new InvalidDataException("Imported asset/meta/GUID changed after the receipt; undo refused.");
        }

        private static bool CleanupCreated(ExternalAssetImportPlan plan, Receipt receipt)
        {
            try
            {
                long ignored;
                if ((File.Exists(plan.DestinationPath) || File.Exists(plan.DestinationPath + ".meta")) &&
                    String.IsNullOrEmpty(receipt.target_sha256)) return false;
                if (File.Exists(plan.DestinationPath) &&
                    ExternalAssetImportContract.HashFile(plan.DestinationPath, out ignored) != receipt.target_sha256) return false;
                if (File.Exists(plan.DestinationPath) || File.Exists(plan.DestinationPath + ".meta")) AssetDatabase.DeleteAsset(plan.AssetPath);
                return !File.Exists(plan.DestinationPath) && !File.Exists(plan.DestinationPath + ".meta") &&
                    (receipt.additional_created_assets == null || receipt.additional_created_assets.Length == 0);
            }
            catch { return false; }
        }

        private static Dictionary<string, object> Result(Receipt r, bool replayed) => new Dictionary<string, object>
        {
            { "operation", r.status == "undone" ? UndoOperation : ImportOperation }, { "operationId", r.operation_id },
            { "status", r.status }, { "replayed", replayed }, { "sourceSha256", r.source_sha256 },
            { "targetSha256", r.target_sha256 }, { "sizeBytes", r.size_bytes }, { "assetPath", r.destination },
            { "guid", r.guid }, { "importerType", r.importer_type }, { "metaSha256", r.meta_sha256 },
            { "additionalCreatedAssets", r.additional_created_assets ?? new string[0] },
            { "existingAssetPostprocessorEffects", "not_observed" }
        };

        private static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static string ReceiptPath(string id)
        {
            EnsureReceiptDirectory(false);
            return Path.Combine(ProjectRoot(), ".fuc", "imports", id + ".json");
        }
        private static bool SafeId(string id) => !String.IsNullOrWhiteSpace(id) && id.Length <= 80 &&
            id.All(character => Char.IsLetterOrDigit(character) || character == '-' || character == '_');
        private static Receipt ReadReceipt(string id)
        {
            if (!SafeId(id)) throw new InvalidDataException("Unsafe external import operation ID.");
            var path = ReceiptPath(id); if (!File.Exists(path)) return null;
            var info = new FileInfo(path); if (info.Length > 64 * 1024 || (info.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("External import receipt is oversized or linked.");
            var value = JsonUtility.FromJson<Receipt>(File.ReadAllText(path, Utf8));
            if (value == null || value.schema_version != "1.0" || value.operation_id != id ||
                !SafeAssetPath(value.destination) || !ExternalAssetImportContract.IsSha256(value.source_sha256))
                throw new InvalidDataException("External import receipt is invalid.");
            return value;
        }
        private static bool SafeAssetPath(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || !value.StartsWith("Assets/", StringComparison.Ordinal) || value.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) return false;
            var root = ProjectRoot(); var path = Path.GetFullPath(Path.Combine(root, value.Replace('/', Path.DirectorySeparatorChar)));
            return path.StartsWith(Path.Combine(root, "Assets") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        private static void WriteReceipt(Receipt value)
        {
            EnsureReceiptDirectory(true); var path = ReceiptPath(value.operation_id);
            var pending = path + ".pending-" + Guid.NewGuid().ToString("N");
            try { File.WriteAllText(pending, JsonUtility.ToJson(value, true), Utf8); if (File.Exists(path)) File.Replace(pending, path, null); else File.Move(pending, path); }
            finally { if (File.Exists(pending)) File.Delete(pending); }
        }
        private static void EnsureReceiptDirectory(bool create)
        {
            var fuc = Path.Combine(ProjectRoot(), ".fuc"); var imports = Path.Combine(fuc, "imports");
            foreach (var path in new[] { fuc, imports })
            {
                if (File.Exists(path) && !Directory.Exists(path)) throw new InvalidDataException("Receipt directory path is a file: " + path);
                if (Directory.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Receipt directory cannot be a reparse point: " + path);
            }
            if (create) { Directory.CreateDirectory(imports); if ((File.GetAttributes(imports) & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("Receipt directory cannot be linked."); }
        }

        [Serializable] private sealed class ImportPayload { public string source_path; public string expected_source_sha256; public string execution_mode; }
        [Serializable] private sealed class UndoPayload { public string import_operation_id; public string execution_mode; public bool confirmed; }
        [Serializable] private sealed class Receipt
        {
            public string schema_version; public string operation_id; public string status; public string source_path;
            public string source_sha256; public string target_sha256; public string destination; public long size_bytes;
            public string guid; public string importer_type; public string meta_sha256; public string created_at;
            public string completed_at; public string undone_at; public string error; public string[] additional_created_assets = new string[0];
        }
    }
}
#endif
