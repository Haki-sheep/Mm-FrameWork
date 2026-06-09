using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.ChainedFsm.Editor
{
    public class ChinedFSMSqueueWindow : EditorWindow
    {
        [Serializable]
        private class StateClassEntry
        {
            public string ClassName = string.Empty;
            public bool Overwrite;
        }

        private const string DefaultOutputFolder = "Assets/MieMieFrameTools/Scripts/GeneralLogics/Fsm/ChainedFsm/Generated";
        private const string DefaultNamespace = "MieMieFrameWork.ChainedFsm";
        private const string DefaultFlowName = "GameFlowSequence";

        private const string PrefsNamespace = "ChainedFsmGenerator.Namespace";
        private const string PrefsOutputFolder = "ChainedFsmGenerator.OutputFolder";
        private const string PrefsFlowName = "ChainedFsmGenerator.FlowName";
        private const string PrefsGenerateSequence = "ChainedFsmGenerator.GenerateSequence";
        private const string PrefsEntries = "ChainedFsmGenerator.Entries";

        private string _namespace = DefaultNamespace;
        private string _outputFolder = DefaultOutputFolder;
        private string _flowName = DefaultFlowName;
        private bool _generateSequence = true;
        private readonly List<StateClassEntry> _stateEntries = new();
        private Vector2 _scroll;

        [MenuItem("Tools/MieMieFrameWork/FSM/链式FSM生成器")]
        public static void Open()
        {
            var window = GetWindow<ChinedFSMSqueueWindow>("链式FSM生成器");
            window.LoadPrefs();
        }

        private void OnEnable() => LoadPrefs();
        private void OnDisable() => SavePrefs();

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("链式 FSM 生成器", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "按顺序配置状态类名，将生成继承 IChainedFsm 的状态类（默认空实现，不处理委托）。\n" +
                "勾选「覆盖」才会重写已存在的类文件；未勾选且文件已存在则跳过，避免覆盖手写逻辑。",
                MessageType.Info);
            EditorGUILayout.Space(4);

            _namespace = EditorGUILayout.TextField("命名空间", _namespace);
            _outputFolder = EditorGUILayout.TextField("输出文件夹", _outputFolder);
            _flowName = EditorGUILayout.TextField("顺序控制器类名", _flowName);
            _generateSequence = EditorGUILayout.Toggle("同时生成顺序控制器", _generateSequence);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("选择输出文件夹", GUILayout.Height(22)))
                    PickOutputFolder();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"状态类顺序 ({_stateEntries.Count})", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加状态类", GUILayout.Height(24)))
                    _stateEntries.Add(new StateClassEntry());

                if (GUILayout.Button("清空全部", GUILayout.Width(80), GUILayout.Height(24)) && _stateEntries.Count > 0)
                {
                    if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有状态类吗？", "清空", "取消"))
                        _stateEntries.Clear();
                }
            }

            EditorGUILayout.Space(2);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(200));

            if (_stateEntries.Count == 0)
            {
                EditorGUILayout.LabelField("暂无状态类，点击「添加状态类」开始。", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                int removeIndex = -1;
                int moveUpIndex = -1;
                int moveDownIndex = -1;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(24);
                    EditorGUILayout.LabelField("覆盖", EditorStyles.miniLabel, GUILayout.Width(36));
                    EditorGUILayout.LabelField("类名", EditorStyles.miniLabel);
                }

                for (int i = 0; i < _stateEntries.Count; i++)
                {
                    var entry = _stateEntries[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(24));
                        entry.Overwrite = EditorGUILayout.Toggle(entry.Overwrite, GUILayout.Width(36));
                        entry.ClassName = EditorGUILayout.TextField(entry.ClassName);

                        using (new EditorGUI.DisabledScope(i == 0))
                        {
                            if (GUILayout.Button("↑", GUILayout.Width(24)))
                                moveUpIndex = i;
                        }

                        using (new EditorGUI.DisabledScope(i == _stateEntries.Count - 1))
                        {
                            if (GUILayout.Button("↓", GUILayout.Width(24)))
                                moveDownIndex = i;
                        }

                        if (GUILayout.Button("删除", GUILayout.Width(48)))
                            removeIndex = i;
                    }

                    string className = entry.ClassName?.Trim();
                    if (!string.IsNullOrEmpty(className))
                    {
                        string filePath = $"{GetOutputFolderPath()}/{className}.cs";
                        if (File.Exists(filePath))
                        {
                            string status = entry.Overwrite ? "已存在，勾选后将覆盖" : "已存在，未勾选将跳过";
                            EditorGUILayout.LabelField(status, EditorStyles.centeredGreyMiniLabel);
                        }
                    }
                }

                if (removeIndex >= 0)
                    _stateEntries.RemoveAt(removeIndex);

                if (moveUpIndex > 0)
                    Swap(moveUpIndex, moveUpIndex - 1);

                if (moveDownIndex >= 0 && moveDownIndex < _stateEntries.Count - 1)
                    Swap(moveDownIndex, moveDownIndex + 1);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("输出预览", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(GetOutputFolderPath(), EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(!CanGenerate(out _)))
            {
                if (GUILayout.Button("生成链式状态类", GUILayout.Height(32)))
                    GenerateFiles();
            }
        }

        private void Swap(int a, int b)
        {
            (_stateEntries[a], _stateEntries[b]) = (_stateEntries[b], _stateEntries[a]);
        }

        private void PickOutputFolder()
        {
            string absolute = EditorUtility.OpenFolderPanel("选择输出文件夹", Application.dataPath, string.Empty);
            if (string.IsNullOrEmpty(absolute))
                return;

            string dataPath = Path.GetFullPath(Application.dataPath);
            string fullPath = Path.GetFullPath(absolute);

            if (!fullPath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("路径无效", "请选择 Assets 目录内的文件夹。", "确定");
                return;
            }

            _outputFolder = "Assets" + fullPath.Substring(dataPath.Length).Replace('\\', '/');
        }

        private void GenerateFiles()
        {
            if (!CanGenerate(out string error))
            {
                EditorUtility.DisplayDialog("无法生成", error, "确定");
                return;
            }

            string folder = GetOutputFolderPath();
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var generatedFiles = new List<string>();
            var skippedFiles = new List<string>();
            var classNames = GetTrimmedClassNames();

            try
            {
                for (int i = 0; i < classNames.Count; i++)
                {
                    string className = classNames[i];
                    string filePath = $"{folder}/{className}.cs";
                    bool fileExists = File.Exists(filePath);

                    if (fileExists && !_stateEntries[i].Overwrite)
                    {
                        skippedFiles.Add(filePath);
                        continue;
                    }

                    File.WriteAllText(filePath, BuildStateClassContent(className), Encoding.UTF8);
                    generatedFiles.Add(filePath);
                }

                if (_generateSequence)
                {
                    string sequencePath = $"{folder}/{_flowName.Trim()}.cs";
                    File.WriteAllText(sequencePath, BuildSequenceClassContent(classNames), Encoding.UTF8);
                    generatedFiles.Add(sequencePath);
                }

                AssetDatabase.Refresh();

                var message = new StringBuilder();
                message.AppendLine($"已生成 {generatedFiles.Count} 个文件：");
                foreach (string file in generatedFiles)
                    message.AppendLine(file);

                if (skippedFiles.Count > 0)
                {
                    message.AppendLine();
                    message.AppendLine($"已跳过 {skippedFiles.Count} 个文件：");
                    foreach (string file in skippedFiles)
                        message.AppendLine(file);
                }

                EditorUtility.DisplayDialog("生成完成", message.ToString(), "确定");
                SavePrefs();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("生成失败", ex.Message, "确定");
            }
        }

        private string BuildStateClassContent(string className)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"namespace {_namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {className} : IChainedFsm");
            sb.AppendLine("    {");
            sb.AppendLine("        public override void OnEnter()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnEnter();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override void OnExit()");
            sb.AppendLine("        {");
            sb.AppendLine("            base.OnExit();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.Append("}");
            return sb.ToString();
        }

        private string BuildSequenceClassContent(List<string> classNames)
        {
            string flowName = _flowName.Trim();
            var sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine($"namespace {_namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// 链式状态顺序控制器（自动生成，按配置顺序切换状态）");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public class {flowName}");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly ChainedFsm chainedFsm = new();");
            sb.AppendLine();
            sb.AppendLine("        public ChainedFsm ChainedFsm => chainedFsm;");
            sb.AppendLine();

            sb.AppendLine("        /// <summary>从第一个状态开始</summary>");
            sb.AppendLine($"        public void Start()");
            sb.AppendLine("        {");
            sb.AppendLine($"            chainedFsm.ChangeState<{classNames[0]}>();");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        /// <summary>切换到顺序中的下一个状态</summary>");
            sb.AppendLine("        public bool GoNext()");
            sb.AppendLine("        {");
            sb.AppendLine("            var curChainedStateType = chainedFsm.CurrentStateType;");

            for (int i = 0; i < classNames.Count - 1; i++)
            {
                sb.AppendLine($"            if (curChainedStateType == typeof({classNames[i]}))");
                sb.AppendLine("            {");
                sb.AppendLine($"                chainedFsm.ChangeState<{classNames[i + 1]}>();");
                sb.AppendLine("                return true;");
                sb.AppendLine("            }");
            }

            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
            sb.AppendLine();

            sb.AppendLine("        /// <summary>当前是否为链式流程的最后一个状态</summary>");
            sb.AppendLine("        public bool IsLastState()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return chainedFsm.CurrentStateType == typeof({classNames[^1]});");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.Append("}");

            return sb.ToString();
        }

        private string GetOutputFolderPath()
        {
            return _outputFolder.Replace('\\', '/').TrimEnd('/');
        }

        private List<string> GetTrimmedClassNames()
        {
            var result = new List<string>(_stateEntries.Count);
            foreach (StateClassEntry entry in _stateEntries)
                result.Add(entry.ClassName.Trim());
            return result;
        }

        private bool CanGenerate(out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(_namespace))
            {
                error = "命名空间不能为空。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_outputFolder))
            {
                error = "输出文件夹不能为空。";
                return false;
            }

            if (_generateSequence)
            {
                if (string.IsNullOrWhiteSpace(_flowName))
                {
                    error = "顺序控制器类名不能为空。";
                    return false;
                }

                if (!IsValidIdentifier(_flowName.Trim()))
                {
                    error = "顺序控制器类名不是合法的 C# 标识符。";
                    return false;
                }
            }

            if (_stateEntries.Count == 0)
            {
                error = "请至少添加一个状态类。";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _stateEntries.Count; i++)
            {
                string className = _stateEntries[i].ClassName?.Trim();
                if (string.IsNullOrEmpty(className))
                {
                    error = $"第 {i + 1} 个状态类名不能为空。";
                    return false;
                }

                if (!IsValidIdentifier(className))
                {
                    error = $"状态类「{className}」不是合法的 C# 标识符。";
                    return false;
                }

                if (!seen.Add(className))
                {
                    error = $"状态类「{className}」重复。";
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidIdentifier(string name)
        {
            return !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }

        private void LoadPrefs()
        {
            _namespace = EditorPrefs.GetString(PrefsNamespace, DefaultNamespace);
            _outputFolder = EditorPrefs.GetString(PrefsOutputFolder, DefaultOutputFolder);
            _flowName = EditorPrefs.GetString(PrefsFlowName, DefaultFlowName);
            _generateSequence = EditorPrefs.GetBool(PrefsGenerateSequence, true);

            _stateEntries.Clear();
            string saved = EditorPrefs.GetString(PrefsEntries, string.Empty);
            if (!string.IsNullOrEmpty(saved))
            {
                foreach (string item in saved.Split('|'))
                {
                    if (string.IsNullOrEmpty(item))
                        continue;

                    int splitIndex = item.LastIndexOf(':');
                    if (splitIndex > 0 && bool.TryParse(item.Substring(splitIndex + 1), out bool overwrite))
                    {
                        _stateEntries.Add(new StateClassEntry
                        {
                            ClassName = item.Substring(0, splitIndex),
                            Overwrite = overwrite
                        });
                    }
                    else
                    {
                        _stateEntries.Add(new StateClassEntry { ClassName = item });
                    }
                }
            }
        }

        private void SavePrefs()
        {
            EditorPrefs.SetString(PrefsNamespace, _namespace);
            EditorPrefs.SetString(PrefsOutputFolder, _outputFolder);
            EditorPrefs.SetString(PrefsFlowName, _flowName);
            EditorPrefs.SetBool(PrefsGenerateSequence, _generateSequence);

            var savedEntries = new List<string>(_stateEntries.Count);
            foreach (StateClassEntry entry in _stateEntries)
                savedEntries.Add($"{entry.ClassName}:{entry.Overwrite}");

            EditorPrefs.SetString(PrefsEntries, string.Join("|", savedEntries));
        }
    }
}
