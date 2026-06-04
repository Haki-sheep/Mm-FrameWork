using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor
{
    [Serializable]
    public class FolderProjectEntry
    {
        public string folderGuid;
        public string iconGuid;
        public string displayName;
        public Color rowColor = new(0.22f, 0.52f, 0.92f, 0.45f);
        public Color labelColor;
        public bool labelBold = true;
    }

    [Serializable]
    public class FolderProjectData
    {
        public bool enableProjectTree = true;
        public Color treeLineColor = new(1f, 1f, 1f, 0.9f);
        public List<FolderProjectEntry> entries = new();
    }

    public static class FolderProjectSettings
    {
        public const string SettingsRelativePath =
            "Assets/MieMieFrameTools/Editor/FolderForEditor/ProjectWindow/FolderProjectSettings.json";

        public static readonly Color DefaultRowColor = new(0.22f, 0.52f, 0.92f, 0.45f);

        private static FolderProjectData _cached;
        private static Texture2D _defaultFolderIcon;

        public static FolderProjectData Load()
        {
            if (_cached != null) return _cached;

            string path = ResolvePath();
            if (File.Exists(path))
            {
                try
                {
                    _cached = JsonUtility.FromJson<FolderProjectData>(File.ReadAllText(path));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[FolderProject] 读取配置失败: {e.Message}");
                }
            }

            _cached ??= new FolderProjectData();
            _cached.entries ??= new List<FolderProjectEntry>();

            if (!File.Exists(path))
                Save(_cached);

            return _cached;
        }

        public static void Save(FolderProjectData data = null)
        {
            data ??= _cached ?? new FolderProjectData();
            _cached = data;
            data.entries ??= new List<FolderProjectEntry>();

            string path = ResolvePath();
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, JsonUtility.ToJson(data, true), System.Text.Encoding.UTF8);
            EditorApplication.RepaintProjectWindow();
        }

        public static void InvalidateCache() => _cached = null;

        public static IReadOnlyList<FolderProjectEntry> GetEntries() => Load().entries;

        public static bool EnableProjectTree
        {
            get => Load().enableProjectTree;
            set
            {
                Load().enableProjectTree = value;
                Save();
            }
        }

        public static Color TreeLineColor
        {
            get => Load().treeLineColor.a > 0.01f ? Load().treeLineColor : new Color(1f, 1f, 1f, 0.9f);
            set
            {
                Load().treeLineColor = value;
                Save();
            }
        }

        public static FolderProjectEntry Find(string folderGuid)
        {
            foreach (FolderProjectEntry entry in Load().entries)
            {
                if (entry.folderGuid == folderGuid)
                    return entry;
            }
            return null;
        }

        public static bool IsBookmarked(string folderGuid) => Find(folderGuid) != null;

        public static bool HasCustomLabelStyle(FolderProjectEntry entry)
        {
            if (entry == null) return false;
            if (!string.IsNullOrEmpty(entry.displayName)) return true;
            if (entry.labelColor.a > 0.01f) return true;
            if (entry.labelBold) return true;
            return false;
        }

        public static string GetProjectLabelText(FolderProjectEntry entry, string folderPath)
        {
            if (!string.IsNullOrEmpty(entry.displayName))
                return entry.displayName;

            return string.IsNullOrEmpty(folderPath) ? string.Empty : Path.GetFileName(folderPath);
        }

        public static bool HasProjectStyle(FolderProjectEntry entry)
        {
            if (entry == null) return false;
            if (!string.IsNullOrEmpty(entry.iconGuid)) return true;
            if (!string.IsNullOrEmpty(entry.displayName)) return true;
            if (entry.rowColor.a > 0.01f) return true;
            if (entry.labelColor.a > 0.01f) return true;
            return false;
        }

        public static void AddBookmark(string folderGuid)
        {
            if (string.IsNullOrEmpty(folderGuid) || IsBookmarked(folderGuid))
                return;

            Load().entries.Add(new FolderProjectEntry
            {
                folderGuid = folderGuid,
                rowColor = DefaultRowColor
            });
            Save();
        }

        public static void RemoveBookmark(string folderGuid)
        {
            FolderProjectData data = Load();
            data.entries.RemoveAll(e => e.folderGuid == folderGuid);
            Save(data);
        }

        public static void UpdateEntry(FolderProjectEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.folderGuid))
                return;

            FolderProjectEntry existing = Find(entry.folderGuid);
            if (existing == null)
                Load().entries.Add(entry);
            else
            {
                existing.iconGuid = entry.iconGuid ?? string.Empty;
                existing.displayName = entry.displayName ?? string.Empty;
                existing.rowColor = entry.rowColor;
                existing.labelColor = entry.labelColor;
                existing.labelBold = entry.labelBold;
            }

            Save();
        }

        public static void ResetStyle(string folderGuid)
        {
            FolderProjectEntry entry = Find(folderGuid);
            if (entry == null) return;

            entry.iconGuid = string.Empty;
            entry.displayName = string.Empty;
            entry.rowColor = DefaultRowColor;
            entry.labelColor = Color.clear;
            entry.labelBold = true;
            Save();
        }

        public static string GetDisplayName(FolderProjectEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.displayName))
                return entry.displayName;

            string path = AssetDatabase.GUIDToAssetPath(entry.folderGuid);
            return string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path);
        }

        public static Texture2D GetCustomIcon(string folderGuid)
        {
            FolderProjectEntry entry = Find(folderGuid);
            if (entry == null || string.IsNullOrEmpty(entry.iconGuid))
                return null;

            string iconPath = AssetDatabase.GUIDToAssetPath(entry.iconGuid);
            if (string.IsNullOrEmpty(iconPath))
                return null;

            return AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        }

        public static Texture2D GetDisplayIcon(FolderProjectEntry entry)
        {
            Texture2D custom = GetCustomIcon(entry.folderGuid);
            if (custom != null)
                return custom;

            string folderPath = AssetDatabase.GUIDToAssetPath(entry.folderGuid);
            if (!string.IsNullOrEmpty(folderPath))
            {
                Texture cached = AssetDatabase.GetCachedIcon(folderPath);
                if (cached is Texture2D tex)
                    return tex;
            }

            return DefaultFolderIcon;
        }

        public static Color ResolveLabelColor(FolderProjectEntry entry)
        {
            if (entry.labelColor.a > 0.01f)
                return entry.labelColor;
            return EditorGUIUtility.isProSkin ? Color.white : new Color(0.1f, 0.1f, 0.1f);
        }

        public static Texture2D DefaultFolderIcon
        {
            get
            {
                if (_defaultFolderIcon == null)
                    _defaultFolderIcon = EditorGUIUtility.IconContent("Folder Icon").image as Texture2D;
                return _defaultFolderIcon;
            }
        }

        private static string ResolvePath()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot, SettingsRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
