using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace MieMieFrameWork.Editor.MmAssets
{
    public static class MmModuleCatalogStore
    {
        public static string CatalogFilePath =>
            Path.Combine(Application.dataPath, "MieMieFrameTools/Editor/MmAssetsTopWindow/MmModuleCatalog.json");

        public static string ManifestFilePath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "../Packages/manifest.json"));

        private static MmModuleCatalogData _catalog;

        public static MmModuleCatalogData Catalog
        {
            get
            {
                EnsureLoaded();
                return _catalog;
            }
        }

        public static void EnsureLoaded()
        {
            if (_catalog != null)
                return;

            if (!File.Exists(CatalogFilePath))
            {
                _catalog = new MmModuleCatalogData();
                return;
            }

            try
            {
                string json = File.ReadAllText(CatalogFilePath);
                _catalog = JsonConvert.DeserializeObject<MmModuleCatalogData>(json) ?? new MmModuleCatalogData();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MmModuleCatalog] 读取清单失败: {ex.Message}");
                _catalog = new MmModuleCatalogData();
            }
        }

        public static void Invalidate()
        {
            _catalog = null;
        }

        public static bool IsInstalled(MmModuleEntry entry)
        {
            if (entry == null)
                return false;

            if (!string.IsNullOrWhiteSpace(entry.packageName) && IsPackageInManifest(entry.packageName))
            {
                if (IsUpmPackageResolved(entry.packageName))
                    return true;
            }

            if (string.IsNullOrWhiteSpace(entry.installCheckPath))
                return false;

            string normalized = entry.installCheckPath.Replace('\\', '/');
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, normalized));

            if (File.Exists(fullPath) || Directory.Exists(fullPath))
                return true;

            if (normalized.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(normalized) != null;

            if (normalized.StartsWith("Packages/", System.StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(normalized) != null;

            return false;
        }

        public static bool IsPackageInManifest(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName) || !File.Exists(ManifestFilePath))
                return false;

            try
            {
                var manifest = JObject.Parse(File.ReadAllText(ManifestFilePath));
                return manifest["dependencies"]?[packageName] != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsUpmPackageResolved(string packageName)
        {
            string virtualPath = $"Packages/{packageName}/package.json";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(virtualPath) != null)
                return true;

            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            string localPath = Path.Combine(projectRoot, "Packages", packageName);
            if (Directory.Exists(localPath))
                return true;

            string cacheDir = Path.Combine(projectRoot, "Library", "PackageCache");
            if (!Directory.Exists(cacheDir))
                return false;

            foreach (string dir in Directory.GetDirectories(cacheDir, packageName + "@*"))
            {
                if (File.Exists(Path.Combine(dir, "package.json")))
                    return true;
            }

            return false;
        }

        public static bool TryInstallPackage(MmModuleEntry entry, out string message)
        {
            message = string.Empty;

            if (entry == null)
            {
                message = "模块数据为空。";
                return false;
            }

            if (IsInstalled(entry))
            {
                message = "模块已安装。";
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.packageName) || string.IsNullOrWhiteSpace(entry.gitUrl))
            {
                message = "该模块尚未配置 packageName / gitUrl，请先在 MmModuleCatalog.json 中填写仓库地址。";
                return false;
            }

            if (!File.Exists(ManifestFilePath))
            {
                message = "未找到 Packages/manifest.json。";
                return false;
            }

            try
            {
                string manifestJson = File.ReadAllText(ManifestFilePath);
                var manifest = JObject.Parse(manifestJson);
                var dependencies = manifest["dependencies"] as JObject ?? new JObject();

                if (dependencies[entry.packageName] != null)
                {
                    message = $"manifest 中已存在 {entry.packageName}。";
                    return false;
                }

                dependencies[entry.packageName] = entry.gitUrl;
                manifest["dependencies"] = dependencies;

                File.WriteAllText(ManifestFilePath, manifest.ToString(Formatting.Indented));
                AssetDatabase.Refresh();
                Client.Resolve();

                message = $"已将 {entry.packageName} 写入 manifest Unity 正在拉取包";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static bool TryRemovePackage(MmModuleEntry entry, out string message)
        {
            message = string.Empty;

            if (entry == null)
            {
                message = "模块数据为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(entry.packageName))
            {
                message = "该模块未配置 UPM 包名 无法通过移除按钮卸载";
                return false;
            }

            if (!IsPackageInManifest(entry.packageName))
            {
                message = $"manifest 中未找到 {entry.packageName}";
                return false;
            }

            if (!File.Exists(ManifestFilePath))
            {
                message = "未找到 Packages/manifest.json";
                return false;
            }

            try
            {
                string manifestJson = File.ReadAllText(ManifestFilePath);
                var manifest = JObject.Parse(manifestJson);
                var dependencies = manifest["dependencies"] as JObject;
                if (dependencies == null)
                {
                    message = "manifest 无 dependencies 节点";
                    return false;
                }

                dependencies.Remove(entry.packageName);
                manifest["dependencies"] = dependencies;

                File.WriteAllText(ManifestFilePath, manifest.ToString(Formatting.Indented));
                AssetDatabase.Refresh();
                Client.Resolve();

                message = $"已从 manifest 移除 {entry.packageName}";
                return true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return false;
            }
        }

        public static void PingInstallPath(MmModuleEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.installCheckPath))
                return;

            string assetPath = entry.installCheckPath.Replace('\\', '/');
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (obj != null)
            {
                EditorGUIUtility.PingObject(obj);
                return;
            }

            if (!string.IsNullOrWhiteSpace(entry.packageName))
            {
                string pkgJson = $"Packages/{entry.packageName}/package.json";
                obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pkgJson);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    return;
                }

                string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
                string cacheDir = Path.Combine(projectRoot, "Library", "PackageCache");
                if (Directory.Exists(cacheDir))
                {
                    foreach (string dir in Directory.GetDirectories(cacheDir, entry.packageName + "@*"))
                    {
                        string fullPkgJson = Path.Combine(dir, "package.json");
                        if (File.Exists(fullPkgJson))
                        {
                            EditorUtility.RevealInFinder(fullPkgJson);
                            return;
                        }
                    }
                }
            }

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (folder != null)
                {
                    EditorGUIUtility.PingObject(folder);
                    return;
                }
            }

            EditorUtility.DisplayDialog("提示", $"未在项目中找到：\n{assetPath}", "确定");
        }
    }
}
