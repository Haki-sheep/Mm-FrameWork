using System;
using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MieMieFrameWork.Localization.FontManagement
{
    public enum FontManagerPageKind
    {
        Identity,
        Preview,
        Missing,
        Performance,
        Apply,
        Create,
        Profile
    }

    [Serializable]
    [HideReferenceObjectPicker]
    public sealed class FontManagerMenuTarget
    {
        /// <summary> 所属工作台窗口 </summary>
        [HideInInspector]
        public FontManagerWindow Window;
        /// <summary> 该菜单项绑定的 TMP </summary>
        [HideInInspector]
        public TMP_FontAsset Font;
        /// <summary> 配置页绑定的 Profile </summary>
        [HideInInspector]
        public FontBakeProfile BoundProfile;
        /// <summary> 右侧要画的功能页 </summary>
        [HideInInspector]
        public FontManagerPageKind Kind;

        [OnInspectorGUI]
        private void Draw()
        {
            if (Window == null) return;
            FontManagerPages.Draw(this);
        }
    }

    public static class FontManagerPages
    {
        public static void Draw(FontManagerMenuTarget target)
        {
            var window = target.Window;
            if (window.profile == null)
            {
                EditorGUILayout.HelpBox("先新建或选择一个字体配置 配置只保存文案来源和预览样式", MessageType.Info);
                if (GUILayout.Button("创建配置", GUILayout.Height(32))) window.Run(window.CreateProfile);
                return;
            }
            switch (target.Kind)
            {
                case FontManagerPageKind.Identity: DrawIdentity(window); break;
                case FontManagerPageKind.Preview: DrawPreviewSettings(window); break;
                case FontManagerPageKind.Missing: DrawMissing(window); break;
                case FontManagerPageKind.Performance: DrawPerformance(window); break;
                case FontManagerPageKind.Apply: DrawApply(window); break;
                case FontManagerPageKind.Create: DrawCreate(window); break;
                case FontManagerPageKind.Profile: DrawProfile(window, target.BoundProfile); break;
            }
        }

        #region 资源关系

        public static void DrawIdentity(FontManagerWindow window)
        {
            var font = window.CurrentTmp;
            GUILayout.Label("资源关系", EditorStyles.boldLabel);
            GUILayout.Label("当前 TMP 的源字体 Fallback 图集与材质", EditorStyles.wordWrappedMiniLabel);
            if (font == null)
            {
                EditorGUILayout.HelpBox("左侧选择一个 TMP 或到「从源字体新建」创建", MessageType.Info);
                return;
            }
            var info = FontBakeService.Inspect(font);
            if (info == null) return;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(new GUIContent(info.assetPath, info.assetPath), EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(6);
                GUILayout.Label("源 TTF / OTF", EditorStyles.miniBoldLabel);
                GUILayout.Label(info.sourceFont == null ? "未记录源文件 仍可显示已烘焙字符" : info.sourceFont.name, EditorStyles.wordWrappedLabel);
                if (info.sourceFont != null)
                {
                    GUILayout.Label(new GUIContent(info.sourceFontPath, info.sourceFontPath), EditorStyles.wordWrappedMiniLabel);
                    if (GUILayout.Button("定位源文件")) window.Ping(info.sourceFont);
                }
                GUILayout.Label(info.populationMode == AtlasPopulationMode.Dynamic ? "Dynamic 运行时按需补字" : "Static 使用已烘焙字形", EditorStyles.wordWrappedLabel);
                GUILayout.Label(info.characterCount + " 个字符  " + info.glyphCount + " 个字形", EditorStyles.wordWrappedMiniLabel);
                if (info.requiresRuntimeSource) EditorGUILayout.HelpBox("动态 TMP 运行时依赖源 TTF 并可能继续增长图集", MessageType.Info);
            }
            GUILayout.Space(8);
            GUILayout.Label("Fallback  " + (info.fallbacks.Count == 0 ? "未配置" : info.fallbacks.Count + " 个备用 TMP"), EditorStyles.boldLabel);
            foreach (var fallback in info.fallbacks)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var fallbackSource = FontBakeService.GetReferencedSourceFont(fallback);
                    GUILayout.Label(fallback.name + "  " + fallback.atlasPopulationMode + (fallbackSource == null ? "" : "\n源 " + fallbackSource.name), EditorStyles.wordWrappedMiniLabel);
                    if (GUILayout.Button("定位", GUILayout.Width(42))) window.Ping(fallback);
                }
            }
            GUILayout.Space(8);
            GUILayout.Label("图集  " + (info.atlases.Count == 0 ? "无纹理" : info.atlases.Count + " 张  " + FontBakeService.FormatBytes(info.estimatedAtlasBytes)), EditorStyles.boldLabel);
            GUILayout.Label("允许多图集 " + (info.multiAtlas ? "是" : "否"), EditorStyles.wordWrappedMiniLabel);
            for (int i = 0; i < info.atlases.Count; i++)
            {
                var atlas = info.atlases[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("#" + i + "  " + atlas.width + " × " + atlas.height + "  " + FontBakeService.FormatBytes(atlas.estimatedAtlasBytes), EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button("定位", GUILayout.Width(42))) window.Ping(atlas.texture);
                }
                GUILayout.Label(new GUIContent(atlas.assetPath, atlas.assetPath), EditorStyles.wordWrappedMiniLabel);
            }
            if (info.atlases.Count > 0)
            {
                window.showAtlasDetails = EditorGUILayout.Foldout(window.showAtlasDetails, "纹理详情与图集预览", true);
                if (window.showAtlasDetails)
                    foreach (var atlas in info.atlases)
                    {
                        GUILayout.Label(atlas.texture.name + "\n" + atlas.format + "  Mipmap " + atlas.mipCount + "  CPU 可读 " + (atlas.readable ? "是" : "否"), EditorStyles.wordWrappedMiniLabel);
                        var rect = GUILayoutUtility.GetRect(1, 160, GUILayout.ExpandWidth(true), GUILayout.MaxHeight(160));
                        EditorGUI.DrawTextureAlpha(rect, atlas.texture, ScaleMode.ScaleToFit);
                    }
            }
            if (info.material != null)
            {
                GUILayout.Space(8);
                GUILayout.Label("材质", EditorStyles.miniBoldLabel);
                GUILayout.Label(info.material.name + "\n" + info.materialPath, EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("定位材质")) window.Ping(info.material);
            }
        }

        #endregion

        #region 预览调参

        public static void DrawPreviewSettings(FontManagerWindow window)
        {
            GUILayout.Label("预览调参", EditorStyles.boldLabel);
            GUILayout.Label("只影响中间预览 不是烘焙采样字号  Padding 也不是排版字距", EditorStyles.wordWrappedMiniLabel);
            var serialized = new SerializedObject(window.profile);
            serialized.Update();
            GUILayout.Label("文本框", EditorStyles.miniBoldLabel);
            Slider(serialized, "style.width", "宽度 px", 64, 1920, true);
            Slider(serialized, "style.height", "高度 px", 32, 1080, true);
            Slider(serialized, "style.fontSize", "字号", 8, 120);
            GUILayout.Space(4);
            GUILayout.Label("样式", EditorStyles.miniBoldLabel);
            Field(serialized, "style.color", "文字颜色");
            Field(serialized, "style.background", "背景颜色");
            Field(serialized, "style.outlineColor", "描边颜色");
            Slider(serialized, "style.outlineWidth", "描边宽度", 0, .5f);
            Field(serialized, "style.shadow", "阴影");
            if (window.profile.style.shadow)
            {
                Field(serialized, "style.shadowColor", "阴影颜色");
                Field(serialized, "style.shadowOffset", "阴影偏移");
            }
            Slider(serialized, "style.characterSpacing", "字距", -10, 30);
            Slider(serialized, "style.lineSpacing", "行距", -10, 30);
            Field(serialized, "style.wrap", "自动换行");
            Field(serialized, "style.overflow", "溢出方式");
            if (serialized.ApplyModifiedProperties()) window.Invalidate();
        }

        #endregion

        #region 缺失补齐

        public static void DrawMissing(FontManagerWindow window)
        {
            GUILayout.Label("缺失补齐", EditorStyles.boldLabel);
            GUILayout.Label("先检查当前 TMP 再定位缺字来自哪段文案 哪个对象与哪个源文件", EditorStyles.wordWrappedMiniLabel);
            if (window.CurrentTmp != null) DrawFallbackSettings(window, window.CurrentTmp);
            else
            {
                var fallbacks = new SerializedObject(window.profile);
                EditorGUILayout.PropertyField(fallbacks.FindProperty("fallbackFontAssets"), new GUIContent("备用 TMP 字体"), true);
                if (fallbacks.ApplyModifiedProperties()) window.Invalidate();
            }
            GUILayout.Space(8);
            GUILayout.Label("检查的文案", EditorStyles.miniBoldLabel);
            var serialized = new SerializedObject(window.profile);
            serialized.Update();
            EditorGUILayout.PropertyField(serialized.FindProperty("manualText"), new GUIContent("手动文案"));
            EditorGUILayout.PropertyField(serialized.FindProperty("locale"), new GUIContent("语言标识"));
            EditorGUILayout.PropertyField(serialized.FindProperty("includeLocalizationTable"), new GUIContent("收集 Luban 语言表"));
            EditorGUILayout.PropertyField(serialized.FindProperty("textFiles"), new GUIContent("文本文件"), true);
            EditorGUILayout.PropertyField(serialized.FindProperty("contentAssets"), new GUIContent("场景 / Prefab"), true);
            window.showSourceOptions = EditorGUILayout.Foldout(window.showSourceOptions, "收集选项", true);
            if (window.showSourceOptions)
            {
                var richText = serialized.FindProperty("richText");
                richText.boolValue = EditorGUILayout.ToggleLeft("识别手动文案和文件中的 TMP 标签", richText.boolValue);
                var placeholders = serialized.FindProperty("stripPlaceholders");
                placeholders.boolValue = EditorGUILayout.ToggleLeft("忽略格式化占位符", placeholders.boolValue);
                var common = serialized.FindProperty("includeCommonCharacters");
                common.boolValue = EditorGUILayout.ToggleLeft("补充数字与常用符号", common.boolValue);
            }
            if (serialized.ApplyModifiedProperties()) window.Invalidate();
            GUILayout.Label("文本文件按正文读取 场景/Prefab 根据文本组件设置收集", EditorStyles.wordWrappedMiniLabel);
            if (window.applyWorkflow != null && GUILayout.Button("导入当前 外部工作流规划文案")) window.Mutate(() => window.profile.manualText += "\n" + window.workflowText);
            GUILayout.Space(8);
            using (new EditorGUI.DisabledScope(window.CurrentTmp == null && window.CurrentSource == null))
                if (GUILayout.Button("检查 TMP 缺失字形", GUILayout.Height(28))) window.Run(window.Collect);
            if (window.CurrentSource == null) EditorGUILayout.HelpBox(window.CurrentTmp == null ? "请选择一个 TMP 或指定源 TTF" : "当前 TMP 没有可追溯的源 TTF 将只检查已烘焙字符和 fallback", MessageType.Info);
            if (window.CurrentTmp != null && !window.GeneratedMatchesSource())
                EditorGUILayout.HelpBox("关联源字体与当前 TMP 不同 请恢复原来源或选择对应 TMP", MessageType.Warning);
            using (new EditorGUI.DisabledScope(window.CurrentSource == null))
                if (GUILayout.Button(window.CurrentTmp == null ? "创建主 TMP" : "补齐当前 TMP", GUILayout.Height(28))) window.Run(window.Bake);
            if (window.characterSet != null && window.coverage != null) DrawReport(window);
        }

        public static void DrawFallbackSettings(FontManagerWindow window, TMP_FontAsset font)
        {
            GUILayout.Label("Fallback（备用 TMP 字体）", EditorStyles.miniBoldLabel);
            GUILayout.Label("主 TMP 找不到字形时按顺序继续查找以下 TMP", EditorStyles.wordWrappedMiniLabel);
            var serialized = new SerializedObject(font);
            serialized.Update();
            var property = serialized.FindProperty("m_FallbackFontAssetTable");
            if (property != null) EditorGUILayout.PropertyField(property, new GUIContent("备用字体"), true);
            if (serialized.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(font);
                if (window.profile != null) window.profile.fallbackFontAssets = font.fallbackFontAssetTable == null ? new System.Collections.Generic.List<TMP_FontAsset>() : font.fallbackFontAssetTable.Where(f => f != null).ToList();
                if (window.profile != null) EditorUtility.SetDirty(window.profile);
                font.fallbackFontAssetTable = window.profile.fallbackFontAssets.Where(f => f != font).Distinct().ToList();
                AssetDatabase.SaveAssetIfDirty(font);
                window.ClearCandidate();
                window.Invalidate();
            }
        }

        public static void DrawReport(FontManagerWindow window)
        {
            var characterSet = window.characterSet;
            var coverage = window.coverage;
            GUILayout.Space(6);
            GUILayout.Label("字符诊断", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(window.ReportSummary(), coverage.unsupported.Count > 0 ? MessageType.Warning : MessageType.Info);
            if (coverage.unsupported.Count > 0)
                EditorGUILayout.HelpBox("这些字符未被主 TMP 源 TTF 或 fallback 覆盖 请用「定位来源」找到具体文案 再补充字形或配置 fallback", MessageType.Warning);
            int next = EditorGUILayout.Popup("显示", window.reportFilter, new[] { "未覆盖  " + coverage.unsupported.Count, "待烘焙  " + coverage.unbaked.Count, "Fallback  " + coverage.resolvedByFallback.Count, "动态待生成  " + coverage.dynamicPending.Count, "全部  " + characterSet.Codes.Length });
            if (window.reportFilter != next) { window.reportFilter = next; window.reportPage = 0; }
            var rows = characterSet.characters.Where(p => window.reportFilter == 4 || (window.reportFilter == 0 ? coverage.unsupported.Contains(p.Key) : window.reportFilter == 1 ? coverage.unbaked.Contains(p.Key) : window.reportFilter == 2 ? coverage.resolvedByFallback.ContainsKey(p.Key) : coverage.dynamicPending.Contains(p.Key))).ToArray();
            const int pageSize = 10;
            int pages = Math.Max(1, (rows.Length + pageSize - 1) / pageSize);
            window.reportPage = Mathf.Clamp(window.reportPage, 0, pages - 1);
            if (rows.Length == 0) GUILayout.Label("此分类没有字符", EditorStyles.wordWrappedMiniLabel);
            foreach (var row in rows.Skip(window.reportPage * pageSize).Take(pageSize))
            {
                string character = row.Key == 32 ? "空格" : char.ConvertFromUtf32((int)row.Key);
                coverage.resolvedByFallback.TryGetValue(row.Key, out var provider);
                string rowState = coverage.unsupported.Contains(row.Key) ? "未覆盖 定位来源并补充字形" : coverage.dynamicPending.Contains(row.Key) ? "动态待生成 容量尚未验证" : provider != null ? "Fallback 已覆盖" : coverage.unbaked.Contains(row.Key) ? "源 TTF 有字 待烘焙" : "当前 TMP 已有";
                GUILayout.Space(5);
                GUILayout.Label(character + "  U+" + row.Key.ToString("X4"), EditorStyles.boldLabel);
                GUILayout.Label(rowState, EditorStyles.wordWrappedLabel);
                if (provider != null)
                {
                    GUILayout.Label("提供字体 " + provider.name, EditorStyles.wordWrappedMiniLabel);
                    if (GUILayout.Button("定位备用 TMP")) window.Ping(provider);
                }
                var glyphFont = provider != null ? provider : window.CurrentTmp;
                if (glyphFont != null && glyphFont.characterLookupTable.TryGetValue(row.Key, out var entry) && entry.glyph != null)
                {
                    var glyph = entry.glyph;
                    var rect = glyph.glyphRect;
                    GUILayout.Label("图集 #" + glyph.atlasIndex + "  区域 " + rect.x + ", " + rect.y + " / " + rect.width + "×" + rect.height, EditorStyles.wordWrappedMiniLabel);
                    if (glyph.atlasIndex < glyphFont.atlasTextures.Length && glyphFont.atlasTextures[glyph.atlasIndex] != null && GUILayout.Button("定位字形纹理")) window.Ping(glyphFont.atlasTextures[glyph.atlasIndex]);
                }
                foreach (var origin in row.Value)
                {
                    GUILayout.Label(new GUIContent("来源 " + origin.label, origin.assetPath), EditorStyles.wordWrappedMiniLabel);
                    if (GUILayout.Button("定位来源")) FontCharacterCollector.Locate(origin);
                }
            }
            GUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(window.reportPage == 0))
                    if (GUILayout.Button("上一页", GUILayout.Width(54))) window.reportPage = Math.Max(0, window.reportPage - 1);
                GUILayout.Label((window.reportPage + 1) + " / " + pages + " 页  " + rows.Length + " 个字符", EditorStyles.wordWrappedMiniLabel, GUILayout.ExpandWidth(true));
                using (new EditorGUI.DisabledScope(window.reportPage == pages - 1))
                    if (GUILayout.Button("下一页", GUILayout.Width(54))) window.reportPage = Math.Min(pages - 1, window.reportPage + 1);
            }
        }

        #endregion

        #region 性能优化

        public static void DrawPerformance(FontManagerWindow window)
        {
            GUILayout.Label("性能优化", EditorStyles.boldLabel);
            GUILayout.Label("先生成候选 检查图集和效果 再采用", EditorStyles.wordWrappedMiniLabel);
            if (window.CurrentTmp != null) DrawFontStats("优化前", window.CurrentTmp);
            GUILayout.Space(8);
            EditorGUI.BeginChangeCheck();
            window.optimizationSubset = EditorGUILayout.ToggleLeft("仅保留文案来源中的字符", window.optimizationSubset);
            GUILayout.Label(window.optimizationSubset ? "使用缺失补齐页收集的文案 未收集的字符将从候选中移除" : "保留当前 TMP 的全部字符 并补入文案中的字符", EditorStyles.wordWrappedMiniLabel);
            window.optimizationSamplingSize = EditorGUILayout.IntSlider("采样字号", window.optimizationSamplingSize, 16, 200);
            window.optimizationPadding = EditorGUILayout.IntSlider("Padding", window.optimizationPadding, 1, 32);
            window.optimizationAtlasSize = EditorGUILayout.IntPopup("图集尺寸", window.optimizationAtlasSize, new[] { "512", "1024", "2048", "4096" }, new[] { 512, 1024, 2048, 4096 });
            window.optimizationRenderMode = (GlyphRenderMode)EditorGUILayout.IntPopup("渲染模式", (int)window.optimizationRenderMode, new[] { "SDFAA", "SDF", "SDF8", "SDF16", "SDF32" }, new[] { (int)GlyphRenderMode.SDFAA, (int)GlyphRenderMode.SDF, (int)GlyphRenderMode.SDF8, (int)GlyphRenderMode.SDF16, (int)GlyphRenderMode.SDF32 });
            window.optimizationPopulationMode = (AtlasPopulationMode)EditorGUILayout.EnumPopup("字体模式", window.optimizationPopulationMode);
            window.optimizationMultiAtlas = EditorGUILayout.Toggle("允许多图集", window.optimizationMultiAtlas);
            if (EditorGUI.EndChangeCheck()) { window.ClearCandidate(); window.Invalidate(); }
            GUILayout.Label("采样字号是图中字形的像素尺度 Padding 是距离场范围 减少字符后还需缩小图集或减少张数才会减少纹理内存", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUI.DisabledScope(window.CurrentSource == null))
                if (GUILayout.Button("生成优化候选并预览", GUILayout.Height(28))) window.Run(window.BuildOptimizationPreview);
            if (window.CurrentSource == null) EditorGUILayout.HelpBox("请先关联可用的源 TTF 才能重建图集 现有 TMP 仍可预览和诊断", MessageType.Info);
            if (window.optimizedPreviewFont != null)
            {
                GUILayout.Space(8);
                DrawFontStats("优化后 候选", window.optimizedPreviewFont);
                long before = FontBakeService.EstimateAtlasBytes(window.CurrentTmp);
                long after = FontBakeService.EstimateAtlasBytes(window.optimizedPreviewFont);
                GUILayout.Label("纹理变化 " + (after <= before ? "减少 " : "增加 ") + FontBakeService.FormatBytes(Math.Abs(before - after)), EditorStyles.wordWrappedLabel);
                if (window.CurrentTmp != null) GUILayout.Label("移除的原有字符 " + window.CurrentTmp.characterTable.Count(c => c.unicode >= 32 && c.unicode != 127 && !window.candidateCodes.Contains(c.unicode)), EditorStyles.wordWrappedMiniLabel);
                bool canUpdate = FontBakeService.CanUpdateInPlace(window.CurrentTmp, window.optimizedPreviewFont, out string reason);
                if (!canUpdate && window.CurrentTmp != null) EditorGUILayout.HelpBox(reason + " 可另存候选后应用到选中的 UI", MessageType.Info);
                using (new EditorGUI.DisabledScope(!canUpdate))
                    if (GUILayout.Button("备份并写回当前 TMP", GUILayout.Height(26))) window.Run(window.ApplyOptimizationCandidate);
                if (GUILayout.Button("另存为新 TMP")) window.Run(() => window.SaveOptimizationCandidate(false));
                if (GUILayout.Button("丢弃候选", EditorStyles.miniButton)) { window.ClearCandidate(); window.Invalidate(); }
            }
            GUILayout.Space(8);
            GUILayout.Label("这里只估算整张图集像素数据 不含 CPU 可读副本 源字体和字符表 TTF 文件大小 构建包大小与运行时内存是不同指标", EditorStyles.wordWrappedMiniLabel);
        }

        public static void DrawFontStats(string title, TMP_FontAsset font)
        {
            if (font == null) return;
            var info = FontBakeService.Inspect(font);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(title, EditorStyles.miniBoldLabel);
                GUILayout.Label(info.populationMode + "  " + info.characterCount + " 字符  " + info.glyphCount + " 字形", EditorStyles.wordWrappedMiniLabel);
                GUILayout.Label("采样 " + font.faceInfo.pointSize + "  Padding " + font.atlasPadding + "  " + font.atlasRenderMode, EditorStyles.wordWrappedMiniLabel);
                GUILayout.Label("图集 " + info.atlases.Count + " 张  " + string.Join(" / ", info.atlases.Select(a => a.width + "×" + a.height)) + "  约 " + FontBakeService.FormatBytes(info.estimatedAtlasBytes), EditorStyles.wordWrappedMiniLabel);
                GUILayout.Label("多图集 " + (info.multiAtlas ? "开" : "关") + "  主字体源 TTF " + (info.requiresRuntimeSource ? "运行时需要" : "静态显示无需"), EditorStyles.wordWrappedMiniLabel);
            }
        }

        #endregion

        #region 应用与新建

        public static void DrawApply(FontManagerWindow window)
        {
            GUILayout.Label("应用到 UI", EditorStyles.boldLabel);
            GUILayout.Label("列出 Hierarchy 选中对象中的 TMP 文本 确认后写入字体和样式 支持 Undo", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUI.DisabledScope(window.CurrentTmp == null))
            {
                if (GUILayout.Button("保存当前样式材质")) window.Run(() => { window.SaveMaterial(); window.Notify("样式材质已保存"); });
                if (GUILayout.Button("预览选中 UI 的应用范围"))
                {
                    window.applyTargets = Selection.gameObjects.Where(g => g.scene.IsValid() && !EditorUtility.IsPersistent(g)).SelectMany(g => g.GetComponentsInChildren<TMP_Text>(true)).Distinct().ToArray();
                    window.Notify(window.applyTargets.Length == 0 ? "请在 Hierarchy 选择含 TMP 文本的对象" : "已列出应用范围 确认后可应用并支持 Undo", window.applyTargets.Length == 0 ? MessageType.Warning : MessageType.Info);
                }
                if (window.applyTargets.Length > 0)
                {
                    GUILayout.Label(string.Join("\n", window.applyTargets.Where(t => t != null).Select(t => AnimationUtility.CalculateTransformPath(t.transform, null))), EditorStyles.wordWrappedMiniLabel);
                    if (GUILayout.Button("应用到以上 " + window.applyTargets.Length + " 个文本组件")) window.Run(window.Apply);
                }
                if (window.applyWorkflow != null && GUILayout.Button("回填 Image2UGUI 字体")) window.Run(() => { window.RequireCurrent(); window.applyWorkflow(window.CurrentTmp); window.Notify("已回填当前 Image2UGUI 任务字体"); });
            }
        }

        public static void DrawCreate(FontManagerWindow window)
        {
            GUILayout.Label("从源字体新建", EditorStyles.boldLabel);
            GUILayout.Label("指定源 TTF 后按缺失补齐的文案创建主 TMP", EditorStyles.wordWrappedMiniLabel);
            if (window.profile == null) return;
            var source = (Font)EditorGUILayout.ObjectField("源 TTF / OTF", window.profile.sourceFont, typeof(Font), false);
            if (source != window.profile.sourceFont) window.Mutate(() => window.profile.sourceFont = source);
            var fallbacks = new SerializedObject(window.profile);
            EditorGUILayout.PropertyField(fallbacks.FindProperty("fallbackFontAssets"), new GUIContent("备用 TMP 字体"), true);
            if (fallbacks.ApplyModifiedProperties()) window.Invalidate();
            GUILayout.Space(8);
            using (new EditorGUI.DisabledScope(window.CurrentSource == null))
                if (GUILayout.Button("创建主 TMP", GUILayout.Height(32))) window.Run(window.Bake);
            if (window.CurrentSource == null) EditorGUILayout.HelpBox("请指定包含目标字形的源 TTF", MessageType.Info);
        }

        public static void DrawProfile(FontManagerWindow window, FontBakeProfile bound)
        {
            GUILayout.Label("字体配置", EditorStyles.boldLabel);
            GUILayout.Label("配置只保存文案来源和预览样式 工作对象仍是左侧 TMP", EditorStyles.wordWrappedMiniLabel);
            var next = (FontBakeProfile)EditorGUILayout.ObjectField("当前配置", window.profile, typeof(FontBakeProfile), false);
            if (next != window.profile)
            {
                window.profile = next;
                window.ClearCandidate();
                window.Invalidate();
                window.RefreshMenuSelection();
            }
            if (bound != null && bound != window.profile && GUILayout.Button("使用此配置"))
            {
                window.profile = bound;
                window.ClearCandidate();
                window.Invalidate();
                window.RefreshMenuSelection();
            }
            if (GUILayout.Button("新建配置", GUILayout.Height(28))) window.Run(window.CreateProfile);
            if (window.profile != null && window.profile.generatedFont != null && GUILayout.Button("打开关联 TMP"))
                window.SelectTmp(window.profile.generatedFont);
        }

        #endregion

        static void Field(SerializedObject obj, string name, string label) => EditorGUILayout.PropertyField(obj.FindProperty(name), new GUIContent(label));

        static void Slider(SerializedObject obj, string name, string label, float min, float max, bool integer = false)
        {
            var prop = obj.FindProperty(name);
            if (integer) prop.intValue = EditorGUILayout.IntSlider(label, prop.intValue, (int)min, (int)max);
            else prop.floatValue = EditorGUILayout.Slider(label, prop.floatValue, min, max);
        }
    }
}
