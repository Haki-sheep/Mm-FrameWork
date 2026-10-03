using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace MieMieFrameWork
{
    internal sealed partial class TimerScheduler
    {
        /// <summary>
        /// 注入指定阶段的时间增量与渲染帧号 驱动该阶段已到期任务
        /// </summary>
        internal void Tick(PlayerLoopTiming eTiming, float deltaTime, float unscaledDeltaTime, int frame)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(TimerScheduler));
            if (isTicking)
                throw new InvalidOperationException("[UniTimerManager] 禁止重入 Tick");
            ValidateSeconds(deltaTime, nameof(deltaTime), true);
            ValidateSeconds(unscaledDeltaTime, nameof(unscaledDeltaTime), true);
            if (!phaseTimerDict.TryGetValue(eTiming, out var TimerList))
                return;

            isTicking = true;
            int Count = TimerList.Count;
            List<Exception> ExceptionList = null;
            try
            {
                // 固定本轮范围并检查创建帧 回调新增任务不会进入当前帧的其他阶段
                for (int Index = 0; Index < Count; Index++)
                {
                    var Entry = TimerList[Index];
                    if (!Entry.IsActive || Entry.IsPaused || Entry.CreatedFrame == frame)
                        continue;
                    float DeltaTime = Entry.IgnoreTimeScale ? unscaledDeltaTime : deltaTime;
                    if (DeltaTime == 0f)
                        continue;

                    Entry.RemainingTime -= DeltaTime;
                    ExecuteDueCallbacks(Entry, ref ExceptionList);
                }
            }
            finally
            {
                isTicking = false;
                foreach (var eDirtyTiming in dirtyPhaseHashList)
                    Compact(phaseTimerDict[eDirtyTiming]);
                dirtyPhaseHashList.Clear();
                if (isDisposed)
                    phaseTimerDict.Clear();
            }
            if (ExceptionList != null)
                throw new AggregateException($"[UniTimerManager] 阶段 {eTiming} 回调失败", ExceptionList);
        }

        /// <summary>
        /// 执行到期回调 跳过模式保留周期相位 补执行模式保留超限积压
        /// </summary>
        private void ExecuteDueCallbacks(TimerEntry entry, ref List<Exception> exceptionList)
        {
            int CallbackCount = 0;
            while (entry.IsActive && !entry.IsPaused && entry.RemainingTime <= 0d)
            {
                var Callback = entry.Callback;
                if (entry.RemainingCount > 0)
                    entry.RemainingCount--;
                entry.RemainingTime += entry.Interval;
                if (entry.RemainingCount == 0)
                    Stop(entry.Id);

                try
                {
                    Callback?.Invoke();
                }
                catch (Exception Exception)
                {
                    Stop(entry.Id);
                    if (exceptionList == null)
                        exceptionList = new List<Exception>();
                    exceptionList.Add(new InvalidOperationException(
                        $"[UniTimerManager] 回调失败 ID {entry.Id} 阶段 {entry.Timing}", Exception));
                    break;
                }

                CallbackCount++;
                if (!entry.IsActive || entry.IsPaused)
                    break;
                if (entry.CatchUpMode == TimerCatchUpMode.SkipMissed)
                {
                    if (entry.RemainingTime <= 0d)
                        entry.RemainingTime += (Math.Floor(-entry.RemainingTime / entry.Interval) + 1d) * entry.Interval;
                    break;
                }
                if (CallbackCount >= entry.MaxCallbacksPerTick)
                    break;
            }
        }

        /// <summary>
        /// 原地稳定压缩列表 删除失效任务且保留注册顺序
        /// </summary>
        private static void Compact(List<TimerEntry> timerList)
        {
            int WriteIndex = 0;
            for (int ReadIndex = 0; ReadIndex < timerList.Count; ReadIndex++)
            {
                var Entry = timerList[ReadIndex];
                if (Entry.IsActive)
                    timerList[WriteIndex++] = Entry;
            }
            if (WriteIndex < timerList.Count)
                timerList.RemoveRange(WriteIndex, timerList.Count - WriteIndex);
        }
    }
}
