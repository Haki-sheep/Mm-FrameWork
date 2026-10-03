namespace MiMieFSM.ChainedFsm
{
    using System;

    /// <summary>
    /// 链式状态基类
    /// </summary>
    public abstract class IChainedFsm
    {
        /// <summary>
        /// 进入回调
        /// </summary>
        public Action OnEnterAction;

        /// <summary>
        /// 退出回调
        /// </summary>
        public Action OnExitAction;

        /// <summary>
        /// 所属链式状态机 由 ChainedFsm 创建状态时注入
        /// </summary>
        public ChainedFsm Owner { get; internal set; }

        /// <summary>
        /// 进入状态
        /// </summary>
        public virtual void OnEnter()
        {
            OnEnterAction?.Invoke();
        }

        /// <summary>
        /// 退出状态
        /// </summary>
        public virtual void OnExit()
        {
            OnExitAction?.Invoke();
        }
    }
}
