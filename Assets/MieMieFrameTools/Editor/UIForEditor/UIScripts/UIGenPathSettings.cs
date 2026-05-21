using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MieMieUITools.Editor
{
    [Serializable]
    public class UIGenPrefabFolderRecord
    {
        public string guid;
        public string prefabName;
        public string folder;
    }

    [Serializable]
    public class UIPrefixMappingEntry
    {
        public string prefix;
        public string componentType;
    }

    [Serializable]
    public class UIGenPathSettingsData
    {
        public string defaultFolder = "Assets/_Scripts/UI/";
        public List<UIPrefixMappingEntry> prefixMappings = new();
        public List<UIGenPrefabFolderRecord> prefabFolders = new();
    }

    /// <summary>
    /// UI 生成器独立配置（JSON，位于 UIForEditor/UIGenPath/）。
    /// </summary>
    public static class UIGenPathSettings
    {
        public const string DefaultRelativePath =
            "Assets/MieMieFrameTools/Editor/UIForEditor/UIGenPath/UIGenPathSettings.json";

        private static UIGenPathSettingsData _cached;

        public static UIGenPathSettingsData Load()
        {
            if (_cached != null) return _cached;

            string path = ResolveSettingsPath();
            if (File.Exists(path))
            {
                try
                {
                    _cached = JsonUtility.FromJson<UIGenPathSettingsData>(File.ReadAllText(path));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[UIGenPathSettings] 读取失败: {e.Message}");
                }
            }

            _cached ??= new UIGenPathSettingsData();
            _cached.defaultFolder = NormalizeFolderPath(_cached.defaultFolder) ?? "Assets/_Scripts/UI/";
            _cached.prefabFolders ??= new List<UIGenPrefabFolderRecord>();
            _cached.prefixMappings ??= new List<UIPrefixMappingEntry>();

            if (_cached.prefixMappings.Count == 0)
                _cached.prefixMappings = CreateBuiltInPrefixMappings();

            if (!File.Exists(path))
                Save(_cached);

            return _cached;
        }

        public static void Save(UIGenPathSettingsData data = null)
        {
            data ??= _cached ?? new UIGenPathSettingsData();
            _cached = data;
            data.defaultFolder = NormalizeFolderPath(data.defaultFolder) ?? "Assets/_Scripts/UI/";
            data.prefabFolders ??= new List<UIGenPrefabFolderRecord>();
            data.prefixMappings ??= new List<UIPrefixMappingEntry>();

            string path = ResolveSettingsPath();
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, JsonUtility.ToJson(data, true), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
        }

        public static void InvalidateCache() => _cached = null;

        public static IReadOnlyList<UIPrefixMappingEntry> GetPrefixMappings() => Load().prefixMappings;

        public static void SetPrefixMappings(IEnumerable<UIPrefixMappingEntry> mappings)
        {
            var data = Load();
            data.prefixMappings = new List<UIPrefixMappingEntry>();
            if (mappings == null)
            {
                Save(data);
                return;
            }

            foreach (var m in mappings)
            {
                if (m == null || string.IsNullOrWhiteSpace(m.prefix) || string.IsNullOrWhiteSpace(m.componentType))
                    continue;
                data.prefixMappings.Add(new UIPrefixMappingEntry
                {
                    prefix = m.prefix.Trim(),
                    componentType = m.componentType.Trim()
                });
            }

            Save(data);
        }

        public static void ResetPrefixMappingsToBuiltIn()
        {
            var data = Load();
            data.prefixMappings = CreateBuiltInPrefixMappings();
            Save(data);
        }

        public static Dictionary<string, string> BuildPrefixToTypeDictionary()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in GetPrefixMappings())
            {
                if (entry == null || string.IsNullOrEmpty(entry.prefix) || string.IsNullOrEmpty(entry.componentType))
                    continue;
                dict[entry.prefix] = entry.componentType;
            }

            if (dict.Count == 0)
            {
                foreach (var kv in GetBuiltInPrefixDefaults())
                    dict[kv.Key] = kv.Value;
            }

            return dict;
        }

        public static IReadOnlyDictionary<string, string> GetBuiltInPrefixDefaults() => BuiltInPrefixDefaults;

        private static readonly Dictionary<string, string> BuiltInPrefixDefaults =
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "Img", "Image" },
                { "Image", "Image" },
                { "Btn", "Button" },
                { "Button", "Button" },
                { "Text", "Text" },
                { "Tmp", "TextMeshProUGUI" },
                { "Toggle", "Toggle" },
                { "Tg", "Toggle" },
                { "Input", "InputField" },
                { "Ipt", "TMP_InputField" },
                { "Drop", "TMP_Dropdown" },
                { "Slider", "Slider" },
                { "Scroll", "ScrollRect" },
                { "ScrollView", "ScrollRect" },
                { "Panel", "RectTransform" },
                { "RawImg", "RawImage" },
                { "RawImage", "RawImage" },
                { "RT", "RectTransform" },
            };

        private static List<UIPrefixMappingEntry> CreateBuiltInPrefixMappings()
        {
            var list = new List<UIPrefixMappingEntry>();
            foreach (var kv in BuiltInPrefixDefaults)
            {
                list.Add(new UIPrefixMappingEntry { prefix = kv.Key, componentType = kv.Value });
            }

            return list;
        }

        public static string GetDefaultFolder() => Load().defaultFolder;

        public static void SetDefaultFolder(string folder)
        {
            var data = Load();
            data.defaultFolder = NormalizeFolderPath(folder) ?? data.defaultFolder;
            Save(data);
        }

        public static string GetLastFolderForPrefab(string prefabGuid)
        {
            if (string.IsNullOrEmpty(prefabGuid)) return null;
            var record = Load().prefabFolders.Find(r => r.guid == prefabGuid);
            return record == null ? null : NormalizeFolderPath(record.folder);
        }

        public static void SetFolderForPrefab(string prefabGuid, string prefabName, string folder)
        {
            if (string.IsNullOrEmpty(prefabGuid)) return;
            string normalized = NormalizeFolderPath(folder);
            if (string.IsNullOrEmpty(normalized)) return;

            var data = Load();
            var record = data.prefabFolders.Find(r => r.guid == prefabGuid);
            if (record != null)
            {
                record.prefabName = prefabName;
                record.folder = normalized;
            }
            else
            {
                data.prefabFolders.Add(new UIGenPrefabFolderRecord
                {
                    guid = prefabGuid,
                    prefabName = prefabName,
                    folder = normalized
                });
            }

            Save(data);
        }

        public static void ClearPrefabFolderRecords()
        {
            var data = Load();
            data.prefabFolders.Clear();
            Save(data);
        }

        public static string NormalizeFolderPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            path = path.Trim().Replace('\\', '/');
            while (path.Length > 1 && path.EndsWith("/"))
                path = path.Substring(0, path.Length - 1);

            if (path.StartsWith("Assets/", StringComparison.Ordinal) ||
                string.Equals(path, "Assets", StringComparison.Ordinal))
                return path;

            string dataPath = Application.dataPath.Replace('\\', '/');
            if (path.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                string relative = path.Substring(dataPath.Length).TrimStart('/');
                return string.IsNullOrEmpty(relative) ? "Assets" : $"Assets/{relative}";
            }

            return path;
        }

        public static string ToAbsoluteFolderForDialog(string assetFolder)
        {
            string normalized = NormalizeFolderPath(assetFolder);
            if (string.IsNullOrEmpty(normalized))
                return Application.dataPath;

            if (normalized.StartsWith("Assets/", StringComparison.Ordinal) ||
                string.Equals(normalized, "Assets", StringComparison.Ordinal))
            {
                string relative = normalized == "Assets" ? "" : normalized.Substring("Assets/".Length);
                return string.IsNullOrEmpty(relative)
                    ? Application.dataPath
                    : Path.GetFullPath(Path.Combine(Application.dataPath,
                        relative.Replace('/', Path.DirectorySeparatorChar)));
            }

            if (Directory.Exists(normalized))
                return normalized;

            return Application.dataPath;
        }

        public static string ResolveSettingsPath()
        {
            foreach (var guid in AssetDatabase.FindAssets("UIGenPathSettings t:TextAsset"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.EndsWith("/UIGenPathSettings.json", StringComparison.OrdinalIgnoreCase))
                    return p;
            }

            if (UIToolLocator.TryGetUIGenPathDirectory(out string dir))
                return $"{dir}/UIGenPathSettings.json";

            return DefaultRelativePath;
        }
    }
}
