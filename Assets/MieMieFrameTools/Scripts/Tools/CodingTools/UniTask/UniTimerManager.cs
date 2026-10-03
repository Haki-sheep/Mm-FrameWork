using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using static MieMieFrameWork.ModuleHub;

namespace MieMieFrameWork
{
    /// <summary>
    /// 统一定时器管理器 由各 PlayerLoop 阶段集中驱动
    /// </summary>
    [ManagerAttribute(6)]
    public partial class UniTimerManager : IManagerBase, IDisposable
    {
        /// <summary> 补执行模式每个任务每次驱动的默认回调上限 </summary>
        public const int DefaultMaxCallbacksPerTick = 32;

        /// <summary> 不依赖 Unity 时间读取的计时内核 </summary>
        private readonly TimerScheduler scheduler = new TimerScheduler();

        /// <summary> 各阶段集中驱动器 </summary>
        private readonly List<TimerPlayerLoopDriver> driverList = new List<TimerPlayerLoopDriver>();

        /// <summary> 是否已完成唯一初始化 </summary>
        private bool isInitialized;

        /// <summary> 是否已释放 </summary>
        private bool isDisposed;

        #region 生命周期

        /// <summary>
        /// 由 ModuleHub 唯一初始化入口注册各阶段驱动器
        /// </summary>
        public void Init()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(UniTimerManager));
            if (isInitialized)
                throw new InvalidOperationException("[UniTimerManager] 禁止重复初始化");

            // 获取所有玩家循环阶段
            var TimingList = (PlayerLoopTiming[])Enum.GetValues(typeof(PlayerLoopTiming));
            try
            {
                // 遍历所有玩家循环阶段 创建驱动器并添加到驱动器列表
                foreach (var eTiming in TimingList)
                {
                    var Driver = new TimerPlayerLoopDriver(scheduler, eTiming);
                    driverList.Add(Driver);
                    // 添加到玩家循环阶段  把Driver 加入 UniTask 对应阶段的执行队列 
                    // 这行是Unitask针对Unity循环做的自定义生命周期 在Update里面更新 UniTask.PlayerLoopRunner.RunCore()
                    PlayerLoopHelper.AddAction(eTiming, Driver);
                }
                isInitialized = true;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// 停止全部定时器并释放驱动器持有的内核引用
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            foreach (var Driver in driverList)
                Driver.Release();
            driverList.Clear();
            scheduler.Dispose();
        }

        /// <summary>
        /// 创建任务前确认管理器已完成初始化且未释放
        /// </summary>
        private void RequireInitialized()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(UniTimerManager));
            if (!isInitialized)
                throw new InvalidOperationException("[UniTimerManager] 请先由 ModuleHub 调用 Init");
        }

        #endregion

        #region 创建定时器

        /// <summary>
        /// 启动单次定时器 返回实例内递增 ID 零延迟也不在创建调用栈执行
        /// </summary>
        public long StartTimer(
            float time,
            Action action = null,
            PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update,
            bool ignoreTimeScale = false)
        {
            RequireInitialized();
            return scheduler.Add(time, 0f, 1, action, playerLoopTiming, ignoreTimeScale,
                TimerCatchUpMode.SkipMissed, 1, UnityEngine.Time.frameCount);
        }

        /// <summary>
        /// 启动循环定时器 首次回调计入次数 负次数无限循环 零次数无效
        /// </summary>
        public long StartRepeatingTimer(
            float startDelay,
            float interval,
            int loopCount,
            Action action,
            PlayerLoopTiming playerLoopTiming = PlayerLoopTiming.Update,
            bool ignoreTimeScale = false,
            TimerCatchUpMode catchUpMode = TimerCatchUpMode.SkipMissed,
            int maxCallbacksPerTick = DefaultMaxCallbacksPerTick)
        {
            RequireInitialized();
            if (interval <= 0f)
                throw new ArgumentOutOfRangeException(nameof(interval), "循环间隔必须大于零");
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            return scheduler.Add(startDelay, interval, loopCount, action, playerLoopTiming,
                ignoreTimeScale, catchUpMode, maxCallbacksPerTick, UnityEngine.Time.frameCount);
        }

        #endregion

        #region 控制定时器

        /// <summary>
        /// 停止指定的计时器 立即使 ID 失效并释放回调引用
        /// </summary>
        public bool StopTimer(long timerId)
        {
            return scheduler.Stop(timerId);
        }

        /// <summary>
        /// 停止所有计时器 不阻止后续创建新任务
        /// </summary>
        public void StopAllTimers()
        {
            scheduler.StopAll();
        }

        /// <summary>
        /// 暂停指定的计时器
        /// </summary>
        public bool PauseTimer(long timerId)
        {
            return scheduler.SetPaused(timerId, true);
        }

        /// <summary>
        /// 恢复指定的计时器
        /// </summary>
        public bool ResumeTimer(long timerId)
        {
            return scheduler.SetPaused(timerId, false);
        }

        /// <summary>
        /// 暂停所有计时器
        /// </summary>
        public void PauseAllTimers()
        {
            scheduler.SetAllPaused(true);
        }

        /// <summary>
        /// 恢复所有计时器
        /// </summary>
        public void ResumeAllTimers()
        {
            scheduler.SetAllPaused(false);
        }

        #endregion

        #region 查询定时器

        /// <summary>
        /// 获取活跃计时器数量 包含暂停与等待下一帧的任务
        /// </summary>
        public int GetActiveTimerCount()
        {
            return scheduler.Count;
        }

        /// <summary>
        /// 检查指定计时器是否还在运行
        /// </summary>
        public bool IsTimerActive(long timerId)
        {
            return scheduler.IsActive(timerId);
        }

        /// <summary>
        /// 检查指定计时器是否暂停
        /// </summary>
        public bool IsTimerPaused(long timerId)
        {
            return scheduler.IsPaused(timerId);
        }

        /// <summary>
        /// 获取指定计时器距下次回调的剩余秒数 已结束与积压任务返回零
        /// </summary>
        public float GetRemainingTime(long timerId)
        {
            return scheduler.GetRemainingTime(timerId);
        }

        #endregion
    }
}
