using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Diagnostics;
using System.Threading.Tasks;
using static MieMieFrameWork.ModuleHub;
namespace MieMieFrameWork
{
    /// <summary>
    /// 计时器信息类
    /// </summary>
    public class TimerInfo
    {
        //令牌
        public CancellationTokenSource Cts { get; set; }
        //时间
        public float TotalTime { get; set; }
        public float RemainingTime { get; set; }
        //参数
        public PlayerLoopTiming PlayerLoopTiming { get; set; }
        public bool IgnoreTimeScale { get; set; }
        public Action CallBack { get; set; }

        //状态
        public bool IsPaused { get; set; }

    }

    [ManagerAttribute(6)]    
    public class UniTimerManager : MonoBehaviour, IManagerBase
    {
        public void Init()
        {
        }

        private Dictionary<int, TimerInfo> activeTimerDict = new();

        /// <summary>
        /// 完全在子线程内延迟执行任务
        /// </summary>
        public async UniTask<TResult> DelayRunBgComplete<TResult>(float delaySeconds, Func<TResult> work)
        {   
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds));
            return await UniTask.RunOnThreadPool(work);
        }

        /// <summary>
        ///在子线程延迟执行任务 并返回主线程完成另一个回调
        /// </summary>
        public async UniTaskVoid DelayRunBg(float delaySeconds, Action action, Action onComplete = null)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds));
            await UniTask.SwitchToThreadPool();

            action?.Invoke();

            await UniTask.SwitchToMainThread();
            onComplete?.Invoke();
        }

        /// <summary>
        /// 启动定时器
        /// </summary>
        /// <param name="time">时间</param>
        /// <param name="playerLoopTiming">定时器执行时机</param>
        /// <param name="ignoreTimeScale">是否忽略时间缩放</param>
        /// <param name="action">定时器执行回调</param>
        /// <returns>计时器ID，用于停止指定计时器</returns>
        public int StartTimer(
            float time,
            Action action = null,
            PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update,
            bool ignoreTimeScale = false
           )
        {
            int timerId = Guid.NewGuid().GetHashCode();

            var timerInfo = new TimerInfo
            {
                Cts = new CancellationTokenSource(),
                TotalTime = time,
                RemainingTime = time,
                PlayerLoopTiming = playerLoopTiming,
                IgnoreTimeScale = ignoreTimeScale,
                CallBack = action,
                IsPaused = false,
            };

            activeTimerDict[timerId] = timerInfo;
            ExecuteTimerAsync(timerId, timerInfo).Forget();

            return timerId;
        }

        /// <summary>
        /// 执行计时器的异步方法
        /// </summary>
        private async UniTaskVoid ExecuteTimerAsync(int timerId, TimerInfo timerInfo)
        {
            Stopwatch runningTimeStopwatch = new Stopwatch();
            runningTimeStopwatch.Start();

            try
            {
                while (true)
                {
                    if (timerInfo.IsPaused)
                    {
                        // 暂停时，停止计时
                        runningTimeStopwatch.Stop();

                        while (timerInfo.IsPaused)
                        {
                            await UniTask.Yield(timerInfo.PlayerLoopTiming, timerInfo.Cts.Token);
                        }

                        // 恢复时，继续计时
                        runningTimeStopwatch.Start();
                    }

                    // 直接获取已运行的秒数
                    double runningSeconds = runningTimeStopwatch.Elapsed.TotalSeconds;
                    timerInfo.RemainingTime = timerInfo.TotalTime - (float)runningSeconds;

                    if (timerInfo.RemainingTime <= 0) break;

                    await UniTask.Yield(timerInfo.PlayerLoopTiming, timerInfo.Cts.Token);
                }

                timerInfo.CallBack?.Invoke();
            }
            finally
            {
                // 清理计时器资源
                CleanupTimer(timerId);
            }
        }
        /// <summary>
        /// 停止指定的计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <returns>是否成功停止</returns>
        public bool StopTimer(int timerId)
        {
            if (activeTimerDict.TryGetValue(timerId, out var timerInfo))
            {
                timerInfo.Cts.Cancel();
                return true;
            }

            print($"计时器 {timerId} 不存在或已停止");
            return false;
        }

        /// <summary>
        /// 停止所有计时器
        /// </summary>
        public void StopAllTimers()
        {
            foreach (var timerInfo in activeTimerDict.Values)
            {
                timerInfo.Cts.Cancel();
            }

            print($"已停止 {activeTimerDict.Count} 个计时器");
        }

        /// <summary>
        /// 获取活跃计时器数量
        /// </summary>
        public int GetActiveTimerCount()
        {
            return activeTimerDict.Count;
        }

        /// <summary>
        /// 检查指定计时器是否还在运行
        /// </summary>
        public bool IsTimerActive(int timerId)
        {
            return activeTimerDict.ContainsKey(timerId);
        }

        /// <summary>
        /// 暂停指定的计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <returns>是否成功暂停</returns>
        public bool PauseTimer(int timerId)
        {
            if (activeTimerDict.TryGetValue(timerId, out var timerInfo))
            {
                if (!timerInfo.IsPaused)
                {
                    timerInfo.IsPaused = true;
                    print($"计时器 {timerId} 已暂停，剩余时间: {timerInfo.RemainingTime:F2}秒");
                    return true;
                }
                else
                {
                    print($"计时器 {timerId} 已经是暂停状态");
                    return false;
                }
            }

            print($"计时器 {timerId} 不存在");
            return false;
        }

        /// <summary>
        /// 恢复指定的计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <returns>是否成功恢复</returns>
        public bool ResumeTimer(int timerId)
        {
            if (activeTimerDict.TryGetValue(timerId, out var timerInfo))
            {
                if (timerInfo.IsPaused)
                {
                    timerInfo.IsPaused = false;
                    print($"计时器 {timerId} 已恢复，剩余时间: {timerInfo.RemainingTime:F2}秒");
                    return true;
                }
                else
                {
                    print($"计时器 {timerId} 不是暂停状态");
                    return false;
                }
            }

            print($"计时器 {timerId} 不存在");
            return false;
        }

        /// <summary>
        /// 检查指定计时器是否暂停
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <returns>是否暂停</returns>
        public bool IsTimerPaused(int timerId)
        {
            if (activeTimerDict.TryGetValue(timerId, out var timerInfo))
            {
                return timerInfo.IsPaused;
            }
            return false;
        }

        /// <summary>
        /// 获取指定计时器的剩余时间
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <returns>剩余时间（秒）</returns>
        public float GetRemainingTime(int timerId)
        {
            if (activeTimerDict.TryGetValue(timerId, out var timerInfo))
            {
                return Mathf.Max(0f, timerInfo.RemainingTime);
            }
            return 0f;
        }

        /// <summary>
        /// 暂停所有计时器
        /// </summary>
        public void PauseAllTimers()
        {
            int pausedCount = 0;
            foreach (var timerInfo in activeTimerDict.Values)
            {
                if (!timerInfo.IsPaused)
                {
                    timerInfo.IsPaused = true;
                    pausedCount++;
                }
            }
            print($"已暂停 {pausedCount} 个计时器");
        }

        /// <summary>
        /// 恢复所有计时器
        /// </summary>
        public void ResumeAllTimers()
        {
            int resumedCount = 0;
            foreach (var timerInfo in activeTimerDict.Values)
            {
                if (timerInfo.IsPaused)
                {
                    timerInfo.IsPaused = false;
                    resumedCount++;
                }
            }
            print($"已恢复 {resumedCount} 个计时器");
        }

        /// <summary>
        /// 清理计时器资源
        /// </summary>
        private void CleanupTimer(int timerId)
        {
            if (activeTimerDict.TryGetValue(timerId, out var timerInfo))
            {
                timerInfo.Cts.Dispose();
                activeTimerDict.Remove(timerId);
            }
        }



        /// <summary>
        /// 组件销毁时清理所有资源
        /// </summary>
        void OnDestroy()
        {
            StopAllTimers();

            // 清理所有资源
            foreach (var timerInfo in activeTimerDict.Values)
            {
                timerInfo.Cts.Dispose();
            }
            activeTimerDict.Clear();
        }
    }
}