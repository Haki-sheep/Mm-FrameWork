using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MieMieFrameWork.Localization.FontManagement
{
    public sealed class FontManagerWindow : OdinMenuEditorWindow
    {
        /// <summary> 当前烘焙配置 </summary>
        [SerializeField]
        internal FontBakeProfile profile;
        TMP_FontAsset selectedTmp => profile == null ? null : profile.generatedFont;
        /// <summary> 中间预览文案 </summary>
        [SerializeField]
        internal string previewText = "开始游戏  设置\n欢迎来到冒险世界！\nLevel 12  HP 100/100  金币 +2,500";
        /// <summary> 中间预览栏宽度 </summary>
        [SerializeField]
        float previewPaneWidth = 420;
        Vector2 previewScroll, settingsScroll;
        /// <summary> 收集到的字符集 </summary>
        internal FontCharacterSet characterSet;
        /// <summary> 覆盖诊断结果 </summary>
        internal FontCoverage coverage;
        readonly List<Font> fonts = new List<Font>();
        readonly List<TMP_FontAsset> tmpAssets = new List<TMP_FontAsset>();
        readonly List<PreviewCard> cards = new List<PreviewCard>();
        /// <summary> 待应用的 TMP 文本 </summary>
        internal TMP_Text[] applyTargets = Array.Empty<TMP_Text>();
        /// <summary> 状态栏文本 </summary>
        string status = "选择一个 TMP 查看源字体 备用字体和图集";
        MessageType statusType = MessageType.Info;
        string previewKey;
        double previewAt;
        /// <summary> 诊断列表页码 </summary>
        internal int reportPage;
        /// <summary> 诊断列表过滤 </summary>
        internal int reportFilter;
        /// <summary> 外部工作流回填回调 </summary>
        internal Action<TMP_FontAsset> applyWorkflow;
        /// <summary> 外部工作流规划文案 </summary>
        internal string workflowText;
        bool previewDirty = true;
        /// <summary> 优化候选 TMP </summary>
        internal TMP_FontAsset optimizedPreviewFont;
        string optimizedPreviewKey;
        /// <summary> 优化采样字号 </summary>
        [SerializeField]
        internal int optimizationSamplingSize = 48;
        /// <summary> 优化 Padding </summary>
        [SerializeField]
        internal int optimizationPadding = 4;
        /// <summary> 优化图集边长 </summary>
        [SerializeField]
        internal int optimizationAtlasSize = 1024;
        /// <summary> 优化渲染模式 </summary>
        [SerializeField]
        internal GlyphRenderMode optimizationRenderMode = GlyphRenderMode.SDFAA;
        /// <summary> 优化静态或动态 </summary>
        [SerializeField]
        internal AtlasPopulationMode optimizationPopulationMode = AtlasPopulationMode.Static;
        /// <summary> 优化允许多图集 </summary>
        [SerializeField]
        internal bool optimizationMultiAtlas;
        /// <summary> 优化是否裁剪到文案 </summary>
        [SerializeField]
        internal bool optimizationSubset;
        /// <summary> 图集详情展开 </summary>
        internal bool showAtlasDetails;
        /// <summary> 收集选项展开 </summary>
        internal bool showSourceOptions;
        FontBakeProfile candidateProfile;
        /// <summary> 候选字符码点 </summary>
        internal uint[] candidateCodes;
        TMP_FontAsset candidateTarget;
        bool rebuildingMenu;
        GUIStyle wrappedHeading;
        GUIStyle WrappedHeading => wrappedHeading ?? (wrappedHeading = new GUIStyle(EditorStyles.boldLabel) { wordWrap = true });

        sealed class PreviewCard : IDisposable
        {
            public string title, error;
            public FontPreviewResult render;
            public void Dispose() => render?.Dispose();
        }

        public static void Open() => GetWindow<FontManagerWindow>("字体管理器");

        public static void OpenForWorkflow(string content, Action<TMP_FontAsset> apply)
        {
            var window = GetWindow<FontManagerWindow>("字体管理器");
            window.workflowText = content;
            window.applyWorkflow = apply;
            window.Notify("已连接当前文案工作流 可导入规划文案并回填生成字体");
        }

        public override float DefaultLabelWidth => 120f;

        protected override void OnEnable()
        {
            minSize = new Vector2(1100, 640);
            Discover();
            DiscoverTmpAssets();
            EnsureProfile();
            base.OnEnable();
            MenuWidth = 240;
            WindowPadding = new Vector4(8, 8, 0, 8);
            UseScrollView = false;
            Undo.undoRedoPerformed += Invalidate;
            EditorApplication.update += Tick;
        }

        protected override void OnDisable()
        {
            EditorApplication.update -= Tick;
            Undo.undoRedoPerformed -= Invalidate;
            ClearPreviews();
            ClearCandidate();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        void OnSelectionChange() => Repaint();

        void OnProjectChange()
        {
            DiscoverTmpAssets();
            RefreshMenuSelection();
            Invalidate();
        }

        #region 菜单与布局

        protected override OdinMenuTree BuildMenuTree()
        {
            DiscoverTmpAssets();
            var tree = new OdinMenuTree(false)
            {
                Config =
                {
                    DrawSearchToolbar = true,
                    AutoFocusSearchBar = false,
                    UseCachedExpandedStates = true
                }
            };
            tree.DefaultMenuStyle.Height = 26;
            tree.Selection.SupportsMultiSelect = false;
            foreach (var tmp in tmpAssets)
            {
                string root = "TMP 字体/" + tmp.name;
                tree.Add(root, Target(tmp, FontManagerPageKind.Identity), EditorIcons.Letter);
                tree.Add(root + "/预览调参", Target(tmp, FontManagerPageKind.Preview), EditorIcons.EyeDropper);
                tree.Add(root + "/缺失补齐", Target(tmp, FontManagerPageKind.Missing), EditorIcons.UnityInfoIcon);
                tree.Add(root + "/性能优化", Target(tmp, FontManagerPageKind.Performance), EditorIcons.SmartPhone);
                tree.Add(root + "/应用到 UI", Target(tmp, FontManagerPageKind.Apply), EditorIcons.File);
            }
            tree.Add("从源字体新建", Target(null, FontManagerPageKind.Create), EditorIcons.Plus);
            var profiles = AssetDatabase.FindAssets("t:FontBakeProfile").Select(AssetDatabase.GUIDToAssetPath).Select(p => AssetDatabase.LoadAssetAtPath<FontBakeProfile>(p)).Where(p => p != null).ToArray();
            foreach (var item in profiles)
                tree.Add("配置/" + item.name, new FontManagerMenuTarget { Window = this, BoundProfile = item, Kind = FontManagerPageKind.Profile }, EditorIcons.SettingsCog);
            tree.Add("配置/新建配置", new FontManagerMenuTarget { Window = this, Kind = FontManagerPageKind.Profile }, EditorIcons.Plus);
            tree.Selection.SelectionChanged += OnMenuSelection;
            return tree;
        }

        FontManagerMenuTarget Target(TMP_FontAsset font, FontManagerPageKind kind)
        {
            return new FontManagerMenuTarget { Window = this, Font = font, Kind = kind };
        }

        void OnMenuSelection(SelectionChangedType type)
        {
            if (rebuildingMenu || type == SelectionChangedType.SelectionCleared) return;
            if (!(MenuTree.Selection.SelectedValue is FontManagerMenuTarget target)) return;
            if (target.Kind == FontManagerPageKind.Profile)
            {
                if (target.BoundProfile != null && target.BoundProfile != profile)
                {
                    profile = target.BoundProfile;
                    ClearCandidate();
                    Invalidate();
                }
                return;
            }
            if (target.Font != selectedTmp) SelectTmp(target.Font);
        }

        internal void RefreshMenuSelection()
        {
            if (MenuTree == null) return;
            rebuildingMenu = true;
            try
            {
                ForceMenuTreeRebuild();
                string path = selectedTmp == null ? "从源字体新建" : "TMP 字体/" + selectedTmp.name;
                var item = MenuTree.GetMenuItem(path);
                if (item != null) item.Select(true);
                else TrySelectMenuItemWithObject(FindTarget(selectedTmp, selectedTmp == null ? FontManagerPageKind.Create : FontManagerPageKind.Identity));
            }
            finally { rebuildingMenu = false; }
        }

        FontManagerMenuTarget FindTarget(TMP_FontAsset font, FontManagerPageKind kind)
        {
            if (MenuTree == null) return null;
            return MenuTree.EnumerateTree(false).Select(i => i.Value as FontManagerMenuTarget).FirstOrDefault(t => t != null && t.Font == font && t.Kind == kind);
        }

        protected override void OnImGUI()
        {
            DrawToolbar();
            DrawStatusBar();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                base.OnImGUI();
        }

        void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("刷新列表", EditorStyles.toolbarButton, GUILayout.Width(76)))
                {
                    DiscoverTmpAssets();
                    RefreshMenuSelection();
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label("TMP 字体工作台", EditorStyles.miniLabel);
            }
        }

        void DrawStatusBar()
        {
            var style = new GUIStyle(EditorStyles.miniLabel) { clipping = TextClipping.Clip };
            string label = (statusType == MessageType.Warning ? "提示  " : statusType == MessageType.Error ? "未完成  " : "") + status;
            var rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            var color = statusType == MessageType.Error ? new Color(.45f, .16f, .16f, .75f) : statusType == MessageType.Warning ? new Color(.45f, .34f, .12f, .75f) : new Color(.16f, .25f, .34f, .75f);
            EditorGUI.DrawRect(rect, color);
            GUI.Label(new Rect(rect.x + 8, rect.y + 3, rect.width - 16, rect.height - 6), label, style);
        }

        protected override void DrawEditor(int index)
        {
            float maxPreview = Mathf.Max(280f, position.width - MenuWidth - 340f);
            previewPaneWidth = Mathf.Clamp(previewPaneWidth, 280f, maxPreview);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(previewPaneWidth), GUILayout.ExpandHeight(true));
            DrawPreviewPane();
            EditorGUILayout.EndVertical();
            var dragRect = GUILayoutUtility.GetRect(6f, 1f, GUILayout.Width(6f), GUILayout.ExpandHeight(true));
            SirenixEditorGUI.DrawSolidRect(dragRect, EditorGUIUtility.isProSkin ? new Color(.12f, .12f, .12f, 1) : new Color(.55f, .55f, .55f, 1));
            Vector2 delta = SirenixEditorGUI.SlideRect(dragRect, MouseCursor.ResizeHorizontal);
            if (delta != Vector2.zero)
            {
                previewPaneWidth = Mathf.Clamp(previewPaneWidth + delta.x, 280f, maxPreview);
                Repaint();
            }
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            settingsScroll = EditorGUILayout.BeginScrollView(settingsScroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (profile == null)
            {
                EditorGUILayout.HelpBox("新建字体配置 或在左侧「配置」中选择已有 FontBakeProfile", MessageType.Info);
                if (GUILayout.Button("创建第一个配置", GUILayout.Height(36))) Run(CreateProfile);
            }
            else base.DrawEditor(index);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 预览

        void DrawPreviewPane()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(previewScroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                previewScroll = scroll.scrollPosition;
                GUILayout.Space(6);
                GUILayout.Label(selectedTmp == null ? "TMP 预览" : selectedTmp.name + "  TMP 预览", WrappedHeading);
                if (CurrentTmp != null)
                    GUILayout.Label(CurrentTmp.atlasPopulationMode + (FontBakeService.GetReferencedSourceFont(CurrentTmp) == null ? "" : "  源 " + FontBakeService.GetReferencedSourceFont(CurrentTmp).name), EditorStyles.wordWrappedMiniLabel);
                EditorGUI.BeginChangeCheck();
                previewText = EditorGUILayout.TextArea(previewText, GUILayout.MinHeight(64), GUILayout.MaxHeight(88));
                if (EditorGUI.EndChangeCheck()) { previewDirty = true; previewAt = EditorApplication.timeSinceStartup + .5; }
                if (GUILayout.Button("刷新预览")) { previewDirty = true; previewKey = null; previewAt = 0; }
                if (selectedTmp == null && cards.Count == 0) EditorGUILayout.HelpBox(profile == null || profile.sourceFont == null ? "左侧选择一个 TMP 或到「从源字体新建」指定源 TTF" : "正在刷新预览", MessageType.Info);
                foreach (var card in cards)
                {
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        GUILayout.Label(card.title, WrappedHeading);
                        if (!string.IsNullOrEmpty(card.error)) EditorGUILayout.HelpBox(card.error, MessageType.Warning);
                        if (card.render == null || card.render.image == null) continue;
                        var image = card.render.image;
                        float aspect = (float)image.width / Math.Max(1, image.height);
                        float height = Mathf.Clamp((previewPaneWidth - 24f) / aspect, 80f, 280f);
                        var rect = GUILayoutUtility.GetRect(previewPaneWidth - 24f, height, GUILayout.ExpandWidth(true), GUILayout.Height(height));
                        GUI.DrawTexture(rect, image, ScaleMode.ScaleToFit);
                        GUILayout.Label(image.width + " × " + image.height + " px  " + card.render.lines + " 行  " + (card.render.overflow ? "存在溢出/截断" : "完整显示"), EditorStyles.wordWrappedMiniLabel);
                    }
                }
                if (selectedTmp != null && cards.Count == 0) EditorGUILayout.HelpBox("正在刷新预览", MessageType.Info);
            }
        }

        void Tick()
        {
            if (!previewDirty || profile == null || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < previewAt) return;
            previewDirty = false;
            string key = JsonUtility.ToJson(profile) + previewText + (selectedTmp == null ? "" : AssetDatabase.GetAssetPath(selectedTmp)) + optimizedPreviewKey;
            if (key == previewKey) return;
            previewKey = key;
            ClearPreviews();
            if (selectedTmp != null)
            {
                var card = new PreviewCard { title = "当前 TMP  " + selectedTmp.name };
                cards.Add(card);
                try { card.render = FontPreviewRenderer.Render(selectedTmp, previewText, profile.style, profile.richText); }
                catch (Exception e) { card.error = e.Message; }
            }
            else if (profile.sourceFont != null)
            {
                var card = new PreviewCard { title = "源字体预览  " + profile.sourceFont.name };
                cards.Add(card);
                TMP_FontAsset temporary = null;
                try
                {
                    var codes = FontCharacterCollector.CodePoints(FontCharacterCollector.VisibleText(previewText, profile.richText, false)).Append(32u).Distinct().ToArray();
                    var check = FontBakeService.Check(profile.sourceFont, codes, null, profile.fallbackFontAssets);
                    var supported = codes.Where(c => !check.unsupported.Contains(c)).ToArray();
                    temporary = FontBakeService.BuildTransient(profile.sourceFont, supported, profile.samplingSize, profile.padding, profile.atlasSize, profile.renderMode, profile.populationMode, profile.enableMultiAtlas, profile.fallbackFontAssets);
                    card.render = FontPreviewRenderer.Render(temporary, previewText, profile.style, profile.richText);
                    if (check.unsupported.Count > 0) card.error = "源 TTF 与 fallback 都缺少 " + check.unsupported.Count + " 个字形 请在「缺失补齐」定位来源并补充字形";
                }
                catch (Exception e) { card.error = e.Message; }
                finally { FontBakeService.DestroyTransient(temporary); }
            }
            if (optimizedPreviewFont != null)
            {
                var candidate = new PreviewCard { title = "优化候选  " + optimizedPreviewFont.name };
                cards.Add(candidate);
                try { candidate.render = FontPreviewRenderer.Render(optimizedPreviewFont, previewText, profile.style, profile.richText); }
                catch (Exception e) { candidate.error = e.Message; }
            }
            Repaint();
        }

        #endregion

        #region 选择与配置

        internal TMP_FontAsset CurrentTmp => selectedTmp;

        internal Font CurrentSource
        {
            get
            {
                if (profile != null && profile.sourceFont != null) return profile.sourceFont;
                return FontBakeService.GetReferencedSourceFont(CurrentTmp);
            }
        }

        internal IEnumerable<TMP_FontAsset> CurrentFallbacks()
        {
            var roots = new List<TMP_FontAsset>();
            if (CurrentTmp != null && CurrentTmp.fallbackFontAssetTable != null) roots.AddRange(CurrentTmp.fallbackFontAssetTable);
            if (CurrentTmp == null && profile != null && profile.fallbackFontAssets != null) roots.AddRange(profile.fallbackFontAssets);
            return roots.Where(f => f != null && f != CurrentTmp).Distinct();
        }

        void EnsureProfile()
        {
            if (Selection.activeObject is FontBakeProfile selected) profile = selected;
            if (profile == null) profile = AssetDatabase.FindAssets("t:FontBakeProfile").Select(AssetDatabase.GUIDToAssetPath).Select(p => AssetDatabase.LoadAssetAtPath<FontBakeProfile>(p)).FirstOrDefault();
            if (Selection.activeObject is TMP_FontAsset active && profile != null) SelectTmp(active);
        }

        internal void SelectTmp(TMP_FontAsset font)
        {
            if (profile != null)
            {
                Undo.RecordObject(profile, "选择当前 TMP 字体");
                profile.generatedFont = font;
                if (font != null)
                {
                    profile.sourceFont = FontBakeService.GetReferencedSourceFont(font);
                    profile.fallbackFontAssets = CurrentFallbacks().ToList();
                    profile.samplingSize = Mathf.RoundToInt(font.faceInfo.pointSize);
                    profile.padding = font.atlasPadding;
                    profile.atlasSize = font.atlasWidth;
                    profile.renderMode = font.atlasRenderMode;
                    profile.populationMode = font.atlasPopulationMode;
                    profile.enableMultiAtlas = font.isMultiAtlasTexturesEnabled;
                    optimizationSamplingSize = Mathf.Clamp(profile.samplingSize, 16, 200);
                    optimizationPadding = Mathf.Clamp(profile.padding, 1, 32);
                    optimizationAtlasSize = profile.atlasSize;
                    optimizationRenderMode = profile.renderMode;
                    optimizationPopulationMode = profile.populationMode;
                    optimizationMultiAtlas = profile.enableMultiAtlas;
                }
                if (font == null || !AssetDatabase.GetAssetPath(font).StartsWith(FontManagerPaths.Generated + "/", StringComparison.Ordinal))
                {
                    profile.lastBakeSignature = null;
                    profile.lastBakedSourceFontGuid = null;
                }
                EditorUtility.SetDirty(profile);
            }
            ClearCandidate();
            DiscoverTmpAssets();
            Invalidate();
        }

        internal void CreateProfile()
        {
            FontBakeService.EnsureFolders();
            profile = CreateInstance<FontBakeProfile>();
            profile.sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontManagerPaths.DefaultSource)
                ?? fonts.FirstOrDefault(f => AssetDatabase.GetAssetPath(f).StartsWith(FontManagerPaths.Source + "/", StringComparison.OrdinalIgnoreCase)) ?? fonts.FirstOrDefault();
            AssetDatabase.CreateAsset(profile, AssetDatabase.GenerateUniqueAssetPath(FontManagerPaths.Profiles + "/FontProfile.asset"));
            AssetDatabase.SaveAssetIfDirty(profile);
            Invalidate();
            RefreshMenuSelection();
        }

        internal void Mutate(Action action)
        {
            Undo.RecordObject(profile, "编辑字体配置");
            action();
            EditorUtility.SetDirty(profile);
            Invalidate();
        }

        internal void Notify(string text, MessageType type = MessageType.Info)
        {
            status = text;
            statusType = type;
            Repaint();
        }

        internal void Run(Action action)
        {
            try { action(); }
            catch (Exception e) { Notify(e.Message, MessageType.Error); Debug.LogException(e); }
            finally { EditorUtility.ClearProgressBar(); }
        }

        internal void Ping(UnityEngine.Object asset)
        {
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        internal void Invalidate()
        {
            previewDirty = true;
            previewKey = null;
            previewAt = EditorApplication.timeSinceStartup + .5;
            characterSet = null;
            coverage = null;
            applyTargets = Array.Empty<TMP_Text>();
            ClearPreviews();
            Repaint();
        }

        void Discover()
        {
            fonts.Clear();
            fonts.AddRange(AssetDatabase.FindAssets("t:Font", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath).Select(p => AssetDatabase.LoadAssetAtPath<Font>(p)).Where(f => f != null).Distinct().OrderBy(f => f.name));
        }

        void DiscoverTmpAssets()
        {
            tmpAssets.Clear();
            foreach (var path in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (asset != null && !tmpAssets.Contains(asset)) tmpAssets.Add(asset);
            }
            if (profile != null && profile.generatedFont != null && !tmpAssets.Contains(profile.generatedFont)) tmpAssets.Add(profile.generatedFont);
            tmpAssets.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        }

        internal void ClearCandidate()
        {
            if (optimizedPreviewFont != null && !EditorUtility.IsPersistent(optimizedPreviewFont)) FontBakeService.DestroyTransient(optimizedPreviewFont);
            optimizedPreviewFont = null;
            optimizedPreviewKey = null;
            if (candidateProfile != null) DestroyImmediate(candidateProfile);
            candidateProfile = null;
            candidateCodes = null;
            candidateTarget = null;
        }

        void ClearPreviews()
        {
            foreach (var card in cards) card.Dispose();
            cards.Clear();
        }

        internal bool GeneratedMatchesSource()
        {
            if (CurrentTmp == null) return false;
            var source = FontBakeService.GetReferencedSourceFont(CurrentTmp);
            return source == null || profile.sourceFont == null || source == profile.sourceFont;
        }

        #endregion

        #region 缺失与烘焙

        internal void Collect()
        {
            EditorUtility.DisplayProgressBar("字体管理器", "收集文案与检查字体覆盖", .2f);
            characterSet = FontCharacterCollector.Collect(profile);
            bool changedSource = CurrentTmp != null && !GeneratedMatchesSource();
            coverage = FontBakeService.Check(CurrentSource, characterSet.Codes, changedSource ? null : CurrentTmp, CurrentFallbacks());
            reportPage = 0;
            reportFilter = coverage.unsupported.Count > 0 ? 0 : coverage.unbaked.Count > 0 ? 1 : 4;
            Notify(ReportSummary(), coverage.unsupported.Count > 0 ? MessageType.Warning : MessageType.Info);
        }

        internal void Bake()
        {
            Collect();
            if (CurrentSource == null) throw new InvalidOperationException("当前 TMP 没有关联源 TTF 先定位并指定用于补字的源文件");
            if (CurrentTmp != null && !GeneratedMatchesSource()) throw new InvalidOperationException("当前关联源字体与 TMP 原始来源不同 请新建 TMP 或恢复原来源 避免误写现有资源");
            if (coverage.unsupported.Count > 0) { Notify(ReportSummary(), MessageType.Warning); return; }
            var codes = characterSet.Codes.AsEnumerable();
            if (CurrentTmp != null) codes = codes.Concat(CurrentTmp.characterTable.Select(c => c.unicode).Where(c => c >= 32 && c != 127));
            uint[] combined = codes.Distinct().OrderBy(c => c).ToArray();
            var settings = Instantiate(profile);
            TMP_FontAsset staged = null;
            try
            {
                settings.sourceFont = CurrentSource;
                settings.fallbackFontAssets = CurrentFallbacks().ToList();
                if (CurrentTmp != null)
                {
                    settings.samplingSize = Mathf.RoundToInt(CurrentTmp.faceInfo.pointSize);
                    settings.padding = CurrentTmp.atlasPadding;
                    settings.atlasSize = CurrentTmp.atlasWidth;
                    settings.renderMode = CurrentTmp.atlasRenderMode;
                    settings.populationMode = CurrentTmp.atlasPopulationMode;
                    settings.enableMultiAtlas = CurrentTmp.isMultiAtlasTexturesEnabled;
                }
                EditorUtility.DisplayProgressBar("字体管理器", "保留原字符 补入所需字形并重建图集", .55f);
                staged = FontBakeService.BuildTransient(settings.sourceFont, combined, settings.samplingSize, settings.padding, settings.atlasSize, settings.renderMode, settings.populationMode, settings.enableMultiAtlas, settings.fallbackFontAssets);
                bool update = CurrentTmp != null && FontBakeService.CanUpdateInPlace(CurrentTmp, staged, out _);
                if (CurrentTmp != null && !update)
                {
                    FontBakeService.CanUpdateInPlace(CurrentTmp, staged, out string reason);
                    EditorUtility.ClearProgressBar();
                    if (!EditorUtility.DisplayDialog("补齐结果需另存", reason + "\n另存后不会自动改变现有 UI 引用", "另存 TMP", "取消")) return;
                }
                var result = FontBakeService.PersistCandidate(settings, staged, combined, update);
                AdoptResult(settings, result);
                ClearCandidate();
                DiscoverTmpAssets();
                Collect();
                previewDirty = true;
                previewKey = null;
                RefreshMenuSelection();
                Notify(update ? "当前 TMP 已补齐 原字符与资源引用保留 备份位于 " + FontManagerPaths.Backups : "已创建完整 TMP 现有 UI 引用保持原字体 可在「应用到 UI」页应用新字体");
            }
            finally { FontBakeService.DestroyTransient(staged); DestroyImmediate(settings); }
        }

        internal string ReportSummary()
        {
            if (characterSet == null || coverage == null) return "尚未检查字符";
            if (characterSet.Codes.Length == 0) return "没有待检查字符 请补充文案";
            if (coverage.sourceUnavailable && coverage.unsupported.Count > 0)
                return "无法读取源字体 " + coverage.sourceUnavailableName + " 当前 TMP 与 fallback 只确认了已烘焙字符 请恢复源文件后再补齐未覆盖字形";
            if (coverage.unsupported.Count > 0)
                return "主 TMP 可用源 TTF 和 fallback 未覆盖 " + coverage.unsupported.Count + " 个字形（如 " + FontBakeService.DescribeShort(coverage.unsupported, 5) + "）请定位来源并补充字形";
            if (CurrentTmp != null && !GeneratedMatchesSource()) return "关联源字体与当前 TMP 不同 请恢复原来源或选择对应的 TMP";
            if (coverage.unbaked.Count > 0) return "有 " + coverage.unbaked.Count + " 个字形在源 TTF 中可用 但尚未烘焙 补齐将保留原有字符并重建图集";
            return "已检查 " + characterSet.Codes.Length + " 个字符 fallback 提供 " + coverage.resolvedByFallback.Count + " 个 动态待生成 " + coverage.dynamicPending.Count + " 个 动态补字仍取决于图集容量";
        }

        #endregion

        #region 性能优化

        uint[] OptimizationCodes(FontCharacterSet set)
        {
            var codes = set.Codes.AsEnumerable();
            if (!optimizationSubset && CurrentTmp != null) codes = codes.Concat(CurrentTmp.characterTable.Select(c => c.unicode).Where(c => c >= 32 && c != 127));
            return codes.Distinct().OrderBy(c => c).ToArray();
        }

        string CandidateKey(uint[] codes)
        {
            string dependency = CurrentTmp == null ? "" : AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(CurrentTmp)).ToString();
            return (CurrentTmp == null ? 0 : CurrentTmp.GetInstanceID()) + "|" + dependency + "|" + FontBakeService.Signature(profile, codes) + "|" + optimizationSamplingSize + "|" + optimizationPadding + "|" + optimizationAtlasSize + "|" + optimizationRenderMode + "|" + optimizationPopulationMode + "|" + optimizationMultiAtlas + "|" + optimizationSubset;
        }

        internal void BuildOptimizationPreview()
        {
            if (CurrentSource == null) throw new InvalidOperationException("当前 TMP 没有关联源 TTF 无法生成优化候选");
            ClearCandidate();
            characterSet = FontCharacterCollector.Collect(profile);
            var codes = OptimizationCodes(characterSet);
            var settings = Instantiate(profile);
            try
            {
                settings.sourceFont = CurrentSource;
                settings.generatedFont = CurrentTmp;
                settings.fallbackFontAssets = CurrentFallbacks().ToList();
                settings.samplingSize = optimizationSamplingSize;
                settings.padding = optimizationPadding;
                settings.atlasSize = optimizationAtlasSize;
                settings.renderMode = optimizationRenderMode;
                settings.populationMode = optimizationPopulationMode;
                settings.enableMultiAtlas = optimizationMultiAtlas;
                optimizedPreviewFont = FontBakeService.BuildTransient(settings.sourceFont, codes, settings.samplingSize, settings.padding, settings.atlasSize, settings.renderMode, settings.populationMode, settings.enableMultiAtlas, settings.fallbackFontAssets);
                candidateProfile = settings;
                candidateCodes = codes;
                candidateTarget = CurrentTmp;
                optimizedPreviewKey = CandidateKey(codes);
            }
            catch { DestroyImmediate(settings); throw; }
            previewDirty = true;
            previewKey = null;
            Notify("候选已生成 中间上下两张预览使用相同文案和样式 工程中的 TMP 尚未改变");
        }

        internal void ApplyOptimizationCandidate() => SaveOptimizationCandidate(true);

        internal void SaveOptimizationCandidate(bool updateExisting)
        {
            if (optimizedPreviewFont == null || candidateProfile == null) throw new InvalidOperationException("请先生成优化候选");
            var codes = OptimizationCodes(FontCharacterCollector.Collect(profile));
            if (candidateTarget != CurrentTmp || optimizedPreviewKey != CandidateKey(codes))
                throw new InvalidOperationException("源资产 文案或参数已变化 请重新生成候选 确认最新预览后采用");
            var result = FontBakeService.PersistCandidate(candidateProfile, optimizedPreviewFont, candidateCodes, updateExisting);
            AdoptResult(candidateProfile, result);
            ClearCandidate();
            DiscoverTmpAssets();
            Invalidate();
            RefreshMenuSelection();
            Notify(updateExisting ? "已备份并写回当前 TMP 原有引用已保留 备份位于 " + FontManagerPaths.Backups : "候选已另存为新 TMP 现有 UI 引用仍指向原字体 可在「应用到 UI」页选择目标后应用");
        }

        void AdoptResult(FontBakeProfile settings, TMP_FontAsset result)
        {
            Undo.RecordObject(profile, "采用 TMP 字体");
            profile.generatedFont = result;
            profile.sourceFont = settings.sourceFont;
            profile.samplingSize = settings.samplingSize;
            profile.padding = settings.padding;
            profile.atlasSize = settings.atlasSize;
            profile.renderMode = settings.renderMode;
            profile.populationMode = settings.populationMode;
            profile.enableMultiAtlas = settings.enableMultiAtlas;
            profile.fallbackFontAssets = settings.fallbackFontAssets.ToList();
            profile.lastBakeSignature = settings.lastBakeSignature;
            profile.lastBakedSourceFontGuid = settings.lastBakedSourceFontGuid;
            profile.lastBakeSummary = settings.lastBakeSummary;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
        }

        #endregion

        #region 应用到 UI

        internal void RequireCurrent()
        {
            if (CurrentTmp == null || CurrentTmp.material == null) throw new InvalidOperationException("请先选择带材质的 TMP 字体");
            if (!GeneratedMatchesSource()) throw new InvalidOperationException("源字体与当前 TMP 不一致 请恢复原来源或选择相应 TMP");
            var codes = FontCharacterCollector.Collect(profile).Codes;
            var check = FontBakeService.Check(CurrentSource, codes, CurrentTmp, CurrentFallbacks());
            if (check.unsupported.Count > 0 || check.unbaked.Count > 0) throw new InvalidOperationException("文案仍有未覆盖字符 请到缺失补齐页定位来源并补齐");
        }

        internal Material SaveMaterial()
        {
            if (CurrentTmp == null || CurrentTmp.material == null) throw new InvalidOperationException("请先选择带材质的 TMP");
            FontBakeService.EnsureFolders();
            var material = new Material(CurrentTmp.material);
            FontPreviewRenderer.StyleMaterial(material, profile.style);
            var path = AssetDatabase.GenerateUniqueAssetPath(FontManagerPaths.Generated + "/" + FontBakeService.SafeName(profile.name) + "_Style.mat");
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssetIfDirty(material);
            EditorGUIUtility.PingObject(material);
            return material;
        }

        internal void Apply()
        {
            RequireCurrent();
            var targets = applyTargets.Where(t => t != null && t.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(t)).ToArray();
            foreach (var target in targets)
            {
                var codes = FontCharacterCollector.CodePoints(FontCharacterCollector.VisibleText(target.text, target.richText, false));
                var missing = codes.Where(c => !FontBakeService.CoversCharacter(CurrentTmp, c)).Distinct().ToArray();
                if (missing.Length > 0) throw new InvalidOperationException(target.name + " 包含尚未烘焙的字符 " + FontBakeService.Describe(missing) + " 请把该场景/Prefab 加入文案来源后重新生成");
            }
            if (targets.Length == 0) throw new InvalidOperationException("应用对象已失效 请重新预览范围");
            var material = SaveMaterial();
            Undo.RecordObjects(targets, "应用字体与文字样式");
            foreach (var text in targets)
            {
                text.font = CurrentTmp;
                text.fontSharedMaterial = material;
                FontPreviewRenderer.StyleText(text, profile.style);
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
                EditorUtility.SetDirty(text);
                EditorSceneManager.MarkSceneDirty(text.gameObject.scene);
            }
            applyTargets = Array.Empty<TMP_Text>();
            Notify("已应用到 " + targets.Length + " 个 TMP 组件 可使用 Undo 撤销");
        }

        #endregion
    }
}
