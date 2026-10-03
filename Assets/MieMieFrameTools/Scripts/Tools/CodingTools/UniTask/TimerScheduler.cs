using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace MieMieFrameWork
{
    /// <summary>
    /// 定时器内核 接收外部时间与帧号 不读取 Unity 全局状态
    /// </summary>
    internal sealed partial class TimerScheduler : IDisposable
    {
        private sealed class TimerEntry
        {
            public long Id { get; set; }
            public double RemainingTime { get; set; }
            public double Interval { get; set; }
            public int RemainingCount { get; set; }
            public Action Callback { get; set; }
            public PlayerLoopTiming Timing { get; set; }
            public bool IgnoreTimeScale { get; set; }
            public bool IsPaused { get; set; }
            public bool IsActive { get; set; }
            public TimerCatchUpMode CatchUpMode { get; set; }
            public int MaxCallbacksPerTick { get; set; }
            public int CreatedFrame { get; set; }
        }

        /// <summary> 活跃 ID 到任务的映射 </summary>
        private readonly Dictionary<long, TimerEntry> timerDict = new Dictionary<long, TimerEntry>();

        /// <summary> 各阶段按注册顺序执行的任务列表 </summary>
        private readonly Dictionary<PlayerLoopTiming, List<TimerEntry>> phaseTimerDict =
            new Dictionary<PlayerLoopTiming, List<TimerEntry>>();

        /// <summary> 本轮取消影响的阶段 在安全阶段统一清理 </summary>
        private readonly HashSet<PlayerLoopTiming> dirtyPhaseHashList = new HashSet<PlayerLoopTiming>();

        /// <summary> 实例内递增且不复用的任务 ID </summary>
        private long nextId;

        /// <summary> 驱动期间禁止改变已有列表位置 </summary>
        private bool isTicking;

        /// <summary> 是否已释放 </summary>
        private bool isDisposed;

        public int Count => timerDict.Count;

        #region 创建任务

        /// <summary>
        /// 添加任务并记录创建帧 返回递增 ID 参数错误立即抛出
        /// </summary>
        internal long Add(float startDelay, float interval, int loopCount, Action action,
            PlayerLoopTiming eTiming, bool ignoreTimeScale, TimerCatchUpMode eCatchUpMode,
            int maxCallbacksPerTick, int createdFrame)
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(TimerScheduler));
            ValidateSeconds(startDelay, nameof(startDelay), true);
            ValidateSeconds(interval, nameof(interval), loopCount == 1);
            if (loopCount == 0)
                throw new ArgumentOutOfRangeException(nameof(loopCount), "执行次数不能为零");
            if (loopCount != 1 && action == null)
                throw new ArgumentNullException(nameof(action));
            if (!Enum.IsDefined(typeof(PlayerLoopTiming), eTiming))
                throw new ArgumentOutOfRangeException(nameof(eTiming));
            if (!Enum.IsDefined(typeof(TimerCatchUpMode), eCatchUpMode))
                throw new ArgumentOutOfRangeException(nameof(eCatchUpMode));
            if (maxCallbacksPerTick <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCallbacksPerTick));

            long Id = checked(++nextId);
            var Entry = new TimerEntry
            {
                Id = Id,
                RemainingTime = startDelay,
                Interval = interval,
                RemainingCount = loopCount,
                Callback = action,
                Timing = eTiming,
                IgnoreTimeScale = ignoreTimeScale,
                IsActive = true,
                CatchUpMode = eCatchUpMode,
                MaxCallbacksPerTick = maxCallbacksPerTick,
                CreatedFrame = createdFrame
            };
            if (!phaseTimerDict.TryGetValue(eTiming, out var TimerList))
            {
                TimerList = new List<TimerEntry>();
                phaseTimerDict.Add(eTiming, TimerList);
            }
            timerDict.Add(Id, Entry);
            TimerList.Add(Entry);
            return Id;
        }

        /// <summary>
        /// 检查秒数有限且符合零值约束 不自动修正非法输入
        /// </summary>
        private static void ValidateSeconds(float seconds, string parameterName, bool allowZero)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f || (!allowZero && seconds == 0f))
                throw new ArgumentOutOfRangeException(parameterName, "时间必须为有限数并满足零值约束");
        }

        #endregion

        #region 控制与查询

        /// <summary>
        /// 立即移除活跃 ID 驱动中的列表删除推迟到安全阶段
        /// </summary>
        internal bool Stop(long timerId)
        {
            if (!timerDict.TryGetValue(timerId, out var Entry))
                return false;

            Entry.IsActive = false;
            Entry.Callback = null;
            timerDict.Remove(timerId);
            if (!isTicking)
                phaseTimerDict[Entry.Timing].Remove(Entry);
            else
                dirtyPhaseHashList.Add(Entry.Timing);
            return true;
        }

        /// <summary>
        /// 停止全部任务并立即释放回调引用 驱动中的列表保持位置稳定
        /// </summary>
        internal void StopAll()
        {
            foreach (var Entry in timerDict.Values)
            {
                Entry.IsActive = false;
                Entry.Callback = null;
            }
            timerDict.Clear();
            if (!isTicking)
            {
                foreach (var TimerList in phaseTimerDict.Values)
                    TimerList.Clear();
            }
            else
            {
                foreach (var Phase in phaseTimerDict)
                {
                    if (Phase.Value.Count > 0)
                        dirtyPhaseHashList.Add(Phase.Key);
                }
            }
        }

        /// <summary>
        /// 修改指定任务的暂停状态 返回是否发生状态变化
        /// </summary>
        internal bool SetPaused(long timerId, bool paused)
        {
            if (!timerDict.TryGetValue(timerId, out var Entry) || Entry.IsPaused == paused)
                return false;
            Entry.IsPaused = paused;
            return true;
        }

        /// <summary>
        /// 修改全部现有任务的暂停状态 不影响后续新建任务
        /// </summary>
        internal void SetAllPaused(bool paused)
        {
            foreach (var Entry in timerDict.Values)
                Entry.IsPaused = paused;
        }

        /// <summary>
        /// 查询任务是否活跃
        /// </summary>
        internal bool IsActive(long timerId)
        {
            return timerDict.ContainsKey(timerId);
        }

        /// <summary>
        /// 查询任务是否暂停
        /// </summary>
        internal bool IsPaused(long timerId)
        {
            return timerDict.TryGetValue(timerId, out var Entry) && Entry.IsPaused;
        }

        /// <summary>
        /// 查询下次回调的剩余秒数 积压与已结束任务返回零
        /// </summary>
        internal float GetRemainingTime(long timerId)
        {
            return timerDict.TryGetValue(timerId, out var Entry) ? (float)Math.Max(0d, Entry.RemainingTime) : 0f;
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 释放全部任务并禁止后续创建
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;
            isDisposed = true;
            StopAll();
            if (!isTicking)
                phaseTimerDict.Clear();
        }

        #endregion
    }
}
