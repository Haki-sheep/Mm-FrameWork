using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MieMieFrameWork.FSM.Editor;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.ChainedFsm.Editor
{
    /// <summary>
    /// 链式 FSM 生成器
    /// </summary>
    public class ChinedFSMSqueueWindow : EditorWindow
    {
        [Serializable]
        private class StateClassEntry
        {
            public string ClassName = string.Empty;
            public bool Overwrite;
        }

        private const string DefaultOutputFolder = "Assets/Scripts/Game/Fsm/ChainedFsm/Generated";
        private const string DefaultNamespace = "Game.Fsm.ChainedFsm";
        private const string DefaultFlowName = "GameFlowSequence";

        private const string PrefsNamespace = "ChainedFsmGenerator.Namespace";
        private const string PrefsOutputFolder = "ChainedFsmGenerator.OutputFolder";
        private const string PrefsFlowName = "ChainedFsmGenerator.FlowName";
        private const string PrefsGenerateSequence = "ChainedFsmGenerator.GenerateSequence";
        private const string PrefsEntries = "ChainedFsmGenerator.Entries";

        private string namespaceValue = DefaultNamespace;
        private string outputFolder = DefaultOutputFolder;
        private string flowName = DefaultFlowName;
        private bool generateSequence = true;
        private readonly List<StateClassEntry> stateEntryList = new List<StateClassEntry>();
        private Vector2 scroll;

        [MenuItem("Tools/MieMieFrameWork/FSM/Chained FSM Generator")]
        public static void Open()
        {
            ChinedFSMSqueueWindow window = GetWindow<ChinedFSMSqueueWindow>("链式FSM生成器");
            window.LoadPrefs();
        }

        private void OnEnable() => LoadPrefs();
        private void OnDisable() => SavePrefs();

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("链式 FSM 生成器", EditorStyles.boldLabel);

            if (!FsmLibraryChecker.DrawInstallGate())
                return;

            EditorGUILayout.HelpBox(
                "按顺序配置状态类名 将生成继承 IChainedFsm 的状态类\n" +
                "勾选覆盖才会重写已存在文件",
                MessageType.Info);
            EditorGUILayout.Space(4);

            namespaceValue = EditorGUILayout.TextField("命名空间", namespaceValue);
            outputFolder = EditorGUILayout.TextField("输出文件夹", outputFolder);
            flowName = EditorGUILayout.TextField("顺序控制器类名", flowName);
            generateSequence = EditorGUILayout.Toggle("同时生成顺序控制器", generateSequence);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("选择输出文件夹", GUILayout.Height(22)))
                    PickOutputFolder();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"状态类顺序 ({stateEntryList.Count})", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加状态类", GUILayout.Height(24)))
                    stateEntryList.Add(new StateClassEntry());

                if (GUILayout.Button("清空全部", GUILayout.Width(80), GUILayout.Height(24)) && stateEntryList.Count > 0)
                {
                    if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有状态类吗？", "清空", "取消"))
                        stateEntryList.Clear();
                }
            }

            EditorGUILayout.Space(2);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(200));

            if (stateEntryList.Count == 0)
            {
                EditorGUILayout.LabelField("暂无状态类 点击添加状态类开始", EditorStyles.centeredGreyMiniLabel);
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

                for (int i = 0; i < stateEntryList.Count; i++)
                {
                    StateClassEntry entry = stateEntryList[i];
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

                        using (new EditorGUI.DisabledScope(i == stateEntryList.Count - 1))
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
                            string status = entry.Overwrite ? "已存在 勾选后将覆盖" : "已存在 未勾选将跳过";
                            EditorGUILayout.LabelField(status, EditorStyles.centeredGreyMiniLabel);
                        }
                    }
                }

                if (removeIndex >= 0)
                    stateEntryList.RemoveAt(removeIndex);

                if (moveUpIndex > 0)
                    Swap(moveUpIndex, moveUpIndex - 1);

                if (moveDownIndex >= 0 && moveDownIndex < stateEntryList.Count - 1)
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
            StateClassEntry temp = stateEntryList[a];
            stateEntryList[a] = stateEntryList[b];
            stateEntryList[b] = temp;
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
                EditorUtility.DisplayDialog("路径无效", "请选择 Assets 目录内的文件夹", "确定");
                return;
            }

            outputFolder = "Assets" + fullPath.Substring(dataPath.Length).Replace('\\', '/');
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

            List<string> generatedFiles = new List<string>();
            List<string> skippedFiles = new List<string>();
            List<string> classNameList = GetTrimmedClassNames();

            try
            {
                for (int i = 0; i < classNameList.Count; i++)
                {
                    string className = classNameList[i];
                    string filePath = $"{folder}/{className}.cs";
                    bool fileExists = File.Exists(filePath);

                    if (fileExists && !stateEntryList[i].Overwrite)
                    {
                        skippedFiles.Add(filePath);
                        continue;
                    }

                    File.WriteAllText(filePath, BuildStateClassContent(className), Encoding.UTF8);
                    generatedFiles.Add(filePath);
                }

                if (generateSequence)
                {
                    string sequencePath = $"{folder}/{flowName.Trim()}.cs";
                    File.WriteAllText(sequencePath, BuildSequenceClassContent(classNameList), Encoding.UTF8);
                    generatedFiles.Add(sequencePath);
                }

                AssetDatabase.Refresh();

                StringBuilder message = new StringBuilder();
                message.AppendLine($"已生成 {generatedFiles.Count} 个文件");
                foreach (string file in generatedFiles)
                    message.AppendLine(file);

                if (skippedFiles.Count > 0)
                {
                    message.AppendLine();
                    message.AppendLine($"已跳过 {skippedFiles.Count} 个文件");
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
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using MiMieFSM.ChainedFsm;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceValue}");
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

        private string BuildSequenceClassContent(List<string> classNameList)
        {
            string trimmedFlowName = flowName.Trim();
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("using System;");
            sb.AppendLine("using MiMieFSM.ChainedFsm;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceValue}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// 链式状态顺序控制器");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public class {trimmedFlowName}");
            sb.AppendLine("    {");
            sb.AppendLine("        private readonly ChainedFsm chainedFsm = new ChainedFsm();");
            sb.AppendLine();
            sb.AppendLine("        public ChainedFsm ChainedFsm => chainedFsm;");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>从第一个状态开始</summary>");
            sb.AppendLine("        public void Start()");
            sb.AppendLine("        {");
            sb.AppendLine($"            chainedFsm.ChangeState<{classNameList[0]}>();");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>切换到顺序中的下一个状态</summary>");
            sb.AppendLine("        public bool GoNext()");
            sb.AppendLine("        {");
            sb.AppendLine("            Type curChainedStateType = chainedFsm.CurrentStateType;");

            for (int i = 0; i < classNameList.Count - 1; i++)
            {
                sb.AppendLine($"            if (curChainedStateType == typeof({classNameList[i]}))");
                sb.AppendLine("            {");
                sb.AppendLine($"                chainedFsm.ChangeState<{classNameList[i + 1]}>();");
                sb.AppendLine("                return true;");
                sb.AppendLine("            }");
            }

            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>当前是否为链式流程的最后一个状态</summary>");
            sb.AppendLine("        public bool IsLastState()");
            sb.AppendLine("        {");
            sb.AppendLine($"            return chainedFsm.CurrentStateType == typeof({classNameList[classNameList.Count - 1]});");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.Append("}");
            return sb.ToString();
        }

        private string GetOutputFolderPath()
        {
            return outputFolder.Replace('\\', '/').TrimEnd('/');
        }

        private List<string> GetTrimmedClassNames()
        {
            List<string> result = new List<string>(stateEntryList.Count);
            foreach (StateClassEntry entry in stateEntryList)
                result.Add(entry.ClassName.Trim());
            return result;
        }

        private bool CanGenerate(out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(namespaceValue))
            {
                error = "命名空间不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                error = "输出文件夹不能为空";
                return false;
            }

            if (generateSequence)
            {
                if (string.IsNullOrWhiteSpace(flowName))
                {
                    error = "顺序控制器类名不能为空";
                    return false;
                }

                if (!IsValidIdentifier(flowName.Trim()))
                {
                    error = "顺序控制器类名不是合法的 C# 标识符";
                    return false;
                }
            }

            if (stateEntryList.Count == 0)
            {
                error = "请至少添加一个状态类";
                return false;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < stateEntryList.Count; i++)
            {
                string className = stateEntryList[i].ClassName?.Trim();
                if (string.IsNullOrEmpty(className))
                {
                    error = $"第 {i + 1} 个状态类名不能为空";
                    return false;
                }

                if (!IsValidIdentifier(className))
                {
                    error = $"状态类 {className} 不是合法的 C# 标识符";
                    return false;
                }

                if (!seen.Add(className))
                {
                    error = $"状态类 {className} 重复";
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
            namespaceValue = EditorPrefs.GetString(PrefsNamespace, DefaultNamespace);
            outputFolder = EditorPrefs.GetString(PrefsOutputFolder, DefaultOutputFolder);
            flowName = EditorPrefs.GetString(PrefsFlowName, DefaultFlowName);
            generateSequence = EditorPrefs.GetBool(PrefsGenerateSequence, true);

            stateEntryList.Clear();
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
                        stateEntryList.Add(new StateClassEntry
                        {
                            ClassName = item.Substring(0, splitIndex),
                            Overwrite = overwrite
                        });
                    }
                    else
                    {
                        stateEntryList.Add(new StateClassEntry { ClassName = item });
                    }
                }
            }
        }

        private void SavePrefs()
        {
            EditorPrefs.SetString(PrefsNamespace, namespaceValue);
            EditorPrefs.SetString(PrefsOutputFolder, outputFolder);
            EditorPrefs.SetString(PrefsFlowName, flowName);
            EditorPrefs.SetBool(PrefsGenerateSequence, generateSequence);

            List<string> savedEntries = new List<string>(stateEntryList.Count);
            foreach (StateClassEntry entry in stateEntryList)
                savedEntries.Add($"{entry.ClassName}:{entry.Overwrite}");

            EditorPrefs.SetString(PrefsEntries, string.Join("|", savedEntries));
        }
    }
}
