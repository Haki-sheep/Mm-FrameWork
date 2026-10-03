#if UNITY_EDITOR
using System;
using System.IO;
using FakeUnity.Development;
using FakeUnity.Preservation;
using UnityEditor;
using UnityEngine;

namespace FakeUnityCLI.EditorBridge
{
    public sealed class FakeUnityCliWindow : EditorWindow
    {
        private static readonly string[] Labels = { "继承内置（自动）", "自动", "现场保护", "自由开发" };
        private static readonly string[] Modes = { "", "auto", "preserve", "free" };
        private string _root;
        private DevelopmentPreferencesObservation _settings;
        private DevelopmentPreferencesResolution _effective;
        private PreservationObservation _guard;
        private int _mode;
        private bool _dirty, _externalChange;
        private string _stamp, _message;
        private double _nextRefresh;
        private Vector2 _scroll;

        [MenuItem("Window/FakeUnityCLI")]
        public static void ShowWindow() { GetWindow<FakeUnityCliWindow>("FakeUnityCLI"); }

        private void OnEnable()
        {
            minSize = new Vector2(460, 530);
            _root = DevelopmentPreferencesStore.NormalizeProjectRoot(Path.GetDirectoryName(Application.dataPath));
            Refresh(true);
            EditorApplication.update += Tick;
        }
        private void OnDisable() { EditorApplication.update -= Tick; }
        private void Tick()
        {
            if (EditorApplication.timeSinceStartup < _nextRefresh) return;
            _nextRefresh = EditorApplication.timeSinceStartup + 1;
            Refresh(false); Repaint();
        }

        private void Refresh(bool discardDraft)
        {
            _settings = DevelopmentPreferencesStore.Read(_root);
            _effective = DevelopmentPreferencesStore.Resolve(_root);
            _guard = EditorPreservationGuard.Read(_root);
            var stamp = _settings.Status + "|" + _settings.Error;
            if (_settings.Settings != null)
            {
                stamp += "|" + _settings.Settings.default_mode;
            }
            if (_dirty && !discardDraft)
            {
                if (_stamp != stamp) _externalChange = true;
                return;
            }
            _stamp = stamp; _dirty = false; _externalChange = false;
            _mode = Index(_settings.Settings == null ? "" : _settings.Settings.default_mode);
        }

        private void OnGUI()
        {
            if (_settings == null) Refresh(true);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("FakeUnityCLI 开发默认设置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("设置保存在当前工程 .fuc/development-settings.json，供 CLI 与新 session 自动读取。此本机配置应由工程 Git 忽略，不提交版本控制。命令参数和任务环境变量仍可覆盖默认值。", MessageType.Info);
            EditorGUILayout.LabelField("当前工程", _root, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            _mode = EditorGUILayout.Popup("当前工程默认模式", _mode, Labels);
            if (EditorGUI.EndChangeCheck()) _dirty = true;
            EditorGUILayout.HelpBox("自动：尽量保持运行，按操作判断允许或延后。\n现场保护：默认拒绝改变现场的操作。\n自由开发：允许影响运行；必要时 CLI 可停止 Play，等待稳定 Edit 后执行。", MessageType.None);
            EditorGUILayout.HelpBox("保存只改变后续操作的默认策略，不会切换 Play、触发编译或修改 Unity 脚本变更偏好。选择“现场保护”不会建立共享保护记录；跨 session 强制保留现场仍使用 guard。", MessageType.Info);

            if (_externalChange) EditorGUILayout.HelpBox("设置已被其他进程修改，已保留你的未保存选择。请重新加载后再保存。", MessageType.Warning);
            if (_settings.Error != null) EditorGUILayout.HelpBox("设置读取失败，按现场保护处理：" + _settings.Error + "\n为保留现有数据，不能直接覆盖此文件。", MessageType.Error);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!_dirty || _externalChange || _settings.Error != null))
                    if (GUILayout.Button("保存默认设置")) Save();
                if (GUILayout.Button(_dirty ? "放弃修改并重新加载" : "重新加载")) { _message = null; Refresh(true); }
            }
            if (!String.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("已保存的有效默认", Labels[Index(_effective.Mode)] + " · " + Source(_effective.Source));
            if (_guard.BlocksWrites)
                EditorGUILayout.HelpBox("共享现场保护生效（" + _guard.Status + "）：所有开发模式均不能绕过。\n" + (_guard.Record == null ? _guard.Reason : _guard.Record.owner + " · " + _guard.Record.reason + "\nGuard: " + _guard.Record.guard_id), MessageType.Warning);
            else EditorGUILayout.LabelField("共享现场保护", "未生效");
            EditorGUILayout.LabelField("本机设置文件", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(_settings.Path ?? "不可用", EditorStyles.wordWrappedLabel, GUILayout.Height(46));
            EditorGUILayout.EndScrollView();
        }

        private void Save()
        {
            // Refresh before saving prevents a stale open window from overwriting observed external edits.
            Refresh(false);
            if (_externalChange || _settings.Error != null) return;
            try
            {
                DevelopmentPreferencesStore.SaveDefault(_mode == 0 ? null : Modes[_mode], _root, _settings);
                _message = "已保存。新的 CLI 操作将读取此默认值；当前运行状态保持不变。";
                Refresh(true);
            }
            catch (Exception ex) { _message = "保存未完成，已保留未保存选择：" + ex.Message; Refresh(false); }
        }
        private static int Index(string mode) { var index = Array.IndexOf(Modes, mode); return index < 0 ? 0 : index; }
        private static string Source(string source)
        {
            switch (source)
            {
                case "project-default": return "工程本机默认";
                case "settings-error": return "设置异常，保守保护";
                default: return "内置默认";
            }
        }
    }
}
#endif

