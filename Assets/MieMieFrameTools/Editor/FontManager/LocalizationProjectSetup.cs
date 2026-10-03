using System;
using System.IO;
using MieMieFrameWork.Data.Luban.Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MieMieFrameWork.Localization.FontManagement
{
    /// <summary>
    /// 多语言接入操作通过 Unity API 创建配置并绑定场景组件
    /// </summary>
    public static class LocalizationProjectSetup
    {
        /// <summary> 默认字体配置资产路径 </summary>
        public const string CatalogPath = FontManagerPaths.Profiles + "/LocalizationFonts.asset";
        /// <summary> 本地已有的中文 TMP 字体 </summary>
        public const string ChineseFontPath = "Assets/MieMieFrameTools/ADefaultRes/Arts/FrontArt/ZLabsRoundPix_16px_M_CN SDF.asset";
        /// <summary> TMP 自带的英文主字体 </summary>
        public const string EnglishFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        /// <summary>
        /// 复用已有字体创建首版简中与英文映射 不覆盖用户配置
        /// </summary>
        public static void CreateDefaultCatalog()
        {
            var Catalog = GetOrCreateDefaultCatalog();
            Selection.activeObject = Catalog;
            EditorGUIUtility.PingObject(Catalog);
        }

        /// <summary>
        /// 返回已有配置或创建语言主字体映射
        /// </summary>
        public static LocalizationFontCatalog GetOrCreateDefaultCatalog()
        {
            var ExistingCatalog = AssetDatabase.LoadAssetAtPath<LocalizationFontCatalog>(CatalogPath);
            if (ExistingCatalog != null)
                return ExistingCatalog;
            if (File.Exists(CatalogPath))
                throw new InvalidOperationException($"目标路径已有其他类型资产 {CatalogPath}");
            var ChineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath);
            var EnglishFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(EnglishFontPath);
            if (ChineseFont == null || EnglishFont == null)
                throw new InvalidOperationException($"缺少默认字体 请检查 {ChineseFontPath} 与 {EnglishFontPath}");
            FontBakeService.EnsureFolders();
            var Catalog = ScriptableObject.CreateInstance<LocalizationFontCatalog>();
            var SerializedCatalog = new SerializedObject(Catalog);
            var EntryList = SerializedCatalog.FindProperty("entryList");
            EntryList.arraySize = 2;
            ConfigureEntry(EntryList.GetArrayElementAtIndex(0), "zh-CN", ChineseFont);
            ConfigureEntry(EntryList.GetArrayElementAtIndex(1), "en", EnglishFont);
            SerializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(Catalog, CatalogPath);
            AssetDatabase.SaveAssetIfDirty(Catalog);
            return Catalog;
        }

        /// <summary>
        /// 设置单个语言的默认正文主字体
        /// </summary>
        private static void ConfigureEntry(SerializedProperty Entry, string Locale, TMP_FontAsset Font)
        {
            Entry.FindPropertyRelative("locale").stringValue = Locale;
            Entry.FindPropertyRelative("style").stringValue = "Body";
            Entry.FindPropertyRelative("font").objectReferenceValue = Font;
            Entry.FindPropertyRelative("address").stringValue = string.Empty;
        }

        /// <summary>
        /// 只给选中的场景框架实例挂载宿主 不修改 Prefab 资产或其他场景
        /// </summary>
        public static void AttachToSelectedRoot()
        {
            var Selected = Selection.activeGameObject;
            var Root = Selected == null ? null : Selected.GetComponentInParent<ModuleHub>();
            if (Root == null || EditorUtility.IsPersistent(Root) || !Root.gameObject.scene.IsValid())
                throw new InvalidOperationException("请先在场景中选中含 ModuleHub 的框架根节点");
            var Host = Root.GetComponent<LubanLocalizationHost>();
            if (Host != null)
                throw new InvalidOperationException("选中框架已接入多语言 请直接修改现有宿主配置");
            var Catalog = GetOrCreateDefaultCatalog();
            Host = Undo.AddComponent<LubanLocalizationHost>(Root.gameObject);
            var SerializedHost = new SerializedObject(Host);
            SerializedHost.FindProperty("frameworkRoot").objectReferenceValue = Root;
            SerializedHost.FindProperty("fontCatalog").objectReferenceValue = Catalog;
            SerializedHost.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(Host);
            EditorSceneManager.MarkSceneDirty(Root.gameObject.scene);
            Selection.activeObject = Host;
        }

        /// <summary>
        /// 绑定选中的 UI 文本 默认 Key 可在 Inspector 修改
        /// </summary>
        public static void BindSelectedText()
        {
            var Selected = Selection.activeGameObject;
            if (Selected == null || Selected.GetComponent<TextMeshProUGUI>() == null || EditorUtility.IsPersistent(Selected))
                throw new InvalidOperationException("请选中场景中的 TextMeshProUGUI 文本");
            if (Selected.GetComponent<LocalizedTMPText>() != null)
                throw new InvalidOperationException("该文本已有多语言绑定组件");
            var Binding = Undo.AddComponent<LocalizedTMPText>(Selected);
            var SerializedBinding = new SerializedObject(Binding);
            SerializedBinding.FindProperty("textKey").stringValue = "common.start";
            SerializedBinding.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(Binding);
            EditorSceneManager.MarkSceneDirty(Selected.scene);
            Selection.activeObject = Binding;
        }
    }
}
