using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using YooAsset.Editor;

namespace MieMieFrameWork.Editor.StaticAtlas
{
    /// <summary>
    /// 静态检查结果 不代替最终 Bundle 构建报告
    /// </summary>
    public sealed class StaticAtlasReport
    {
        /// <summary>
        /// 阻止保存的问题
        /// </summary>
        public readonly List<string> ErrorList = new List<string>();
        /// <summary>
        /// 需要人工确认的风险
        /// </summary>
        public readonly List<string> WarningList = new List<string>();
    }

    /// <summary>
    /// 检查重复归属与来源 不修改图片或采集器
    /// </summary>
    public static class StaticAtlasDiagnostics
    {
        /// <summary>
        /// 展开图集的纹理 Sprite 与目录收录 返回纹理路径集合
        /// </summary>
        public static HashSet<string> GetTexturePathHashList(SpriteAtlas atlas)
        {
            var PathHashList = new HashSet<string>(StringComparer.Ordinal);
            foreach (var Packable in atlas.GetPackables())
            {
                string Path = AssetDatabase.GetAssetPath(Packable);
                if (AssetDatabase.IsValidFolder(Path))
                {
                    foreach (string Guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Path }))
                    {
                        string TexturePath = AssetDatabase.GUIDToAssetPath(Guid);
                        var Importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
                        if (Importer != null && Importer.textureType == TextureImporterType.Sprite)
                            PathHashList.Add(TexturePath);
                    }
                }
                else if (Packable is Texture2D || Packable is Sprite)
                    PathHashList.Add(Path);
            }
            return PathHashList;
        }

        /// <summary>
        /// 检查尺寸 同名 Sprite 跨图集重复归属与更新时的收录差异
        /// </summary>
        public static StaticAtlasReport Check(IReadOnlyList<Texture2D> textureList, StaticAtlasOptions options, string targetPath)
        {
            var Report = new StaticAtlasReport();
            if (textureList.Count == 0)
                Report.ErrorList.Add("来源目录没有 Sprite 纹理 普通 Texture2D 不会被自动改成 Sprite");
            var PathHashList = new HashSet<string>(textureList.Select(AssetDatabase.GetAssetPath), StringComparer.Ordinal);
            foreach (var Texture in textureList)
            {
                string SourcePath = AssetDatabase.GetAssetPath(Texture);
                if (!AssetDatabase.LoadAllAssetsAtPath(SourcePath).OfType<Sprite>().Any())
                    Report.ErrorList.Add($"Sprite 纹理未产生可收录的 Sprite 子资产 请检查 Sprite Mode 与切片 {SourcePath}");
                if (Texture.width + options.Padding * 2 > options.MaxSize || Texture.height + options.Padding * 2 > options.MaxSize)
                    Report.ErrorList.Add($"图片含边距超过最大尺寸 {AssetDatabase.GetAssetPath(Texture)} {Texture.width}x{Texture.height}");
            }
            foreach (var Group in textureList.GroupBy(Texture => Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(Texture)), StringComparer.Ordinal))
            {
                if (Group.Count() > 1)
                    Report.WarningList.Add($"同名源文件 {Group.Key} 使用按文件名寻址时可能冲突");
            }
            foreach (var Group in textureList.SelectMany(Texture => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(Texture)).OfType<Sprite>())
                         .GroupBy(Sprite => Sprite.name, StringComparer.Ordinal))
            {
                if (Group.Count() > 1)
                    Report.WarningList.Add($"同名 Sprite {Group.Key} 不适合通过图集 GetSprite 按名称查找");
            }
            foreach (string Guid in AssetDatabase.FindAssets("t:SpriteAtlas"))
            {
                string Path = AssetDatabase.GUIDToAssetPath(Guid);
                if (Path == targetPath)
                    continue;
                var Atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(Path);
                if (Atlas.isVariant)
                    continue;
                foreach (string SourcePath in GetTexturePathHashList(Atlas))
                {
                    if (PathHashList.Contains(SourcePath))
                        Report.ErrorList.Add($"重复归属 {SourcePath} 已被 {Path} 收录");
                }
            }
            var Target = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(targetPath);
            if (Target != null)
            {
                foreach (string Path in GetTexturePathHashList(Target).Except(PathHashList))
                    Report.WarningList.Add($"更新将移除原收录 {Path}");
            }
            return Report;
        }

        /// <summary>
        /// 只读列出覆盖输出路径的 YooAsset 采集器 不执行过滤规则或自动更改配置
        /// </summary>
        public static List<string> GetCollectorHintList(string targetPath)
        {
            var HintList = new List<string>();
            foreach (string Guid in AssetDatabase.FindAssets("t:BundleCollectorSetting"))
            {
                var Setting = AssetDatabase.LoadAssetAtPath<BundleCollectorSetting>(AssetDatabase.GUIDToAssetPath(Guid));
                foreach (var Package in Setting.Packages)
                foreach (var Group in Package.Groups)
                foreach (var Collector in Group.Collectors)
                {
                    string Path = Collector.CollectPath.TrimEnd('/');
                    if (targetPath == Path || (AssetDatabase.IsValidFolder(Path) && targetPath.StartsWith(Path + "/", StringComparison.Ordinal)))
                        HintList.Add($"{Package.PackageName}/{Group.GroupName} {Collector.CollectorType} {Path} 过滤 {Collector.FilterRuleName} 打包 {Collector.PackRuleName} 寻址 {Collector.AddressRuleName}");
                }
            }
            if (HintList.Count == 0)
                HintList.Add("未发现路径覆盖 请在现有 YooAsset 采集器中确认图集被收集 不会自动增加采集规则");
            HintList.Add("路径覆盖不代表通过过滤或实际被打包 原图与图集依赖是否重复须查看 YooAsset 构建报告");
            return HintList;
        }
    }
}
