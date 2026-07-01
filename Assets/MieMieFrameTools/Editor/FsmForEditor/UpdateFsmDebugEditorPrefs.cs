namespace MieMieFrameWork.FSM.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// UpdateFSM 智能调试 EditorPrefs 与 MiMieFSM.Unity 共用同一套键
    /// </summary>
    internal static class UpdateFsmDebugEditorPrefs
    {
        private const string PrefsEnabled = "MiMieFSM.UpdateFsm.SmartDebugEnabled";
        private const string PrefsFontSize = "MiMieFSM.UpdateFsm.DebugFontSize";
        private const string PrefsColorR = "MiMieFSM.UpdateFsm.DebugColorR";
        private const string PrefsColorG = "MiMieFSM.UpdateFsm.DebugColorG";
        private const string PrefsColorB = "MiMieFSM.UpdateFsm.DebugColorB";
        private const string PrefsColorA = "MiMieFSM.UpdateFsm.DebugColorA";
        private const string PrefsPosX = "MiMieFSM.UpdateFsm.DebugPosX";
        private const string PrefsPosY = "MiMieFSM.UpdateFsm.DebugPosY";
        private const string PrefsLineHeight = "MiMieFSM.UpdateFsm.DebugLineHeight";

        /// <summary>
        /// 是否启用智能调试
        /// </summary>
        public static bool Enabled
        {
            get => EditorPrefs.GetInt(PrefsEnabled, 0) == 1;
            set => EditorPrefs.SetInt(PrefsEnabled, value ? 1 : 0);
        }

        /// <summary>
        /// 字体大小
        /// </summary>
        public static int FontSize
        {
            get => EditorPrefs.GetInt(PrefsFontSize, 18);
            set => EditorPrefs.SetInt(PrefsFontSize, Mathf.Clamp(value, 8, 48));
        }

        /// <summary>
        /// 文本颜色
        /// </summary>
        public static Color TextColor
        {
            get
            {
                return new Color(
                    EditorPrefs.GetFloat(PrefsColorR, 0.2f),
                    EditorPrefs.GetFloat(PrefsColorG, 1f),
                    EditorPrefs.GetFloat(PrefsColorB, 0.4f),
                    EditorPrefs.GetFloat(PrefsColorA, 1f));
            }
            set
            {
                EditorPrefs.SetFloat(PrefsColorR, value.r);
                EditorPrefs.SetFloat(PrefsColorG, value.g);
                EditorPrefs.SetFloat(PrefsColorB, value.b);
                EditorPrefs.SetFloat(PrefsColorA, value.a);
            }
        }

        /// <summary>
        /// 屏幕绘制位置
        /// </summary>
        public static Vector2 ScreenPosition
        {
            get
            {
                return new Vector2(
                    EditorPrefs.GetFloat(PrefsPosX, 12f),
                    EditorPrefs.GetFloat(PrefsPosY, 12f));
            }
            set
            {
                EditorPrefs.SetFloat(PrefsPosX, value.x);
                EditorPrefs.SetFloat(PrefsPosY, value.y);
            }
        }

        /// <summary>
        /// 行高
        /// </summary>
        public static float LineHeight
        {
            get => EditorPrefs.GetFloat(PrefsLineHeight, 24f);
            set => EditorPrefs.SetFloat(PrefsLineHeight, Mathf.Max(12f, value));
        }

        /// <summary>
        /// 刷新运行时调试层
        /// </summary>
        public static void RefreshRuntimeOverlay()
        {
            if (!EditorApplication.isPlaying)
                return;

            Type overlayType = Type.GetType("MiMieFSM.Unity.UpdateFsmDebugOverlay, MiMieFSM.Unity");
            overlayType?.GetMethod("RefreshOverlay")?.Invoke(null, null);
        }
    }
}
