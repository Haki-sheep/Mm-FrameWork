using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MieMieFrameWork.Localization.FontManagement
{
    [CreateAssetMenu(menuName = "MieMieFramework/字体/字体烘焙配置", fileName = "FontProfile")]
    public sealed class FontBakeProfile : ScriptableObject
    {
        public Font sourceFont;
        public string locale = "zh-CN";
        public string usage = "正文";
        // Kept hidden for asset compatibility; the first editor version does not manage licenses or comparisons.
        [HideInInspector] public TextAsset license;
        [HideInInspector] public List<Font> comparisonFonts = new List<Font>();
        public List<TextAsset> textFiles = new List<TextAsset>();
        public List<UnityEngine.Object> contentAssets = new List<UnityEngine.Object>();
        [TextArea(4, 12)] public string manualText = "开始游戏  设置  背包\n欢迎来到冒险世界！\nLevel 12  HP 100/100  金币 +2,500";
        public bool richText;
        public bool stripPlaceholders;
        public bool includeCommonCharacters = true;
        /// <summary> 收集配置语言的 Luban 文案 </summary>
        public bool includeLocalizationTable;
        public int samplingSize = 64;
        public int padding = 8;
        public int atlasSize = 2048;
        public GlyphRenderMode renderMode = GlyphRenderMode.SDFAA;
        // Static assets are the default for fixed UI. Dynamic assets keep the
        // source TTF and can fill the atlas when an unseen character appears.
        public AtlasPopulationMode populationMode = AtlasPopulationMode.Static;
        public bool enableMultiAtlas;
        // Fallbacks are TMP Font Assets (never raw TTF files). They are checked
        // before a character is reported as missing from the source font.
        public List<TMP_FontAsset> fallbackFontAssets = new List<TMP_FontAsset>();
        public TMP_FontAsset generatedFont;
        [HideInInspector] public string lastBakeSignature;
        [HideInInspector] public string lastBakedSourceFontGuid;
        [HideInInspector] public string lastBakeSummary;
        public FontPreviewStyle style = new FontPreviewStyle();
    }

    [Serializable]
    public sealed class FontPreviewStyle
    {
        public float fontSize = 32;
        public Color color = Color.white;
        public Color background = new Color(.12f, .14f, .18f, 1);
        public Color outlineColor = Color.black;
        public float outlineWidth;
        public bool shadow;
        public Color shadowColor = new Color(0, 0, 0, .65f);
        public Vector2 shadowOffset = new Vector2(1, -1);
        public float characterSpacing;
        public float lineSpacing;
        public int width = 480;
        public int height = 180;
        public bool wrap = true;
        public TextOverflowModes overflow = TextOverflowModes.Truncate;
    }
}
