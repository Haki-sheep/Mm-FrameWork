#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FakeUnityCLI.EditorBridge
{
    internal sealed class EditorObjectTarget : IDisposable
    {
        internal GameObject Root;
        internal GameObject GameObject;
        internal Component Component;
        internal string AssetPath;
        internal string AssetHash;
        private bool ownsPrefab;

        internal static EditorObjectTarget Resolve(JObject target, bool write)
        {
            if (target == null) throw new ArgumentException("target is required.");
            var result = new EditorObjectTarget();
            try
            {
                result.AssetPath = (string)target["asset_path"];
                var path = (string)target["object_path"];
                if (string.IsNullOrEmpty(path) || !path.StartsWith("/", StringComparison.Ordinal))
                    throw new ArgumentException("target.object_path must be an absolute hierarchy path beginning with /.");
                if (!string.IsNullOrEmpty(result.AssetPath))
                {
                    ValidateAssetPath(result.AssetPath);
                    result.AssetHash = FileHash(result.AssetPath);
                    if (write && !String.Equals(result.AssetHash, (string)target["asset_sha256"], StringComparison.Ordinal))
                        throw new InvalidOperationException("Prefab changed or asset_sha256 is missing; read again before writing.");
                    if (write && PrefabStageUtility.GetCurrentPrefabStage() != null)
                        throw new InvalidOperationException("Close the current Prefab Stage before editing an asset.");
                    result.Root = write ? PrefabUtility.LoadPrefabContents(result.AssetPath) : AssetDatabase.LoadAssetAtPath<GameObject>(result.AssetPath);
                    result.ownsPrefab = write;
                    if (result.Root == null) throw new ArgumentException("Prefab does not exist.");
                    result.GameObject = Find(new[] { result.Root }, path);
                }
                else
                {
                    var scenePath = (string)target["scene_path"];
                    var sceneHandle = EditorIdentity.OptionalToken(target, "scene_handle");
                    var session = (string)target["session_id"];
                    var instance = EditorIdentity.OptionalToken(target, "instance_id");
                    if (scenePath == null && sceneHandle == null)
                        throw new ArgumentException("Use an explicit scene_path or scene_handle; Selection is never a fallback.");
                    if ((write || instance != null) && String.IsNullOrEmpty(session)) throw new ArgumentException("Object identity requires session_id from inspection.");
                    if (session != null && session != LogCaptureBridge.CurrentSessionId) throw new InvalidOperationException("Object selector belongs to another Editor session.");
                    if (write && instance == null) throw new ArgumentException("Writes require instance_id returned by inspection.");
                    var scenes = new List<Scene>();
                    for (var i = 0; i < SceneManager.sceneCount; i++)
                    {
                        var scene = SceneManager.GetSceneAt(i);
                        if (scene.isLoaded && (scenePath == null || scene.path == scenePath) &&
                            (sceneHandle == null || EditorIdentity.Matches(EditorIdentity.SceneId(scene), sceneHandle))) scenes.Add(scene);
                    }
                    if (instance != null)
                    {
                        var resolved = EditorIdentity.Resolve(instance) as GameObject;
                        if (resolved == null || EditorUtility.IsPersistent(resolved) || !resolved.scene.IsValid() || !resolved.scene.isLoaded ||
                            EditorSceneManager.IsPreviewScene(resolved.scene) || sceneHandle != null && !EditorIdentity.Matches(EditorIdentity.SceneId(resolved.scene), sceneHandle) ||
                            scenePath != null && resolved.scene.path != scenePath || HierarchyPath(resolved.transform) != path)
                            throw new InvalidOperationException("Object identity/path/scene changed; inspect again.");
                        if (scenes.Count == 0 && (!EditorApplication.isPlaying || resolved.scene.name != "DontDestroyOnLoad"))
                            throw new ArgumentException("Object does not belong to an addressable loaded scene.");
                        result.GameObject = resolved;
                    }
                    else
                    {
                        if (scenes.Count != 1) throw new ArgumentException("Scene selector is absent or ambiguous.");
                        result.GameObject = Find(scenes[0].GetRootGameObjects(), path);
                    }
                    var stage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (stage != null && stage.scene == result.GameObject.scene)
                        throw new InvalidOperationException("Prefab Stage objects require the explicit asset workflow.");
                    result.Root = null;
                }
                var typeName = (string)target["component_type"];
                if (typeName != null)
                {
                    var components = result.GameObject.GetComponents<Component>().Where(c => c != null &&
                        (c.GetType().FullName == typeName || c.GetType().Name == typeName)).ToArray();
                    var index = (int?)target["component_index"];
                    if (index == null && components.Length != 1)
                        throw new ArgumentException("Component is absent or ambiguous; supply component_index from inspection.");
                    if (index < 0 || (index ?? 0) >= components.Length) throw new ArgumentException("component_index is out of range.");
                    result.Component = components[index ?? 0];
                    var componentId = EditorIdentity.OptionalToken(target, "component_instance_id");
                    if (String.IsNullOrEmpty(result.AssetPath) && write && componentId == null)
                        throw new ArgumentException("Writes require component_instance_id from inspection.");
                    if (String.IsNullOrEmpty(result.AssetPath) && componentId != null && !EditorIdentity.Matches(EditorIdentity.InstanceId(result.Component), componentId))
                        throw new InvalidOperationException("Component identity changed; inspect again.");
                }
                return result;
            }
            catch { result.Dispose(); throw; }
        }

        internal JObject Describe()
        {
            var value = new JObject { ["object_path"] = HierarchyPath(GameObject.transform) };
            if (!String.IsNullOrEmpty(AssetPath))
            {
                value["asset_path"] = AssetPath;
                value["asset_sha256"] = AssetHash;
                value["source"] = "prefab_asset";
            }
            else
            {
                value["scene_path"] = GameObject.scene.path;
                value["scene_handle"] = JToken.FromObject(EditorIdentity.SceneId(GameObject.scene));
                value["instance_id"] = JToken.FromObject(EditorIdentity.InstanceId(GameObject));
                value["session_id"] = LogCaptureBridge.CurrentSessionId;
                if (EditorApplication.isPlaying)
                    value["play_mode_observed_since"] = EditorActivityBridge.Snapshot().mode_observed_since;
                value["source"] = EditorApplication.isPlaying ? GameObject.scene.name == "DontDestroyOnLoad" ? "play_persistent_scene" : "play_scene" : "edit_scene";
            }
            if (Component != null)
            {
                value["component_type"] = Component.GetType().FullName;
                value["component_index"] = Array.IndexOf(GameObject.GetComponents(Component.GetType()), Component);
                if (String.IsNullOrEmpty(AssetPath)) value["component_instance_id"] = JToken.FromObject(EditorIdentity.InstanceId(Component));
            }
            return value;
        }

        internal static GameObject Find(IEnumerable<GameObject> roots, string path)
        {
            var segments = path.Substring(1).Split('/');
            if (segments.Any(String.IsNullOrEmpty)) throw new ArgumentException("Hierarchy path has an empty segment.");
            var candidates = roots.Where(r => r.name == segments[0]).ToList();
            foreach (var segment in segments.Skip(1))
                candidates = candidates.SelectMany(r => r.transform.Cast<Transform>()).Where(t => t.name == segment).Select(t => t.gameObject).ToList();
            if (candidates.Count != 1) throw new ArgumentException("Object selector is absent or ambiguous: " + path);
            return candidates[0];
        }

        internal static string HierarchyPath(Transform transform)
        {
            var names = new List<string>();
            for (var current = transform; current != null; current = current.parent) names.Add(current.name);
            names.Reverse();
            return "/" + String.Join("/", names);
        }

        internal static void ValidateAssetPath(string path)
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                path.Contains('\\') || path.Split('/').Any(s => s == "." || s == ".." || s.Length == 0))
                throw new ArgumentException("Only canonical Assets/*.prefab paths are supported.");
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var full = Path.GetFullPath(Path.Combine(root, path));
            for (var current = full; current != root; current = Path.GetDirectoryName(current))
            {
                if (current == null) throw new ArgumentException("Asset path escaped the project.");
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new ArgumentException("Linked asset paths are not allowed.");
            }
        }

        internal static string FileHash(string path)
        {
            using (var stream = File.OpenRead(Path.Combine(Application.dataPath, "..", path)))
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        public void Dispose()
        {
            if (ownsPrefab && Root != null) PrefabUtility.UnloadPrefabContents(Root);
            ownsPrefab = false;
        }
    }
}
#endif
