#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FakeUnityCLI.EditorBridge
{
    internal static partial class EditorUiOperations
    {
        private static JObject ScreenInfo() => new JObject
        {
            ["width"] = Screen.width, ["height"] = Screen.height, ["safe_area"] = RectInfo(Screen.safeArea),
            ["dpi"] = Screen.dpi, ["orientation"] = Screen.orientation.ToString(), ["origin"] = "bottom-left",
            ["frame_count"] = Time.frameCount, ["sampled_at"] = DateTime.UtcNow.ToString("o"),
            ["is_playing"] = EditorApplication.isPlaying, ["is_paused"] = EditorApplication.isPaused,
            ["session_id"] = LogCaptureBridge.CurrentSessionId, ["source"] = "UnityEngine.Screen"
        };

        private static JObject EventSystemInfo()
        {
            var current = EventSystem.current;
            return new JObject { ["present"] = current != null, ["target"] = current == null ? null : Target(current.gameObject),
                ["current_input_module"] = current?.currentInputModule == null ? null : current.currentInputModule.GetType().FullName,
                ["selected"] = current == null ? null : Target(current.currentSelectedGameObject) };
        }

        private static JObject CanvasInfo(Canvas canvas)
        {
            if (canvas == null) return null;
            var root = canvas.rootCanvas;
            var scaler = root.GetComponent<CanvasScaler>();
            var camera = canvas.worldCamera ?? root.worldCamera;
            return new JObject { ["target"] = Target(canvas.gameObject), ["root"] = Target(root.gameObject),
                ["enabled"] = canvas.isActiveAndEnabled, ["render_mode"] = root.renderMode.ToString(),
                ["target_display"] = root.targetDisplay, ["scale_factor"] = canvas.scaleFactor, ["pixel_rect"] = RectInfo(canvas.pixelRect),
                ["sorting_layer_id"] = canvas.sortingLayerID, ["sorting_order"] = canvas.sortingOrder, ["override_sorting"] = canvas.overrideSorting,
                ["camera"] = camera == null ? null : new JObject { ["target"] = Target(camera.gameObject), ["rect"] = RectInfo(camera.rect),
                    ["pixel_rect"] = RectInfo(camera.pixelRect), ["renders_to_texture"] = camera.targetTexture != null },
                ["scaler"] = scaler == null ? null : new JObject { ["enabled"] = scaler.isActiveAndEnabled,
                    ["mode"] = scaler.uiScaleMode.ToString(), ["reference_resolution"] = Vec(scaler.referenceResolution),
                    ["screen_match_mode"] = scaler.screenMatchMode.ToString(), ["match_width_or_height"] = scaler.matchWidthOrHeight,
                    ["reference_pixels_per_unit"] = scaler.referencePixelsPerUnit, ["scale_factor"] = scaler.scaleFactor } };
        }

        private static JArray Groups(GameObject go) => new JArray(go.GetComponentsInParent<CanvasGroup>(true).Take(64).Select(group => new JObject
        {
            ["target"] = Target(group.gameObject), ["enabled"] = group.isActiveAndEnabled, ["alpha"] = group.alpha,
            ["interactable"] = group.interactable, ["blocks_raycasts"] = group.blocksRaycasts, ["ignore_parent_groups"] = group.ignoreParentGroups
        }));

        private static JObject Metrics(JObject request)
        {
            var result = new JObject { ["screen"] = ScreenInfo(), ["game_view"] = GameViewInfo(),
                ["device_screen"] = new JObject { ["width"] = UnityEngine.Device.Screen.width, ["height"] = UnityEngine.Device.Screen.height,
                    ["safe_area"] = RectInfo(UnityEngine.Device.Screen.safeArea), ["source"] = "UnityEngine.Device.Screen" },
                ["interpretation"] = "raw_coordinate_domains_project_adaptation_policy_not_applied" };
            if (!(request["target"] is JObject selector)) return result;
            using (var target = EditorObjectTarget.Resolve(selector, false))
            {
                if (!Live(target.GameObject)) throw new ArgumentException("UI metrics requires a live Scene target.");
                var chain = new JArray();
                var depth = 0;
                for (var transform = target.GameObject.transform; transform != null; transform = transform.parent)
                {
                    if (++depth > 64) throw new InvalidOperationException("Transform parent chain exceeds 64 nodes.");
                    if (!(transform is RectTransform rect)) continue;
                    var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                    chain.Add(new JObject { ["target"] = Target(rect.gameObject), ["rect"] = RectInfo(rect.rect),
                        ["anchor_min"] = Vec(rect.anchorMin), ["anchor_max"] = Vec(rect.anchorMax), ["pivot"] = Vec(rect.pivot),
                        ["anchored_position"] = Vec(rect.anchoredPosition), ["size_delta"] = Vec(rect.sizeDelta),
                        ["local_scale"] = new JArray(rect.localScale.x, rect.localScale.y, rect.localScale.z),
                        ["world_corners"] = new JArray(corners.Select(v => new JArray(v.x, v.y, v.z))) });
                }
                result["target"] = Target(target.GameObject);
                result["rect_chain"] = chain;
                result["canvas"] = CanvasInfo(target.GameObject.GetComponentInParent<Canvas>(true));
                result["geometry"] = Element(target.GameObject, false);
            }
            return result;
        }

        private static JObject GameViewInfo()
        {
            var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            var views = type == null ? new UnityEngine.Object[0] : Resources.FindObjectsOfTypeAll(type);
            var result = new JObject { ["view_count"] = views.Length, ["supported"] = false,
                ["focused_window"] = EditorWindow.focusedWindow == null ? null : EditorWindow.focusedWindow.GetType().FullName };
            if (views.Length != 1) return result;
            var property = type.GetProperty("targetRenderSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? type.GetProperty("targetSize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            try
            {
                if (property?.GetValue(views[0]) is Vector2 size)
                { result["size"] = Vec(size); result["supported"] = true; result["source"] = property.Name; }
            }
            catch (Exception exception) { result["unsupported_reason"] = exception.GetType().Name; }
            return result;
        }
    }
}
#endif
