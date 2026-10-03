using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MieMieFrameWork.Localization.FontManagement
{
    public sealed class FontTextOrigin
    {
        public string label;
        public UnityEngine.Object target;
        public string globalId;
        public string assetPath;
    }

    public sealed class FontCharacterSet
    {
        public readonly SortedDictionary<uint, List<FontTextOrigin>> characters = new SortedDictionary<uint, List<FontTextOrigin>>();
        public readonly List<string> warnings = new List<string>();
        public uint[] Codes => characters.Keys.ToArray();

        public void Add(string text, FontTextOrigin origin, bool richText, bool stripPlaceholders)
        {
            foreach (var code in FontCharacterCollector.CodePoints(FontCharacterCollector.VisibleText(text, richText, stripPlaceholders)).Distinct())
            {
                if (!characters.TryGetValue(code, out var origins)) characters.Add(code, origins = new List<FontTextOrigin>());
                // Scene/Prefab components can share the same hierarchy path and
                // label (for example two Text components on one GameObject).
                // Keep each GlobalObjectId so every missing glyph remains
                // navigable; fallback to the old label/path key for non-object
                // origins such as manual text and TextAssets.
                bool duplicate = !string.IsNullOrEmpty(origin.globalId)
                    ? origins.Any(o => o.globalId == origin.globalId)
                    : origins.Any(o => string.IsNullOrEmpty(o.globalId) && o.label == origin.label && o.assetPath == origin.assetPath);
                if (!duplicate) origins.Add(origin);
            }
        }
    }

    public static class FontCharacterCollector
    {
        // Only TMP tags are removed. Literal comparisons such as "a < b" remain text.
        static readonly Regex Tags = new Regex(@"</?(?:b|i|u|s|color|size|font|material|align|alpha|cspace|mspace|voffset|indent|line-indent|line-height|margin|margin-left|margin-right|mark|nobr|page|pos|rotate|space|sprite|style|sub|sup|uppercase|lowercase|smallcaps|width|br|link|gradient|font-weight|allcaps)(?=[\s=>])[^>]*>|<#[0-9a-fA-F]{6,8}>", RegexOptions.IgnoreCase);
        static readonly Regex Placeholders = new Regex(@"(?<!\{)\{(?:\d+|[A-Za-z_]\w*)(?:,-?\d+)?(?::[^{}]*)?\}(?!\})");

        public static string VisibleText(string text, bool richText, bool stripPlaceholders)
        {
            text = text ?? "";
            if (richText)
            {
                var result = new StringBuilder(); int position = 0;
                while (position < text.Length)
                {
                    int start = text.IndexOf("<noparse>", position, StringComparison.OrdinalIgnoreCase);
                    if (start < 0) { result.Append(Tags.Replace(text.Substring(position), "")); break; }
                    result.Append(Tags.Replace(text.Substring(position, start - position), ""));
                    int end = text.IndexOf("</noparse>", start + 9, StringComparison.OrdinalIgnoreCase);
                    if (end < 0) { result.Append(text.Substring(start + 9)); break; }
                    result.Append(text.Substring(start + 9, end - start - 9)); position = end + 10;
                }
                text = result.ToString();
            }
            return stripPlaceholders ? Placeholders.Replace(text, "").Replace("{{", "{").Replace("}}", "}") : text;
        }

        public static IEnumerable<uint> CodePoints(string text)
        {
            for (int i = 0; i < (text ?? "").Length; i++)
            {
                uint value = text[i];
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) value = (uint)char.ConvertToUtf32(text[i], text[++i]);
                else if (char.IsSurrogate(text[i])) throw new ArgumentException("文案包含无效的 Unicode 代理字符，请修正文案后重试。");
                if (value < 32 || value == 127) continue;
                yield return value;
            }
        }

        public static FontCharacterSet Collect(FontBakeProfile profile)
        {
            var set = new FontCharacterSet();
            if (profile.includeLocalizationTable)
                set.Add(LocalizationFontTextExporter.GetText(profile.locale), new FontTextOrigin { label = "Luban " + profile.locale, target = profile }, profile.richText, profile.stripPlaceholders);
            set.Add(profile.manualText, new FontTextOrigin { label = "手动文案", target = profile }, profile.richText, profile.stripPlaceholders);
            if (profile.includeCommonCharacters) set.Add(" 0123456789.,:;!?+-*/%=()[]￥¥$€£元年月日时分秒", new FontTextOrigin { label = "运行时数字与常用符号", target = profile }, false, false);
            foreach (var file in profile.textFiles.Where(f => f != null).Distinct())
                set.Add(file.text, new FontTextOrigin { label = file.name, target = file, assetPath = AssetDatabase.GetAssetPath(file) }, profile.richText, profile.stripPlaceholders);
            foreach (var asset in profile.contentAssets.Where(a => a != null).Distinct())
            {
                var path = AssetDatabase.GetAssetPath(asset);
                if (asset is SceneAsset)
                {
                    var loaded = SceneManager.GetSceneByPath(path);
                    bool opened = !loaded.IsValid() || !loaded.isLoaded;
                    var active = SceneManager.GetActiveScene();
                    var scene = opened ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive) : loaded;
                    try
                    {
                        if (!opened && scene.isDirty) throw new InvalidOperationException("场景有未保存修改，请先保存再收集：" + path);
                        foreach (var root in scene.GetRootGameObjects()) CollectRoot(root, path, set, profile.stripPlaceholders, asset);
                    }
                    finally { if (opened) EditorSceneManager.CloseScene(scene, true); if (active.IsValid() && active.isLoaded) SceneManager.SetActiveScene(active); }
                }
                else if (asset is GameObject root && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) CollectRoot(root, path, set, profile.stripPlaceholders, null);
                else throw new ArgumentException("文案来源必须是 Scene 或 Prefab：" + asset.name);
            }
            return set;
        }

        static void CollectRoot(GameObject root, string path, FontCharacterSet set, bool placeholders, UnityEngine.Object sceneAsset)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                set.Add(text.text, Origin(text, path, sceneAsset), text.richText, placeholders);
            foreach (var text in root.GetComponentsInChildren<Text>(true))
                set.Add(text.text, Origin(text, path, sceneAsset), text.supportRichText, placeholders);
        }

        static FontTextOrigin Origin(Component component, string path, UnityEngine.Object sceneAsset)
        {
            return new FontTextOrigin { label = AnimationUtility.CalculateTransformPath(component.transform, null), assetPath = path,
                target = sceneAsset != null ? sceneAsset : component,
                // Keep the component identifier even when the source is a Scene
                // asset.  The previous implementation only stored the scene
                // asset in that case, which made the "定位来源" action stop at
                // the scene file instead of the text component containing the
                // missing character.
                globalId = GlobalObjectId.GetGlobalObjectIdSlow(component).ToString() };
        }

        public static void Locate(FontTextOrigin origin)
        {
            var target = origin.target;
            if (!string.IsNullOrEmpty(origin.globalId) && GlobalObjectId.TryParse(origin.globalId, out var id))
                target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) ?? target;
            // A scene object cannot be resolved while its scene is closed. Open
            // the source scene additively, then resolve by the stored Global ID
            // or hierarchy path so the developer lands on the actual TMP/Text
            // component instead of only seeing the Scene asset.
            if (target == origin.target && !string.IsNullOrEmpty(origin.assetPath) && origin.assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var scene = SceneManager.GetSceneByPath(origin.assetPath);
                    if (!scene.IsValid() || !scene.isLoaded)
                        scene = EditorSceneManager.OpenScene(origin.assetPath, OpenSceneMode.Additive);
                    if (!string.IsNullOrEmpty(origin.globalId) && GlobalObjectId.TryParse(origin.globalId, out var sceneId))
                        target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(sceneId) ?? target;
                    if (target == origin.target && scene.IsValid() && scene.isLoaded)
                    {
                        foreach (var root in scene.GetRootGameObjects())
                        {
                            var transform = FindTransform(root.transform, origin.label);
                            if (transform == null) continue;
                            var textComponent = (Component)transform.GetComponent<TMP_Text>() ?? transform.GetComponent<Text>();
                            target = textComponent != null ? (UnityEngine.Object)textComponent : transform.gameObject;
                            break;
                        }
                    }
                    if (target != null && target != origin.target && scene.IsValid() && scene.isLoaded)
                        SceneManager.SetActiveScene(scene);
                }
                catch (Exception e) { Debug.LogWarning("无法打开文案来源场景：" + origin.assetPath + "。" + e.Message); }
            }
            if (target == null && !string.IsNullOrEmpty(origin.assetPath)) target = AssetDatabase.LoadMainAssetAtPath(origin.assetPath);
            if (target != null) { Selection.activeObject = target; EditorGUIUtility.PingObject(target); }
        }

        static Transform FindTransform(Transform root, string path)
        {
            if (root == null) return null;
            if (string.Equals(AnimationUtility.CalculateTransformPath(root, null), path, StringComparison.Ordinal)) return root;
            foreach (Transform child in root)
            {
                var found = FindTransform(child, path);
                if (found != null) return found;
            }
            return null;
        }
    }
}
