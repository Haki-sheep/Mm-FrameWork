using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset.Editor;

namespace MieMieFrameWork.HotUpdate.Editor
{
    public static class HotUpdateDemoTools
    {
        /// <summary> 隔离测试的源码与场景目录 </summary>
        public const string DemoRoot = "Assets/MmHotUpdateValidation";
        /// <summary> 隔离测试的发布配置地址 </summary>
        public const string ProfilePath = DemoRoot + "/DemoProfile.asset";
        /// <summary> 测试首包固定身份 </summary>
        public const string PlayerBuildId = "mm-demo-visible-v1";

        public static void InstallSource()
        {
            var GuidList = AssetDatabase.FindAssets("HotUpdateDemoTools t:MonoScript");
            if (GuidList.Length != 1)
                throw new InvalidOperationException("无法唯一定位热更测试模块");
            string EditorRoot = Path.GetDirectoryName(AssetDatabase.GUIDToAssetPath(GuidList[0]));
            string SourceRoot = Path.Combine(Path.GetDirectoryName(EditorRoot), "Samples~");
            foreach (string Source in Directory.GetFiles(SourceRoot, "*", SearchOption.AllDirectories))
            {
                if (!Source.EndsWith(".cs", StringComparison.Ordinal) && !Source.EndsWith(".asmdef", StringComparison.Ordinal))
                    continue;
                string Destination = Path.Combine(DemoRoot, Source.Substring(SourceRoot.Length + 1));
                if (File.Exists(Destination))
                    throw new InvalidOperationException($"测试源码已存在 不覆盖 {Destination}");
                Directory.CreateDirectory(Path.GetDirectoryName(Destination));
                File.Copy(Source, Destination);
            }
            AssetDatabase.Refresh();
        }

        public static void CreateFixture()
        {
            string ScenePath = DemoRoot + "/Demo.unity";
            if (AssetDatabase.LoadMainAssetAtPath(ScenePath) != null || AssetDatabase.LoadMainAssetAtPath(ProfilePath) != null)
                throw new InvalidOperationException("测试场景或配置已存在 不覆盖");
            var BootstrapType = Type.GetType("MmHotUpdate.Validation.DemoBootstrap, MmHotUpdate.DemoBootstrap", true);
            var PreviousScene = SceneManager.GetActiveScene();
            if (PreviousScene.isDirty || PrefabStageUtility.GetCurrentPrefabStage() != null)
                throw new InvalidOperationException("创建测试场景前须保存当前场景并退出 Prefab 编辑");
            bool IsEmptyBatchScene = Application.isBatchMode && string.IsNullOrEmpty(PreviousScene.path) && PreviousScene.rootCount == 0;
            if (string.IsNullOrEmpty(PreviousScene.path) && !IsEmptyBatchScene)
                throw new InvalidOperationException("创建测试场景前须保存当前未命名场景");
            var Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, IsEmptyBatchScene ? NewSceneMode.Single : NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(Scene);
                var Root = new GameObject("HotUpdateBootstrap");
                Root.AddComponent(BootstrapType);
                if (!EditorSceneManager.SaveScene(Scene, ScenePath))
                    throw new InvalidOperationException("测试场景保存失败");
            }
            finally
            {
                if (!IsEmptyBatchScene)
                {
                    SceneManager.SetActiveScene(PreviousScene);
                    EditorSceneManager.CloseScene(Scene, true);
                }
            }
            var Profile = ScriptableObject.CreateInstance<HotUpdateBuildProfile>();
            Profile.playerBuildId = PlayerBuildId;
            Profile.entryAssembly = "MmHotUpdate.Demo";
            Profile.entryType = "MmHotUpdate.Demo.DemoEntry";
            Profile.hotAssemblyList = new[] { Profile.entryAssembly };
            Profile.assetOutputDirectory = DemoRoot + "/Data";
            AssetDatabase.CreateAsset(Profile, ProfilePath);
            Directory.CreateDirectory(Profile.assetOutputDirectory);
            File.WriteAllText(Profile.assetOutputDirectory + "/ResourceMarker.txt", "RESOURCE_V1");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            AssetDatabase.Refresh();
            HotUpdateBuildTools.RegisterCollector(Profile);
        }

        public static string BuildPlayer()
        {
            var Profile = AssetDatabase.LoadAssetAtPath<HotUpdateBuildProfile>(ProfilePath);
            var Report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { DemoRoot + "/Demo.unity" },
                target = BuildTarget.StandaloneWindows64,
                locationPathName = "Builds/MmHotUpdateDemo/MmHotUpdateDemo.exe",
                options = EditorUserBuildSettings.development ? BuildOptions.Development : BuildOptions.None
            });
            if (Report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                string Errors = string.Join("\n", Report.steps.SelectMany(Step => Step.messages)
                    .Where(Message => Message.type == LogType.Error || Message.type == LogType.Exception)
                    .Select(Message => Message.content));
                throw new InvalidOperationException($"测试 Player 构建失败 {Report.summary.result}\n{Errors}");
            }
            HotUpdatePublisher.VerifyPlayerBaseline(Profile);
            string OutputRoot = Path.GetDirectoryName(Report.summary.outputPath);
            foreach (string Name in new[] { "Start-LocalDemo.ps1", "Start-LocalDemo.cmd", "Start-Server.cmd" })
                File.Copy(Path.Combine("Tools", "HotUpdate", Name), Path.Combine(OutputRoot, Name), true);
            return Path.GetFullPath(Report.summary.outputPath);
        }
    }
}
