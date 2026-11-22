using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Localization.FontManagement
{
    public sealed partial class FontManagerWindow
    {
        private const float MenuWidth = 240f;
        private static readonly (FontManagerPageKind Kind, string Label)[] FontPageList =
        {
            (FontManagerPageKind.Preview, "预览调参"),
            (FontManagerPageKind.Missing, "缺失补齐"),
            (FontManagerPageKind.Performance, "性能优化"),
            (FontManagerPageKind.Apply, "应用到 UI")
        };

        private readonly HashSet<TMP_FontAsset> ExpandedFontHashList = new HashSet<TMP_FontAsset>();
        private FontBakeProfile[] ProfileList = Array.Empty<FontBakeProfile>();
        private FontManagerMenuTarget SelectedTarget;
        private FontManagerMenuTarget PendingTarget;
        private Vector2 NavigationScroll;
        private string NavigationSearch = string.Empty;
        private bool FontsExpanded = true;
        private bool ProfilesExpanded = true;

        internal void RefreshMenuSelection()
        {
            DiscoverTmpAssets();
            ProfileList = AssetDatabase.FindAssets("t:FontBakeProfile")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<FontBakeProfile>)
                .Where(Profile => Profile != null)
                .OrderBy(Profile => Profile.name)
                .ToArray();
            SelectedTarget = selectedTmp == null ? Target(FontManagerPageKind.Create) : Target(FontManagerPageKind.Identity, selectedTmp);
            if (selectedTmp != null)
                ExpandedFontHashList.Add(selectedTmp);
            Repaint();
        }

        private void DrawNavigation()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(MenuWidth), GUILayout.ExpandHeight(true)))
            {
                NavigationSearch = EditorGUILayout.TextField(NavigationSearch, EditorStyles.toolbarSearchField);
                using var Scroll = new EditorGUILayout.ScrollViewScope(NavigationScroll);
                NavigationScroll = Scroll.scrollPosition;
                bool Searching = !string.IsNullOrWhiteSpace(NavigationSearch);
                FontsExpanded = EditorGUILayout.Foldout(FontsExpanded, "TMP 字体", true);
                if (FontsExpanded || Searching)
                    foreach (var Font in tmpAssets)
                        DrawFontPages(Font, Searching);

                if (Matches("从源字体新建"))
                    DrawNavigationButton("从源字体新建", Target(FontManagerPageKind.Create));
                ProfilesExpanded = EditorGUILayout.Foldout(ProfilesExpanded, "配置", true);
                if (ProfilesExpanded || Searching)
                {
                    using var Indent = new EditorGUI.IndentLevelScope();
                    foreach (var Profile in ProfileList)
                        if (Matches("配置/" + Profile.name))
                            DrawNavigationButton(Profile.name, Target(FontManagerPageKind.Profile, boundProfile: Profile));
                    if (Matches("配置/新建配置"))
                        DrawNavigationButton("新建配置", Target(FontManagerPageKind.Profile));
                }
            }
        }

        private void DrawFontPages(TMP_FontAsset font, bool searching)
        {
            string Path = "TMP 字体/" + font.name;
            bool NameMatches = Matches(Path);
            if (!NameMatches && !FontPageList.Any(Page => Matches(Path + "/" + Page.Label)))
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                bool WasExpanded = ExpandedFontHashList.Contains(font);
                bool Expanded = EditorGUI.Foldout(GUILayoutUtility.GetRect(18f, 20f, GUILayout.Width(18f)), WasExpanded, GUIContent.none);
                if (Expanded != WasExpanded)
                {
                    if (Expanded)
                        ExpandedFontHashList.Add(font);
                    else
                        ExpandedFontHashList.Remove(font);
                }
                DrawNavigationButton(font.name, Target(FontManagerPageKind.Identity, font));
                if (!Expanded && !searching)
                    return;
            }

            foreach (var Page in FontPageList)
            {
                if (!NameMatches && !Matches(Path + "/" + Page.Label))
                    continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(22f);
                    DrawNavigationButton(Page.Label, Target(Page.Kind, font));
                }
            }
        }

        private FontManagerMenuTarget Target(FontManagerPageKind kind, TMP_FontAsset font = null, FontBakeProfile boundProfile = null) =>
            new FontManagerMenuTarget { Window = this, Kind = kind, Font = font, BoundProfile = boundProfile };

        private bool Matches(string path) => string.IsNullOrWhiteSpace(NavigationSearch) ||
            path.IndexOf(NavigationSearch, StringComparison.OrdinalIgnoreCase) >= 0;

        private void DrawNavigationButton(string label, FontManagerMenuTarget target)
        {
            bool Selected = SelectedTarget.Kind == target.Kind && SelectedTarget.Font == target.Font &&
                SelectedTarget.BoundProfile == target.BoundProfile;
            if (GUILayout.Toggle(Selected, label, EditorStyles.miniButton) && !Selected)
                PendingTarget = target;
        }

        private void ApplyNavigationSelection()
        {
            if (PendingTarget == null)
                return;

            var Target = PendingTarget;
            PendingTarget = null;
            SelectedTarget = Target;
            settingsScroll = Vector2.zero;
            if (Target.Kind == FontManagerPageKind.Profile)
            {
                if (Target.BoundProfile != null && Target.BoundProfile != profile)
                {
                    profile = Target.BoundProfile;
                    ClearCandidate();
                    Invalidate();
                }
            }
            else if (Target.Font != selectedTmp)
            {
                SelectTmp(Target.Font);
            }

            Repaint();
        }

        private static float ReadSplitterDelta(Rect rect)
        {
            int Id = GUIUtility.GetControlID(FocusType.Passive, rect);
            var Current = Event.current;
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal, Id);
            switch (Current.GetTypeForControl(Id))
            {
                case EventType.MouseDown when Current.button == 0 && rect.Contains(Current.mousePosition):
                    GUIUtility.hotControl = Id;
                    Current.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == Id:
                    float Delta = Current.delta.x;
                    Current.Use();
                    return Delta;
                case EventType.MouseUp when GUIUtility.hotControl == Id:
                    GUIUtility.hotControl = 0;
                    Current.Use();
                    break;
            }

            return 0f;
        }
    }
}
