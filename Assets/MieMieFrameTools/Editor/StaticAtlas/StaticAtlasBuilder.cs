using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace MieMieFrameWork.Editor.StaticAtlas
{
    /// <summary>
    /// 使用 Unity 原生图集 API 显式创建或更新 不改采集器与原图
    /// </summary>
    public static class StaticAtlasBuilder
    {
        #region 来源与参数

        /// <summary>
        /// 扫描选中目录中的 Sprite 纹理 去重并保持路径排序
        /// </summary>
        public static List<Texture2D> CollectTextureList(IEnumerable<DefaultAsset> folderList)
        {
            var PathHashList = new HashSet<string>(StringComparer.Ordinal);
            foreach (var Folder in folderList)
            {
                string Path = AssetDatabase.GetAssetPath(Folder);
                if (!Path.StartsWith("Assets/", StringComparison.Ordinal) || !AssetDatabase.IsValidFolder(Path))
                    throw new ArgumentException($"图集来源必须是 Assets 下的有效目录 {Path}");
                foreach (string Guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Path }))
                {
                    string TexturePath = AssetDatabase.GUIDToAssetPath(Guid);
                    var Importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
                    if (Importer != null && Importer.textureType == TextureImporterType.Sprite)
                        PathHashList.Add(TexturePath);
                }
            }
            return PathHashList.OrderBy(Path => Path, StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>).ToList();
        }

        /// <summary>
        /// 仅接受原生图集可用的尺寸与 Padding 不静默修正输入
        /// </summary>
        private static void ValidateOptions(StaticAtlasOptions options)
        {
            if (options.MaxSize < 32 || options.MaxSize > 8192 || !Mathf.IsPowerOfTwo(options.MaxSize))
                throw new ArgumentException("图集最大尺寸必须是 32 到 8192 的二次幂");
            if (options.Padding != 2 && options.Padding != 4 && options.Padding != 8)
                throw new ArgumentException("图集 Padding 必须是 2 4 或 8");
            if (options.WritePlatformOverride && options.PlatformName != "Android" && options.PlatformName != "iPhone" && options.PlatformName != "Standalone")
                throw new ArgumentException($"不支持的平台覆盖 {options.PlatformName}");
            if (options.WritePlatformOverride && options.PlatformOverrideEnabled && options.PlatformFormat != TextureImporterFormat.Automatic)
            {
                var eTarget = options.PlatformName == "Android" ? BuildTarget.Android : options.PlatformName == "iPhone" ? BuildTarget.iOS : BuildTarget.StandaloneWindows64;
                if (!TextureImporter.IsPlatformTextureFormatValid(TextureImporterType.Sprite, eTarget, options.PlatformFormat))
                    throw new ArgumentException($"纹理格式不适用于指定平台 {options.PlatformName} {options.PlatformFormat}");
            }
        }

        /// <summary>
        /// 校验图集模式与磁盘目标 防止覆盖未知资产或覆盖脏 Inspector
        /// </summary>
        private static void ValidateTarget(string path, bool create)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                throw new InvalidOperationException("图集写入须在空闲 Edit Mode 执行");
            if (EditorSettings.spritePackerMode == SpritePackerMode.Disabled)
                throw new InvalidOperationException("Sprite Packer 已关闭 请先显式启用 Sprite Atlas 模式");
            string Folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !AssetDatabase.IsValidFolder(Folder))
                throw new ArgumentException($"图集目标必须位于 Assets 下的已有目录 {path}");
            string Extension = Path.GetExtension(path);
            if (Extension != ".spriteatlas" && Extension != ".spriteatlasv2")
                throw new ArgumentException($"不是支持的图集路径 {path}");
            bool IsV2 = EditorSettings.spritePackerMode.ToString().Contains("AtlasV2");
            if (create && (IsV2 ? Extension != ".spriteatlasv2" : Extension != ".spriteatlas"))
                throw new ArgumentException("新图集扩展名必须匹配当前 Sprite Packer 模式");
            if (create && (File.Exists(path) || AssetDatabase.LoadMainAssetAtPath(path) != null))
                throw new InvalidOperationException($"目标已存在 请改用更新入口 {path}");
            if (!create && AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path) == null)
                throw new InvalidOperationException($"目标不是可更新图集 {path}");
            var Target = AssetDatabase.LoadMainAssetAtPath(path);
            if (Target != null && EditorUtility.IsDirty(Target))
                throw new InvalidOperationException($"图集有未保存修改 请先处理 Inspector 修改 {path}");
            var Importer = AssetImporter.GetAtPath(path);
            if (Importer != null && EditorUtility.IsDirty(Importer))
                throw new InvalidOperationException($"图集导入器有未保存修改 请先处理 {path}");
            if (!create && !AssetDatabase.IsOpenForEdit(path))
                throw new InvalidOperationException($"图集不可写或未签出 {path}");
        }

        #endregion

        #region 显式写入

        /// <summary>
        /// 输入已检查的源图列表 创建新图集或替换原图集收录 保持原 GUID
        /// </summary>
        public static SpriteAtlas Save(string path, IReadOnlyList<Texture2D> textureList, StaticAtlasOptions options, bool create)
        {
            ValidateOptions(options);
            ValidateTarget(path, create);
            var Report = StaticAtlasDiagnostics.Check(textureList, options, path);
            if (Report.ErrorList.Count > 0)
                throw new InvalidOperationException(string.Join("\n", Report.ErrorList));
            if (textureList.Count == 0)
                throw new ArgumentException("没有可收录的 Sprite 纹理 请先检查来源目录");
            foreach (var Texture in textureList)
            {
                string SourcePath = AssetDatabase.GetAssetPath(Texture);
                var Importer = AssetImporter.GetAtPath(SourcePath) as TextureImporter;
                if (Importer == null || Importer.textureType != TextureImporterType.Sprite)
                    throw new ArgumentException($"源图片不是 Sprite 纹理 {SourcePath}");
                if (Texture.width + options.Padding * 2 > options.MaxSize || Texture.height + options.Padding * 2 > options.MaxSize)
                    throw new ArgumentException($"图片含边距超出图集最大尺寸 {SourcePath} {Texture.width}x{Texture.height}");
            }
            var PackableList = textureList.Cast<UnityEngine.Object>().ToArray();
            // 明确替换收录列表 保留原图 GUID 和其他平台覆盖
            if (path.EndsWith(".spriteatlasv2", StringComparison.Ordinal))
            {
                var Existing = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                var Asset = create ? new SpriteAtlasAsset() : SpriteAtlasAsset.Load(path);
                if (Asset.isVariant)
                    throw new InvalidOperationException("不更新 Variant 图集 请编辑对应 Master");
                if (!create)
                    Asset.Remove(Existing.GetPackables());
                Asset.Add(PackableList);
                SpriteAtlasAsset.Save(Asset, path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var Importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
                Importer.packingSettings = Packing(Importer.packingSettings, options);
                Importer.textureSettings = TextureSettings(Importer.textureSettings, options);
                Importer.includeInBuild = options.IncludeInBuild;
                Importer.SetPlatformSettings(DefaultPlatform(Importer.GetPlatformSettings("DefaultTexturePlatform"), options));
                if (options.WritePlatformOverride)
                    Importer.SetPlatformSettings(OverridePlatform(Importer.GetPlatformSettings(options.PlatformName), options));
                Importer.SaveAndReimport();
            }
            else
            {
                var Atlas = create ? new SpriteAtlas() : AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (Atlas.isVariant)
                    throw new InvalidOperationException("不更新 Variant 图集 请编辑对应 Master");
                if (!create)
                {
                    Undo.RecordObject(Atlas, "更新静态图集");
                    Atlas.Remove(Atlas.GetPackables());
                }
                Atlas.Add(PackableList);
                Atlas.SetPackingSettings(Packing(Atlas.GetPackingSettings(), options));
                Atlas.SetTextureSettings(TextureSettings(Atlas.GetTextureSettings(), options));
                Atlas.SetIncludeInBuild(options.IncludeInBuild);
                Atlas.SetPlatformSettings(DefaultPlatform(Atlas.GetPlatformSettings("DefaultTexturePlatform"), options));
                if (options.WritePlatformOverride)
                    Atlas.SetPlatformSettings(OverridePlatform(Atlas.GetPlatformSettings(options.PlatformName), options));
                if (create)
                    AssetDatabase.CreateAsset(Atlas, path);
                else
                {
                    EditorUtility.SetDirty(Atlas);
                    AssetDatabase.SaveAssetIfDirty(Atlas);
                }
            }
            return AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path)
                ?? throw new InvalidOperationException($"图集已写入但导入未产生 SpriteAtlas {path}");
        }

        /// <summary>
        /// 创建 UI 装箱设置
        /// </summary>
        private static SpriteAtlasPackingSettings Packing(SpriteAtlasPackingSettings settings, StaticAtlasOptions options)
        {
            settings.padding = options.Padding;
            settings.enableRotation = options.Rotation;
            settings.enableTightPacking = options.TightPacking;
            return settings;
        }

        /// <summary>
        /// 创建无 CPU 副本与无 MipMap 的颜色图设置
        /// </summary>
        private static SpriteAtlasTextureSettings TextureSettings(SpriteAtlasTextureSettings settings, StaticAtlasOptions options)
        {
            settings.readable = false;
            settings.generateMipMaps = false;
            settings.sRGB = options.Srgb;
            settings.filterMode = options.Filter;
            return settings;
        }

        /// <summary>
        /// 保留默认平台其他参数 仅写明确配置的尺寸和压缩
        /// </summary>
        private static TextureImporterPlatformSettings DefaultPlatform(TextureImporterPlatformSettings settings, StaticAtlasOptions options)
        {
            settings.name = "DefaultTexturePlatform";
            settings.maxTextureSize = options.MaxSize;
            settings.textureCompression = options.Compression;
            return settings;
        }

        /// <summary>
        /// 仅更新明确选择的平台 不触碰其他平台覆盖
        /// </summary>
        private static TextureImporterPlatformSettings OverridePlatform(TextureImporterPlatformSettings settings, StaticAtlasOptions options)
        {
            settings.name = options.PlatformName;
            settings.overridden = options.PlatformOverrideEnabled;
            settings.maxTextureSize = options.MaxSize;
            settings.format = options.PlatformFormat;
            settings.textureCompression = options.Compression;
            return settings;
        }

        #endregion
    }
}
