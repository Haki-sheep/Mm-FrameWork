using System;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.HotUpdate.Editor
{
    public sealed class HotUpdateWindow : EditorWindow
    {
        /// <summary> 当前选择的发布配置 </summary>
        [SerializeField]
        private HotUpdateBuildProfile profile;
        /// <summary> 本次资源包版本 </summary>
        [SerializeField]
        private string packageVersion = "1";
        /// <summary> 首包构建时显式复制 StreamingAssets </summary>
        [SerializeField]
        private bool copyBuiltin;
        /// <summary> 发布配置编辑视图 </summary>
        private UnityEditor.Editor profileEditor;
        /// <summary> 本次请求未创建构建任务时的失败原因 </summary>
        [SerializeField]
        private string requestError;

        [MenuItem("Tools/MmFramework/热更新")]
        public static void Open()
        {
            GetWindow<HotUpdateWindow>("Mm 热更新");
        }

        private void OnGUI()
        {
            DrawEmbeddedGUI();
        }

        public void DrawEmbeddedGUI()
        {
            profile = (HotUpdateBuildProfile)EditorGUILayout.ObjectField("发布配置", profile, typeof(HotUpdateBuildProfile), false);
            if (GUILayout.Button("创建发布配置 SO"))
                CreateProfile();
            if (profile == null)
            {
                EditorGUILayout.HelpBox("点击创建发布配置 SO 或选择已有配置", MessageType.Info);
                return;
            }
            UnityEditor.Editor.CreateCachedEditor(profile, null, ref profileEditor);
            profileEditor.OnInspectorGUI();
            EditorGUILayout.HelpBox("代码和资源可以同时修改 每次构建须填写未使用的资源版本 等待发布成功后重启 EXE", MessageType.Info);
            if (!string.IsNullOrEmpty(requestError))
                EditorGUILayout.HelpBox(requestError, MessageType.Error);
            if (GUILayout.Button("配置 HybridCLR 与 IL2CPP")) HotUpdateBuildTools.Configure(profile);
            if (GUILayout.Button("从 Gitee 安装工程本地运行库")) StartBuild(HotUpdateBuildOperation.InstallRuntime);
            if (GUILayout.Button("准备首包并冻结 AOT 基线")) StartBuild(HotUpdateBuildOperation.PrepareBaseline);
            if (GUILayout.Button("导出当前代码补丁")) StartBuild(HotUpdateBuildOperation.ExportPatch);
            if (GUILayout.Button("核对实际 Player 的 AOT 基线")) HotUpdatePublisher.VerifyPlayerBaseline(profile);
            packageVersion = EditorGUILayout.TextField(new GUIContent("资源发布版本", "标记包含代码 DLL 和普通资源的整包 每次构建使用未占用的新版本"), packageVersion);
            copyBuiltin = EditorGUILayout.Toggle("复制首包资源", copyBuiltin);
            if (GUILayout.Button("构建 YooAsset 资源"))
                StartBuild(HotUpdateBuildOperation.BuildResources);
            if (GUILayout.Button("导出代码并发布资源到本机服务器"))
                StartBuild(HotUpdateBuildOperation.PublishUpdate);
            if (GUILayout.Button("打开本机发布目录")) EditorUtility.RevealInFinder(profile.GetServerPath());
            var Job = HotUpdateBuildJob.GetState();
            if (Job != null)
                EditorGUILayout.HelpBox($"上次任务 {Job.operation} {Job.state} 资源版本 {Job.packageVersion}\n{Job.output}\n{Job.error}", Job.state == "Failed" ? MessageType.Error : MessageType.Info);
        }

        private void CreateProfile()
        {
            string Path = EditorUtility.SaveFilePanelInProject("创建热更新发布配置", "HotUpdateBuildProfile", "asset", "选择发布配置的保存位置");
            if (string.IsNullOrEmpty(Path))
                return;
            var Profile = CreateInstance<HotUpdateBuildProfile>();
            AssetDatabase.CreateAsset(Profile, AssetDatabase.GenerateUniqueAssetPath(Path));
            profile = Profile;
            requestError = string.Empty;
            Selection.activeObject = Profile;
            EditorGUIUtility.PingObject(Profile);
        }

        private void StartBuild(HotUpdateBuildOperation operation)
        {
            requestError = string.Empty;
            try
            {
                HotUpdateBuildJob.Start(operation, profile, packageVersion, copyBuiltin);
            }
            catch (ArgumentException Error)
            {
                requestError = Error.Message;
                Debug.LogException(Error);
            }
            catch (InvalidOperationException Error)
            {
                requestError = Error.Message;
                Debug.LogException(Error);
            }
        }

        private void OnDisable()
        {
            DestroyImmediate(profileEditor);
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}
