using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.FSM
{
    public class UpdateFsmEditorWindow : EditorWindow
    {
        private const string DefaultOutputFolder = "Assets/MieMieFrameTools/Scripts/GeneralLogics/Fsm/Update_FSM";
        private const string DefaultEnumName = "EBlockBoardParme";
        private const string DefaultNamespace = "MieMieFrameWork.FSM";

        private const string PrefsEnumName = "UpdateFsmEditor.EnumName";
        private const string PrefsNamespace = "UpdateFsmEditor.Namespace";
        private const string PrefsOutputFolder = "UpdateFsmEditor.OutputFolder";
        private const string PrefsEntries = "UpdateFsmEditor.Entries";

        private string _enumName = DefaultEnumName;
        private string _namespace = DefaultNamespace;
        private string _outputFolder = DefaultOutputFolder;
        private readonly List<string> _enumEntries = new();
        private Vector2 _scroll;

        [MenuItem("Tools/MieMieFrameWork/FSM/FSM枚举生成器")]
        public static void Open()
        {
            var window = GetWindow<UpdateFsmEditorWindow>("FSM枚举生成器");
            window.LoadPrefs();
        }

        private void OnEnable()
        {
            LoadPrefs();
        }

        private void OnDisable()
        {
            SavePrefs();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("FSM 枚举生成器", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            _enumName = EditorGUILayout.TextField("枚举类型名", _enumName);
            _namespace = EditorGUILayout.TextField("命名空间", _namespace);
            _outputFolder = EditorGUILayout.TextField("输出文件夹", _outputFolder);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("选择输出文件夹", GUILayout.Height(22)))
                    PickOutputFolder();

                if (GUILayout.Button("从现有文件加载", GUILayout.Height(22)))
                    LoadFromExistingFile();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"枚举成员 ({_enumEntries.Count})", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加枚举", GUILayout.Height(24)))
                    _enumEntries.Add(string.Empty);

                if (GUILayout.Button("清空全部", GUILayout.Width(80), GUILayout.Height(24)) && _enumEntries.Count > 0)
                {
                    if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有枚举成员吗？", "清空", "取消"))
                        _enumEntries.Clear();
                }
            }

            EditorGUILayout.Space(2);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(160));

            if (_enumEntries.Count == 0)
            {
                EditorGUILayout.LabelField("暂无枚举成员，点击「添加枚举」开始。", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                int removeIndex = -1;
                for (int i = 0; i < _enumEntries.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(24));
                        _enumEntries[i] = EditorGUILayout.TextField(_enumEntries[i]);
                        if (GUILayout.Button("删除", GUILayout.Width(48)))
                            removeIndex = i;
                    }
                }

                if (removeIndex >= 0)
                    _enumEntries.RemoveAt(removeIndex);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);
            string previewPath = GetOutputFilePath();
            EditorGUILayout.LabelField("生成路径", previewPath, EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(!CanGenerate(out _)))
            {
                if (GUILayout.Button("生成枚举文件", GUILayout.Height(32)))
                    GenerateEnumFile();
            }
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

        private void LoadFromExistingFile()
        {
            string filePath = GetOutputFilePath();
            if (!File.Exists(filePath))
            {
                EditorUtility.DisplayDialog("文件不存在", $"未找到文件：\n{filePath}", "确定");
                return;
            }

            try
            {
                string content = File.ReadAllText(filePath);
                if (!TryParseEnumFile(content, out string ns, out string enumName, out List<string> entries))
                {
                    EditorUtility.DisplayDialog("解析失败", "无法从文件中解析枚举定义，请检查文件格式。", "确定");
                    return;
                }

                _namespace = ns;
                _enumName = enumName;
                _enumEntries.Clear();
                _enumEntries.AddRange(entries);
                Repaint();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("读取失败", ex.Message, "确定");
            }
        }

        private void GenerateEnumFile()
        {
            if (!CanGenerate(out string error))
            {
                EditorUtility.DisplayDialog("无法生成", error, "确定");
                return;
            }

            string filePath = GetOutputFilePath();
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            try
            {
                File.WriteAllText(filePath, BuildEnumFileContent(), Encoding.UTF8);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("生成成功", $"已生成：\n{filePath}", "确定");
                SavePrefs();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("生成失败", ex.Message, "确定");
            }
        }

        private string BuildEnumFileContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"namespace {_namespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public enum {_enumName}");
            sb.AppendLine("    {");

            for (int i = 0; i < _enumEntries.Count; i++)
            {
                string entry = _enumEntries[i].Trim();
                string suffix = i < _enumEntries.Count - 1 ? "," : string.Empty;
                sb.AppendLine($"        {entry}{suffix}");
            }

            sb.AppendLine("    }");
            sb.Append("}");
            return sb.ToString();
        }

        private string GetOutputFilePath()
        {
            string folder = _outputFolder.Replace('\\', '/').TrimEnd('/');
            return $"{folder}/{_enumName}.cs";
        }

        private bool CanGenerate(out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(_enumName))
            {
                error = "枚举类型名不能为空。";
                return false;
            }

            if (!IsValidIdentifier(_enumName))
            {
                error = "枚举类型名不是合法的 C# 标识符。";
                return false;
            }

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

            if (_enumEntries.Count == 0)
            {
                error = "请至少添加一个枚举成员。";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _enumEntries.Count; i++)
            {
                string entry = _enumEntries[i]?.Trim();
                if (string.IsNullOrEmpty(entry))
                {
                    error = $"第 {i + 1} 个枚举成员不能为空。";
                    return false;
                }

                if (!IsValidIdentifier(entry))
                {
                    error = $"枚举成员「{entry}」不是合法的 C# 标识符。";
                    return false;
                }

                if (!seen.Add(entry))
                {
                    error = $"枚举成员「{entry}」重复。";
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidIdentifier(string name)
        {
            return !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }

        private static bool TryParseEnumFile(string content, out string ns, out string enumName, out List<string> entries)
        {
            ns = DefaultNamespace;
            enumName = DefaultEnumName;
            entries = new List<string>();

            var nsMatch = Regex.Match(content, @"namespace\s+([\w\.]+)");
            if (nsMatch.Success)
                ns = nsMatch.Groups[1].Value;

            var enumMatch = Regex.Match(content, @"public\s+enum\s+(\w+)");
            if (!enumMatch.Success)
                return false;

            enumName = enumMatch.Groups[1].Value;

            int braceStart = content.IndexOf('{', enumMatch.Index);
            if (braceStart < 0)
                return false;

            int depth = 0;
            int enumBodyStart = -1;
            int enumBodyEnd = -1;

            for (int i = braceStart; i < content.Length; i++)
            {
                if (content[i] == '{')
                {
                    depth++;
                    if (depth == 1)
                        enumBodyStart = i + 1;
                }
                else if (content[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        enumBodyEnd = i;
                        break;
                    }
                }
            }

            if (enumBodyStart < 0 || enumBodyEnd < 0)
                return false;

            string body = content.Substring(enumBodyStart, enumBodyEnd - enumBodyStart);
            foreach (string rawLine in body.Split('\n'))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                int commentIndex = line.IndexOf("//", StringComparison.Ordinal);
                if (commentIndex >= 0)
                    line = line.Substring(0, commentIndex).Trim();

                line = line.TrimEnd(',');
                if (string.IsNullOrEmpty(line))
                    continue;

                int assignIndex = line.IndexOf('=');
                if (assignIndex >= 0)
                    line = line.Substring(0, assignIndex).Trim();

                if (!string.IsNullOrEmpty(line))
                    entries.Add(line);
            }

            return true;
        }

        private void LoadPrefs()
        {
            _enumName = EditorPrefs.GetString(PrefsEnumName, DefaultEnumName);
            _namespace = EditorPrefs.GetString(PrefsNamespace, DefaultNamespace);
            _outputFolder = EditorPrefs.GetString(PrefsOutputFolder, DefaultOutputFolder);

            _enumEntries.Clear();
            string saved = EditorPrefs.GetString(PrefsEntries, string.Empty);
            if (!string.IsNullOrEmpty(saved))
            {
                foreach (string item in saved.Split('|'))
                {
                    if (!string.IsNullOrEmpty(item))
                        _enumEntries.Add(item);
                }
            }
        }

        private void SavePrefs()
        {
            EditorPrefs.SetString(PrefsEnumName, _enumName);
            EditorPrefs.SetString(PrefsNamespace, _namespace);
            EditorPrefs.SetString(PrefsOutputFolder, _outputFolder);
            EditorPrefs.SetString(PrefsEntries, string.Join("|", _enumEntries));
        }
    }
}
