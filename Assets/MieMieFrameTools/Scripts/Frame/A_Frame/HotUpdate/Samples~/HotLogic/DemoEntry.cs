using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using MieMieFrameWork.Asset;
using MieMieFrameWork.HotUpdate;
using UnityEngine;
using YooAsset;

namespace MmHotUpdate.Demo
{
    public sealed class DemoEntry : IHotUpdateEntry
    {
        /// <summary> V2 只改该业务返回值 不重建 Player </summary>
        private const string CodeMarker = "CODE_V1";
        /// <summary> 热更入口创建并持有的演示界面 </summary>
        private GameObject view;

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            var Handle = YooAssetMgr.LoadAssetAsync<TextAsset>("ResourceMarker");
            try
            {
                await UniTask.WaitUntil(() => Handle.IsDone);
                cancellationToken.ThrowIfCancellationRequested();
                if (Handle.Status != EOperationStatus.Succeeded)
                    throw new InvalidOperationException(Handle.Error);
                var ResultList = new List<string> { CodeMarker, ((TextAsset)Handle.AssetObject).text.Trim() };
                string Result = string.Join("|", ResultList);
                Debug.Log("MM_HOTUPDATE_RESULT=" + Result);
                view = new GameObject("HotUpdateResult");
                view.AddComponent<DemoView>().result = Result;
                foreach (string Argument in Environment.GetCommandLineArgs())
                    if (Argument.StartsWith("--hotupdate-result=", StringComparison.Ordinal))
                        File.WriteAllText(Argument.Substring("--hotupdate-result=".Length), Result);
            }
            finally
            {
                Handle.Release();
            }
        }

        public void Dispose()
        {
            if (view != null) UnityEngine.Object.Destroy(view);
        }
    }
}
