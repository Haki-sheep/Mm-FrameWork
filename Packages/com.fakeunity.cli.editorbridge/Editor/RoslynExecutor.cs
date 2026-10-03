#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using ReflectionAssembly = System.Reflection.Assembly;

namespace FakeUnityCLI.EditorBridge
{
    [Serializable]
    internal sealed class RoslynDiagnostic
    {
        public string code;
        public string severity;
        public string message;
    }

    internal sealed class RoslynExecutionResult
    {
        public bool Success;
        public string State;
        public string ResultJson;
        public string ResultText;
        public string ResultType;
        public string ErrorType;
        public string ErrorMessage;
        public string ErrorStackTrace;
        public RoslynDiagnostic[] Diagnostics = new RoslynDiagnostic[0];
        public string[] Artifacts = new string[0];
        public int CompileMilliseconds;
        public int ExecuteMilliseconds;
    }

    internal static class RoslynExecutor
    {
        private static readonly object Sync = new object();
        private static bool _ready;
        private static ReflectionAssembly _codeAnalysis;
        private static ReflectionAssembly _csharp;
        private static object[] _metadataReferences;
        private static Type _metadataReferenceType;
        private static ResolveEventHandler _resolver;

        internal static RoslynExecutionResult Execute(string source)
        {
            var result = new RoslynExecutionResult { State = "compile_error" };
            var phase = "compiler_setup";
            try
            {
                EnsureCompiler();
                var finalSource = LooksLikeFullSource(source) ? source : WrapSnippet(source);
                var compileStarted = DateTime.UtcNow;
                object compilation;
                object emitResult;
                byte[] assemblyBytes;
                phase = "compile";
                Compile(finalSource, out compilation, out emitResult, out assemblyBytes);
                result.CompileMilliseconds = ElapsedMilliseconds(compileStarted);
                phase = "diagnostics";
                result.Diagnostics = ReadDiagnostics(emitResult);

                var success = (bool)emitResult.GetType().GetProperty("Success").GetValue(emitResult, null);
                if (!success)
                    return result;

                phase = "execution";
                var executeStarted = DateTime.UtcNow;
                var assembly = ReflectionAssembly.Load(assemblyBytes);
                var entry = FindEntry(assembly);
                if (entry == null)
                    throw new MissingMethodException("Compiled code must expose one static parameterless Execute method.");

                object value;
                try
                {
                    value = entry.Invoke(null, null);
                }
                catch (TargetInvocationException exception)
                {
                    throw exception.InnerException ?? exception;
                }

                var task = value as Task;
                if (task != null)
                {
                    if (!task.IsCompleted)
                        throw new NotSupportedException("An incomplete Task cannot be awaited on the Unity Editor main thread. Start an EditorApplication.update state machine and return its handle instead.");
                    task.GetAwaiter().GetResult();
                    value = TaskResult(task);
                }

                result.ExecuteMilliseconds = ElapsedMilliseconds(executeStarted);
                result.ResultType = value == null ? null : value.GetType().FullName;
                result.ResultText = value == null ? null : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
                result.ResultJson = ResultJson.Serialize(value);
                result.Success = true;
                result.State = "success";
                return result;
            }
            catch (Exception exception)
            {
                result.State = phase == "execution" ? "runtime_error" : "bridge_error";
                result.ErrorType = exception.GetType().FullName;
                result.ErrorMessage = exception.Message;
                result.ErrorStackTrace = exception.ToString();
                return result;
            }
        }

        private static void EnsureCompiler()
        {
            if (_ready) return;
            lock (Sync)
            {
                if (_ready) return;
                var roslynDirectory = Path.Combine(EditorApplication.applicationContentsPath,
                    "MonoBleedingEdge", "lib", "mono", "4.5");
                var commonPath = Path.Combine(roslynDirectory, "Microsoft.CodeAnalysis.dll");
                var csharpPath = Path.Combine(roslynDirectory, "Microsoft.CodeAnalysis.CSharp.dll");
                if (!File.Exists(commonPath) || !File.Exists(csharpPath))
                    throw new FileNotFoundException("Unity's Mono-compatible Roslyn compiler assemblies were not found.", roslynDirectory);

                _resolver = delegate(object sender, ResolveEventArgs args)
                {
                    var name = new AssemblyName(args.Name).Name + ".dll";
                    var candidate = Path.Combine(roslynDirectory, name);
                    return File.Exists(candidate) ? ReflectionAssembly.LoadFrom(candidate) : null;
                };
                AppDomain.CurrentDomain.AssemblyResolve += _resolver;
                _codeAnalysis = LoadAssembly(commonPath, "Microsoft.CodeAnalysis");
                _csharp = LoadAssembly(csharpPath, "Microsoft.CodeAnalysis.CSharp");
                _metadataReferenceType = RequireType(_codeAnalysis, "Microsoft.CodeAnalysis.MetadataReference");
                _metadataReferences = BuildMetadataReferences();
                if (_metadataReferences.Length == 0)
                    throw new InvalidOperationException("No Unity compilation references were available to Roslyn.");
                _ready = true;
            }
        }

        private static ReflectionAssembly LoadAssembly(string path, string simpleName)
        {
            var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(item =>
                String.Equals(item.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase));
            return loaded ?? ReflectionAssembly.LoadFrom(path);
        }

        private static object[] BuildMetadataReferences()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                AddAssemblyLocation(paths, assembly);

            try
            {
                foreach (var assembly in CompilationPipeline.GetAssemblies(AssembliesType.Editor))
                {
                    AddPath(paths, assembly.outputPath);
                    if (assembly.compiledAssemblyReferences == null) continue;
                    foreach (var path in assembly.compiledAssemblyReferences) AddPath(paths, path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[FakeUnityCLI] CompilationPipeline reference discovery degraded: " + exception.Message);
            }

            var create = _metadataReferenceType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(item => item.Name == "CreateFromFile" && item.GetParameters().Length == 3);
            var propertiesType = create.GetParameters()[1].ParameterType;
            var defaultProperties = Activator.CreateInstance(propertiesType);
            var references = new List<object>();
            foreach (var path in paths.OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
            {
                try { references.Add(create.Invoke(null, new[] { path, defaultProperties, null })); }
                catch { }
            }
            return references.ToArray();
        }

        private static void Compile(string source, out object compilation, out object emitResult, out byte[] assemblyBytes)
        {
            var syntaxTreeType = RequireType(_codeAnalysis, "Microsoft.CodeAnalysis.SyntaxTree");
            var csharpSyntaxTree = RequireType(_csharp, "Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree");
            var parse = csharpSyntaxTree.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(item => item.Name == "ParseText" && item.GetParameters().Length == 5 && item.GetParameters()[0].ParameterType == typeof(string));
            var syntaxTree = parse.Invoke(null, new object[] { source, null, "fuc-online.cs", Encoding.UTF8, CancellationToken.None });

            var outputKindType = RequireType(_codeAnalysis, "Microsoft.CodeAnalysis.OutputKind");
            var optionsType = RequireType(_csharp, "Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions");
            var optionsConstructor = optionsType.GetConstructors().OrderBy(item => item.GetParameters().Length).First();
            var optionArguments = DefaultArguments(optionsConstructor.GetParameters());
            optionArguments[0] = Enum.Parse(outputKindType, "DynamicallyLinkedLibrary");
            SetNamedArgument(optionsConstructor.GetParameters(), optionArguments, "allowUnsafe", false);
            var options = optionsConstructor.Invoke(optionArguments);

            var syntaxTrees = Array.CreateInstance(syntaxTreeType, 1);
            syntaxTrees.SetValue(syntaxTree, 0);
            var references = Array.CreateInstance(_metadataReferenceType, _metadataReferences.Length);
            for (var index = 0; index < _metadataReferences.Length; index++) references.SetValue(_metadataReferences[index], index);

            var compilationType = RequireType(_csharp, "Microsoft.CodeAnalysis.CSharp.CSharpCompilation");
            var create = compilationType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(item => item.Name == "Create" && item.GetParameters().Length == 4);
            compilation = create.Invoke(null, new object[] { "__FakeUnityCLI_" + Guid.NewGuid().ToString("N"), syntaxTrees, references, options });

            using (var stream = new MemoryStream())
            {
                var emit = compilation.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(item => item.Name == "Emit").OrderBy(item => item.GetParameters().Length).First();
                var emitArguments = DefaultArguments(emit.GetParameters());
                emitArguments[0] = stream;
                emitResult = emit.Invoke(compilation, emitArguments);
                assemblyBytes = stream.ToArray();
            }
        }

        private static RoslynDiagnostic[] ReadDiagnostics(object emitResult)
        {
            var value = emitResult.GetType().GetProperty("Diagnostics").GetValue(emitResult, null) as IEnumerable;
            if (value == null) return new RoslynDiagnostic[0];
            var diagnostics = new List<RoslynDiagnostic>();
            foreach (var item in value)
            {
                try
                {
                    var type = item.GetType();
                    var severity = Convert.ToString(type.GetProperty("Severity").GetValue(item, null));
                    if (String.Equals(severity, "Hidden", StringComparison.OrdinalIgnoreCase)) continue;
                    var getMessage = type.GetMethods()
                        .Where(method => method.Name == "GetMessage")
                        .OrderBy(method => method.GetParameters().Length)
                        .FirstOrDefault(method => method.GetParameters().Length == 0 ||
                            method.GetParameters().Length == 1 &&
                            typeof(IFormatProvider).IsAssignableFrom(method.GetParameters()[0].ParameterType));
                    var message = getMessage == null
                        ? Convert.ToString(item)
                        : Convert.ToString(getMessage.Invoke(item,
                            getMessage.GetParameters().Length == 0 ? null : new object[] { null }));
                    diagnostics.Add(new RoslynDiagnostic
                    {
                        code = Convert.ToString(type.GetProperty("Id").GetValue(item, null)),
                        severity = severity == null ? null : severity.ToLowerInvariant(),
                        message = message
                    });
                }
                catch (Exception exception)
                {
                    // Diagnostic formatting is observability only. A Roslyn adapter mismatch must not
                    // prevent successfully emitted user code from running on the Editor main thread.
                    diagnostics.Add(new RoslynDiagnostic
                    {
                        code = "FUC_ROSLYN_DIAGNOSTIC_READ",
                        severity = "warning",
                        message = "Unable to format a Roslyn diagnostic: " + exception.GetBaseException().Message +
                                  "; raw=" + Convert.ToString(item)
                    });
                }
            }
            return diagnostics.ToArray();
        }

        private static MethodInfo FindEntry(ReflectionAssembly assembly)
        {
            return assembly.GetTypes().SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                .Where(method => method.Name == "Execute" && method.GetParameters().Length == 0)
                .OrderBy(method => method.DeclaringType.FullName, StringComparer.Ordinal).FirstOrDefault();
        }

        private static object TaskResult(Task task)
        {
            var property = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
            return property == null ? null : property.GetValue(task, null);
        }

        private static bool LooksLikeFullSource(string source)
        {
            return source != null && (source.Contains(" class ") || source.Contains(" class\n") ||
                source.Contains(" class\r") || source.TrimStart().StartsWith("class ") ||
                source.Contains(" struct "));
        }

        private static string WrapSnippet(string source)
        {
            return "using System;\nusing System.Linq;\nusing UnityEngine;\nusing UnityEditor;\n" +
                   "public static class __FakeUnityCLIOnlineScript\n{\n" +
                   "    public static object Execute()\n    {\n" + source + "\n        return null;\n    }\n}\n";
        }

        private static object[] DefaultArguments(ParameterInfo[] parameters)
        {
            var values = new object[parameters.Length];
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                if (parameter.ParameterType == typeof(CancellationToken)) values[index] = CancellationToken.None;
                else if (parameter.HasDefaultValue && parameter.DefaultValue != DBNull.Value) values[index] = parameter.DefaultValue;
                else values[index] = parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
            }
            return values;
        }

        private static void SetNamedArgument(ParameterInfo[] parameters, object[] values, string name, object value)
        {
            for (var index = 0; index < parameters.Length; index++)
                if (String.Equals(parameters[index].Name, name, StringComparison.Ordinal)) values[index] = value;
        }

        private static void AddAssemblyLocation(HashSet<string> paths, ReflectionAssembly assembly)
        {
            try { if (!assembly.IsDynamic) AddPath(paths, assembly.Location); }
            catch { }
        }

        private static void AddPath(HashSet<string> paths, string path)
        {
            if (String.IsNullOrWhiteSpace(path)) return;
            try
            {
                var full = Path.GetFullPath(path);
                if (File.Exists(full)) paths.Add(full);
            }
            catch { }
        }

        private static Type RequireType(ReflectionAssembly assembly, string name)
        {
            return assembly.GetType(name, true);
        }

        private static int ElapsedMilliseconds(DateTime started)
        {
            return Math.Max(0, (int)(DateTime.UtcNow - started).TotalMilliseconds);
        }
    }

}
#endif
