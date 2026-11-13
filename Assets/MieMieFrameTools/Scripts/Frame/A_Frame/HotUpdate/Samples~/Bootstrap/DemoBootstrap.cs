using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MieMieFrameWork.Asset;
using MieMieFrameWork.HotUpdate;
using UnityEngine;

namespace MmHotUpdate.Validation
{
    public sealed class DemoBootstrap : MonoBehaviour
    {
        /// <summary> 本地测试服务器 指向当前资源发布目录 </summary>
        public string remoteUrl = "http://127.0.0.1:18780";
        /// <summary> 固定测试首包基线 </summary>
        public string playerBuildId = "mm-demo-visible-v1";
        /// <summary> 示例入口的生命周期所有者 </summary>
        private HotUpdateRuntime runtime;
        /// <summary> 当前启动阶段或首个失败原因 </summary>
        private string status = "Connecting to local update server...";
        /// <summary> 热更入口接管画面后停止启动界面 </summary>
        private bool started;

        private void Start()
        {
            RunAsync(this.GetCancellationTokenOnDestroy()).Forget(Error =>
            {
                Debug.LogException(Error);
                status = Error.ToString();
                if (Array.Exists(Environment.GetCommandLineArgs(), Argument => Argument == "--hotupdate-quit"))
                    Application.Quit(1);
            });
        }

        private async UniTask RunAsync(CancellationToken cancellationToken)
        {
            foreach (string Argument in Environment.GetCommandLineArgs())
                if (Argument.StartsWith("--hotupdate-url=", StringComparison.Ordinal))
                    remoteUrl = Argument.Substring("--hotupdate-url=".Length);
            await YooAssetMgr.InitializeAsync("DefaultPackage", 30, new ResourceUpdateConfig
            {
                playMode = ResourcePlayMode.Host, simulateInEditor = false, remoteUrl = remoteUrl
            }, cancellationToken);
            runtime = new HotUpdateRuntime();
            status = "Loading updated code...";
            await runtime.LoadAsync(YooAssetMgr.DefaultPackage, new HotUpdateConfig
            {
                enabled = true, playerBuildId = playerBuildId
            }, cancellationToken);
            await runtime.StartAsync(cancellationToken);
            started = true;
            if (Array.Exists(Environment.GetCommandLineArgs(), Argument => Argument == "--hotupdate-quit"))
                Application.Quit(0);
        }

        private void OnDestroy()
        {
            runtime?.Dispose();
        }

        private void OnGUI()
        {
            if (started) return;
            var Style = new GUIStyle(GUI.skin.label) { fontSize = 24, wordWrap = true };
            GUI.Label(new Rect(40, 40, Screen.width - 80, Screen.height - 80), "MmFramework Hot Update\n" + remoteUrl + "\n\n" + status, Style);
        }
    }
}
