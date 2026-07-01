using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MieMieFrameWork.FSM.Editor;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.FSM
{
    /// <summary>
    /// TickFSM 黑板枚举生成与调试窗口
    /// </summary>
    public class TickFSMWindow : EditorWindow
    {
        private const string DefaultOutputFolder = "Assets/Scripts/Game/Fsm/UpdateFsm";
        private const string DefaultEnumName = "EBlockBoardParme";
        private const string DefaultNamespace = "MiMieFSM.UpdateFsm";

        private const string PrefsEnumName = "UpdateFsmEditor.EnumName";
        private const string PrefsNamespace = "UpdateFsmEditor.Namespace";
        private const string PrefsOutputFolder = "UpdateFsmEditor.OutputFolder";
        private const string PrefsEntries = "UpdateFsmEditor.Entries";
        private const string PrefsSmartDebugEnabled = "UpdateFsmEditor.SmartDebugEnabled";

        /// <summary>
        /// 枚举类型名
        /// </summary>
        private string enumName = DefaultEnumName;

        /// <summary>
        /// 命名空间
        /// </summary>
        private string namespaceValue = DefaultNamespace;

        /// <summary>
        /// 输出文件夹
        /// </summary>
        private string outputFolder = DefaultOutputFolder;

        /// <summary>
        /// 枚举成员列表
        /// </summary>
        private readonly List<string> enumEntryList = new List<string>();

        /// <summary>
        /// 主滚动位置
        /// </summary>
        private Vector2 mainScroll;

        /// <summary>
        /// 是否启用智能调试
        /// </summary>
        private bool smartDebugEnabled;

        /// <summary>
        /// 调试字体大小
        /// </summary>
        private int debugFontSize = 18;

        /// <summary>
        /// 调试文本颜色
        /// </summary>
        private Color debugTextColor = new Color(0.2f, 1f, 0.4f, 1f);

        /// <summary>
        /// 调试绘制位置
        /// </summary>
        private Vector2 debugScreenPos = new Vector2(12f, 12f);

        /// <summary>
        /// 调试行高
        /// </summary>
        private float debugLineHeight = 24f;

        [MenuItem("Tools/MieMieFrameWork/FSM/TickFSMWindow")]
        public static void Open()
        {
            TickFSMWindow window = GetWindow<TickFSMWindow>("TickFSMWindow");
            window.minSize = new Vector2(360f, 320f);
            window.LoadPrefs();
        }

        private void OnEnable()
        {
            minSize = new Vector2(360f, 320f);
            LoadPrefs();
        }

        private void OnDisable()
        {
            SavePrefs();
        }

        private void OnGUI()
        {
            mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("TickFSMWindow", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            if (!FsmLibraryChecker.DrawInstallGate())
            {
                EditorGUILayout.EndScrollView();
                return;
            }

            enumName = EditorGUILayout.TextField("枚举类型名", enumName);
            namespaceValue = EditorGUILayout.TextField("命名空间", namespaceValue);
            outputFolder = EditorGUILayout.TextField("输出文件夹", outputFolder);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("选择输出文件夹", GUILayout.Height(22)))
                    PickOutputFolder();

                if (GUILayout.Button("从现有文件加载", GUILayout.Height(22)))
                    LoadFromExistingFile();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"枚举成员 ({enumEntryList.Count})", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加枚举", GUILayout.Height(24)))
                    enumEntryList.Add(string.Empty);

                if (GUILayout.Button("清空全部", GUILayout.Width(80), GUILayout.Height(24)) && enumEntryList.Count > 0)
                {
                    if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有枚举成员吗？", "清空", "取消"))
                        enumEntryList.Clear();
                }
            }

            EditorGUILayout.Space(2);

            if (enumEntryList.Count == 0)
            {
                EditorGUILayout.LabelField("暂无枚举成员 点击添加枚举开始", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                int removeIndex = -1;
                for (int i = 0; i < enumEntryList.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(24));
                        enumEntryList[i] = EditorGUILayout.TextField(enumEntryList[i]);
                        if (GUILayout.Button("删除", GUILayout.Width(48)))
                            removeIndex = i;
                    }
                }

                if (removeIndex >= 0)
                    enumEntryList.RemoveAt(removeIndex);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("生成路径", GetOutputFilePath(), EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(!CanGenerate(out _)))
            {
                if (GUILayout.Button("生成枚举文件", GUILayout.Height(32)))
                    GenerateEnumFile();
            }

            EditorGUILayout.Space(12);
            DrawSmartDebugSection();

            EditorGUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制智能调试配置
        /// </summary>
        private void DrawSmartDebugSection()
        {
            EditorGUILayout.LabelField("智能调试", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "开启后在 Play 模式下于 Game 视图左上角实时显示 UpdateFsmHost 当前状态\n" +
                "需场景中存在 UpdateFsmHost 组件",
                MessageType.Info);

            bool newEnabled = EditorGUILayout.Toggle("启用智能调试", smartDebugEnabled);
            if (newEnabled != smartDebugEnabled)
            {
                smartDebugEnabled = newEnabled;
                ApplySmartDebugSettings();
            }

            using (new EditorGUI.DisabledScope(!smartDebugEnabled))
            {
                debugFontSize = EditorGUILayout.IntSlider("字体大小", debugFontSize, 8, 48);
                debugTextColor = EditorGUILayout.ColorField("文本颜色", debugTextColor);
                debugScreenPos = EditorGUILayout.Vector2Field("绘制位置", debugScreenPos);
                debugLineHeight = EditorGUILayout.Slider("行高", debugLineHeight, 12f, 48f);
            }

            if (GUILayout.Button("应用调试设置", GUILayout.Height(24)))
                ApplySmartDebugSettings();
        }

        /// <summary>
        /// 写入调试设置
        /// </summary>
        private void ApplySmartDebugSettings()
        {
            UpdateFsmDebugEditorPrefs.Enabled = smartDebugEnabled;
            UpdateFsmDebugEditorPrefs.FontSize = debugFontSize;
            UpdateFsmDebugEditorPrefs.TextColor = debugTextColor;
            UpdateFsmDebugEditorPrefs.ScreenPosition = debugScreenPos;
            UpdateFsmDebugEditorPrefs.LineHeight = debugLineHeight;
            UpdateFsmDebugEditorPrefs.RefreshRuntimeOverlay();
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

        private void LoadFromExistingFile()
        {
            string filePath = GetOutputFilePath();
            if (!File.Exists(filePath))
            {
                EditorUtility.DisplayDialog("文件不存在", $"未找到文件\n{filePath}", "确定");
                return;
            }

            try
            {
                string content = File.ReadAllText(filePath);
                if (!TryParseEnumFile(content, out string ns, out string parsedEnumName, out List<string> entries))
                {
                    EditorUtility.DisplayDialog("解析失败", "无法从文件中解析枚举定义", "确定");
                    return;
                }

                namespaceValue = ns;
                enumName = parsedEnumName;
                enumEntryList.Clear();
                enumEntryList.AddRange(entries);
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
                EditorUtility.DisplayDialog("生成成功", $"已生成\n{filePath}", "确定");
                SavePrefs();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("生成失败", ex.Message, "确定");
            }
        }

        private string BuildEnumFileContent()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"namespace {namespaceValue}");
            sb.AppendLine("{");
            sb.AppendLine($"    public enum {enumName}");
            sb.AppendLine("    {");

            for (int i = 0; i < enumEntryList.Count; i++)
            {
                string entry = enumEntryList[i].Trim();
                string suffix = i < enumEntryList.Count - 1 ? "," : string.Empty;
                sb.AppendLine($"        {entry}{suffix}");
            }

            sb.AppendLine("    }");
            sb.Append("}");
            return sb.ToString();
        }

        private string GetOutputFilePath()
        {
            string folder = outputFolder.Replace('\\', '/').TrimEnd('/');
            return $"{folder}/{enumName}.cs";
        }

        private bool CanGenerate(out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(enumName))
            {
                error = "枚举类型名不能为空";
                return false;
            }

            if (!IsValidIdentifier(enumName))
            {
                error = "枚举类型名不是合法的 C# 标识符";
                return false;
            }

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

            if (enumEntryList.Count == 0)
            {
                error = "请至少添加一个枚举成员";
                return false;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < enumEntryList.Count; i++)
            {
                string entry = enumEntryList[i]?.Trim();
                if (string.IsNullOrEmpty(entry))
                {
                    error = $"第 {i + 1} 个枚举成员不能为空";
                    return false;
                }

                if (!IsValidIdentifier(entry))
                {
                    error = $"枚举成员 {entry} 不是合法的 C# 标识符";
                    return false;
                }

                if (!seen.Add(entry))
                {
                    error = $"枚举成员 {entry} 重复";
                    return false;
                }
            }

            return true;
        }

        private static bool IsValidIdentifier(string name)
        {
            return !string.IsNullOrEmpty(name) && Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
        }

        private static bool TryParseEnumFile(string content, out string ns, out string parsedEnumName, out List<string> entries)
        {
            ns = DefaultNamespace;
            parsedEnumName = DefaultEnumName;
            entries = new List<string>();

            Match nsMatch = Regex.Match(content, @"namespace\s+([\w\.]+)");
            if (nsMatch.Success)
                ns = nsMatch.Groups[1].Value;

            Match enumMatch = Regex.Match(content, @"public\s+enum\s+(\w+)");
            if (!enumMatch.Success)
                return false;

            parsedEnumName = enumMatch.Groups[1].Value;

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
            enumName = EditorPrefs.GetString(PrefsEnumName, DefaultEnumName);
            namespaceValue = EditorPrefs.GetString(PrefsNamespace, DefaultNamespace);
            outputFolder = EditorPrefs.GetString(PrefsOutputFolder, DefaultOutputFolder);
            smartDebugEnabled = EditorPrefs.GetBool(PrefsSmartDebugEnabled, false);

            debugFontSize = UpdateFsmDebugEditorPrefs.FontSize;
            debugTextColor = UpdateFsmDebugEditorPrefs.TextColor;
            debugScreenPos = UpdateFsmDebugEditorPrefs.ScreenPosition;
            debugLineHeight = UpdateFsmDebugEditorPrefs.LineHeight;

            enumEntryList.Clear();
            string saved = EditorPrefs.GetString(PrefsEntries, string.Empty);
            if (!string.IsNullOrEmpty(saved))
            {
                foreach (string item in saved.Split('|'))
                {
                    if (!string.IsNullOrEmpty(item))
                        enumEntryList.Add(item);
                }
            }
        }

        private void SavePrefs()
        {
            EditorPrefs.SetString(PrefsEnumName, enumName);
            EditorPrefs.SetString(PrefsNamespace, namespaceValue);
            EditorPrefs.SetString(PrefsOutputFolder, outputFolder);
            EditorPrefs.SetBool(PrefsSmartDebugEnabled, smartDebugEnabled);
            EditorPrefs.SetString(PrefsEntries, string.Join("|", enumEntryList));
            ApplySmartDebugSettings();
        }
    }
}
