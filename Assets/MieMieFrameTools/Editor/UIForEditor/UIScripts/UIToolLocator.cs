using System;
using System.IO;
using UnityEditor;

namespace MieMieUITools.Editor
{
    /// <summary>
    /// UI 生成器资源定位（仅 UIForEditor 目录，不依赖框架其它配置）。
    /// </summary>
    public static class UIToolLocator
    {
        private const string GenerateUITempBaseName = "GenerateUITemp";

        public static bool TryFindGenerateUITempUxmlPath(out string assetPath)
        {
            return TryPickBestPath(
                AssetDatabase.FindAssets($"{GenerateUITempBaseName} t:VisualTreeAsset"),
                p => p.EndsWith($"{GenerateUITempBaseName}.uxml", StringComparison.OrdinalIgnoreCase),
                "UIForEditor",
                out assetPath);
        }

        public static bool TryGetUIGenPathDirectory(out string directory)
        {
            foreach (var guid in AssetDatabase.FindAssets("UIGenPathSettings t:TextAsset"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (!p.EndsWith("/UIGenPathSettings.json", StringComparison.OrdinalIgnoreCase)) continue;
                directory = Path.GetDirectoryName(p)?.Replace('\\', '/');
                return !string.IsNullOrEmpty(directory);
            }

            if (TryGetUIForEditorRoot(out string root))
            {
                directory = $"{root}/UIGenPath";
                return true;
            }

            directory = null;
            return false;
        }

        public static bool TryGetUIForEditorRoot(out string root)
        {
            if (TryFindGenerateUITempUxmlPath(out string uxml))
            {
                string dir = Path.GetDirectoryName(uxml)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(dir))
                {
                    root = null;
                    return false;
                }

                // .../UIForEditor/UIToolkits -> UIForEditor
                root = Path.GetDirectoryName(dir)?.Replace('\\', '/');
                return !string.IsNullOrEmpty(root);
            }

            foreach (var guid in AssetDatabase.FindAssets("UIGenPathSettings t:MonoScript"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (!p.EndsWith("/UIGenPathSettings.cs", StringComparison.OrdinalIgnoreCase)) continue;
                root = Path.GetDirectoryName(Path.GetDirectoryName(p))?.Replace('\\', '/');
                return !string.IsNullOrEmpty(root);
            }

            root = null;
            return false;
        }

        private static bool TryPickBestPath(string[] guids, Func<string, bool> pathFilter, string subPathMustContain,
            out string assetPath)
        {
            string best = null;
            foreach (var guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (!pathFilter(p)) continue;
                if (p.IndexOf(subPathMustContain, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    assetPath = p;
                    return true;
                }

                best ??= p;
            }

            assetPath = best;
            return best != null;
        }
    }
}
