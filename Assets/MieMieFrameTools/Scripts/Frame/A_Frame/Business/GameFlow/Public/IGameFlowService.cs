namespace MieMieFrameWork.Business.GameFlow
{
    /// <summary>
    /// 游戏流程公共入口 通过 GameHub 查询与切换状态
    /// </summary>
    public interface IGameFlowService : IGameService
    {
        public EGameFlowState CurrentState { get; }

        /// <summary>
        /// 同步切换已注册状态 重复状态或回调重入返回 false
        /// </summary>
        public bool ChangeState(EGameFlowState eState);
    }
}
