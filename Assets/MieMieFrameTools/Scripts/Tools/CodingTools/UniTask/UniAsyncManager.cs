using Cysharp.Threading.Tasks;
using MieMieFrameWork;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static MieMieFrameWork.ModuleHub;

/// <summary>
/// 多线程任务调度管理器
/// 基于UniTask实现线程切换和任务调度
/// </summary>
[ManagerAttribute(5)]
public class AsyncTaskManager : IManagerBase, IDisposable
{
    private readonly object taskLock = new();
    private readonly CancellationToken ownerCancellationToken;

    //任务统计信息
    private TaskStatistics statistics = new();
    private readonly Dictionary<string, CancellationTokenSource> taskCancellationTokens = new();

    /// <summary>
    /// 全部运行中任务的令牌 含未命名任务 供整体取消
    /// </summary>
    private readonly HashSet<CancellationTokenSource> activeTokenHashList = new();

    private bool isDisposed;

    public AsyncTaskManager()
    {
    }

    internal AsyncTaskManager(CancellationToken ownerCancellationToken)
    {
        this.ownerCancellationToken = ownerCancellationToken;
    }

    public void Init()
    {
    }

    public void Dispose()
    {
        lock (taskLock)
        {
            if (isDisposed)
                return;

            isDisposed = true;
            CancelActiveTasks();
        }
    }

    #region 基础任务执行

    /// <summary>
    /// 在线程池执行CPU密集型任务，然后在主线程处理结果
    /// </summary>
    /// <typeparam name="TResult">返回结果类型</typeparam>
    /// <param name="backgroundWork">后台工作（在子线程执行）</param>
    /// <param name="onComplete">完成回调（在主线程执行）</param>
    /// <param name="onError">错误回调（在主线程执行）</param>
    /// <param name="taskName">任务名称，用于取消和统计</param>
    public UniTaskVoid ExecuteAsync<TResult>(
        Func<TResult> backgroundWork,
        Action<TResult> onComplete = null,
        Action<Exception> onError = null,
        string taskName = null)
    {
        return RunTaskAsync(async Token =>
        {
            // 切换到线程池执行CPU密集型任务
            await UniTask.SwitchToThreadPool();
            Token.ThrowIfCancellationRequested();
            return backgroundWork.Invoke();
        }, onComplete, onError, taskName);
    }

    /// <summary>
    /// 在线程池执行异步任务
    /// </summary>
    public UniTaskVoid ExecuteAsync<TResult>(
        Func<UniTask<TResult>> asyncWork,
        Action<TResult> onComplete = null,
        Action<Exception> onError = null,
        string taskName = null)
    {
        return RunTaskAsync(async Token =>
        {
            // 切换到线程池
            await UniTask.SwitchToThreadPool();
            Token.ThrowIfCancellationRequested();
            return await asyncWork.Invoke();
        }, onComplete, onError, taskName);
    }

    #endregion

    #region 高级任务执行

    /// <summary>
    /// 批量执行任务（并行）
    /// </summary>
    /// <typeparam name="T">输入数据类型</typeparam>
    /// <typeparam name="TResult">返回结果类型</typeparam>
    /// <param name="items">要处理的数据集合</param>
    /// <param name="processor">处理器函数</param>
    /// <param name="onComplete">所有任务完成回调</param>
    /// <param name="maxConcurrency">最大并发数，0表示不限制</param>
    public UniTaskVoid ExecuteBatch<T, TResult>(
        IEnumerable<T> items,
        Func<T, TResult> processor,
        Action<List<TResult>> onComplete = null,
        Action<Exception> onError = null,
        int maxConcurrency = 0)
    {
        return RunTaskAsync(Token => RunBatchAsync(items, processor, maxConcurrency, Token), onComplete, onError);
    }

    private static async UniTask<List<TResult>> RunBatchAsync<T, TResult>(
        IEnumerable<T> items, Func<T, TResult> processor, int maxConcurrency, CancellationToken cancellationToken)
    {
        var ItemList = new List<T>();
        foreach (var Item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ItemList.Add(Item);
        }

        // 创建信号灯类
        // 等所有子任务退出再释放信号量 其中一个失败仍须等待其他处理器退出
        using var Semaphore = maxConcurrency > 0 ? new SemaphoreSlim(maxConcurrency) : null;
        var TaskList = new List<Task<TResult>>(ItemList.Count);
        foreach (var Item in ItemList)
            TaskList.Add(RunBatchItemAsync(Item, processor, Semaphore, cancellationToken));

        // 创建结果列表
        return new List<TResult>(await Task.WhenAll(TaskList).ConfigureAwait(false));
    }

    private static async Task<TResult> RunBatchItemAsync<T, TResult>(
        T item, Func<T, TResult> processor, SemaphoreSlim semaphore, CancellationToken cancellationToken)
    {
        if (semaphore != null)
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return processor(item);
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            semaphore?.Release();
        }
    }

    /// <summary>
    /// 执行链式任务（前一个任务的结果作为下一个任务的输入）
    /// </summary>
    public UniTaskVoid ExecuteChain<T1, T2, T3>(
        Func<T1> task1,
        Func<T1, T2> task2,
        Func<T2, T3> task3,
        Action<T3> onComplete = null,
        Action<Exception> onError = null)
    {
        return RunTaskAsync(async Token =>
        {
            // 第一个任务在线程池执行
            await UniTask.SwitchToThreadPool();
            Token.ThrowIfCancellationRequested();
            T1 Result1 = task1();

            // 第二个任务继续在线程池执行
            Token.ThrowIfCancellationRequested();
            T2 Result2 = task2(Result1);

            // 第三个任务继续在线程池执行
            Token.ThrowIfCancellationRequested();
            return task3(Result2);
        }, onComplete, onError);
    }

    #endregion

    #region 任务管理

    private async UniTaskVoid RunTaskAsync<TResult>(Func<CancellationToken, UniTask<TResult>> work,
        Action<TResult> onComplete, Action<Exception> onError, string taskName = null)
    {
        var Source = CreateTaskToken(taskName);
        if (Source == null)
            return;

        var Token = Source.Token;
        try
        {
            TResult Result = await work(Token);

            // 切换回主线程处理结果
            await UniTask.SwitchToMainThread();
            lock (taskLock)
            {
                Token.ThrowIfCancellationRequested();
                onComplete?.Invoke(Result);
                statistics.taskCompleted++;
            }
        }
        catch (OperationCanceledException)
        {
            lock (taskLock)
                RecordCancellation(taskName);
        }
        catch (Exception Error)
        {
            // 确保错误回调在主线程执行
            await UniTask.SwitchToMainThread();
            lock (taskLock)
            {
                if (Token.IsCancellationRequested)
                {
                    RecordCancellation(taskName);
                    return;
                }

                Debug.LogError($"任务执行失败 [{taskName ?? "未命名任务"}]: {Error}");
                if (Token.IsCancellationRequested)
                {
                    RecordCancellation(taskName);
                    return;
                }

                statistics.taskFailed++;
                onError?.Invoke(Error);
            }
        }
        finally
        {
            ReleaseTaskToken(taskName, Source);
        }
    }

    private void RecordCancellation(string taskName)
    {
        Debug.Log($"任务被取消: {taskName ?? "未命名任务"}");
        statistics.taskCancelled++;
    }

    /// <summary>
    /// 创建任务取消令牌
    /// </summary>
    private CancellationTokenSource CreateTaskToken(string taskName)
    {
        lock (taskLock)
        {
            if (isDisposed || ownerCancellationToken.IsCancellationRequested)
                return null;

            var Source = ownerCancellationToken.CanBeCanceled
                ? CancellationTokenSource.CreateLinkedTokenSource(ownerCancellationToken)
                : new CancellationTokenSource();
            activeTokenHashList.Add(Source);
            if (!string.IsNullOrEmpty(taskName))
                taskCancellationTokens[taskName] = Source;
            statistics.taskStarted++;
            return Source;
        }
    }

    /// <summary>
    /// 任务结束时释放本任务令牌 同名后续任务的令牌不受影响
    /// </summary>
    private void ReleaseTaskToken(string taskName, CancellationTokenSource source)
    {
        lock (taskLock)
        {
            activeTokenHashList.Remove(source);
            if (!string.IsNullOrEmpty(taskName)
                && taskCancellationTokens.TryGetValue(taskName, out var CurrentSource)
                && CurrentSource == source)
                taskCancellationTokens.Remove(taskName);
            source.Dispose();
        }
    }

    /// <summary>
    /// 取消指定任务
    /// </summary>
    public void CancelTask(string taskName)
    {
        lock (taskLock)
        {
            if (taskCancellationTokens.TryGetValue(taskName, out var Source))
            {
                Source.Cancel();
                Debug.Log($"已取消任务: {taskName}");
            }
        }
    }

    /// <summary>
    /// 取消所有任务
    /// </summary>
    public void CancelAllTasks()
    {
        lock (taskLock)
        {
            CancelActiveTasks();
            Debug.Log("已取消所有任务");
        }
    }

    private void CancelActiveTasks()
    {
        // 只取消不释放 令牌由各任务结束时释放 取消回调可能同步结束任务故先复制
        var ActiveTokenList = new List<CancellationTokenSource>(activeTokenHashList);
        foreach (var Source in ActiveTokenList)
        {
            if (activeTokenHashList.Contains(Source))
                Source.Cancel();
        }
    }

    #endregion

    #region Debug

    //获取任务统计信息
    public TaskStatistics GetStatistics()
    {
        lock (taskLock)
        {
            return new TaskStatistics
            {
                taskStarted = statistics.taskStarted,
                taskCompleted = statistics.taskCompleted,
                taskFailed = statistics.taskFailed,
                taskCancelled = statistics.taskCancelled
            };
        }
    }

    // 重置统计信息
    public void ResetStatistics()
    {
        lock (taskLock)
            statistics = new TaskStatistics();
    }
    /// <summary>
    /// 任务信息
    /// </summary>
    [System.Serializable]
    public class TaskStatistics
    {
        public int taskStarted;      // 已启动的任务数
        public int taskCompleted;    // 已完成的任务数
        public int taskFailed;       // 失败的任务数
        public int taskCancelled;    // 已取消的任务数

        public override string ToString()
        {
            return $"任务统计 - 启动: {taskStarted}, 完成: {taskCompleted}, 失败: {taskFailed}, 取消: {taskCancelled}";
        }
    }

    #endregion
}
