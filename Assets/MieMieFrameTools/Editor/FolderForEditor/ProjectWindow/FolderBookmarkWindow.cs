#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor
{
    public class FolderBookmarkWindow : EditorWindow
    {
        private Vector2 _scroll;
        private readonly Dictionary<string, Texture2D> _iconFieldCache = new();

        [MenuItem("Tools/MieMieFrameWork/Folder/Folder Bookmarks")]
        public static void Open() 
        {
            GetWindow<FolderBookmarkWindow>("文件夹收藏");
        } 

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("常用文件夹收藏", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            bool enableTree = EditorGUILayout.Toggle("启用文件夹树形连线", FolderProjectSettings.EnableProjectTree);
            Color treeColor = EditorGUILayout.ColorField("树形线条颜色", FolderProjectSettings.TreeLineColor);
            if (EditorGUI.EndChangeCheck())
            {
                FolderProjectSettings.EnableProjectTree = enableTree;
                FolderProjectSettings.TreeLineColor = treeColor;
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加当前选中文件夹", GUILayout.Height(24)))
                    AddSelectedFolders();

                if (GUILayout.Button("刷新", GUILayout.Width(60), GUILayout.Height(24)))
                {
                    FolderProjectSettings.InvalidateCache();
                    _iconFieldCache.Clear();
                    Repaint();
                }
            }

            EditorGUILayout.Space(4);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            List<FolderProjectEntry> entries = new(FolderProjectSettings.GetEntries());
            if (entries.Count == 0)
            {
                EditorGUILayout.LabelField("暂无收藏，在 Project 中右键文件夹添加。", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                for (int i = 0; i < entries.Count; i++)
                    DrawEntry(entries[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawEntry(FolderProjectEntry entry)
        {
            string folderPath = AssetDatabase.GUIDToAssetPath(entry.folderGuid);
            if (string.IsNullOrEmpty(folderPath))
            {
                EditorGUILayout.HelpBox($"无效 GUID: {entry.folderGuid}", MessageType.Warning);
                return;
            }

            string folderName = Path.GetFileName(folderPath);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawPreviewBar(entry, folderPath);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(folderPath, EditorStyles.miniLabel))
                        PingFolder(entry.folderGuid);

                    if (GUILayout.Button("移除", GUILayout.Width(44)))
                    {
                        FolderProjectSettings.RemoveBookmark(entry.folderGuid);
                        _iconFieldCache.Remove(entry.folderGuid);
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUI.BeginChangeCheck();

                entry.displayName = EditorGUILayout.TextField("显示别名", entry.displayName);
                EditorGUILayout.LabelField("真实名称", folderName, EditorStyles.miniLabel);
                entry.rowColor = EditorGUILayout.ColorField("行背景色", entry.rowColor);
                entry.labelColor = EditorGUILayout.ColorField("文字颜色", entry.labelColor);
                entry.labelBold = EditorGUILayout.Toggle("文字加粗", entry.labelBold);

                if (!_iconFieldCache.TryGetValue(entry.folderGuid, out Texture2D iconFieldValue))
                {
                    iconFieldValue = FolderProjectSettings.GetCustomIcon(entry.folderGuid);
                    _iconFieldCache[entry.folderGuid] = iconFieldValue;
                }

                iconFieldValue = (Texture2D)EditorGUILayout.ObjectField("自定义 Icon", iconFieldValue, typeof(Texture2D), false);

                if (EditorGUI.EndChangeCheck())
                {
                    _iconFieldCache[entry.folderGuid] = iconFieldValue;
                    entry.iconGuid = iconFieldValue != null
                        ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(iconFieldValue))
                        : string.Empty;
                    FolderProjectSettings.UpdateEntry(entry);
                }

                if (GUILayout.Button("重置样式为默认"))
                {
                    FolderProjectSettings.ResetStyle(entry.folderGuid);
                    _iconFieldCache.Remove(entry.folderGuid);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static void DrawPreviewBar(FolderProjectEntry entry, string folderPath)
        {
            Rect barRect = GUILayoutUtility.GetRect(0f, 22f, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint)
                return;

            if (entry.rowColor.a > 0.01f)
                EditorGUI.DrawRect(barRect, entry.rowColor);

            Texture2D icon = FolderProjectSettings.GetDisplayIcon(entry);
            if (icon != null)
            {
                Rect iconRect = new(barRect.x + 4f, barRect.y + 3f, 16f, 16f);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }

            string label = FolderProjectSettings.GetProjectLabelText(entry, folderPath);
            GUIStyle style = new(EditorStyles.boldLabel)
            {
                fontStyle = entry.labelBold ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = FolderProjectSettings.ResolveLabelColor(entry) }
            };
            GUI.Label(new Rect(barRect.x + 24f, barRect.y + 2f, barRect.width - 28f, barRect.height), label, style);
        }

        private static void AddSelectedFolders()
        {
            bool added = false;
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!AssetDatabase.IsValidFolder(path))
                    continue;

                FolderProjectSettings.AddBookmark(guid);
                added = true;
            }

            if (!added)
                EditorUtility.DisplayDialog("文件夹收藏", "请先在 Project 中选中一个或多个文件夹。", "确定");
        }

        internal static void PingFolder(string folderGuid)
        {
            string path = AssetDatabase.GUIDToAssetPath(folderGuid);
            UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (folder == null)
                return;

            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }

        #region Context Menu

        [MenuItem("Assets/文件夹收藏/添加到收藏", false, 20)]
        private static void MenuAddBookmark()
        {
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path))
                    FolderProjectSettings.AddBookmark(guid);
            }
        }

        [MenuItem("Assets/文件夹收藏/添加到收藏", true)]
        private static bool MenuAddBookmarkValidate()
        {
            foreach (string guid in Selection.assetGUIDs)
            {
                if (AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(guid)))
                    return true;
            }
            return false;
        }

        [MenuItem("Assets/文件夹收藏/从收藏移除", false, 21)]
        private static void MenuRemoveBookmark()
        {
            foreach (string guid in Selection.assetGUIDs)
                FolderProjectSettings.RemoveBookmark(guid);
        }

        [MenuItem("Assets/文件夹收藏/从收藏移除", true)]
        private static bool MenuRemoveBookmarkValidate()
        {
            foreach (string guid in Selection.assetGUIDs)
            {
                if (FolderProjectSettings.IsBookmarked(guid))
                    return true;
            }
            return false;
        }

        [MenuItem("Assets/文件夹收藏/重置样式", false, 22)]
        private static void MenuResetStyle()
        {
            foreach (string guid in Selection.assetGUIDs)
            {
                if (FolderProjectSettings.IsBookmarked(guid))
                    FolderProjectSettings.ResetStyle(guid);
            }
        }

        [MenuItem("Assets/文件夹收藏/重置样式", true)]
        private static bool MenuResetStyleValidate()
        {
            foreach (string guid in Selection.assetGUIDs)
            {
                if (FolderProjectSettings.IsBookmarked(guid))
                    return true;
            }
            return false;
        }

        #endregion
    }

    [InitializeOnLoad]
    internal static class FolderProjectOverlay
    {
        private const float IconPadding = 1f;

        static FolderProjectOverlay()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItem;
        }

        private static void OnProjectWindowItem(string guid, Rect rect)
        {
            if (Event.current.type != EventType.Repaint || !IsTreeRow(rect))
                return;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetDatabase.IsValidFolder(path))
                return;

            FolderProjectEntry entry = FolderProjectSettings.Find(guid);

            if (FolderProjectSettings.EnableProjectTree)
            {
                Color foldoutCover = GetFoldoutCoverColor(guid, entry);
                FolderProjectTreeDrawer.Draw(path, rect, FolderProjectSettings.TreeLineColor, foldoutCover);
            }

            if (!FolderProjectSettings.HasProjectStyle(entry))
                return;

            bool hasRowColor = entry.rowColor.a > 0.01f;
            bool shouldDrawLabel = FolderProjectSettings.HasCustomLabelStyle(entry);
            Texture2D customIcon = FolderProjectSettings.GetCustomIcon(guid);

            if (hasRowColor)
                EditorGUI.DrawRect(rect, entry.rowColor);

            float iconSize = rect.height;
            Rect iconRect = new(rect.x + IconPadding, rect.y + IconPadding, iconSize - IconPadding * 2f, iconSize - IconPadding * 2f);
            Rect labelRect = new(rect.x + iconSize + 2f, rect.y, rect.xMax - (rect.x + iconSize + 2f), rect.height);

            if (shouldDrawLabel)
            {
                Color cover = hasRowColor
                    ? new Color(entry.rowColor.r, entry.rowColor.g, entry.rowColor.b, Mathf.Max(entry.rowColor.a, 0.85f))
                    : GetSkinBackgroundColor();
                EditorGUI.DrawRect(labelRect, cover);

                GUIStyle labelStyle = new(EditorStyles.label)
                {
                    fontStyle = entry.labelBold ? FontStyle.Bold : FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = FolderProjectSettings.ResolveLabelColor(entry) }
                };
                GUI.Label(labelRect, FolderProjectSettings.GetProjectLabelText(entry, path), labelStyle);
            }

            if (customIcon != null)
            {
                Color iconCover = hasRowColor
                    ? new Color(entry.rowColor.r, entry.rowColor.g, entry.rowColor.b, Mathf.Max(entry.rowColor.a, 0.85f))
                    : GetSkinBackgroundColor();
                EditorGUI.DrawRect(iconRect, iconCover);
                GUI.DrawTexture(iconRect, customIcon, ScaleMode.ScaleToFit);
            }
        }

        private static Color GetSkinBackgroundColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f)
                : new Color(0.76f, 0.76f, 0.76f);
        }

        private static Color GetFoldoutCoverColor(string guid, FolderProjectEntry entry)
        {
            if (entry != null && entry.rowColor.a > 0.01f)
                return new Color(entry.rowColor.r, entry.rowColor.g, entry.rowColor.b, Mathf.Max(entry.rowColor.a, 0.92f));

            if (IsFolderSelected(guid))
            {
                return EditorGUIUtility.isProSkin
                    ? new Color(0.17f, 0.36f, 0.53f)
                    : new Color(0.20f, 0.41f, 0.72f);
            }

            return GetSkinBackgroundColor();
        }

        private static bool IsFolderSelected(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (UnityEngine.Object obj in Selection.objects)
            {
                if (AssetDatabase.GetAssetPath(obj) == path)
                    return true;
            }

            return false;
        }

        private static bool IsTreeRow(Rect rect) => rect.height > 0f && rect.height <= 20f;
    }

    internal static class FolderProjectTreeDrawer
    {
        private const float FillLineOffset = 14f;
        private const float FoldoutWidth = 14f;
        private const string BranchResourcePrefix =
            "Assets/MieMieFrameTools/Plugins/Hierarchy Designer/Editor/Resources/Hierarchy Designer Tree Branch Icon Default ";

        private static Texture2D _branchI;
        private static Texture2D _branchL;
        private static Texture2D _branchT;
        private static Texture2D _branchTerminal;

        private static readonly Dictionary<string, string[]> SubfolderCache = new();

        static FolderProjectTreeDrawer()
        {
            EditorApplication.projectChanged += SubfolderCache.Clear;
        }

        public static void Draw(string folderPath, Rect rect, Color lineColor, Color foldoutCoverColor)
        {
            folderPath = NormalizePath(folderPath);
            int depth = GetFolderDepth(folderPath);
            if (depth <= 0)
                return;

            HideDefaultFoldout(rect, foldoutCoverColor);

            EnsureBranchTextures();
            if (_branchI == null)
                return;

            float size = rect.height;
            float foldoutX = rect.x - FoldoutWidth;
            float junctionX = foldoutX - FillLineOffset;
            bool isLast = IsLastChildFolder(folderPath);

            Color previousColor = GUI.color;
            GUI.color = lineColor;

            Texture2D connector = isLast ? _branchL : _branchT;
            GUI.DrawTexture(new Rect(junctionX, rect.y, size, size), connector, ScaleMode.ScaleToFit);

            float rectX = junctionX;
            for (int level = depth - 1; level >= 1; level--)
            {
                rectX -= FillLineOffset;
                string ancestorPath = GetPathAtDepth(folderPath, level);
                if (IsLastChildFolder(ancestorPath))
                    continue;

                GUI.DrawTexture(new Rect(rectX, rect.y, size, size), _branchI, ScaleMode.ScaleToFit);
            }

            // 每个文件夹都用小方块替代原折叠箭头（不限于同级最后一个）
            GUI.DrawTexture(new Rect(foldoutX, rect.y, size, size), _branchTerminal, ScaleMode.ScaleToFit);

            GUI.color = previousColor;
        }

        private static void HideDefaultFoldout(Rect rect, Color coverColor)
        {
            EditorGUI.DrawRect(new Rect(rect.x - FoldoutWidth, rect.y, FoldoutWidth, rect.height), coverColor);
        }

        private static void EnsureBranchTextures()
        {
            if (_branchI != null)
                return;

            _branchI = LoadBranchTexture("I");
            _branchL = LoadBranchTexture("L");
            _branchT = LoadBranchTexture("T");
            _branchTerminal = LoadBranchTexture("Terminal Bud");
        }

        private static Texture2D LoadBranchTexture(string suffix)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"{BranchResourcePrefix}{suffix}.png");
        }

        private static int GetFolderDepth(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "Assets")
                return 0;

            return path.Split('/').Length - 1;
        }

        private static string GetPathAtDepth(string path, int depth)
        {
            string[] parts = path.Split('/');
            int count = Mathf.Clamp(depth + 1, 1, parts.Length);
            return string.Join("/", parts, 0, count);
        }

        private static bool IsLastChildFolder(string folderPath)
        {
            folderPath = NormalizePath(folderPath);
            string parentPath = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parentPath))
                return true;

            string[] siblings = GetSubfolders(parentPath);
            return siblings.Length == 0 || siblings[siblings.Length - 1] == folderPath;
        }

        private static string[] GetSubfolders(string parentPath)
        {
            parentPath = NormalizePath(parentPath);
            if (SubfolderCache.TryGetValue(parentPath, out string[] cached))
                return cached;

            string[] subfolders = AssetDatabase.GetSubFolders(parentPath);
            for (int i = 0; i < subfolders.Length; i++)
                subfolders[i] = NormalizePath(subfolders[i]);

            Array.Sort(subfolders, StringComparer.OrdinalIgnoreCase);
            SubfolderCache[parentPath] = subfolders;
            return subfolders;
        }

        private static string NormalizePath(string path) => path?.Replace('\\', '/');
    }
}
#endif
