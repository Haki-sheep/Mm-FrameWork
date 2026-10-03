using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MieMieFrameWork.Localization.FontManagement
{
    /// <summary>
    /// 字体工具迁移验证 只创建瞬态资源 不覆盖场景与已有字库
    /// </summary>
    public static class FontManagerVerification
    {
        /// <summary> 本次验证结果输出文件 </summary>
        private const string ReportPath = "Logs/FontManager-verification.txt";

        /// <summary>
        /// 验证文案收集 缺字诊断 字库生成与真实 TMP 渲染
        /// </summary>
        public static void Run()
        {
            var ResultList = new List<string>();
            try
            {
                Check(FontCharacterCollector.VisibleText("<b>Start</b> {0:N0}", true, true) == "Start ",
                    "过滤富文本与格式化占位符", ResultList);
                Check(FontCharacterCollector.CodePoints("A\U0001F431").SequenceEqual(new uint[] { 'A', 0x1F431 }),
                    "按 Unicode 码点收集代理对", ResultList);
                string ChineseText = LocalizationFontTextExporter.GetText("zh-CN");
                string EnglishText = LocalizationFontTextExporter.GetText("en");
                Check(ChineseText.Contains("开始游戏") && !ChineseText.Contains("Start Game"),
                    "Luban 按简中收集实际翻译", ResultList);
                Check(EnglishText.Contains("Start Game") && !EnglishText.Contains("common.start"),
                    "Luban 按英文收集文案而非 Key", ResultList);
                var SourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontManagerPaths.DefaultSource);
                if (SourceFont == null)
                    throw new InvalidOperationException($"验证所需源字体不存在 {FontManagerPaths.DefaultSource}");
                var Profile = ScriptableObject.CreateInstance<FontBakeProfile>();
                TMP_FontAsset Font = null;
                try
                {
                    Profile.sourceFont = SourceFont;
                    Profile.locale = "zh-CN";
                    Profile.manualText = string.Empty;
                    Profile.includeLocalizationTable = true;
                    Profile.includeCommonCharacters = false;
                    Profile.stripPlaceholders = true;
                    var CharacterSet = FontCharacterCollector.Collect(Profile);
                    Check(CharacterSet.Codes.Contains('始') && !CharacterSet.Codes.Contains('{'),
                        "工作台收集语言表并过滤占位符", ResultList);
                    var Unsupported = FontBakeService.Check(SourceFont, new uint[] { 0x10FFFF }, null);
                    Check(Unsupported.unsupported.Contains(0x10FFFF), "诊断源字体不支持的字符", ResultList);
                    Font = FontBakeService.BuildTransient(SourceFont, CharacterSet.Codes, 48, 4, 1024,
                        GlyphRenderMode.SDFAA, AtlasPopulationMode.Static, false, Array.Empty<TMP_FontAsset>());
                    Check(CharacterSet.Codes.All(Code => Font.characterLookupTable.ContainsKey(Code)),
                        "静态字库覆盖收集文案", ResultList);
                    Check(Font.atlasPopulationMode == AtlasPopulationMode.Static && Font.sourceFontFile == null,
                        "静态字库不依赖运行时源字体", ResultList);
                    Check(Font.atlasTextures.Length == 1 && FontBakeService.EstimateAtlasBytes(Font) > 0,
                        "图集生成与内存诊断", ResultList);
                    Profile.style.width = 640;
                    Profile.style.height = 160;
                    using (var Preview = FontPreviewRenderer.Render(Font, "开始游戏\n设置", Profile.style, false))
                    {
                        Check(Preview.lines == 2 && !Preview.overflow, "真实 TMP 排版与溢出诊断", ResultList);
                        var PixelList = Preview.image.GetPixels32();
                        Check(PixelList.Any(Pixel => Pixel.r > 128), "真实 TMP 预览包含文字像素", ResultList);
                        Directory.CreateDirectory("Logs");
                        File.WriteAllBytes("Logs/FontManager-preview.png", Preview.image.EncodeToPNG());
                    }
                    var Lease = new LocaleFontLease(new Dictionary<string, TMP_FontAsset> { { "Body", Font } }, () => { });
                    Check(Lease.GetFont("Body") == Font, "运行时主字体样式映射", ResultList);
                    Lease.Dispose();
                }
                finally
                {
                    FontBakeService.DestroyTransient(Font);
                    UnityEngine.Object.DestroyImmediate(Profile);
                }
                ResultList.Add($"PASS {ResultList.Count} 项验证");
                Directory.CreateDirectory("Logs");
                File.WriteAllLines(ReportPath, ResultList);
                Debug.Log(string.Join("\n", ResultList));
            }
            catch (Exception Error)
            {
                ResultList.Add(Error.ToString());
                Directory.CreateDirectory("Logs");
                File.WriteAllLines(ReportPath, ResultList);
                throw;
            }
        }

        /// <summary>
        /// 批处理验证入口 失败返回非零退出码
        /// </summary>
        public static void RunBatch()
        {
            try
            {
                Run();
                EditorApplication.Exit(0);
            }
            catch (Exception Error)
            {
                Debug.LogException(Error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// 校验条件并记录真实验证结果
        /// </summary>
        private static void Check(bool Condition, string Description, List<string> ResultList)
        {
            if (!Condition)
                throw new InvalidOperationException($"FAIL {Description}");
            ResultList.Add($"PASS {Description}");
        }
    }
}
