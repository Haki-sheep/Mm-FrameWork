namespace MieMieFrameWork.Business.GameFlow
{
    /// <summary>
    /// 游戏级流程状态 不对应固定场景或 UI
    /// </summary>
    public enum EGameFlowState
    {
        None,
        GameMainMenu,
        GameLoding,
        Gaming,
        GameReset,
        GameOver,
        GameExit,
    }
}
