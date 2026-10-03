using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace MieMieFrameWork.Editor.AsmdefTool
{
    /// <summary>
    /// 拖文件夹生成 asmdef 并可自动分析引用
    /// </summary>
    public sealed class AsmdefToolWindow : EditorWindow, MieMieFrameWork.Editor.ToolsCenter.IMieMieToolsEmbeddedWindow
    {
        /// <summary> 目标文件夹 </summary>
        private DefaultAsset targetFolder;

        /// <summary> 程序集名 </summary>
        private string assemblyName = string.Empty;

        /// <summary> 根命名空间 </summary>
        private string rootNamespace = string.Empty;

        /// <summary> 是否拆 Editor 子目录 </summary>
        private bool carveEditorFolders = true;

        /// <summary> 是否 Editor 专用程序集 </summary>
        private bool editorOnly;

        /// <summary> 是否强制覆盖已有 asmdef </summary>
        private bool overwriteExisting;

        /// <summary> 是否自动分析引用 </summary>
        private bool autoAnalyzeReferences = true;

        /// <summary> 分析结果里的程序集引用 </summary>
        private readonly List<string> analyzedReferenceList = new();

        /// <summary> 分析结果里的预编译 dll </summary>
        private readonly List<string> analyzedPrecompiledList = new();

        /// <summary> 日志 </summary>
        private string logText = string.Empty;

        /// <summary> 滚动 </summary>
        private Vector2 scrollPos;

        public static void Open()
        {
            var window = GetWindow<AsmdefToolWindow>("AsmdefTool");
            window.minSize = new Vector2(520f, 420f);
        }

        public void DrawEmbeddedGUI()
        {
            OnGUI();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Asmdef 生成器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "拖入文件夹生成 Runtime asmdef 可选拆 Editor 子目录 并可扫描 using 自动填引用",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            targetFolder = (DefaultAsset)EditorGUILayout.ObjectField(
                "目标文件夹", targetFolder, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck())
                OnFolderChanged();

            using (new EditorGUI.DisabledScope(targetFolder == null))
            {
                assemblyName = EditorGUILayout.TextField("程序集名", assemblyName);
                rootNamespace = EditorGUILayout.TextField("根命名空间", rootNamespace);
                carveEditorFolders = EditorGUILayout.Toggle("拆分 Editor 子目录", carveEditorFolders);
                editorOnly = EditorGUILayout.Toggle("仅 Editor 程序集", editorOnly);
                overwriteExisting = EditorGUILayout.Toggle("覆盖已有 asmdef", overwriteExisting);
                autoAnalyzeReferences = EditorGUILayout.Toggle("生成前自动分析引用", autoAnalyzeReferences);

                EditorGUILayout.Space(8);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("分析引用", GUILayout.Height(28)))
                        AnalyzeReferences();

                    if (GUILayout.Button("生成 asmdef", GUILayout.Height(28)))
                        GenerateAsmdefs();
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("分析结果", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MinHeight(180f));
            DrawStringList("程序集引用", analyzedReferenceList);
            DrawStringList("预编译 DLL", analyzedPrecompiledList);
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("日志", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(logText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 文件夹变更时刷新默认名
        /// </summary>
        private void OnFolderChanged()
        {
            analyzedReferenceList.Clear();
            analyzedPrecompiledList.Clear();
            logText = string.Empty;

            if (targetFolder == null)
            {
                assemblyName = string.Empty;
                return;
            }

            string folderPath = AssetDatabase.GetAssetPath(targetFolder);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                targetFolder = null;
                AppendLog("请选择 Project 中的文件夹");
                return;
            }

            if (string.IsNullOrWhiteSpace(assemblyName))
                assemblyName = Path.GetFileName(folderPath).Replace(" ", string.Empty);
        }

        /// <summary>
        /// 绘制可编辑字符串列表
        /// </summary>
        private static void DrawStringList(string title, List<string> list)
        {
            EditorGUILayout.LabelField($"{title} ({list.Count})");
            for (int i = 0; i < list.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    list[i] = EditorGUILayout.TextField(list[i]);
                    if (GUILayout.Button("X", GUILayout.Width(24)))
                    {
                        list.RemoveAt(i);
                        break;
                    }
                }
            }

            if (GUILayout.Button($"+ 添加{title}", GUILayout.Width(120)))
                list.Add(string.Empty);
        }

        /// <summary>
        /// 扫描 using 与类型归属 推断引用
        /// </summary>
        private void AnalyzeReferences()
        {
            analyzedReferenceList.Clear();
            analyzedPrecompiledList.Clear();

            if (targetFolder == null)
            {
                AppendLog("未选择文件夹");
                return;
            }

            string folderPath = AssetDatabase.GetAssetPath(targetFolder);
            string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { folderPath });
            var usingHash = new HashSet<string>();
            for (int i = 0; i < scriptGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
                if (!assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Editor 子目录脚本留给 Editor asmdef 分析
                if (!editorOnly && IsUnderEditorFolder(assetPath, folderPath))
                    continue;

                CollectUsings(assetPath, usingHash);
            }

            var referenceHash = new HashSet<string>(StringComparer.Ordinal);
            var precompiledHash = new HashSet<string>(StringComparer.Ordinal);
            MapUsingsToReferences(usingHash, referenceHash, precompiledHash);

            // 自身程序集不要写进引用
            referenceHash.Remove(assemblyName);

            analyzedReferenceList.AddRange(referenceHash.OrderBy(x => x));
            analyzedPrecompiledList.AddRange(precompiledHash.OrderBy(x => x));
            AppendLog($"分析完成 using={usingHash.Count} refs={analyzedReferenceList.Count} dll={analyzedPrecompiledList.Count}");
        }

        /// <summary>
        /// 生成 Runtime 与可选 Editor asmdef
        /// </summary>
        private void GenerateAsmdefs()
        {
            if (targetFolder == null)
            {
                AppendLog("未选择文件夹");
                return;
            }

            string folderPath = AssetDatabase.GetAssetPath(targetFolder);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AppendLog("目标不是有效文件夹");
                return;
            }

            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                AppendLog("程序集名不能为空");
                return;
            }

            if (autoAnalyzeReferences)
                AnalyzeReferences();

            string runtimeAsmdefPath = Path.Combine(folderPath, assemblyName + ".asmdef");
            if (!WriteAsmdefFile(
                    runtimeAsmdefPath,
                    assemblyName,
                    rootNamespace,
                    editorOnly,
                    analyzedReferenceList,
                    analyzedPrecompiledList))
            {
                return;
            }

            int editorCount = 0;
            if (!editorOnly && carveEditorFolders)
                editorCount = GenerateEditorAsmdefs(folderPath, assemblyName);

            AssetDatabase.Refresh();
            AppendLog($"已生成 {runtimeAsmdefPath} Editor子程序集={editorCount}");
        }

        /// <summary>
        /// 为各 Editor 子目录生成 asmdef
        /// </summary>
        private int GenerateEditorAsmdefs(string rootFolderPath, string runtimeAssemblyName)
        {
            string[] editorFolderList = FindEditorFolders(rootFolderPath);
            int created = 0;
            for (int i = 0; i < editorFolderList.Length; i++)
            {
                string editorFolder = editorFolderList[i];
                string editorAsmName = BuildEditorAssemblyName(runtimeAssemblyName, rootFolderPath, editorFolder);
                string editorAsmdefPath = Path.Combine(editorFolder, editorAsmName + ".asmdef");

                var editorRefList = new List<string> { runtimeAssemblyName };
                var editorDllList = new List<string>(analyzedPrecompiledList);
                EnsureDll(editorDllList, "Sirenix.OdinInspector.Editor.dll");
                EnsureDll(editorDllList, "Sirenix.Utilities.Editor.dll");

                if (WriteAsmdefFile(
                        editorAsmdefPath,
                        editorAsmName,
                        rootNamespace,
                        true,
                        editorRefList,
                        editorDllList))
                {
                    created++;
                }
            }

            return created;
        }

        /// <summary>
        /// 写入 asmdef json
        /// </summary>
        private bool WriteAsmdefFile(
            string assetPath,
            string name,
            string ns,
            bool isEditor,
            List<string> referenceList,
            List<string> precompiledList)
        {
            if (File.Exists(assetPath) && !overwriteExisting)
            {
                AppendLog($"已存在未覆盖 {assetPath}");
                return false;
            }

            var cleanRefList = referenceList
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x)
                .ToList();
            var cleanDllList = precompiledList
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x)
                .ToList();

            var sb = new StringBuilder(512);
            sb.AppendLine("{");
            sb.AppendLine($"    \"name\": \"{EscapeJson(name)}\",");
            sb.AppendLine($"    \"rootNamespace\": \"{EscapeJson(ns ?? string.Empty)}\",");
            sb.AppendLine("    \"references\": [");
            for (int i = 0; i < cleanRefList.Count; i++)
            {
                string comma = i < cleanRefList.Count - 1 ? "," : string.Empty;
                sb.AppendLine($"        \"{EscapeJson(cleanRefList[i])}\"{comma}");
            }

            sb.AppendLine("    ],");
            sb.AppendLine("    \"includePlatforms\": [");
            if (isEditor)
                sb.AppendLine("        \"Editor\"");
            sb.AppendLine("    ],");
            sb.AppendLine("    \"excludePlatforms\": [],");
            sb.AppendLine("    \"allowUnsafeCode\": false,");
            sb.AppendLine("    \"overrideReferences\": true,");
            sb.AppendLine("    \"precompiledReferences\": [");
            for (int i = 0; i < cleanDllList.Count; i++)
            {
                string comma = i < cleanDllList.Count - 1 ? "," : string.Empty;
                sb.AppendLine($"        \"{EscapeJson(cleanDllList[i])}\"{comma}");
            }

            sb.AppendLine("    ],");
            sb.AppendLine("    \"autoReferenced\": true,");
            sb.AppendLine("    \"defineConstraints\": [],");
            sb.AppendLine("    \"versionDefines\": [],");
            sb.AppendLine("    \"noEngineReferences\": false");
            sb.AppendLine("}");

            string absolutePath = ToAbsolutePath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? Application.dataPath);
            File.WriteAllText(absolutePath, sb.ToString(), Encoding.UTF8);
            return true;
        }

        /// <summary>
        /// 收集脚本 using
        /// </summary>
        private static void CollectUsings(string assetPath, HashSet<string> usingHash)
        {
            string absolutePath = ToAbsolutePath(assetPath);
            if (!File.Exists(absolutePath))
                return;

            string text = File.ReadAllText(absolutePath);
            MatchCollection matchList = Regex.Matches(
                text,
                @"^\s*using\s+([A-Za-z_][\w.]*)\s*;",
                RegexOptions.Multiline);
            for (int i = 0; i < matchList.Count; i++)
            {
                string ns = matchList[i].Groups[1].Value;
                if (ns == "System"
                    || ns.StartsWith("System.", StringComparison.Ordinal)
                    || ns == "UnityEngine"
                    || ns.StartsWith("UnityEngine.", StringComparison.Ordinal)
                    || ns == "UnityEditor"
                    || ns.StartsWith("UnityEditor.", StringComparison.Ordinal))
                {
                    // UnityEngine.UI / InputSystem 等需要单独映射
                    if (ns.StartsWith("UnityEngine.UI", StringComparison.Ordinal)
                        || ns.StartsWith("UnityEngine.InputSystem", StringComparison.Ordinal)
                        || ns.StartsWith("UnityEditor.UI", StringComparison.Ordinal))
                    {
                        usingHash.Add(ns);
                    }

                    continue;
                }

                usingHash.Add(ns);
            }
        }

        /// <summary>
        /// using 映射到 asmdef 与 dll
        /// </summary>
        private static void MapUsingsToReferences(
            HashSet<string> usingHash,
            HashSet<string> referenceHash,
            HashSet<string> precompiledHash)
        {
            // 预置常见映射
            foreach (string ns in usingHash)
            {
                if (ns.StartsWith("Cysharp.Threading.Tasks", StringComparison.Ordinal))
                    referenceHash.Add("UniTask");
                else if (ns.StartsWith("TMPro", StringComparison.Ordinal))
                    referenceHash.Add("Unity.TextMeshPro");
                else if (ns.StartsWith("UnityEngine.InputSystem", StringComparison.Ordinal)
                         || ns.StartsWith("Unity.InputSystem", StringComparison.Ordinal))
                    referenceHash.Add("Unity.InputSystem");
                else if (ns.StartsWith("UnityEngine.UI", StringComparison.Ordinal))
                    referenceHash.Add("UnityEngine.UI");
                else if (ns.StartsWith("Unity.Cinemachine", StringComparison.Ordinal))
                    referenceHash.Add("Unity.Cinemachine");
                else if (ns.StartsWith("YooAsset", StringComparison.Ordinal))
                {
                    referenceHash.Add("YooAsset");
                }
                else if (ns.StartsWith("MieMieFrameWork.Asset", StringComparison.Ordinal))
                    referenceHash.Add("YooAsset.RuntimeAdapter");
                else if (ns.StartsWith("MiMieFSM.Unity", StringComparison.Ordinal)
                         || ns.StartsWith("MiMieFSM", StringComparison.Ordinal))
                    referenceHash.Add("MieMieFrameWork.Runtime");
                else if (ns.StartsWith("MiMieEventBus", StringComparison.Ordinal))
                    referenceHash.Add("MieMieFrameWork.Runtime");
                else if (ns.StartsWith("MieMieUIFrameWork.UI", StringComparison.Ordinal))
                    referenceHash.Add("MieMieUIFrameWork.UI");
                else if (ns.StartsWith("MieMieFrameWork", StringComparison.Ordinal))
                    referenceHash.Add("MieMieFrameWork.Runtime");
                else if (ns.StartsWith("MmInventory", StringComparison.Ordinal)
                         || ns.StartsWith("cfg.", StringComparison.Ordinal))
                {
                    referenceHash.Add("MmInventory.ItemData");
                    referenceHash.Add("LubanTableData.Gen");
                }
                else if (ns.StartsWith("Newtonsoft.Json", StringComparison.Ordinal))
                    precompiledHash.Add("Newtonsoft.Json.dll");
                else if (ns.StartsWith("Sirenix.OdinInspector.Editor", StringComparison.Ordinal))
                {
                    precompiledHash.Add("Sirenix.OdinInspector.Attributes.dll");
                    precompiledHash.Add("Sirenix.OdinInspector.Editor.dll");
                    precompiledHash.Add("Sirenix.Utilities.dll");
                    precompiledHash.Add("Sirenix.Utilities.Editor.dll");
                }
                else if (ns.StartsWith("Sirenix", StringComparison.Ordinal))
                {
                    precompiledHash.Add("Sirenix.OdinInspector.Attributes.dll");
                    precompiledHash.Add("Sirenix.Serialization.dll");
                    precompiledHash.Add("Sirenix.Utilities.dll");
                }
            }

            // 用 CompilationPipeline 把命名空间落到已有程序集
            Assembly[] assemblyList = CompilationPipeline.GetAssemblies();
            var namespaceToAssemblyDict = BuildNamespaceOwnerDict(assemblyList);
            foreach (string ns in usingHash)
            {
                if (TryResolveAssemblyByNamespace(ns, namespaceToAssemblyDict, out string asmName))
                    referenceHash.Add(asmName);
            }
        }

        /// <summary>
        /// 从编译程序集构建 命名空间到程序集 的归属
        /// </summary>
        private static Dictionary<string, string> BuildNamespaceOwnerDict(Assembly[] assemblyList)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < assemblyList.Length; i++)
            {
                Assembly assembly = assemblyList[i];
                if (assembly.sourceFiles == null)
                    continue;

                for (int s = 0; s < assembly.sourceFiles.Length; s++)
                {
                    string sourcePath = assembly.sourceFiles[s];
                    if (!sourcePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!File.Exists(sourcePath))
                        continue;

                    string text = File.ReadAllText(sourcePath);
                    MatchCollection matchList = Regex.Matches(
                        text,
                        @"^\s*namespace\s+([A-Za-z_][\w.]*)",
                        RegexOptions.Multiline);
                    for (int m = 0; m < matchList.Count; m++)
                    {
                        string ns = matchList[m].Groups[1].Value;
                        if (!dict.ContainsKey(ns))
                            dict[ns] = assembly.name;
                    }
                }
            }

            return dict;
        }

        /// <summary>
        /// 按最长前缀匹配命名空间归属程序集
        /// </summary>
        private static bool TryResolveAssemblyByNamespace(
            string ns,
            Dictionary<string, string> namespaceToAssemblyDict,
            out string assemblyName)
        {
            assemblyName = null;
            string bestKey = null;
            foreach (var pair in namespaceToAssemblyDict)
            {
                if (ns == pair.Key
                    || ns.StartsWith(pair.Key + ".", StringComparison.Ordinal))
                {
                    if (bestKey == null || pair.Key.Length > bestKey.Length)
                    {
                        bestKey = pair.Key;
                        assemblyName = pair.Value;
                    }
                }
            }

            if (string.IsNullOrEmpty(assemblyName))
                return false;

            // 过滤默认程序集与自身无 asmdef 的程序集名
            if (assemblyName == "Assembly-CSharp"
                || assemblyName == "Assembly-CSharp-Editor"
                || assemblyName == "Assembly-CSharp-firstpass")
            {
                assemblyName = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// 查找根下所有名为 Editor 的目录
        /// </summary>
        private static string[] FindEditorFolders(string rootFolderPath)
        {
            string absoluteRoot = ToAbsolutePath(rootFolderPath);
            if (!Directory.Exists(absoluteRoot))
                return Array.Empty<string>();

            string[] absoluteEditorList = Directory.GetDirectories(
                absoluteRoot,
                "Editor",
                SearchOption.AllDirectories);
            var assetPathList = new List<string>(absoluteEditorList.Length);
            string dataPath = Application.dataPath.Replace('\\', '/');
            for (int i = 0; i < absoluteEditorList.Length; i++)
            {
                string abs = absoluteEditorList[i].Replace('\\', '/');
                if (!abs.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
                    continue;
                string assetPath = "Assets" + abs.Substring(dataPath.Length);
                assetPathList.Add(assetPath);
            }

            return assetPathList.ToArray();
        }

        /// <summary>
        /// 是否位于目标根下的 Editor 目录中
        /// </summary>
        private static bool IsUnderEditorFolder(string assetPath, string rootFolderPath)
        {
            string normalizedAsset = assetPath.Replace('\\', '/');
            string normalizedRoot = rootFolderPath.Replace('\\', '/').TrimEnd('/');
            if (!normalizedAsset.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase))
                return false;

            string relative = normalizedAsset.Substring(normalizedRoot.Length + 1);
            string[] partList = relative.Split('/');
            for (int i = 0; i < partList.Length - 1; i++)
            {
                if (string.Equals(partList[i], "Editor", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 生成 Editor 程序集名
        /// </summary>
        private static string BuildEditorAssemblyName(
            string runtimeAssemblyName,
            string rootFolderPath,
            string editorFolderPath)
        {
            string normalizedRoot = rootFolderPath.Replace('\\', '/').TrimEnd('/');
            string normalizedEditor = editorFolderPath.Replace('\\', '/').TrimEnd('/');
            string relative = normalizedEditor.Substring(normalizedRoot.Length).Trim('/');
            string[] partList = relative.Split('/');
            if (partList.Length <= 1)
                return runtimeAssemblyName + ".Editor";

            // 多 Editor 目录时带上父级名避免重名
            string parentName = partList[partList.Length - 2];
            return $"{runtimeAssemblyName}.{parentName}.Editor";
        }

        /// <summary>
        /// 确保 dll 列表含指定项
        /// </summary>
        private static void EnsureDll(List<string> dllList, string dllName)
        {
            if (!dllList.Contains(dllName))
                dllList.Add(dllName);
        }

        /// <summary>
        /// Assets 路径转绝对路径
        /// </summary>
        private static string ToAbsolutePath(string assetPath)
        {
            string normalized = assetPath.Replace('\\', '/');
            if (normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return Path.Combine(Application.dataPath, normalized.Substring("Assets/".Length));
            if (string.Equals(normalized, "Assets", StringComparison.OrdinalIgnoreCase))
                return Application.dataPath;
            return assetPath;
        }

        /// <summary>
        /// 追加日志
        /// </summary>
        private void AppendLog(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            logText = string.IsNullOrEmpty(logText) ? line : logText + "\n" + line;
            Repaint();
        }

        /// <summary>
        /// json 字符串转义
        /// </summary>
        private static string EscapeJson(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }
    }
}
