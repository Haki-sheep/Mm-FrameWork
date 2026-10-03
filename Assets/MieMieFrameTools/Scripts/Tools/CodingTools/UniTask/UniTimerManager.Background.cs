using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MieMieFrameWork
{
    public partial class UniTimerManager
    {
        /// <summary>
        /// 延迟后在线程池执行任务并返回结果 取消由调用者令牌控制
        /// </summary>
        public async UniTask<TResult> DelayRunBgComplete<TResult>(float delaySeconds, Func<TResult> work,
            CancellationToken cancellationToken = default)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: cancellationToken);
            return await UniTask.RunOnThreadPool(work, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 延迟后在线程池执行任务 并返回主线程完成另一个回调
        /// </summary>
        public async UniTaskVoid DelayRunBg(float delaySeconds, Action action, Action onComplete = null,
            CancellationToken cancellationToken = default)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken: cancellationToken);
            await UniTask.SwitchToThreadPool();
            cancellationToken.ThrowIfCancellationRequested();
            action?.Invoke();
            await UniTask.SwitchToMainThread(cancellationToken);
            onComplete?.Invoke();
        }
    }
}
