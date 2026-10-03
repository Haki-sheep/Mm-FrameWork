#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FakeUnityCLI.EditorBridge
{
    internal static partial class EditorUiOperations
    {
        private static JObject Click(JObject request, string id)
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("UI clicks require stable, unpaused Play Mode.");
            var dryRun = (bool?)request["dry_run"] == true;
            if (!dryRun && (bool?)request["confirmed"] != true) throw new ArgumentException("UI clicks require --yes or --dry-run.");
            if (!(request["target"] is JObject selector) || selector["asset_path"] != null)
                throw new ArgumentException("UI click requires an explicit live Scene target identity.");
            using (var target = EditorObjectTarget.Resolve(selector, true))
            {
                var go = target.GameObject;
                var point = Point(request, go, true);
                var hits = Raycast(point);
                var reason = Eligibility(go, point, hits);
                if (reason != null) throw new InvalidOperationException("UI click rejected before dispatch: " + reason);
                var result = new JObject { ["operation_id"] = id, ["target"] = Target(go), ["point"] = Vec(point),
                    ["screen"] = ScreenInfo(), ["hits"] = HitInfo(hits), ["dry_run"] = dryRun, ["click_dispatched"] = false,
                    ["undo_supported"] = false, ["business_postcondition"] = "not_evaluated", ["input_scope"] = "UGUI_EventSystem_not_OS_input" };
                if (dryRun) return result;
                var eventSystem = EventSystem.current;
                var data = new PointerEventData(eventSystem) { pointerId = -1, button = PointerEventData.InputButton.Left,
                    position = point, pressPosition = point, pointerCurrentRaycast = hits[0], pointerPressRaycast = hits[0],
                    rawPointerPress = hits[0].gameObject, eligibleForClick = true, clickCount = 1, clickTime = Time.unscaledTime };
                PersistClick(id, "prepared", result);
                var errors = new List<string>();
                Application.LogCallback log = (message, stack, type) =>
                {
                    if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && errors.Count < 8)
                        errors.Add(message.Length > 2000 ? message.Substring(0, 2000) : message);
                };
                GameObject press = null;
                var sentUp = false;
                Application.logMessageReceived += log;
                try
                {
                    eventSystem.SetSelectedGameObject(go, data);
                    RequireUnchangedTarget(go, point, eventSystem);
                    press = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, data, ExecuteEvents.pointerDownHandler);
                    if (press == null) press = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
                    data.pointerPress = press;
                    if (press != go) throw new InvalidOperationException("Pointer-down handler changed during dispatch.");
                    RequireUnchangedTarget(go, point, eventSystem);
                    sentUp = true;
                    ExecuteEvents.Execute(press, data, ExecuteEvents.pointerUpHandler);
                    RequireUnchangedTarget(go, point, eventSystem);
                    if (errors.Count != 0) throw new InvalidOperationException("A UI event handler reported an error.");
                    result["click_dispatch_attempted"] = true;
                    ExecuteEvents.Execute(go, data, ExecuteEvents.pointerClickHandler);
                    if (errors.Count != 0) throw new InvalidOperationException("The click callback reported an error; its side effects are unknown.");
                    result["click_dispatched"] = true;
                    result["selected_after"] = Target(eventSystem == null ? null : eventSystem.currentSelectedGameObject);
                    result["target_exists_after"] = go != null;
                    result["frame_after"] = Time.frameCount;
                    PersistClick(id, "completed", result);
                    return result;
                }
                catch (Exception exception)
                {
                    result["error"] = exception.Message;
                    result["callback_errors"] = new JArray(errors);
                    try { PersistClick(id, "failed", result); } catch { }
                    throw new EditorInspectorOperations.WriteOutcomeUnknownException(id, exception);
                }
                finally
                {
                    try { if (press != null && !sentUp) ExecuteEvents.Execute(press, data, ExecuteEvents.pointerUpHandler); }
                    finally
                    {
                        Application.logMessageReceived -= log;
                        data.eligibleForClick = false; data.pointerPress = null; data.rawPointerPress = null;
                    }
                }
            }
        }

        private static void RequireUnchangedTarget(GameObject go, Vector2 point, EventSystem originalEventSystem)
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused || EventSystem.current != originalEventSystem)
                throw new InvalidOperationException("Mode or EventSystem changed during pointer dispatch.");
            var reason = Eligibility(go, point, Raycast(point));
            if (reason != null) throw new InvalidOperationException("Target changed during pointer sequence: " + reason);
        }

        private static JObject ReadClickReceipt(string id)
        {
            var stages = new JObject();
            foreach (var state in new[] { "prepared", "completed", "failed" })
            {
                var path = ClickReceiptPath(id, state);
                if (File.Exists(path)) stages[state] = EditorInspectorOperations.Parse(File.ReadAllText(path), 1024 * 1024);
            }
            if (!stages.HasValues) throw new FileNotFoundException("UI click receipt not found.");
            return new JObject { ["operation_id"] = id, ["stages"] = stages,
                ["state"] = stages["failed"] != null ? "outcome_unknown" : stages["completed"] != null ? "completed" : "outcome_unknown",
                ["automatic_retry_safe"] = false, ["undo_supported"] = false };
        }
        private static string ClickReceiptPath(string id, string state)
        {
            if (!Guid.TryParseExact(id, "N", out _)) throw new ArgumentException("operation_id requires a 32-character GUID.");
            return Path.Combine(Application.dataPath, "..", "Library", "FakeUnityCLI", "ui-clicks", id + "." + state + ".json");
        }
        private static void PersistClick(string id, string state, JObject result)
        {
            var path = ClickReceiptPath(id, state);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            File.WriteAllText(temporary, result.ToString(Formatting.None), new UTF8Encoding(false));
            File.Move(temporary, path);
        }
    }
}
#endif
