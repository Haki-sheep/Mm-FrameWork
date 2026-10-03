using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MieMieFrameWork.Localization.FontManagement
{
    public sealed class FontPreviewResult : IDisposable
    {
        public Texture2D image;
        public bool overflow;
        public int lines;
        public void Dispose() { if (image != null) Object.DestroyImmediate(image); }
    }

    public static class FontPreviewRenderer
    {
        public static void StyleMaterial(Material material, FontPreviewStyle style)
        {
            material.SetColor("_OutlineColor", style.outlineColor); material.SetFloat("_OutlineWidth", style.outlineWidth);
            if (style.shadow) material.EnableKeyword("UNDERLAY_ON"); else material.DisableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", style.shadowColor);
            material.SetFloat("_UnderlayOffsetX", style.shadowOffset.x); material.SetFloat("_UnderlayOffsetY", style.shadowOffset.y);
        }

        public static void StyleText(TMP_Text text, FontPreviewStyle style)
        {
            text.fontSize = style.fontSize; text.color = style.color;
            text.characterSpacing = style.characterSpacing; text.lineSpacing = style.lineSpacing;
            text.enableWordWrapping = style.wrap; text.overflowMode = style.overflow; text.enableAutoSizing = false;
        }

        public static FontPreviewResult Render(TMP_FontAsset font, string content, FontPreviewStyle style, bool richText)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));
            // TMP may generate missing characters while ForceMeshUpdate runs.  Never
            // hand the editor's persistent asset to the preview scene: dynamic fonts
            // and dynamic fallbacks would otherwise append glyphs to the real atlas.
            var cloneMap = new Dictionary<TMP_FontAsset, TMP_FontAsset>();
            var isolatedFont = CloneFontChain(font, cloneMap);
            var globalFallbacks = TMP_Settings.fallbackFontAssets;
            var originalGlobalFallbacks = globalFallbacks == null ? null : globalFallbacks.ToList();
            if (globalFallbacks != null)
            {
                var isolatedGlobalFallbacks = originalGlobalFallbacks == null
                    ? new List<TMP_FontAsset>()
                    : originalGlobalFallbacks.Where(f => f != null).Select(f => CloneFontChain(f, cloneMap)).ToList();
                globalFallbacks.Clear();
                globalFallbacks.AddRange(isolatedGlobalFallbacks);
            }
            var scene = EditorSceneManager.NewPreviewScene();
            var previous = RenderTexture.active;
            RenderTexture render = null; Material material = null; Texture2D pixels = null;
            try
            {
                var root = new GameObject("Font Preview", typeof(RectTransform), typeof(Canvas));
                SceneManager.MoveGameObjectToScene(root, scene);
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
                var dimensions = new Vector2(Mathf.Clamp(style.width, 64, 1920), Mathf.Clamp(style.height, 32, 1080));
                ((RectTransform)root.transform).sizeDelta = dimensions;
                var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(root.transform, false);
                var text = go.AddComponent<TextMeshProUGUI>(); text.font = isolatedFont; text.text = content; text.richText = richText; text.raycastTarget = false;
                text.rectTransform.sizeDelta = dimensions; text.alignment = TextAlignmentOptions.TopLeft;
                material = new Material(isolatedFont.material); StyleMaterial(material, style); text.fontSharedMaterial = material; StyleText(text, style);
                var cameraObject = new GameObject("Font Preview Camera", typeof(Camera)); SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false; camera.scene = scene;
                camera.transform.position = new Vector3(0, 0, -10); camera.orthographic = true; camera.orthographicSize = dimensions.y / 2;
                camera.aspect = dimensions.x / dimensions.y; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = style.background; camera.allowHDR = false; camera.allowMSAA = false;
                canvas.worldCamera = camera;
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = 5;
                camera.cullingMask = 1 << 5;
                render = new RenderTexture((int)dimensions.x, (int)dimensions.y, 24, RenderTextureFormat.ARGB32); render.Create(); camera.targetTexture = render;
                Canvas.ForceUpdateCanvases(); text.ForceMeshUpdate(); Canvas.ForceUpdateCanvases();
                // preferredWidth measures an unwrapped line, so it is not an overflow test when wrapping is enabled.
                bool overflow = text.isTextOverflowing || text.isTextTruncated || text.preferredHeight > dimensions.y || (!style.wrap && text.preferredWidth > dimensions.x);
                int lines = text.textInfo.lineCount;
                camera.Render(); RenderTexture.active = render;
                pixels = new Texture2D(render.width, render.height, TextureFormat.RGBA32, false);
                pixels.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0); pixels.Apply();
                return new FontPreviewResult { image = pixels, lines = lines, overflow = overflow };
            }
            catch { if (pixels != null) Object.DestroyImmediate(pixels); throw; }
            finally
            {
                RenderTexture.active = previous;
                if (render != null) { render.Release(); Object.DestroyImmediate(render); }
                EditorSceneManager.ClosePreviewScene(scene);
                if (material != null) Object.DestroyImmediate(material);
                if (globalFallbacks != null && originalGlobalFallbacks != null)
                {
                    globalFallbacks.Clear();
                    globalFallbacks.AddRange(originalGlobalFallbacks);
                }
                var destroyed = new HashSet<TMP_FontAsset>();
                foreach (var clone in cloneMap.Values.ToArray()) DestroyFontChain(clone, destroyed);
            }
        }

        static TMP_FontAsset CloneFontChain(TMP_FontAsset source, Dictionary<TMP_FontAsset, TMP_FontAsset> clones)
        {
            if (source == null) return null;
            if (clones.TryGetValue(source, out var existing)) return existing;
            var clone = Object.Instantiate(source);
            clone.name = source.name + " (Preview)";
            clones[source] = clone;
            var sourceTextures = source.atlasTextures ?? Array.Empty<Texture2D>();
            var clonedTextures = sourceTextures.Select(t => t == null ? null : Object.Instantiate(t)).ToArray();
            clone.atlasTextures = clonedTextures;
            if (source.material != null)
            {
                clone.material = Object.Instantiate(source.material);
                clone.material.name = source.material.name + " (Preview)";
                if (clonedTextures.Length > 0 && clonedTextures[0] != null) clone.material.mainTexture = clonedTextures[0];
            }
            var fallback = source.fallbackFontAssetTable;
            clone.fallbackFontAssetTable = fallback == null ? new List<TMP_FontAsset>() : fallback.Where(f => f != null).Select(f => CloneFontChain(f, clones)).ToList();
            return clone;
        }

        static void DestroyFontChain(TMP_FontAsset font, HashSet<TMP_FontAsset> destroyed)
        {
            if (font == null || !destroyed.Add(font)) return;
            foreach (var fallback in font.fallbackFontAssetTable ?? new List<TMP_FontAsset>()) DestroyFontChain(fallback, destroyed);
            foreach (var texture in font.atlasTextures ?? Array.Empty<Texture2D>()) if (texture != null) Object.DestroyImmediate(texture);
            if (font.material != null) Object.DestroyImmediate(font.material);
            Object.DestroyImmediate(font);
        }
    }
}
