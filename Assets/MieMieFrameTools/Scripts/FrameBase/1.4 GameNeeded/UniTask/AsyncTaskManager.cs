using Cysharp.Threading.Tasks;
using MieMieFrameWork;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// 多线程任务调度管理器
/// 基于UniTask实现线程切换和任务调度
/// </summary>
[ManagerAttribute(5)]
public class AsyncTaskManager : MonoBehaviour, IManagerBase
{
    //任务统计信息
    private TaskStatistics statistics = new();
    private Dictionary<string, CancellationTokenSource> taskCancellationTokens = new();

    public void Init()
    {
        
    }

    private void OnDestroy()
    {
        CancelAllTasks();
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
    public async UniTaskVoid ExecuteAsync<TResult>(
        Func<TResult> backgroundWork,
        Action<TResult> onComplete = null,
        Action<Exception> onError = null,
        string taskName = null)
    {
        CancellationTokenSource cts = CreateTaskToken(taskName);

        try
        {
            statistics.taskStarted++;

            // 切换到线程池执行CPU密集型任务
            await UniTask.SwitchToThreadPool();
         
            TResult result = backgroundWork.Invoke();

            // 切换回主线程处理结果
            await UniTask.SwitchToMainThread();

            onComplete?.Invoke(result);

            statistics.taskCompleted++;
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"任务被取消: {taskName ?? "未命名任务"}");
            statistics.taskCancelled++;
        }
        catch (Exception ex)
        {
            // 确保错误回调在主线程执行
            await UniTask.SwitchToMainThread();

            Debug.LogError($"任务执行失败: {ex.Message}");
            onError?.Invoke(ex);

            statistics.taskFailed++;
        }
        finally
        {
            RemoveTaskToken(taskName);
        }
    }

    /// <summary>
    /// 在线程池执行异步任务
    /// </summary>
    public async UniTaskVoid ExecuteAsync<TResult>(
        Func<UniTask<TResult>> asyncWork,
        Action<TResult> onComplete = null,
        Action<Exception> onError = null,
        string taskName = null)
    {
        CancellationTokenSource cts = CreateTaskToken(taskName);

        try
        {
            statistics.taskStarted++;

            // 切换到线程池
            await UniTask.SwitchToThreadPool();

            TResult result = await asyncWork.Invoke();

            // 切换回主线程处理结果
            await UniTask.SwitchToMainThread();

            onComplete?.Invoke(result);

            statistics.taskCompleted++;
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"任务被取消: {taskName ?? "未命名任务"}");
            statistics.taskCancelled++;
        }
        catch (Exception ex)
        {
            await UniTask.SwitchToMainThread();

            Debug.LogError($"任务执行失败: {ex.Message}");
            onError?.Invoke(ex);

            statistics.taskFailed++;
        }
        finally
        {
            RemoveTaskToken(taskName);
        }
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
    public async UniTaskVoid ExecuteBatch<T, TResult>(
        IEnumerable<T> items,
        Func<T, TResult> processor,
        Action<List<TResult>> onComplete = null,
        Action<Exception> onError = null,
        int maxConcurrency = 0)
    {
        try
        {
            statistics.taskStarted++;
            //创建结果列表
            List<TResult> results = new List<TResult>();

        if (maxConcurrency > 0)
        {
            // 创建信号灯类
            using (var semaphore = new SemaphoreSlim(maxConcurrency))
            {
                //创建任务列表
                var tasks = new List<UniTask<TResult>>();

                foreach (var item in items)
                {
                    //获取一个许可证 如果许可证不够用 则等待
                    await semaphore.WaitAsync();

                    var task = UniTask.RunOnThreadPool(async () =>
                    {
                        try
                        {
                            return processor(item);
                        }
                        finally
                        {
                            // 切换回主线程释放信号量
                            await UniTask.SwitchToMainThread();
                            semaphore.Release();
                        }
                    });

                    tasks.Add(task);
                }

                var taskResults = await UniTask.WhenAll(tasks);
                results.AddRange(taskResults);
            }
        }
            else   // 不限制并发数
            {
             
                var tasks = new List<UniTask<TResult>>();

                foreach (var item in items)
                {
                    var task = UniTask.RunOnThreadPool(() => processor(item));
                    tasks.Add(task);
                }

                var taskResults = await UniTask.WhenAll(tasks);
                results.AddRange(taskResults);
            }

            // 切换回主线程处理结果
            await UniTask.SwitchToMainThread();

            onComplete?.Invoke(results);

            statistics.taskCompleted++;
        }
        catch (Exception ex)
        {
            await UniTask.SwitchToMainThread();

            Debug.LogError($"批量任务执行失败: {ex.Message}");
            onError?.Invoke(ex);

            statistics.taskFailed++;
        }
    }

    /// <summary>
    /// 执行链式任务（前一个任务的结果作为下一个任务的输入）
    /// </summary>
    public async UniTaskVoid ExecuteChain<T1, T2, T3>(
        Func<T1> task1,
        Func<T1, T2> task2,
        Func<T2, T3> task3,
        Action<T3> onComplete = null,
        Action<Exception> onError = null)
    {
        try
        {
            statistics.taskStarted++;

            // 第一个任务在线程池执行
            await UniTask.SwitchToThreadPool();
            T1 result1 = task1();

            // 第二个任务继续在线程池执行
            T2 result2 = task2(result1);

            // 第三个任务继续在线程池执行
            T3 result3 = task3(result2);

            // 切换回主线程处理最终结果
            await UniTask.SwitchToMainThread();

            onComplete?.Invoke(result3);

            statistics.taskCompleted++;
        }
        catch (Exception ex)
        {
            await UniTask.SwitchToMainThread();

            Debug.LogError($"链式任务执行失败: {ex.Message}");
            onError?.Invoke(ex);

            statistics.taskFailed++;
        }
    }

    #endregion

    #region 任务管理

    /// <summary>
    /// 创建任务取消令牌
    /// </summary>
    private CancellationTokenSource CreateTaskToken(string taskName)
    {
        if (string.IsNullOrEmpty(taskName))
            return new CancellationTokenSource();

        var cts = new CancellationTokenSource();
        taskCancellationTokens[taskName] = cts;
        return cts;
    }

    /// <summary>
    /// 移除任务并取消令牌
    /// </summary>
    private void RemoveTaskToken(string taskName)
    {
        if (!string.IsNullOrEmpty(taskName) && taskCancellationTokens.ContainsKey(taskName))
        {
            taskCancellationTokens[taskName]?.Dispose();
            taskCancellationTokens.Remove(taskName);
        }
    }

    /// <summary>
    /// 取消指定任务
    /// </summary>
    public void CancelTask(string taskName)
    {
        if (taskCancellationTokens.TryGetValue(taskName, out var cts))
        {
            cts.Cancel();
            Debug.Log($"已取消任务: {taskName}");
        }
    }

    /// <summary>
    /// 取消所有任务
    /// </summary>
    public void CancelAllTasks()
    {
        //取消所有任务
        foreach (var cts in taskCancellationTokens.Values)
        {
            cts.Cancel();
        }

        taskCancellationTokens.Clear();
        Debug.Log("已取消所有任务");
    }

    #endregion


    #region Debug

    //获取任务统计信息
    public TaskStatistics GetStatistics()
    {
        return statistics;
    }

    // 重置统计信息
    public void ResetStatistics()
    {
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
