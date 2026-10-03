#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace FakeUnityCLI.EditorBridge
{
    // SerializedProperty conversion follows the Unity API approach reviewed in AI Bridge 1.5.9.
    // The public field names, strict validation and reference identities are FakeUnityCLI contracts.
    internal static class EditorPropertyValues
    {
        private static readonly PropertyInfo GradientProperty = typeof(SerializedProperty).GetProperty("gradientValue",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            {"m_Enabled", "enabled"}, {"m_LocalPosition", "local_position"}, {"m_LocalScale", "local_scale"},
            {"m_AnchorMin", "anchor_min"}, {"m_AnchorMax", "anchor_max"}, {"m_AnchoredPosition", "anchored_position"},
            {"m_SizeDelta", "size_delta"}, {"m_Pivot", "pivot"}, {"m_Color", "color"}, {"m_RaycastTarget", "raycast_target"},
            {"m_Interactable", "interactable"}, {"m_Alpha", "alpha"}, {"m_BlocksRaycasts", "blocks_raycasts"},
            {"m_IgnoreParentGroups", "ignore_parent_groups"}, {"m_SortingOrder", "sorting_order"},
            {"m_SortingLayerID", "sorting_layer_id"}, {"m_Material", "material"}, {"m_Sprite", "sprite"},
            {"m_ConnectedBody", "connected_body"}
        };

        internal static string PublicName(SerializedProperty property)
        {
            var path = property.propertyPath;
            if (Aliases.TryGetValue(path, out var alias)) return alias;
            return path.Split('.').Any(p => p.StartsWith("m_", StringComparison.Ordinal)) ? null : path;
        }

        internal static SerializedProperty Find(SerializedObject owner, string name)
        {
            if (String.IsNullOrEmpty(name) || name.Split('.').Any(p => p.StartsWith("m_", StringComparison.Ordinal)))
                throw new ArgumentException("Use a semantic property name returned by inspector-get, never m_* fields.");
            var mapped = Aliases.FirstOrDefault(p => p.Value == name).Key ?? name;
            var property = owner.FindProperty(mapped);
            if (property == null || PublicName(property) != name) throw new ArgumentException("Unknown semantic property: " + name);
            return property;
        }

        internal static bool Supported(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer: case SerializedPropertyType.Boolean: case SerializedPropertyType.Float:
                case SerializedPropertyType.String: case SerializedPropertyType.Color: case SerializedPropertyType.Enum:
                case SerializedPropertyType.Vector2: case SerializedPropertyType.Vector3: case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Vector2Int: case SerializedPropertyType.Vector3Int: case SerializedPropertyType.Rect:
                case SerializedPropertyType.Bounds: case SerializedPropertyType.AnimationCurve: return true;
                case SerializedPropertyType.ObjectReference: return ReferenceType(p) != null;
                case SerializedPropertyType.Gradient: return GradientProperty?.GetGetMethod(true) != null && GradientProperty.GetSetMethod(true) != null;
                default: return false;
            }
        }

        internal static JToken Read(SerializedProperty p, GameObject localRoot = null)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer: return p.longValue;
                case SerializedPropertyType.Boolean: return p.boolValue;
                case SerializedPropertyType.Float: return p.doubleValue;
                case SerializedPropertyType.String: return p.stringValue;
                case SerializedPropertyType.Enum: return new JObject { ["index"] = p.enumValueIndex, ["names"] = new JArray(p.enumNames) };
                case SerializedPropertyType.Color: var c = p.colorValue; return new JArray(c.r, c.g, c.b, c.a);
                case SerializedPropertyType.Vector2: var v2 = p.vector2Value; return new JArray(v2.x, v2.y);
                case SerializedPropertyType.Vector3: return Vector(p.vector3Value);
                case SerializedPropertyType.Vector4: var v4 = p.vector4Value; return new JArray(v4.x, v4.y, v4.z, v4.w);
                case SerializedPropertyType.Vector2Int: var i2 = p.vector2IntValue; return new JArray(i2.x, i2.y);
                case SerializedPropertyType.Vector3Int: var i3 = p.vector3IntValue; return new JArray(i3.x, i3.y, i3.z);
                case SerializedPropertyType.Rect: var rect = p.rectValue; return new JArray(rect.x, rect.y, rect.width, rect.height);
                case SerializedPropertyType.Bounds: var bounds = p.boundsValue; return new JObject { ["center"] = Vector(bounds.center), ["size"] = Vector(bounds.size) };
                case SerializedPropertyType.ObjectReference: return DescribeReference(p.objectReferenceValue, localRoot);
                case SerializedPropertyType.AnimationCurve:
                    var curve = p.animationCurveValue;
                    return new JObject { ["pre_wrap"] = curve.preWrapMode.ToString(), ["post_wrap"] = curve.postWrapMode.ToString(),
                        ["keys"] = new JArray(curve.keys.Select(k => new JObject { ["time"] = k.time, ["value"] = k.value,
                            ["in_tangent"] = k.inTangent, ["out_tangent"] = k.outTangent, ["in_weight"] = k.inWeight,
                            ["out_weight"] = k.outWeight, ["weighted_mode"] = k.weightedMode.ToString() })) };
                case SerializedPropertyType.Gradient:
                    if (!Supported(p)) return JValue.CreateNull();
                    var gradient = (Gradient)GradientProperty.GetValue(p);
                    return new JObject { ["mode"] = gradient.mode.ToString(),
                        ["color_keys"] = new JArray(gradient.colorKeys.Select(k => new JObject { ["time"] = k.time,
                            ["color"] = new JArray(k.color.r, k.color.g, k.color.b, k.color.a) })),
                        ["alpha_keys"] = new JArray(gradient.alphaKeys.Select(k => new JObject { ["time"] = k.time, ["alpha"] = k.alpha })) };
                default: return JValue.CreateNull();
            }
        }

        internal static void Write(SerializedProperty p, JToken value, GameObject localRoot = null)
        {
            if (!p.editable || !Supported(p)) throw new ArgumentException("Property is read-only or has an unsupported type: " + PublicName(p));
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:
                    var integer = Integer(value);
                    if (p.type != "long" && (integer < Int32.MinValue || integer > Int32.MaxValue)) throw new ArgumentException("Integer outside Int32 range.");
                    p.longValue = integer; break;
                case SerializedPropertyType.Boolean: RequireType(value, JTokenType.Boolean); p.boolValue = (bool)value; break;
                case SerializedPropertyType.Float:
                    var number = Number(value);
                    if (p.type != "double" && Math.Abs(number) > Single.MaxValue) throw new ArgumentException("Float overflow.");
                    p.doubleValue = number; break;
                case SerializedPropertyType.String: RequireType(value, JTokenType.String); p.stringValue = (string)value; break;
                case SerializedPropertyType.Enum:
                    var index = value.Type == JTokenType.String ? System.Array.IndexOf(p.enumNames, (string)value) :
                        checked((int)Integer(value is JObject ? value["index"] : value));
                    if (index < 0 || index >= p.enumNames.Length) throw new ArgumentException("Unknown enum name/index.");
                    p.enumValueIndex = index; break;
                case SerializedPropertyType.Color: var color = Numbers(value, 4); p.colorValue = new Color(color[0], color[1], color[2], color[3]); break;
                case SerializedPropertyType.Vector2: var v2 = Numbers(value, 2); p.vector2Value = new Vector2(v2[0], v2[1]); break;
                case SerializedPropertyType.Vector3: p.vector3Value = Vector3Value(value); break;
                case SerializedPropertyType.Vector4: var v4 = Numbers(value, 4); p.vector4Value = new Vector4(v4[0], v4[1], v4[2], v4[3]); break;
                case SerializedPropertyType.Vector2Int: var i2 = Ints(value, 2); p.vector2IntValue = new Vector2Int(i2[0], i2[1]); break;
                case SerializedPropertyType.Vector3Int: var i3 = Ints(value, 3); p.vector3IntValue = new Vector3Int(i3[0], i3[1], i3[2]); break;
                case SerializedPropertyType.Rect: var r = Numbers(value, 4); p.rectValue = new Rect(r[0], r[1], r[2], r[3]); break;
                case SerializedPropertyType.Bounds: p.boundsValue = new Bounds(Vector3Value(value["center"]), Vector3Value(value["size"])); break;
                case SerializedPropertyType.ObjectReference:
                    var reference = ResolveReference(value, localRoot);
                    if (reference != null && !ReferenceType(p).IsInstanceOfType(reference)) throw new ArgumentException("Object reference type mismatch.");
                    if (reference != null && !EditorUtility.IsPersistent(reference))
                    {
                        if (EditorUtility.IsPersistent(p.serializedObject.targetObject)) throw new ArgumentException("Asset cannot reference a live scene object.");
                        var owner = p.serializedObject.targetObject as Component;
                        var referenced = reference as Component;
                        var referencedObject = referenced != null ? referenced.gameObject : reference as GameObject;
                        if (owner != null && referencedObject != null && owner.gameObject.scene != referencedObject.scene)
                            throw new ArgumentException("Cross-scene references are not supported.");
                    }
                    p.objectReferenceValue = reference; break;
                case SerializedPropertyType.AnimationCurve: p.animationCurveValue = Curve(value); break;
                case SerializedPropertyType.Gradient: GradientProperty.SetValue(p, GradientValue(value)); break;
                default: throw new ArgumentException("Unsupported property type.");
            }
        }

        private static AnimationCurve Curve(JToken token)
        {
            var keys = Array(token?["keys"], 0, 1024);
            var frames = new List<Keyframe>();
            var last = Single.NegativeInfinity;
            foreach (var k in keys)
            {
                var time = Float(k["time"]);
                if (time <= last) throw new ArgumentException("Curve times must be strictly increasing.");
                last = time;
                var frame = new Keyframe(time, Float(k["value"]), Float(k["in_tangent"]), Float(k["out_tangent"]),
                    Unit(k["in_weight"]), Unit(k["out_weight"]));
                frame.weightedMode = EnumValue<WeightedMode>(k["weighted_mode"]);
                frames.Add(frame);
            }
            return new AnimationCurve(frames.ToArray()) { preWrapMode = EnumValue<WrapMode>(token["pre_wrap"]), postWrapMode = EnumValue<WrapMode>(token["post_wrap"]) };
        }

        private static Gradient GradientValue(JToken token)
        {
            var colors = Array(token?["color_keys"], 2, 8).Select(k => { var c = Numbers(k["color"], 4);
                return new GradientColorKey(new Color(c[0], c[1], c[2], c[3]), Unit(k["time"])); }).ToArray();
            var alphas = Array(token?["alpha_keys"], 2, 8).Select(k => new GradientAlphaKey(Unit(k["alpha"]), Unit(k["time"]))).ToArray();
            if (!Sorted(colors.Select(k => k.time)) || !Sorted(alphas.Select(k => k.time))) throw new ArgumentException("Gradient key times must be strictly increasing.");
            var result = new Gradient { mode = EnumValue<GradientMode>(token["mode"]) };
            result.SetKeys(colors, alphas);
            return result;
        }

        private static bool Sorted(IEnumerable<float> values)
        {
            var last = Single.NegativeInfinity;
            foreach (var value in values) { if (value <= last) return false; last = value; }
            return true;
        }

        private static T EnumValue<T>(JToken token) where T : struct
        {
            RequireType(token, JTokenType.String);
            if (!Enum.TryParse<T>((string)token, false, out var value) || !Enum.IsDefined(typeof(T), value)) throw new ArgumentException("Unknown " + typeof(T).Name);
            return value;
        }

        private static Type ReferenceType(SerializedProperty p)
        {
            var type = p.serializedObject.targetObject.GetType();
            foreach (var part in p.propertyPath.Split('.'))
            {
                if (part == "Array") continue;
                if (part.StartsWith("data[", StringComparison.Ordinal))
                { type = type.IsArray ? type.GetElementType() : type.IsGenericType ? type.GetGenericArguments()[0] : null; if (type == null) return null; continue; }
                FieldInfo field = null;
                for (var ancestor = type; ancestor != null && field == null; ancestor = ancestor.BaseType)
                    field = ancestor.GetField(part, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field != null) { type = field.FieldType; continue; }
                var publicProperty = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(info => String.Equals(info.Name, part.StartsWith("m_", StringComparison.Ordinal) ? part.Substring(2) : part, StringComparison.OrdinalIgnoreCase));
                if (publicProperty == null) return null;
                type = publicProperty.PropertyType;
            }
            return typeof(UnityEngine.Object).IsAssignableFrom(type) ? type : null;
        }

        internal static JToken DescribeReference(UnityEngine.Object value, GameObject localRoot = null)
        {
            if (value == null) return JValue.CreateNull();
            var localComponent = value as Component;
            var localObject = localComponent != null ? localComponent.gameObject : value as GameObject;
            if (localRoot != null && localObject != null && localObject.transform.IsChildOf(localRoot.transform))
                return localComponent == null ? new JObject { ["object"] = EditorObjectTarget.HierarchyPath(localObject.transform) } :
                    new JObject { ["component"] = EditorObjectTarget.HierarchyPath(localObject.transform) + "::" + localComponent.GetType().FullName,
                        ["component_index"] = System.Array.IndexOf(localObject.GetComponents(localComponent.GetType()), localComponent) };
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long id) && EditorUtility.IsPersistent(value))
                return new JObject { ["asset_guid"] = guid, ["local_id"] = id, ["asset_path"] = AssetDatabase.GUIDToAssetPath(guid), ["type"] = value.GetType().FullName };
            var component = value as Component;
            var gameObject = component != null ? component.gameObject : value as GameObject;
            if (gameObject == null) return new JObject { ["unsupported"] = "non_asset_non_scene_reference" };
            using (var target = new EditorObjectTarget { GameObject = gameObject, Component = component }) return target.Describe();
        }

        internal static bool Equivalent(JToken expected, JToken actual)
        {
            if (expected == null || actual == null) return expected == actual;
            if (expected.Type == JTokenType.Integer || expected.Type == JTokenType.Float)
                return (actual.Type == JTokenType.Integer || actual.Type == JTokenType.Float) &&
                    Math.Abs(expected.Value<double>() - actual.Value<double>()) <= 0.000001d;
            if (expected.Type != actual.Type)
                return false;
            if (expected is JObject expectedObject && actual is JObject actualObject)
            {
                if (expectedObject.Count != actualObject.Count) return false;
                foreach (var property in expectedObject.Properties())
                    if (!actualObject.TryGetValue(property.Name, out var value) || !Equivalent(property.Value, value)) return false;
                return true;
            }
            if (expected is JArray expectedArray && actual is JArray actualArray)
            {
                if (expectedArray.Count != actualArray.Count) return false;
                for (var index = 0; index < expectedArray.Count; index++)
                    if (!Equivalent(expectedArray[index], actualArray[index])) return false;
                return true;
            }
            return JToken.DeepEquals(expected, actual);
        }

        private static UnityEngine.Object ResolveReference(JToken token, GameObject localRoot)
        {
            if (token == null || token.Type == JTokenType.Null) return null;
            if (!(token is JObject obj)) throw new ArgumentException("Reference must be null or an object selector.");
            if (obj["object"] != null || obj["component"] != null)
            {
                if (localRoot == null || (obj["object"] != null && obj["component"] != null))
                    throw new ArgumentException("Local reference needs an owned Prefab root and exactly one object/component selector.");
                if (obj["object"] != null) return EditorObjectTarget.Find(new[] { localRoot }, (string)obj["object"]);
                var selector = (string)obj["component"];
                var separator = selector.LastIndexOf("::", StringComparison.Ordinal);
                if (separator < 1) throw new ArgumentException("Local component reference requires /path::type.");
                var owner = EditorObjectTarget.Find(new[] { localRoot }, selector.Substring(0, separator));
                var typeName = selector.Substring(separator + 2);
                var matches = owner.GetComponents<Component>().Where(c => c != null && c.GetType().FullName == typeName).ToArray();
                var index = (int?)obj["component_index"];
                if (index == null && matches.Length != 1 || (index ?? 0) < 0 || (index ?? 0) >= matches.Length)
                    throw new ArgumentException("Local component reference is missing or ambiguous.");
                return matches[index ?? 0];
            }
            if (obj["asset_guid"] != null)
            {
                var path = AssetDatabase.GUIDToAssetPath((string)obj["asset_guid"]);
                if (string.IsNullOrEmpty(path)) throw new ArgumentException("Reference asset GUID is missing.");
                var expected = Integer(obj["local_id"]);
                var assets = AssetDatabase.LoadAllAssetsAtPath(path).ToList();
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (root != null)
                {
                    foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    { assets.Add(transform.gameObject); assets.AddRange(transform.GetComponents<Component>()); }
                }
                foreach (var asset in assets.Where(a => a != null))
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long id) && id == expected) return asset;
                throw new ArgumentException("Reference local_id is missing.");
            }
            if (obj["asset_path"] == null && (obj["session_id"] == null || obj["instance_id"] == null))
                throw new ArgumentException("Scene references require session_id and instance_id.");
            using (var target = EditorObjectTarget.Resolve(obj, false)) return target.Component != null ? (UnityEngine.Object)target.Component : target.GameObject;
        }

        private static JArray Vector(Vector3 v) => new JArray(v.x, v.y, v.z);
        private static Vector3 Vector3Value(JToken token) { var v = Numbers(token, 3); return new Vector3(v[0], v[1], v[2]); }
        private static float[] Numbers(JToken token, int count) => Array(token, count, count).Select(Float).ToArray();
        private static int[] Ints(JToken token, int count) => Array(token, count, count).Select(t => checked((int)Integer(t))).ToArray();
        private static JArray Array(JToken token, int min, int max)
        {
            if (!(token is JArray array) || array.Count < min || array.Count > max) throw new ArgumentException("Invalid array size; expected " + min + ".." + max);
            return array;
        }
        private static void RequireType(JToken token, JTokenType type) { if (token == null || token.Type != type) throw new ArgumentException("Expected JSON " + type); }
        private static long Integer(JToken token) { RequireType(token, JTokenType.Integer); return (long)token; }
        private static double Number(JToken token)
        {
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)) throw new ArgumentException("Expected JSON number.");
            var value = (double)token;
            if (Double.IsNaN(value) || Double.IsInfinity(value)) throw new ArgumentException("Non-finite number.");
            return value;
        }
        private static float Float(JToken token) { var value = Number(token); if (Math.Abs(value) > Single.MaxValue) throw new ArgumentException("Float overflow."); return (float)value; }
        private static float Unit(JToken token) { var value = Float(token); if (value < 0 || value > 1) throw new ArgumentException("Expected 0..1."); return value; }
    }
}
#endif
