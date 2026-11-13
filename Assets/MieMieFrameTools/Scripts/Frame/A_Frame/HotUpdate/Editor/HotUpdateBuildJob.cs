using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.HotUpdate.Editor
{
    public enum HotUpdateBuildOperation
    {
        InstallRuntime,
        PrepareBaseline,
        ExportPatch,
        BuildResources,
        PublishUpdate,
        BuildDemoPlayer
    }

    [Serializable]
    public sealed class HotUpdateBuildJobState
    {
        public string id;
        public string state;
        public HotUpdateBuildOperation operation;
        public string profilePath;
        public string packageVersion;
        public bool copyBuiltin;
        public string resultPath;
        public string output;
        public string error;
    }

    [InitializeOnLoad]
    public static class HotUpdateBuildJob
    {
        /// <summary> 重载期间保留的单个构建任务 </summary>
        private const string StateKey = "MmFramework.HotUpdate.BuildJob";

        static HotUpdateBuildJob()
        {
            var Job = GetState();
            if (Job != null && Job.state == "WaitingForReload")
                EditorApplication.update += Run;
            else if (Job != null && Job.state == "Running")
            {
                Job.state = "Failed";
                Job.error = "构建任务在运行中被程序集重载中断 请检查原始构建日志";
                Save(Job);
                Debug.LogError(Job.error);
            }
        }

        /// <summary>
        /// 正常重载后执行构建 清除临时内存程序集对原生反射构建回调的影响
        /// </summary>
        public static HotUpdateBuildJobState Start(HotUpdateBuildOperation operation, HotUpdateBuildProfile profile,
            string packageVersion = null, bool copyBuiltin = false)
        {
            var Previous = GetState();
            if (Previous != null && (Previous.state == "WaitingForReload" || Previous.state == "Running"))
                throw new InvalidOperationException($"已有构建任务未完成 {Previous.id}");
            profile.Validate();
            if (operation == HotUpdateBuildOperation.BuildResources || operation == HotUpdateBuildOperation.PublishUpdate)
                HotUpdateBuildTools.ValidateResourceVersion(profile, packageVersion);
            var Job = new HotUpdateBuildJobState
            {
                id = Guid.NewGuid().ToString("N"),
                state = "WaitingForReload",
                operation = operation,
                profilePath = AssetDatabase.GetAssetPath(profile),
                packageVersion = packageVersion,
                copyBuiltin = copyBuiltin
            };
            if (string.IsNullOrEmpty(Job.profilePath))
                throw new InvalidOperationException("构建任务需要已保存的发布 Profile");
            Job.resultPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "FakeUnityCLI", "HotUpdateJobs", Job.id + ".json"));
            AssetDatabase.SaveAssetIfDirty(profile);
            Save(Job);
            EditorUtility.RequestScriptReload();
            return Job;
        }

        public static HotUpdateBuildJobState GetState()
        {
            string Json = SessionState.GetString(StateKey, string.Empty);
            return string.IsNullOrEmpty(Json) ? null : JsonUtility.FromJson<HotUpdateBuildJobState>(Json);
        }

        private static void Run()
        {
            var Job = GetState();
            if (Job == null || Job.state != "WaitingForReload")
            {
                EditorApplication.update -= Run;
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            EditorApplication.update -= Run;
            Job.state = "Running";
            Save(Job);
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("构建任务需要稳定 Edit 模式");
                var Profile = AssetDatabase.LoadAssetAtPath<HotUpdateBuildProfile>(Job.profilePath);
                switch (Job.operation)
                {
                    case HotUpdateBuildOperation.InstallRuntime:
                        HotUpdateBuildTools.InstallRuntime();
                        break;
                    case HotUpdateBuildOperation.PrepareBaseline:
                        HotUpdateBuildTools.PrepareBaseline(Profile);
                        Job.output = Profile.GetBaselinePath();
                        break;
                    case HotUpdateBuildOperation.ExportPatch:
                        HotUpdateBuildTools.ExportPatch(Profile);
                        Job.output = Profile.assetOutputDirectory;
                        break;
                    case HotUpdateBuildOperation.BuildResources:
                        Job.output = HotUpdateBuildTools.BuildResources(Profile, Job.packageVersion, Job.copyBuiltin);
                        break;
                    case HotUpdateBuildOperation.PublishUpdate:
                        HotUpdateBuildTools.ValidateResourceVersion(Profile, Job.packageVersion);
                        HotUpdateBuildTools.ExportPatch(Profile);
                        string PackageDirectory = HotUpdateBuildTools.BuildResources(Profile, Job.packageVersion, false);
                        Job.output = HotUpdateServerPublisher.Publish(Profile, PackageDirectory, Job.packageVersion);
                        break;
                    case HotUpdateBuildOperation.BuildDemoPlayer:
                        Job.output = HotUpdateDemoTools.BuildPlayer();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Job.operation));
                }
                Job.state = "Succeeded";
                Save(Job);
                Debug.Log($"热更构建任务完成 {Job.id} {Job.output}");
            }
            catch (Exception Error)
            {
                Job.state = "Failed";
                Job.error = Error.ToString();
                Save(Job);
                Debug.LogException(Error);
            }
        }

        private static void Save(HotUpdateBuildJobState job)
        {
            string Json = JsonUtility.ToJson(job, true);
            SessionState.SetString(StateKey, Json);
            Directory.CreateDirectory(Path.GetDirectoryName(job.resultPath));
            File.WriteAllText(job.resultPath, Json);
        }
    }
}
