#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FakeUnityCLI.EditorBridge
{
    internal static partial class EditorUiOperations
    {
        internal const string SnapshotOperation = "editor-ui-snapshot";
        internal const string FindOperation = "editor-ui-find";
        internal const string RaycastOperation = "editor-ui-raycast";
        internal const string ClickOperation = "editor-ui-click";
        internal const string MetricsOperation = "editor-ui-metrics";
        internal const string ResultOperation = "editor-ui-result";
        internal static bool Handles(string op) => op == SnapshotOperation || op == FindOperation || op == RaycastOperation || op == ClickOperation || op == MetricsOperation || op == ResultOperation;
        internal static bool ReadOnly(string op) => Handles(op) && op != ClickOperation;

        internal static string Execute(string operation, string json, string requestId)
        {
            var request = EditorInspectorOperations.Parse(json);
            JObject result;
            if (operation == ResultOperation) result = ReadClickReceipt((string)request["operation_id"]);
            else if (operation == MetricsOperation) result = Metrics(request);
            else if (operation == ClickOperation) result = Click(request, requestId);
            else if (operation == RaycastOperation)
            {
                using (var target = request["target"] is JObject selector ? EditorObjectTarget.Resolve(selector, false) : null)
                {
                    var point = Point(request, target?.GameObject, false);
                    var hits = Raycast(point);
                    result = new JObject { ["screen"] = ScreenInfo(), ["point"] = Vec(point), ["hits"] = HitInfo(hits),
                        ["hit_count"] = hits.Count, ["truncated"] = hits.Count > 100, ["origin"] = "bottom-left" };
                }
            }
            else result = Snapshot(request, operation == FindOperation);
            var text = result.ToString(Formatting.None);
            if (Encoding.UTF8.GetByteCount(text) > 1024 * 1024)
                throw new ArgumentException("UI result exceeds 1 MiB; narrow the query.");
            return text;
        }

        private static JObject Snapshot(JObject request, bool find)
        {
            var name = (string)request["name"];
            var path = (string)request["path"];
            if (find && String.IsNullOrEmpty(name) && String.IsNullOrEmpty(path)) throw new ArgumentException("ui-find requires name or path.");
            var limit = (int?)request["limit"] ?? 100;
            if (limit < 1 || limit > 500) throw new ArgumentException("limit must be 1..500.");
            var includeInactive = (bool?)request["include_inactive"] == true;
            var withRaycast = (bool?)request["raycast"] == true;
            if (withRaycast && limit > 100) throw new ArgumentException("Raycast snapshots are limited to 100 elements.");
            var all = Resources.FindObjectsOfTypeAll<RectTransform>().Where(t => Live(t.gameObject)).ToArray();
            if (all.Length > 20000) throw new InvalidOperationException("UI scan exceeds 20000 live RectTransforms.");
            var matches = all.Where(t => (includeInactive || t.gameObject.activeInHierarchy) &&
                (name == null || t.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0) &&
                (path == null || EditorObjectTarget.HierarchyPath(t).StartsWith(path, StringComparison.Ordinal))).OrderBy(t => EditorIdentity.SceneId(t.gameObject.scene))
                .ThenBy(t => EditorObjectTarget.HierarchyPath(t), StringComparer.Ordinal).ThenBy(t => EditorIdentity.InstanceId(t)).ToArray();
            return new JObject { ["screen"] = ScreenInfo(), ["event_system"] = EventSystemInfo(),
                ["elements"] = new JArray(matches.Take(limit).Select(t => Element(t.gameObject, withRaycast))),
                ["match_count"] = matches.Length, ["truncated"] = matches.Length > limit, ["scanned"] = all.Length,
                ["evidence_scope"] = "UGUI_geometry_state_and_optional_raycast_not_final_pixels" };
        }

        private static bool Live(GameObject go)
        {
            if (go == null || EditorUtility.IsPersistent(go) || !go.scene.IsValid() || !go.scene.isLoaded || EditorSceneManager.IsPreviewScene(go.scene)) return false;
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            return stage == null || stage.scene != go.scene;
        }

        private static JObject Target(GameObject go)
        {
            if (go == null) return null;
            using (var target = new EditorObjectTarget { GameObject = go }) return target.Describe();
        }

        private static JObject Element(GameObject go, bool withRaycast)
        {
            var rect = go.transform as RectTransform;
            var selectable = go.GetComponent<Selectable>();
            var canvas = go.GetComponentInParent<Canvas>(true);
            var result = new JObject { ["target"] = Target(go), ["active_self"] = go.activeSelf, ["active_in_hierarchy"] = go.activeInHierarchy,
                ["selectable_type"] = selectable == null ? null : selectable.GetType().FullName,
                ["interactable"] = selectable == null ? (JToken)JValue.CreateNull() : selectable.IsActive() && selectable.IsInteractable(),
                ["canvas"] = CanvasInfo(canvas), ["canvas_groups"] = Groups(go), ["raycast_evaluated"] = false,
                ["pixel_visibility"] = "not_evaluated" };
            if (rect == null) { result["geometry_error"] = "not_a_rect_transform"; return result; }
            try
            {
                var camera = EventCamera(canvas);
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                var screenCorners = corners.Select(c => RectTransformUtility.WorldToScreenPoint(camera, c)).ToArray();
                var bounds = Rect.MinMaxRect(screenCorners.Min(v => v.x), screenCorners.Min(v => v.y), screenCorners.Max(v => v.x), screenCorners.Max(v => v.y));
                result["screen_rect"] = RectInfo(bounds);
                result["screen_corners"] = new JArray(screenCorners.Select(Vec));
                result["geometry_on_screen"] = bounds.width > 0 && bounds.height > 0 && bounds.Overlaps(new Rect(0, 0, Screen.width, Screen.height));
                var center = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
                result["center"] = Vec(center);
                if (withRaycast && EventSystem.current != null)
                {
                    var hits = Raycast(center);
                    var eligible = Eligibility(go, center, hits);
                    result["raycast_evaluated"] = true;
                    result["top_hit"] = hits.Count == 0 ? null : Target(hits[0].gameObject);
                    result["click_eligible"] = eligible == null;
                    result["click_block_reason"] = eligible;
                }
            }
            catch (InvalidOperationException exception) { result["geometry_error"] = exception.Message; }
            return result;
        }

        private static Vector2 Point(JObject request, GameObject target, bool write)
        {
            if (write)
            {
                var size = request["screen_size"] as JArray;
                if (size == null || size.Count != 2 || size[0].Type != JTokenType.Integer || size[1].Type != JTokenType.Integer ||
                    (int)size[0] != Screen.width || (int)size[1] != Screen.height)
                    throw new InvalidOperationException("screen_size is missing or changed; take a fresh snapshot before clicking.");
            }
            Vector2 point;
            if (request["point"] != null)
            {
                var values = request["point"] as JArray;
                if (values == null || values.Count != 2) throw new ArgumentException("point requires [x,y].");
                point = new Vector2(Number(values[0]), Number(values[1]));
                var space = (string)request["coordinate_space"] ?? "unity_screen";
                if (space == "normalized")
                {
                    if (point.x < 0 || point.x > 1 || point.y < 0 || point.y > 1) throw new ArgumentException("Normalized coordinates require 0..1.");
                    point = new Vector2(point.x * Screen.width, point.y * Screen.height);
                }
                else if (space != "unity_screen") throw new ArgumentException("coordinate_space must be unity_screen or normalized, bottom-left origin.");
            }
            else
            {
                if (target == null || !(target.transform is RectTransform rect)) throw new ArgumentException("Provide a UGUI target or point.");
                point = RectTransformUtility.WorldToScreenPoint(EventCamera(target.GetComponentInParent<Canvas>(true)), rect.TransformPoint(rect.rect.center));
            }
            if (Single.IsNaN(point.x) || Single.IsInfinity(point.x) || Single.IsNaN(point.y) || Single.IsInfinity(point.y) ||
                Screen.width <= 0 || Screen.height <= 0 || point.x < 0 || point.y < 0 || point.x >= Screen.width || point.y >= Screen.height)
                throw new ArgumentException("Point is outside the current Unity screen.");
            return point;
        }

        private static Camera EventCamera(Canvas canvas)
        {
            if (canvas == null || !canvas.isActiveAndEnabled) throw new InvalidOperationException("No active Canvas.");
            var root = canvas.rootCanvas;
            if (root.targetDisplay != 0) throw new InvalidOperationException("Only display 0 screen coordinates are supported.");
            if (root.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            var raycaster = canvas.GetComponent<GraphicRaycaster>() ?? root.GetComponent<GraphicRaycaster>();
            var camera = raycaster == null ? canvas.worldCamera : raycaster.eventCamera;
            if (camera == null || !camera.isActiveAndEnabled || camera.targetTexture != null) throw new InvalidOperationException("A live screen-rendering event camera is required.");
            return camera;
        }

        private static List<RaycastResult> Raycast(Vector2 point)
        {
            RequireEventSystem();
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            if (hits.Count > 2000) throw new InvalidOperationException("Raycast result budget exceeded.");
            return hits.Where(h => h.gameObject != null).ToList();
        }

        private static void RequireEventSystem()
        {
            var systems = Resources.FindObjectsOfTypeAll<EventSystem>().Where(s => Live(s.gameObject) && s.isActiveAndEnabled).ToArray();
            if (systems.Length != 1 || EventSystem.current != systems[0]) throw new InvalidOperationException("Exactly one active current EventSystem is required.");
        }

        private static JArray HitInfo(List<RaycastResult> hits) => new JArray(hits.Take(100).Select(h => new JObject
        {
            ["target"] = Target(h.gameObject), ["module"] = h.module == null ? null : h.module.GetType().FullName,
            ["sorting_layer"] = h.sortingLayer, ["sorting_order"] = h.sortingOrder, ["depth"] = h.depth,
            ["distance"] = h.distance, ["screen_position"] = Vec(h.screenPosition),
            ["click_handler"] = Target(ExecuteEvents.GetEventHandler<IPointerClickHandler>(h.gameObject))
        }));

        private static string Eligibility(GameObject target, Vector2 point, List<RaycastResult> hits)
        {
            if (!Live(target) || !target.activeInHierarchy) return "inactive_or_destroyed";
            if (!(target.transform is RectTransform rect)) return "not_ugui";
            var selectable = target.GetComponent<Selectable>();
            if (selectable != null && (!selectable.IsActive() || !selectable.IsInteractable())) return "not_interactable";
            var canvas = target.GetComponentInParent<Canvas>(true);
            Camera camera;
            try { camera = EventCamera(canvas); } catch (InvalidOperationException e) { return e.Message; }
            if (!RectTransformUtility.RectangleContainsScreenPoint(rect, point, camera)) return "point_outside_target_rect";
            foreach (var group in target.GetComponentsInParent<CanvasGroup>(true))
                if (group.isActiveAndEnabled && group.alpha <= 0.001f) return "transparent_canvas_group";
            if (hits.Count == 0) return "no_raycast_hit";
            var first = hits[0].gameObject;
            if (first != target && !first.transform.IsChildOf(target.transform)) return "occluded_by_other_target";
            var graphic = first.GetComponent<Graphic>();
            if (graphic == null || !graphic.isActiveAndEnabled || graphic.canvasRenderer.cull || graphic.color.a <= 0.001f || graphic.canvasRenderer.GetInheritedAlpha() <= 0.001f)
                return "no_visible_hit_graphic";
            var click = ExecuteEvents.GetEventHandler<IPointerClickHandler>(first);
            var down = ExecuteEvents.GetEventHandler<IPointerDownHandler>(first);
            if (click != target || down != null && down != target) return "event_handler_does_not_match_target";
            return null;
        }

        private static float Number(JToken token)
        {
            if (token == null || token.Type != JTokenType.Float && token.Type != JTokenType.Integer) throw new ArgumentException("Expected numeric coordinate.");
            var number = (double)token;
            if (Double.IsNaN(number) || Double.IsInfinity(number) || Math.Abs(number) > Single.MaxValue) throw new ArgumentException("Non-finite coordinate.");
            return (float)number;
        }
        private static JArray Vec(Vector2 v) => new JArray(v.x, v.y);
        private static JObject RectInfo(Rect r) => new JObject { ["x"] = r.x, ["y"] = r.y, ["width"] = r.width, ["height"] = r.height };
    }
}
#endif
