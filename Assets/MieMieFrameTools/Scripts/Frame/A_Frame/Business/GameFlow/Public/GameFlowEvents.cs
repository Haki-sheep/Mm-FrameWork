namespace MieMieFrameWork.Business.GameFlow
{
    using MiMieEventBus;

    /// <summary>
    /// 状态切换的只读快照
    /// </summary>
    public readonly struct GameFlowTransition
    {
        public EGameFlowState From { get; }
        public EGameFlowState To { get; }

        /// <summary>
        /// 接收来源与目标 保存本次切换快照
        /// </summary>
        public GameFlowTransition(EGameFlowState eFrom, EGameFlowState eTo)
        {
            From = eFrom;
            To = eTo;
        }
    }

    /// <summary>
    /// 游戏状态通知 消费者负责释放订阅令牌
    /// </summary>
    public static class GameFlowEvents
    {
        /// <summary> FSM 进入回调完成后的状态变更通知 </summary>
        public static readonly EventKey<GameFlowTransition> StateChanged = new EventKey<GameFlowTransition>("GameFlow.StateChanged");
    }
}
