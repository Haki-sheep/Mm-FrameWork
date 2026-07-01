using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

namespace MieMieFrameWork.Editor
{
    /// <summary>
    /// 文件夹工具 - 提供常用路径的查看和创建功能
    /// 整合于MieMieFrameWork框架
    /// </summary>
    public static class CheckFolder  
    {
        #region 查看路径菜单

        [MenuItem("Tools/MieMieFrameWork/Folder/Open/Assets")]
        public static void OpenAssetsFolder()
        {
            OpenFolder(Application.dataPath);
        }


        [MenuItem("Tools/MieMieFrameWork/Folder/Open/Archive Data")]
        public static void OpenSaveDataFolder()
        {
            // // 尝试获取SaveManager实例
            // var saveManager = UnityEngine.Object.FindFirstObjectByType<Mm_SaveManager>();
            // if (saveManager != null)
            // {
            //     try
            //     {
            //         string savePath = saveManager.GetSaveDirectoryPath();
            //         if (Directory.Exists(savePath))
            //         {
            //             OpenFolder(savePath);
            //         }
            //         else
            //         {
            //             EditorUtility.DisplayDialog("提示", $"存档目录不存在：{savePath}\n请先运行游戏并保存数据", "确定");
            //         }
            //     }
            //     catch (System.Exception ex)
            //     {
            //         EditorUtility.DisplayDialog("错误", $"获取存档路径失败：{ex.Message}", "确定");
            //     }
            // }
            // else
            // {
            //     // 如果没有SaveManager实例，使用默认的存档路径
            //     string defaultPath = Application.persistentDataPath;
            //     EditorUtility.DisplayDialog("提示",
            //         $"未找到SaveManager实例，打开默认存档目录：\n{defaultPath}", "确定");
            //     OpenFolder(defaultPath);
            // }
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Open/StreamingAssets")]
        public static void OpenStreamingAssetsFolder()
        {
            string path = Application.streamingAssetsPath;
            if (Directory.Exists(path))
            {
                OpenFolder(path);
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "StreamingAssets文件夹不存在，请先创建！", "确定");
            }
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Open/Persistent Data")]
        public static void OpenPersistentDataFolder()
        {
            OpenFolder(Application.persistentDataPath);
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Open/Temp")]
        public static void OpenTempFolder()
        {
            OpenFolder(Application.dataPath + "/../Temp");
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Open/Logs")]
        public static void OpenLogsFolder()
        {
            OpenFolder(Application.dataPath + "/../Logs");
        }


        #endregion

        #region 创建路径菜单

        [MenuItem("Tools/MieMieFrameWork/Folder/Create/StreamingAssets")]
        public static void CreateStreamingAssetsFolder()
        {
            CreateFolder(Application.streamingAssetsPath, "StreamingAssets");
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Create/Resources")]
        public static void CreateResourcesFolder()
        {
            CreateFolder(Application.dataPath + "/Resources", "Resources");
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Create/Editor")]
        public static void CreateEditorFolder()
        {
            CreateFolder(Application.dataPath + "/Editor", "Editor");
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Create/Plugins")]
        public static void CreatePluginsFolder()
        {
            CreateFolder(Application.dataPath + "/Plugins", "Plugins");
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Create/Scripts")]
        public static void CreateScriptsFolder()
        {
            CreateFolder(Application.dataPath + "/Scripts", "Scripts");
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Create/Prefabs")]
        public static void CreatePrefabsFolder()
        {
            CreateFolder(Application.dataPath + "/Prefabs", "Prefabs");
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Create/Materials")]
        public static void CreateMaterialsFolder()
        {
            CreateFolder(Application.dataPath + "/Materials", "Materials");
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Create/Textures")]
        public static void CreateTexturesFolder()
        {
            CreateFolder(Application.dataPath + "/Textures", "Textures");
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Create/Audio")]
        public static void CreateAudioFolder()
        {
            CreateFolder(Application.dataPath + "/Audio", "Audio");
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Create/Animations")]
        public static void CreateAnimationsFolder()
        {
            CreateFolder(Application.dataPath + "/Animations", "Animations");
        }

        [MenuItem("Tools/MieMieFrameWork/Folder/Create/Common Structure")]
        public static void CreateCommonFolderStructure()
        {
            string[] folders = {
                "Scripts",
                "Prefabs",
                "Materials",
                "Textures",
                "Audio",
                "Animations",
                "Scenes",
                "Resources",
                "StreamingAssets"
            };

            int createdCount = 0;
            foreach (string folder in folders)
            {
                string folderPath = Application.dataPath + "/" + folder;
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    createdCount++;
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("创建完成",
                $"常用文件夹结构创建完成！\n新创建了 {createdCount} 个文件夹", "确定");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 打开指定文件夹
        /// </summary>
        /// <param name="path">文件夹路径</param>
        private static void OpenFolder(string path)
        {
            if (Directory.Exists(path))
            {
                path = path.Replace("/", "\\");
                Process.Start("explorer.exe", path);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", $"路径不存在：{path}", "确定");
            }
        }

        /// <summary>
        /// 创建文件夹
        /// </summary>
        /// <param name="path">文件夹路径</param>
        /// <param name="folderName">文件夹名称（用于显示）</param>
        private static void CreateFolder(string path, string folderName)
        {
            if (Directory.Exists(path))
            {
                EditorUtility.DisplayDialog("提示", $"{folderName} 文件夹已存在！", "确定");
                return;
            }

            try
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("成功", $"{folderName} 文件夹创建成功！", "确定");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("错误", $"创建 {folderName} 文件夹失败：{e.Message}", "确定");
            }
        }

        #endregion
    }
}
