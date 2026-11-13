using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MieMieFrameWork.HotUpdate
{
    public interface IHotUpdateEntry : IDisposable
    {
        /// <summary>
        /// 框架管理器已初始化 此处完成业务启动 不等待 ModuleHub ReadyTask
        /// </summary>
        public UniTask StartAsync(CancellationToken cancellationToken);
    }
}
