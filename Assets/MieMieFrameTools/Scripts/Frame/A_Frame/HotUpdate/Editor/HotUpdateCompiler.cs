using System;
using System.Collections.Generic;
using System.IO;
using HybridCLR;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace MieMieFrameWork.HotUpdate.Editor
{
    internal static class HotUpdateCompiler
    {
        internal static string Compile(HotUpdateBuildProfile profile)
        {
            string Root = Path.Combine(SettingsUtil.HybridCLRDataDir, "PatchCompilation", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
            var ErrorList = new List<string>();
            Application.LogCallback CaptureError = (Message, Stack, eType) =>
            {
                if (eType == LogType.Error || eType == LogType.Exception || eType == LogType.Assert)
                    ErrorList.Add(Message);
            };
            Application.logMessageReceived += CaptureError;
            try
            {
                var eTarget = EditorUserBuildSettings.activeBuildTarget;
                var Result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings
                {
                    target = eTarget,
                    group = BuildPipeline.GetBuildTargetGroup(eTarget),
                    options = EditorUserBuildSettings.development ? ScriptCompilationOptions.DevelopmentBuild : ScriptCompilationOptions.None
                }, Root);
                if (ErrorList.Count != 0 || Result.assemblies == null || Result.assemblies.Count == 0)
                    throw new InvalidOperationException("Player 程序集编译失败 " + string.Join("\n", ErrorList));
                foreach (string Name in profile.hotAssemblyList)
                    if (!File.Exists(Path.Combine(Root, Name + ".dll")))
                        throw new InvalidOperationException($"本次编译未生成热更程序集 {Name}");
                return Root;
            }
            finally
            {
                Application.logMessageReceived -= CaptureError;
            }
        }
    }
}
