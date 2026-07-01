using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MieMieFrameWork;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.EventCenterEditor
{
    public class EventCenterEditorWindow : EditorWindow
    {
        private const string DefaultOutputPath = "Assets/MieMieFrameTools/Scripts/FrameBase/1.5 Tools/EventCenter/EventConstKey.cs";
        private const string DefaultNamespace = "MieMieFrameWork";
        private const string DefaultEnumName = "E_EventConstKey";
        private const string PrefsTab = "EventCenterEditor.Tab";
        private const string PrefsEntries = "EventCenterEditor.Entries";

        private enum E_Tab
        {
            EnumGenerator,
            RuntimeMonitor,
            CodeScan
        }

        private E_Tab currentTab = E_Tab.EnumGenerator;
        private readonly List<string> enumEntryList = new();
        private Vector2 enumScroll;
        private Vector2 monitorScroll;
        private Vector2 scanScroll;

        private readonly List<EventScanRecord> scanResultList = new();
        private bool hasScanned;

        [MenuItem("Tools/MieMieFrameWork/事件中心")]
        public static void Open()
        {
            var window = GetWindow<EventCenterEditorWindow>("事件中心");
            window.LoadPrefs();
            window.LoadEnumFromFile();
        }

        private void OnEnable()
        {
            LoadPrefs();
            LoadEnumFromFile();
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            SavePrefs();
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (currentTab == E_Tab.RuntimeMonitor && Application.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            currentTab = (E_Tab)GUILayout.Toolbar((int)currentTab, new[] { "枚举生成", "运行时监控", "代码扫描" });
            EditorGUILayout.Space(4);

            switch (currentTab)
            {
                case E_Tab.EnumGenerator:
                    DrawEnumGeneratorTab();
                    break;
                case E_Tab.RuntimeMonitor:
                    DrawRuntimeMonitorTab();
                    break;
                case E_Tab.CodeScan:
                    DrawCodeScanTab();
                    break;
            }
        }

        #region 枚举生成

        private void DrawEnumGeneratorTab()
        {
            EditorGUILayout.LabelField("E_EventConstKey 枚举生成器", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("输出路径", DefaultOutputPath, EditorStyles.miniLabel);
            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("从文件加载", GUILayout.Height(24)))
                    LoadEnumFromFile();

                if (GUILayout.Button("添加成员", GUILayout.Height(24)))
                    enumEntryList.Add(string.Empty);
            }

            enumScroll = EditorGUILayout.BeginScrollView(enumScroll, GUILayout.MinHeight(200));
            if (enumEntryList.Count == 0)
            {
                EditorGUILayout.LabelField("暂无枚举成员 点击添加成员开始", EditorStyles.centeredGreyMiniLabel);
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

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(8);

            using (new EditorGUI.DisabledScope(!CanGenerateEnum(out _)))
            {
                if (GUILayout.Button("生成枚举文件", GUILayout.Height(32)))
                    GenerateEnumFile();
            }
        }

        private void LoadEnumFromFile()
        {
            if (!File.Exists(DefaultOutputPath))
                return;

            try
            {
                string content = File.ReadAllText(DefaultOutputPath);
                if (!TryParseEnumFile(content, out _, out _, out List<string> entries))
                    return;

                enumEntryList.Clear();
                enumEntryList.AddRange(entries);
                Repaint();
            }
            catch (Exception ex)
            {
                Debug.LogError($"读取枚举文件失败: {ex.Message}");
            }
        }

        private void GenerateEnumFile()
        {
            if (!CanGenerateEnum(out string error))
            {
                EditorUtility.DisplayDialog("无法生成", error, "确定");
                return;
            }

            if (!EditorUtility.DisplayDialog("确认生成", $"将覆盖写入:\n{DefaultOutputPath}", "生成", "取消"))
                return;

            string directory = Path.GetDirectoryName(DefaultOutputPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            try
            {
                File.WriteAllText(DefaultOutputPath, BuildEnumFileContent(), Encoding.UTF8);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("生成成功", $"已生成:\n{DefaultOutputPath}", "确定");
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
            sb.AppendLine($"namespace {DefaultNamespace}");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// 事件常量枚举");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public enum {DefaultEnumName}");
            sb.AppendLine("    {");

            for (int i = 0; i < enumEntryList.Count; i++)
            {
                string entry = enumEntryList[i].Trim();
                string suffix = i < enumEntryList.Count - 1 ? "," : string.Empty;
                sb.AppendLine($"        {entry}{suffix}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private bool CanGenerateEnum(out string error)
        {
            error = string.Empty;
            if (enumEntryList.Count == 0)
            {
                error = "请至少添加一个枚举成员";
                return false;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
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

        #endregion

        #region 运行时监控

        private void DrawRuntimeMonitorTab()
        {
            EditorGUILayout.LabelField("全局总线运行时监控", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play 模式后查看监听数据", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"已注册事件种类: {EventCenter.GetEventCount()}");
            EditorGUILayout.LabelField($"最近触发: {EventBus.LastTriggeredKey}  时间: {EventBus.LastTriggeredTime:F2}s");
            EditorGUILayout.Space(4);

            monitorScroll = EditorGUILayout.BeginScrollView(monitorScroll);
            foreach (E_EventConstKey eventKey in Enum.GetValues(typeof(E_EventConstKey)))
            {
                int count = EventCenter.GetListenerCount(eventKey);
                var style = count > 0 ? EditorStyles.boldLabel : EditorStyles.label;
                var prevColor = GUI.color;
                if (count == 0)
                    GUI.color = Color.gray;

                EditorGUILayout.LabelField($"{eventKey}", $"监听数: {count}", style);
                GUI.color = prevColor;
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region 代码扫描

        private void DrawCodeScanTab()
        {
            EditorGUILayout.LabelField("EventCenter 代码引用扫描", EditorStyles.boldLabel);

            if (GUILayout.Button("扫描项目", GUILayout.Height(28)))
                ScanProject();

            if (!hasScanned)
            {
                EditorGUILayout.HelpBox("点击扫描项目 查找 AddEventListener TriggerEvent RemoveListener 引用", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"扫描结果: {scanResultList.Count} 条", EditorStyles.miniLabel);
            EditorGUILayout.Space(4);

            scanScroll = EditorGUILayout.BeginScrollView(scanScroll);
            var groupedDict = new Dictionary<E_EventConstKey, List<EventScanRecord>>();
            var unknownList = new List<EventScanRecord>();

            foreach (EventScanRecord record in scanResultList)
            {
                if (record.EventKey.HasValue)
                {
                    if (!groupedDict.ContainsKey(record.EventKey.Value))
                        groupedDict[record.EventKey.Value] = new List<EventScanRecord>();
                    groupedDict[record.EventKey.Value].Add(record);
                }
                else
                {
                    unknownList.Add(record);
                }
            }

            foreach (var pair in groupedDict)
            {
                EditorGUILayout.LabelField(pair.Key.ToString(), EditorStyles.boldLabel);
                foreach (EventScanRecord record in pair.Value)
                    DrawScanRecord(record);
                EditorGUILayout.Space(4);
            }

            if (unknownList.Count > 0)
            {
                EditorGUILayout.LabelField("未识别事件 Key", EditorStyles.boldLabel);
                foreach (EventScanRecord record in unknownList)
                    DrawScanRecord(record);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawScanRecord(EventScanRecord record)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"[{record.Kind}]", GUILayout.Width(72));
                if (GUILayout.Button($"{record.ScriptPath}:{record.Line}", EditorStyles.linkLabel))
                {
                    var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(record.ScriptPath);
                    if (mono != null)
                        AssetDatabase.OpenAsset(mono, record.Line);
                }
            }
        }

        private void ScanProject()
        {
            scanResultList.Clear();
            string assetsPath = Application.dataPath.Replace('\\', '/');
            string[] files = Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories);

            var addPattern = new Regex(@"EventCenter\.AddEventListener(?:<[^>]+>)?\s*\(\s*E_EventConstKey\.(\w+)", RegexOptions.Compiled);
            var triggerPattern = new Regex(@"EventCenter\.TriggerEvent(?:<[^>]+>)?\s*\(\s*E_EventConstKey\.(\w+)", RegexOptions.Compiled);
            var removePattern = new Regex(@"EventCenter\.RemoveListener(?:<[^>]+>)?\s*\(\s*E_EventConstKey\.(\w+)", RegexOptions.Compiled);

            foreach (string absolutePath in files)
            {
                string assetPath = "Assets" + absolutePath.Substring(assetsPath.Length).Replace('\\', '/');
                if (assetPath.Contains("/Editor/") || assetPath.Contains("/Plugins/"))
                    continue;

                string[] lines;
                try
                {
                    lines = File.ReadAllLines(absolutePath);
                }
                catch
                {
                    continue;
                }

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    TryMatchScanLine(addPattern, line, assetPath, i + 1, "监听", scanResultList);
                    TryMatchScanLine(triggerPattern, line, assetPath, i + 1, "触发", scanResultList);
                    TryMatchScanLine(removePattern, line, assetPath, i + 1, "移除", scanResultList);
                }
            }

            hasScanned = true;
            Repaint();
        }

        private static void TryMatchScanLine(Regex pattern, string line, string scriptPath, int lineNumber, string kind, List<EventScanRecord> resultList)
        {
            Match match = pattern.Match(line);
            if (!match.Success)
                return;

            E_EventConstKey? eventKey = null;
            if (Enum.TryParse(match.Groups[1].Value, out E_EventConstKey parsed))
                eventKey = parsed;

            resultList.Add(new EventScanRecord
            {
                EventKey = eventKey,
                Kind = kind,
                ScriptPath = scriptPath,
                Line = lineNumber
            });
        }

        #endregion

        #region 工具方法

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
            currentTab = (E_Tab)EditorPrefs.GetInt(PrefsTab, 0);
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
            EditorPrefs.SetInt(PrefsTab, (int)currentTab);
            EditorPrefs.SetString(PrefsEntries, string.Join("|", enumEntryList));
        }

        #endregion

        private class EventScanRecord
        {
            public E_EventConstKey? EventKey;
            public string Kind;
            public string ScriptPath;
            public int Line;
        }
    }
}
