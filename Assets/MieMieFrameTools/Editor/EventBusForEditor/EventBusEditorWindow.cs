using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using MieMieFrameWork;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.EventBusForEditor
{
    /// <summary>
    /// EventBus 运行时监控与代码扫描
    /// </summary>
    public class EventBusEditorWindow : EditorWindow
    {
        /// <summary>
        /// 当前页签偏好键
        /// </summary>
        private const string PrefsTab = "EventBusEditor.Tab";

        /// <summary>
        /// 扫描时跳过的框架内部路径片段
        /// </summary>
        private const string FrameworkInternalPath = "/D_EventCenter/";

        /// <summary>
        /// 页签类型
        /// </summary>
        private enum E_Tab
        {
            RuntimeMonitor,
            CodeScan
        }

        /// <summary>
        /// 当前页签
        /// </summary>
        private E_Tab currentTab = E_Tab.RuntimeMonitor;

        /// <summary>
        /// 监控滚动位置
        /// </summary>
        private Vector2 monitorScroll;

        /// <summary>
        /// 扫描滚动位置
        /// </summary>
        private Vector2 scanScroll;

        /// <summary>
        /// 按事件 Key 分组的扫描结果
        /// </summary>
        private readonly List<EventScanGroup> scanGroupList = new();

        /// <summary>
        /// 是否已扫描
        /// </summary>
        private bool hasScanned;

        /// <summary>
        /// 打开窗口
        /// </summary>
        [MenuItem("Tools/MieMieFrameWork/Event Bus")]
        public static void Open()
        {
            var window = GetWindow<EventBusEditorWindow>("EventBus");
            window.LoadPrefs();
        }

        /// <summary>
        /// 启用时注册刷新
        /// </summary>
        private void OnEnable()
        {
            LoadPrefs();
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// 禁用时保存偏好
        /// </summary>
        private void OnDisable()
        {
            SavePrefs();
            EditorApplication.update -= OnEditorUpdate;
        }

        /// <summary>
        /// Play 模式下刷新监控
        /// </summary>
        private void OnEditorUpdate()
        {
            if (currentTab == E_Tab.RuntimeMonitor && Application.isPlaying)
                Repaint();
        }

        /// <summary>
        /// 绘制窗口
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            currentTab = (E_Tab)GUILayout.Toolbar((int)currentTab, new[] { "运行时监控", "代码扫描" });
            EditorGUILayout.Space(4);

            switch (currentTab)
            {
                case E_Tab.RuntimeMonitor:
                    DrawRuntimeMonitorTab();
                    break;
                case E_Tab.CodeScan:
                    DrawCodeScanTab();
                    break;
            }
        }

        /// <summary>
        /// 绘制运行时监控页
        /// </summary>
        private void DrawRuntimeMonitorTab()
        {
            EditorGUILayout.LabelField("全局总线运行时监控", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("进入 Play 模式后查看监听数据", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"已注册事件槽: {MmGlobalEventBus.Bus.GetEventCount()}");
            EditorGUILayout.LabelField(
                $"最近触发: {TypedEventBusTrace.LastKeyName}  时间: {TypedEventBusTrace.LastTime:F2}s");
            EditorGUILayout.Space(4);

            monitorScroll = EditorGUILayout.BeginScrollView(monitorScroll);
            List<EventKeyInfo> keyInfoList = CollectEventKeyFields();
            for (int i = 0; i < keyInfoList.Count; i++)
            {
                EventKeyInfo keyInfo = keyInfoList[i];
                int count = MmGlobalEventBus.Bus.GetListenerCountByName(keyInfo.KeyName);
                var style = count > 0 ? EditorStyles.boldLabel : EditorStyles.label;
                Color prevColor = GUI.color;
                if (count == 0)
                    GUI.color = Color.gray;

                EditorGUILayout.LabelField(keyInfo.KeyName, $"{keyInfo.OwnerType.Name}.{keyInfo.FieldName}  监听 {count}", style);
                GUI.color = prevColor;
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制代码扫描页
        /// </summary>
        private void DrawCodeScanTab()
        {
            EditorGUILayout.LabelField("业务代码 EventBus 引用", EditorStyles.boldLabel);

            if (GUILayout.Button("扫描项目", GUILayout.Height(28)))
                ScanProject();

            if (!hasScanned)
            {
                EditorGUILayout.HelpBox("按事件 Key 分组展示 已自动跳过 D_EventCenter 框架内部实现", MessageType.Info);
                return;
            }

            int totalCount = 0;
            for (int i = 0; i < scanGroupList.Count; i++)
                totalCount += scanGroupList[i].RecordList.Count;

            EditorGUILayout.LabelField($"业务引用: {totalCount} 条  事件 Key: {scanGroupList.Count} 个", EditorStyles.miniLabel);
            EditorGUILayout.Space(4);

            scanScroll = EditorGUILayout.BeginScrollView(scanScroll);
            for (int i = 0; i < scanGroupList.Count; i++)
                DrawScanGroup(scanGroupList[i]);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 绘制单个事件 Key 分组
        /// </summary>
        private void DrawScanGroup(EventScanGroup group)
        {
            EditorGUILayout.LabelField(group.KeyDisplay, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            for (int i = 0; i < group.RecordList.Count; i++)
                DrawScanRecord(group.RecordList[i]);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(6);
        }

        /// <summary>
        /// 绘制单条扫描记录
        /// </summary>
        private void DrawScanRecord(EventScanRecord record)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(record.Kind, GUILayout.Width(88));
                string fileName = Path.GetFileName(record.ScriptPath);
                if (GUILayout.Button($"{fileName}:{record.Line}", EditorStyles.linkLabel))
                {
                    var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(record.ScriptPath);
                    if (mono != null)
                        AssetDatabase.OpenAsset(mono, record.Line);
                }

                EditorGUILayout.LabelField(record.MemberHint, EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// 扫描项目脚本
        /// </summary>
        private void ScanProject()
        {
            scanGroupList.Clear();
            hasScanned = true;

            Dictionary<string, string> keyLookupDict = BuildEventKeyLookupDict();
            var groupDict = new Dictionary<string, EventScanGroup>();

            var callPattern = new Regex(
                @"(?:MmGlobalEventBus\.Bus|(?:TypedEventBus|EventBus)\s+\w+|\.Bus)\.(Subscribe|Publish|Unsubscribe)(?:<[^>]*>)?\s*\(\s*([A-Za-z_][\w]*(?:\.[A-Za-z_][\w]*)*)",
                RegexOptions.Compiled);
            var listenPattern = new Regex(
                @"\bListen(?:Local)?(?:<[^>]*>)?\s*\(\s*(?:TypedEventBus\s+\w+\s*,\s*)?([A-Za-z_][\w]*(?:\.[A-Za-z_][\w]*)*)",
                RegexOptions.Compiled);

            string[] scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
            for (int i = 0; i < scriptGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
                if (!path.EndsWith(".cs") || path.Contains("/Editor/") || path.Contains(FrameworkInternalPath))
                    continue;

                string[] lines;
                try
                {
                    lines = File.ReadAllLines(path);
                }
                catch
                {
                    continue;
                }

                string memberHint = Path.GetFileNameWithoutExtension(path);
                for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
                {
                    string line = lines[lineIndex];
                    Match callMatch = callPattern.Match(line);
                    if (callMatch.Success)
                    {
                        AddScanRecord(groupDict, keyLookupDict, callMatch.Groups[2].Value, callMatch.Groups[1].Value, path, lineIndex + 1, memberHint);
                        continue;
                    }

                    Match listenMatch = listenPattern.Match(line);
                    if (listenMatch.Success)
                        AddScanRecord(groupDict, keyLookupDict, listenMatch.Groups[1].Value, "Listen", path, lineIndex + 1, memberHint);
                }
            }

            scanGroupList.AddRange(groupDict.Values);
            scanGroupList.Sort((a, b) => string.Compare(a.KeyDisplay, b.KeyDisplay, StringComparison.Ordinal));
            for (int i = 0; i < scanGroupList.Count; i++)
            {
                scanGroupList[i].RecordList.Sort((a, b) =>
                {
                    int kindCompare = string.Compare(a.Kind, b.Kind, StringComparison.Ordinal);
                    return kindCompare != 0 ? kindCompare : a.Line.CompareTo(b.Line);
                });
            }
        }

        /// <summary>
        /// 添加扫描记录到分组
        /// </summary>
        private static void AddScanRecord(
            Dictionary<string, EventScanGroup> groupDict,
            Dictionary<string, string> keyLookupDict,
            string keyToken,
            string kind,
            string scriptPath,
            int line,
            string memberHint)
        {
            string keyDisplay = ResolveKeyDisplay(keyToken, keyLookupDict);
            if (!groupDict.TryGetValue(keyDisplay, out EventScanGroup group))
            {
                group = new EventScanGroup(keyDisplay);
                groupDict.Add(keyDisplay, group);
            }

            group.RecordList.Add(new EventScanRecord(kind, scriptPath, line, memberHint));
        }

        /// <summary>
        /// 解析 Key 展示名
        /// </summary>
        private static string ResolveKeyDisplay(string keyToken, Dictionary<string, string> keyLookupDict)
        {
            if (keyLookupDict.TryGetValue(keyToken, out string keyName))
                return keyName;

            return keyToken;
        }

        /// <summary>
        /// 构建 EventKey 查找表
        /// </summary>
        private static Dictionary<string, string> BuildEventKeyLookupDict()
        {
            var lookupDict = new Dictionary<string, string>();
            List<EventKeyInfo> keyInfoList = CollectEventKeyFields();
            for (int i = 0; i < keyInfoList.Count; i++)
            {
                EventKeyInfo keyInfo = keyInfoList[i];
                string token = $"{keyInfo.OwnerType.Name}.{keyInfo.FieldName}";
                if (!lookupDict.ContainsKey(token))
                    lookupDict.Add(token, keyInfo.KeyName);
            }

            return lookupDict;
        }

        /// <summary>
        /// 收集项目中 EventKey 静态字段
        /// </summary>
        private static List<EventKeyInfo> CollectEventKeyFields()
        {
            var resultList = new List<EventKeyInfo>();
            Assembly[] assemblyList = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblyList.Length; i++)
            {
                Type[] typeList;
                try
                {
                    typeList = assemblyList[i].GetTypes();
                }
                catch
                {
                    continue;
                }

                for (int j = 0; j < typeList.Length; j++)
                {
                    Type type = typeList[j];
                    FieldInfo[] fieldList = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                    for (int k = 0; k < fieldList.Length; k++)
                    {
                        FieldInfo field = fieldList[k];
                        if (!typeof(IEventKeyName).IsAssignableFrom(field.FieldType))
                            continue;

                        if (field.GetValue(null) is IEventKeyName eventKey)
                            resultList.Add(new EventKeyInfo(type, field.Name, eventKey.Name));
                    }
                }
            }

            resultList.Sort((a, b) => string.Compare(a.KeyName, b.KeyName, StringComparison.Ordinal));
            return resultList;
        }

        /// <summary>
        /// 读取页签偏好
        /// </summary>
        private void LoadPrefs()
        {
            currentTab = (E_Tab)EditorPrefs.GetInt(PrefsTab, (int)E_Tab.RuntimeMonitor);
        }

        /// <summary>
        /// 保存页签偏好
        /// </summary>
        private void SavePrefs()
        {
            EditorPrefs.SetInt(PrefsTab, (int)currentTab);
        }

        /// <summary>
        /// EventKey 字段信息
        /// </summary>
        private sealed class EventKeyInfo
        {
            /// <summary>
            /// 所属类型
            /// </summary>
            public Type OwnerType { get; }

            /// <summary>
            /// 字段名
            /// </summary>
            public string FieldName { get; }

            /// <summary>
            /// Key 名称
            /// </summary>
            public string KeyName { get; }

            /// <summary>
            /// 构造 Key 信息
            /// </summary>
            public EventKeyInfo(Type ownerType, string fieldName, string keyName)
            {
                OwnerType = ownerType;
                FieldName = fieldName;
                KeyName = keyName;
            }
        }

        /// <summary>
        /// 扫描分组
        /// </summary>
        private sealed class EventScanGroup
        {
            /// <summary>
            /// Key 展示名
            /// </summary>
            public string KeyDisplay { get; }

            /// <summary>
            /// 组内记录
            /// </summary>
            public List<EventScanRecord> RecordList { get; } = new();

            /// <summary>
            /// 构造扫描分组
            /// </summary>
            public EventScanGroup(string keyDisplay)
            {
                KeyDisplay = keyDisplay;
            }
        }

        /// <summary>
        /// 扫描记录
        /// </summary>
        private sealed class EventScanRecord
        {
            /// <summary>
            /// 引用类型
            /// </summary>
            public string Kind { get; }

            /// <summary>
            /// 脚本路径
            /// </summary>
            public string ScriptPath { get; }

            /// <summary>
            /// 行号
            /// </summary>
            public int Line { get; }

            /// <summary>
            /// 脚本名提示
            /// </summary>
            public string MemberHint { get; }

            /// <summary>
            /// 构造扫描记录
            /// </summary>
            public EventScanRecord(string kind, string scriptPath, int line, string memberHint)
            {
                Kind = kind;
                ScriptPath = scriptPath;
                Line = line;
                MemberHint = memberHint;
            }
        }
    }
}
