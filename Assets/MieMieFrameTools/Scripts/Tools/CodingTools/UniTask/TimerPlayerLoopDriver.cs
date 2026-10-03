using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MieMieFrameWork
{
    /// <summary>
    /// 每个 PlayerLoop 阶段仅注册一个驱动器 不为单个任务注册异步循环
    /// </summary>
    internal sealed class TimerPlayerLoopDriver : IPlayerLoopItem
    {
        /// <summary> 所属的 PlayerLoop 阶段 </summary>
        private readonly PlayerLoopTiming timing;

        /// <summary> 释放时立即断开对内核的引用 </summary>
        private TimerScheduler scheduler;

        /// <summary>
        /// 绑定计时内核与执行阶段 不读取组件
        /// </summary>
        internal TimerPlayerLoopDriver(TimerScheduler timerScheduler, PlayerLoopTiming eTiming)
        {
            scheduler = timerScheduler;
            timing = eTiming;
        }

        /// <summary>
        /// 注入当前阶段的时间 固定阶段使用固定步长
        /// </summary>
        public bool MoveNext()
        {
            var Scheduler = scheduler;
            if (Scheduler == null)
                return false;

            bool IsFixed = timing == PlayerLoopTiming.FixedUpdate || timing == PlayerLoopTiming.LastFixedUpdate;
            float DeltaTime = IsFixed ? Time.fixedDeltaTime : Time.deltaTime;
            float UnscaledDeltaTime = IsFixed ? Time.fixedUnscaledDeltaTime : Time.unscaledDeltaTime;
            try
            {
                Scheduler.Tick(timing, DeltaTime, UnscaledDeltaTime, Time.frameCount);
            }
            catch (System.AggregateException)
            {
                // 仅续接已取消的业务回调异常 其他驱动异常不重排
                if (scheduler != null)
                    PlayerLoopHelper.AddAction(timing, this);
                throw;
            }
            return scheduler != null;
        }

        /// <summary>
        /// 释放内核引用 下次阶段驱动自动注销
        /// </summary>
        internal void Release()
        {
            scheduler = null;
        }
    }
}
