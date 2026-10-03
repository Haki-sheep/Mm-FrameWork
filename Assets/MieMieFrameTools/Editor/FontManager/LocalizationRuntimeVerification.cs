using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using MieMieFrameWork.Business;
using MieMieFrameWork.Data.Luban.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MieMieFrameWork.Localization.FontManagement
{
    /// <summary>
    /// 原生编辑器中检查语言数据与动态 UI 绑定契约 不修改用户场景
    /// </summary>
    public static class LocalizationRuntimeVerification
    {
        /// <summary>
        /// 批处理异步验证入口
        /// </summary>
        public static void RunBatch()
        {
            RunAsync().Forget(Error => { Debug.LogException(Error); EditorApplication.Exit(1); });
        }

        /// <summary>
        /// 读取真实数据与字体 在预览场景显式调用绑定生命周期并检查切换
        /// </summary>
        private static async UniTask RunAsync()
        {
            if (GameHub.TryGet<ILocalizationService>(out var ExistingService))
                throw new InvalidOperationException("验证需要无业务服务的独立批处理会话");
            var ResultList = new List<string>();
            var Catalog = ScriptableObject.CreateInstance<LocalizationFontCatalog>();
            var PreviewScene = EditorSceneManager.NewPreviewScene();
            LocalizationService Service = null;
            LocalizedTMPText Binding = null;
            GameObject Root = null;
            try
            {
                var ChineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LocalizationProjectSetup.ChineseFontPath);
                var EnglishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LocalizationProjectSetup.EnglishFontPath);
                var CatalogObject = new SerializedObject(Catalog);
                var EntryList = CatalogObject.FindProperty("entryList");
                EntryList.arraySize = 2;
                Configure(EntryList.GetArrayElementAtIndex(0), "zh-CN", ChineseFont);
                Configure(EntryList.GetArrayElementAtIndex(1), "en", EnglishFont);
                CatalogObject.ApplyModifiedPropertiesWithoutUndo();
                Service = new LocalizationService(new LubanLocalizationTextSource(),
                    new YooAssetLocalizationFontLoader(Catalog), new MemoryPreferences());
                GameHub.Register<ILocalizationService>(Service);
                await Service.InitAsync("zh-CN", CancellationToken.None);
                Check(Service.GetText("common.start") == "开始游戏", "UnityWebRequest 读取真实 StreamingAssets 语言表", ResultList);
                Root = new GameObject("LocalizationTransientUI", typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(Root, PreviewScene);
                var Text = Root.AddComponent<TextMeshProUGUI>();
                Text.font = EnglishFont;
                Text.fontSharedMaterial = EnglishFont.material;
                Binding = Root.AddComponent<LocalizedTMPText>();
                var BindingObject = new SerializedObject(Binding);
                BindingObject.FindProperty("textKey").stringValue = "common.start";
                BindingObject.ApplyModifiedPropertiesWithoutUndo();
                Invoke(Binding, "Awake");
                Invoke(Binding, "Start");
                Check(Text.text == "开始游戏" && Text.font == ChineseFont, "动态创建文本从 GameHub 绑定 无场景宿主引用", ResultList);
                await Service.SetLocaleAsync("en");
                Check(Text.text == "Start Game" && Text.font == EnglishFont, "统一 EventKey 驱动 TMP 主字体与文案切换", ResultList);
                Invoke(Binding, "OnDisable");
                await Service.SetLocaleAsync("zh-CN");
                Check(Text.text == "Start Game" && Text.font == EnglishFont, "停用绑定保留原显示 不接收通知", ResultList);
                Invoke(Binding, "OnEnable");
                Check(Text.text == "开始游戏" && Text.font == ChineseFont, "重新启用刷新为当前语言", ResultList);
                BindingObject.Update();
                BindingObject.FindProperty("textKey").stringValue = "common.coins";
                BindingObject.ApplyModifiedPropertiesWithoutUndo();
                Binding.SetArguments(1234);
                Check(Text.text == "金币 1,234", "动态参数按语言文化格式化", ResultList);
                Invoke(Binding, "OnDisable");
                Invoke(Binding, "OnDestroy");
                Check(Text.font == EnglishFont, "移除绑定前恢复原字体并归还资源租约", ResultList);
                UnityEngine.Object.DestroyImmediate(Binding);
                Binding = null;
                ResultList.Add($"PASS {ResultList.Count} 项原生编辑器契约验证 不代表 PlayMode 全流程");
                Directory.CreateDirectory("Logs");
                File.WriteAllLines("Logs/Localization-native-verification.txt", ResultList);
                Debug.Log(string.Join("\n", ResultList));
            }
            finally
            {
                if (Binding != null)
                    Invoke(Binding, "OnDisable");
                if (Root != null)
                    UnityEngine.Object.DestroyImmediate(Root);
                GameHub.Unregister<ILocalizationService>();
                Service?.Dispose();
                UnityEngine.Object.DestroyImmediate(Catalog);
                EditorSceneManager.ClosePreviewScene(PreviewScene);
            }
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// 配置瞬态语言主字体 不持久化资产
        /// </summary>
        private static void Configure(SerializedProperty Entry, string Locale, TMP_FontAsset Font)
        {
            Entry.FindPropertyRelative("locale").stringValue = Locale;
            Entry.FindPropertyRelative("style").stringValue = "Body";
            Entry.FindPropertyRelative("font").objectReferenceValue = Font;
        }

        /// <summary>
        /// 显式调用编辑器中不会自动执行的非 ExecuteAlways 组件生命周期
        /// </summary>
        private static void Invoke(LocalizedTMPText Binding, string Method)
        {
            typeof(LocalizedTMPText).GetMethod(Method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(Binding, null);
        }

        /// <summary>
        /// 记录成功条件 失败立即中断
        /// </summary>
        private static void Check(bool Condition, string Description, List<string> ResultList)
        {
            if (!Condition)
                throw new InvalidOperationException($"FAIL {Description}");
            ResultList.Add($"PASS {Description}");
        }

        /// <summary>
        /// 验证会话设置不写入用户 PlayerPrefs
        /// </summary>
        private sealed class MemoryPreferences : ILocalizationPreferences
        {
            /// <summary> 验证会话选择的语言 </summary>
            private string locale;

            /// <summary>
            /// 返回内存设置或明确的默认语言
            /// </summary>
            public string GetLocale(string DefaultLocale) => locale ?? DefaultLocale;

            /// <summary>
            /// 只更新验证会话状态
            /// </summary>
            public void SetLocale(string Locale) => locale = Locale;
        }
    }
}
