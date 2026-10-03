#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FakeUnityCLI.EditorBridge
{
    /// <summary>
    /// Discovers project-owned Editor providers declared by committed/local manifests. Providers
    /// are structural JSON adapters and never take a compile-time dependency on this assembly.
    /// </summary>
    internal static class OnlineProviderRegistry
    {
        internal const string ExecuteOperation = "provider-exec";
        internal const string ContractVersion = "1.0";
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        private static ProviderRecord[] _providers = new ProviderRecord[0];
        private static ProviderDiagnostic[] _diagnostics = new ProviderDiagnostic[0];
        private static string _catalogFingerprint = String.Empty;

        internal static bool Refresh()
        {
            var providers = new List<ProviderRecord>();
            var diagnostics = new List<ProviderDiagnostic>();
            var blockedProviderIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var root in Roots())
            {
                if (!Directory.Exists(root.path) || IsReparse(root.path)) continue;
                string[] manifests;
                try
                {
                    manifests = Directory.GetFiles(root.path, "manifest.json", SearchOption.AllDirectories)
                        .OrderBy(path => path, StringComparer.Ordinal).ToArray();
                }
                catch (Exception exception)
                {
                    diagnostics.Add(Diagnostic(null, "plugin_root_unreadable", root.path, exception.Message));
                    continue;
                }
                foreach (var manifestPath in manifests)
                {
                    LoadManifest(root, manifestPath, providers, diagnostics, blockedProviderIds);
                }
            }
            var nextProviders = providers.OrderBy(provider => provider.ProviderId, StringComparer.Ordinal).ToArray();
            var nextDiagnostics = diagnostics.ToArray();
            var fingerprint = Hash(Utf8.GetBytes(String.Join("\n", nextProviders.Select(provider =>
                provider.ProviderId + ":" + provider.ManifestSha256 + ":" + provider.DescriptorSha256).Concat(
                nextDiagnostics.Select(diagnostic => diagnostic.code + ":" + diagnostic.provider_id + ":" + diagnostic.path)))));
            var changed = !String.Equals(fingerprint, _catalogFingerprint, StringComparison.Ordinal);
            _providers = nextProviders;
            _diagnostics = nextDiagnostics;
            _catalogFingerprint = fingerprint;
            return changed;
        }

        internal static ProviderHandshake[] HandshakeProviders()
        {
            return _providers.Select(provider => new ProviderHandshake
            {
                provider_id = provider.ProviderId,
                contract_version = provider.ContractVersion,
                provider_version = provider.ProviderVersion,
                manifest_sha256 = provider.ManifestSha256,
                descriptor_sha256 = provider.DescriptorSha256,
                source = provider.Source,
                operations = provider.Operations
            }).ToArray();
        }

        internal static ProviderDiagnostic[] Diagnostics()
        {
            return _diagnostics.ToArray();
        }

        internal static bool IsReadOnlyOperation(string payloadJson)
        {
            try
            {
                var payload = JsonUtility.FromJson<ProviderExecutePayload>(payloadJson ?? "{}");
                if (payload == null) return false;
                var matches = _providers.Where(p => p.ProviderId == payload.provider_id &&
                    FixedHashEquals(p.ManifestSha256, payload.manifest_sha256) &&
                    FixedHashEquals(p.DescriptorSha256, payload.descriptor_sha256)).ToArray();
                return matches.Length == 1 && matches[0].Operations.Count(o => o.id == payload.operation) == 1 &&
                    matches[0].Operations.Any(o => o.id == payload.operation && !o.mutates_project);
            }
            catch { return false; }
        }

        internal static ProviderExecution Execute(string payloadJson, string requestId)
        {
            var payload = JsonUtility.FromJson<ProviderExecutePayload>(payloadJson ?? "{}");
            if (payload == null || String.IsNullOrWhiteSpace(payload.provider_id) ||
                String.IsNullOrWhiteSpace(payload.operation))
                throw new ArgumentException("provider-exec requires provider_id and operation.");
            var matches = _providers.Where(provider =>
                String.Equals(provider.ProviderId, payload.provider_id, StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Online provider is unavailable or ambiguous: " + payload.provider_id);
            var provider = matches[0];
            if (!FixedHashEquals(payload.manifest_sha256, provider.ManifestSha256))
                throw new InvalidDataException("Provider manifest hash changed after request creation.");
            if (!FixedHashEquals(payload.descriptor_sha256, provider.DescriptorSha256))
                throw new InvalidDataException("Provider descriptor hash changed after request creation.");
            var operations = provider.Operations.Where(operation =>
                String.Equals(operation.id, payload.operation, StringComparison.Ordinal)).ToArray();
            if (operations.Length != 1)
                throw new NotSupportedException("Provider operation is unavailable: " + payload.provider_id + "/" + payload.operation);
            if (operations[0].mutates_project && !payload.confirmed)
                throw new InvalidOperationException("Mutating provider operations require explicit confirmation.");
            if (operations[0].requires_play_mode && !EditorApplication.isPlaying)
                throw new InvalidOperationException("Provider operation requires the Editor to be in Play Mode.");
            var argumentsJson = String.IsNullOrWhiteSpace(payload.arguments_json) ? "{}" : payload.arguments_json.Trim();
            if (argumentsJson.Length > 1024 * 1024 || argumentsJson.IndexOf('\0') >= 0 ||
                argumentsJson[0] != '{' || argumentsJson[argumentsJson.Length - 1] != '}')
                throw new InvalidDataException("Provider arguments_json must be a JSON object no larger than 1 MiB.");

            var invocation = JsonUtility.ToJson(new ProviderInvocation
            {
                contract_version = ContractVersion,
                request_id = requestId,
                provider_id = provider.ProviderId,
                operation = operations[0].id,
                payload = new ProviderInvocationPayload { arguments_json = argumentsJson },
                editor = new ProviderInvocationEditor
                {
                    project_path = ProjectRoot(),
                    editor_pid = BridgeBootstrap.EditorPid,
                    editor_instance_id = BridgeBootstrap.EditorInstanceId,
                    editor_session_id = LogCaptureBridge.CurrentSessionId
                }
            }, false);
            string resultJson;
            try
            {
                resultJson = (string)provider.ExecuteMethod.Invoke(null, new object[] { invocation });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
            return ValidateResult(resultJson);
        }

        private static void LoadManifest(PluginRoot root, string manifestPath,
            List<ProviderRecord> providers, List<ProviderDiagnostic> diagnostics,
            HashSet<string> blockedProviderIds)
        {
            var packageRoot = Path.GetDirectoryName(manifestPath);
            if (!IsWithin(root.path, packageRoot) || PackageContainsReparse(packageRoot)) return;
            byte[] bytes;
            ProviderPluginManifest manifest;
            try
            {
                bytes = File.ReadAllBytes(manifestPath);
                manifest = JsonUtility.FromJson<ProviderPluginManifest>(Utf8.GetString(bytes));
            }
            catch (Exception exception)
            {
                diagnostics.Add(Diagnostic(null, "manifest_unreadable", manifestPath, exception.Message));
                return;
            }
            if (manifest == null || manifest.editorProvider == null) return;
            var reference = manifest.editorProvider;
            if (manifest.sdkVersion != "1.1" || !ValidName(manifest.pluginId) ||
                reference.contractVersion != ContractVersion || !ValidName(reference.providerId) ||
                String.IsNullOrWhiteSpace(reference.typeName) || reference.describeMethod != "Describe" ||
                reference.executeMethod != "Execute")
            {
                diagnostics.Add(Diagnostic(reference.providerId, "manifest_contract_invalid", manifestPath,
                    "Provider manifest does not satisfy SDK 1.1 structural contract 1.0."));
                return;
            }
            if (blockedProviderIds.Contains(reference.providerId))
            {
                diagnostics.Add(Diagnostic(reference.providerId, "provider_duplicate_blocked", manifestPath,
                    "This provider ID was already rejected because of a same-priority duplicate."));
                return;
            }
            if (providers.Any(provider => String.Equals(provider.ProviderId, reference.providerId, StringComparison.Ordinal)))
            {
                var existing = providers.First(provider => String.Equals(provider.ProviderId, reference.providerId, StringComparison.Ordinal));
                diagnostics.Add(Diagnostic(reference.providerId,
                    existing.RootPriority == root.priority ? "provider_duplicate" : "provider_shadowed",
                    manifestPath, "A higher-priority or same-priority provider already owns this provider ID."));
                if (existing.RootPriority == root.priority)
                {
                    providers.Remove(existing);
                    blockedProviderIds.Add(reference.providerId);
                }
                return;
            }

            var typeMatches = AppDomain.CurrentDomain.GetAssemblies().Select(assembly =>
            {
                try { return assembly.GetType(reference.typeName, false); }
                catch { return null; }
            }).Where(type => type != null).ToArray();
            if (typeMatches.Length != 1)
            {
                diagnostics.Add(Diagnostic(reference.providerId, "provider_type_unavailable", manifestPath,
                    "Declared provider type matched " + typeMatches.Length + " loaded Editor type(s)."));
                return;
            }
            var type = typeMatches[0];
            var describe = ExactMethod(type, reference.describeMethod, Type.EmptyTypes);
            var execute = ExactMethod(type, reference.executeMethod, new[] { typeof(string) });
            if (describe == null || execute == null)
            {
                diagnostics.Add(Diagnostic(reference.providerId, "provider_signature_invalid", manifestPath,
                    "Provider requires public static string Describe() and Execute(string)."));
                return;
            }
            string descriptorJson;
            try { descriptorJson = (string)describe.Invoke(null, null); }
            catch (Exception exception)
            {
                diagnostics.Add(Diagnostic(reference.providerId, "provider_describe_failed", manifestPath,
                    (exception is TargetInvocationException && exception.InnerException != null
                        ? exception.InnerException : exception).Message));
                return;
            }
            ProviderDescription description;
            try
            {
                var mutability = FakeUnity.ProviderContracts.ProviderDescriptionGuard.RequireExplicitMutability(descriptorJson);
                description = JsonUtility.FromJson<ProviderDescription>(descriptorJson);
                if (description == null || description.operations == null || description.operations.Length != mutability.Length)
                    throw new FormatException("Provider descriptor parsers disagree on the operations array.");
                for (var index = 0; index < mutability.Length; index++)
                {
                    if (description.operations[index] == null)
                        throw new FormatException("Provider operation cannot be null.");
                    // The BCL typed value, not JsonUtility's default, is authoritative.
                    description.operations[index].mutates_project = mutability[index];
                }
            }
            catch (Exception exception)
            {
                diagnostics.Add(Diagnostic(reference.providerId, "provider_description_invalid", manifestPath, exception.Message));
                return;
            }
            if (!ValidDescription(reference, description, out var reason))
            {
                diagnostics.Add(Diagnostic(reference.providerId, "provider_description_invalid", manifestPath, reason));
                return;
            }
            providers.Add(new ProviderRecord(reference.providerId, reference.contractVersion,
                description.provider_version ?? String.Empty, Hash(bytes), Hash(Utf8.GetBytes(descriptorJson)),
                root.name, root.priority, execute, description.operations));
        }

        private static bool ValidDescription(ProviderReference reference, ProviderDescription description, out string reason)
        {
            reason = null;
            if (description == null || description.contract_version != ContractVersion ||
                !String.Equals(description.provider_id, reference.providerId, StringComparison.Ordinal) ||
                String.IsNullOrWhiteSpace(description.provider_version) || description.operations == null)
            {
                reason = "Provider description identity or contract version does not match the manifest.";
                return false;
            }
            if (description.operations.Any(operation => operation == null || !ValidName(operation.id) ||
                !ValidName(operation.payload_schema_id) ||
                operation.default_timeout_seconds < 1 || operation.default_timeout_seconds > 300) ||
                description.operations.Select(operation => operation.id).Distinct(StringComparer.Ordinal).Count() !=
                description.operations.Length)
            {
                reason = "Provider operations must have unique stable IDs and timeout 1..300.";
                return false;
            }
            return true;
        }

        private static MethodInfo ExactMethod(Type type, string name, Type[] parameters)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(method =>
                method.Name == name && method.ReturnType == typeof(string) &&
                method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameters)).ToArray();
            return methods.Length == 1 ? methods[0] : null;
        }

        private static ProviderExecution ValidateResult(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value.Length > 1024 * 1024 || value.IndexOf('\0') >= 0 ||
                value.TrimStart()[0] != '{')
                throw new InvalidDataException("Provider result must be a JSON object no larger than 1 MiB.");
            ProviderResultProbe probe;
            try { probe = JsonUtility.FromJson<ProviderResultProbe>(value); }
            catch (Exception exception) { throw new InvalidDataException("Provider result is not valid JSON: " + exception.Message); }
            if (probe == null || String.IsNullOrWhiteSpace(probe.state) ||
                value.IndexOf("\"success\"", StringComparison.Ordinal) < 0)
                throw new InvalidDataException("Provider result requires state and success fields.");
            if (probe.state != "completed" && probe.state != "deferred" && probe.state != "rejected" &&
                probe.state != "failed")
                throw new InvalidDataException("Provider result state must be completed, deferred, rejected, or failed.");
            if (probe.success && (probe.state == "rejected" || probe.state == "failed"))
                throw new InvalidDataException("Provider result success is inconsistent with its state.");
            return new ProviderExecution(value, probe.success, probe.state, probe.error);
        }

        private static PluginRoot[] Roots()
        {
            var project = ProjectRoot();
            return new[]
            {
                new PluginRoot(CommittedPluginRoot(project), "committed-project", 0),
                new PluginRoot(Path.Combine(project, ".fuc", "plugins"), "local-state", 1)
            };
        }

        private static string CommittedPluginRoot(string project)
        {
            var fallback = Path.Combine(project, "Tools", "FakeUnityCLI", "plugins");
            var locationPath = Path.Combine(project, "Library", "FakeUnityCLI", "deployment-location.json");
            try
            {
                if (!File.Exists(locationPath) || IsReparse(locationPath)) return fallback;
                var location = JsonUtility.FromJson<DeploymentLocation>(File.ReadAllText(locationPath, Utf8));
                if (location == null || location.manifest_version != "1.0" ||
                    !SafeRelativePath(location.plugins_relative_path)) return fallback;
                var candidate = Path.GetFullPath(Path.Combine(project,
                    location.plugins_relative_path.Replace('/', Path.DirectorySeparatorChar)));
                return IsWithin(project, candidate) ? candidate : fallback;
            }
            catch { return fallback; }
        }

        private static bool SafeRelativePath(string value)
        {
            if (String.IsNullOrWhiteSpace(value) || value.StartsWith("/", StringComparison.Ordinal) || value.Contains(":"))
                return false;
            return value.Replace('\\', '/').Split('/').All(segment =>
                segment.Length > 0 && segment != "." && segment != "..");
        }

        private static string ProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static bool PackageContainsReparse(string root)
        {
            try
            {
                if (IsReparse(root)) return true;
                return Directory.GetFileSystemEntries(root, "*", SearchOption.AllDirectories).Any(IsReparse);
            }
            catch { return true; }
        }

        private static bool IsReparse(string path)
        {
            return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
        }

        private static bool IsWithin(string root, string path)
        {
            var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedPath = Path.GetFullPath(path);
            return String.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase) ||
                normalizedPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ValidName(string value)
        {
            return !String.IsNullOrWhiteSpace(value) && value.Length <= 120 && value.All(character =>
                Char.IsLetterOrDigit(character) || character == '.' || character == '-' || character == '_');
        }

        private static ProviderDiagnostic Diagnostic(string provider, string code, string path, string message)
        {
            return new ProviderDiagnostic { provider_id = provider, code = code, path = path, message = message };
        }

        private static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", String.Empty).ToLowerInvariant();
        }

        private static bool FixedHashEquals(string left, string right)
        {
            if (left == null || right == null || left.Length != 64 || right.Length != 64) return false;
            var a = Utf8.GetBytes(left.ToLowerInvariant());
            var b = Utf8.GetBytes(right.ToLowerInvariant());
            var difference = 0;
            for (var index = 0; index < a.Length; index++) difference |= a[index] ^ b[index];
            return difference == 0;
        }

        private sealed class ProviderRecord
        {
            public ProviderRecord(string providerId, string contractVersion, string providerVersion,
                string manifestSha256, string descriptorSha256, string source, int rootPriority,
                MethodInfo executeMethod, ProviderOperation[] operations)
            {
                ProviderId = providerId; ContractVersion = contractVersion; ProviderVersion = providerVersion;
                ManifestSha256 = manifestSha256; DescriptorSha256 = descriptorSha256; Source = source;
                RootPriority = rootPriority; ExecuteMethod = executeMethod; Operations = operations;
            }
            public readonly string ProviderId; public readonly string ContractVersion; public readonly string ProviderVersion;
            public readonly string ManifestSha256; public readonly string DescriptorSha256; public readonly string Source;
            public readonly int RootPriority; public readonly MethodInfo ExecuteMethod; public readonly ProviderOperation[] Operations;
        }

        private sealed class PluginRoot
        {
            public PluginRoot(string path, string name, int priority) { this.path = path; this.name = name; this.priority = priority; }
            public readonly string path; public readonly string name; public readonly int priority;
        }

        [Serializable] private sealed class ProviderPluginManifest { public string pluginId; public string sdkVersion; public ProviderReference editorProvider; }
        [Serializable] private sealed class DeploymentLocation
        {
            public string manifest_version; public string plugins_relative_path;
        }
        [Serializable] private sealed class ProviderReference
        {
            public string contractVersion; public string providerId; public string typeName;
            public string describeMethod = "Describe"; public string executeMethod = "Execute";
        }
        [Serializable] private sealed class ProviderDescription
        {
            public string provider_id; public string contract_version; public string provider_version;
            public ProviderOperation[] operations = new ProviderOperation[0];
        }
        [Serializable] internal sealed class ProviderOperation
        {
            public string id; public bool mutates_project; public bool idempotent; public bool requires_play_mode;
            public int default_timeout_seconds; public string payload_schema_id;
        }
        [Serializable] private sealed class ProviderExecutePayload
        {
            public string provider_id; public string operation; public string arguments_json;
            public string manifest_sha256; public string descriptor_sha256; public bool confirmed;
        }
        [Serializable] private sealed class ProviderInvocation
        {
            public string contract_version; public string request_id; public string provider_id; public string operation;
            public ProviderInvocationPayload payload; public ProviderInvocationEditor editor;
        }
        [Serializable] private sealed class ProviderInvocationPayload { public string arguments_json; }
        [Serializable] private sealed class ProviderInvocationEditor
        {
            public string project_path; public int editor_pid; public string editor_instance_id; public string editor_session_id;
        }
        internal sealed class ProviderExecution
        {
            public ProviderExecution(string resultJson, bool success, string state, string error)
            { ResultJson = resultJson; Success = success; State = state; Error = error; }
            public readonly string ResultJson; public readonly bool Success; public readonly string State; public readonly string Error;
        }
        [Serializable] private sealed class ProviderResultProbe { public bool success; public string state; public string error; }
        [Serializable] internal sealed class ProviderHandshake
        {
            public string provider_id; public string contract_version; public string provider_version;
            public string manifest_sha256; public string descriptor_sha256; public string source; public ProviderOperation[] operations;
        }
        [Serializable] internal sealed class ProviderDiagnostic
        {
            public string provider_id; public string code; public string path; public string message;
        }
    }
}
#endif

// BCL-only contract validation is shared verbatim with host selfchecks. JsonUtility
// cannot distinguish an omitted bool from an explicit false; absence must never
// grant the read-only exception while an investigation is protected.
namespace FakeUnity.ProviderContracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;

    public static class ProviderDescriptionGuard
    {
        public static bool[] RequireExplicitMutability(string json)
        {
            if (String.IsNullOrWhiteSpace(json) || json.Length > 1024 * 1024)
                throw new FormatException("Provider descriptor must be a JSON object no larger than 1 MiB.");
            var trimmed = json.Trim();
            if (trimmed[0] != '{' || trimmed[trimmed.Length - 1] != '}')
                throw new FormatException("Provider descriptor requires a complete JSON object.");
            var quotas = new XmlDictionaryReaderQuotas
            {
                MaxDepth = 64,
                MaxArrayLength = 1024 * 1024,
                MaxStringContentLength = 1024 * 1024
            };
            using (var reader = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(json), quotas))
            {
                var root = XElement.Load(reader);
                var arrays = root.Elements().Where(element => IsProperty(element, "operations")).ToArray();
                if (root.Attribute("type")?.Value != "object" || arrays.Length != 1 ||
                    arrays[0].Attribute("type")?.Value != "array")
                    throw new FormatException("Provider descriptor requires one operations array.");
                var flags = new List<bool>();
                foreach (var operation in arrays[0].Elements())
                {
                    var values = operation.Elements().Where(element => IsProperty(element, "mutates_project")).ToArray();
                    if (operation.Attribute("type")?.Value != "object" || values.Length != 1 ||
                        values[0].Attribute("type")?.Value != "boolean" ||
                        (values[0].Value != "true" && values[0].Value != "false"))
                        throw new FormatException("Each provider operation requires exactly one explicit boolean mutates_project; missing/null/string values are not read-only.");
                    flags.Add(values[0].Value == "true");
                }
                return flags.ToArray();
            }
        }

        private static bool IsProperty(XElement element, string name)
        {
            // The BCL JSON/XML mapping uses the item namespace for escaped
            // property names; compare its decoded name, never raw JSON text.
            return element.Name == name || (element.Name.LocalName == "item" &&
                element.Name.NamespaceName == "item" && element.Attribute("item")?.Value == name);
        }
    }
}
