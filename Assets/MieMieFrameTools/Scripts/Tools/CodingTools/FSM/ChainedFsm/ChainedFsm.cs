namespace MiMieFSM.ChainedFsm
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// 链式有限状态机
    /// </summary>
    public class ChainedFsm
    {
        /// <summary>
        /// 当前链式状态
        /// </summary>
        private IChainedFsm curChainedFsm;

        /// <summary>
        /// 状态字典
        /// </summary>
        public Dictionary<Type, IChainedFsm> chainedFsmDict = new();

        /// <summary>
        /// 当前状态类型
        /// </summary>
        public Type CurrentStateType => curChainedFsm?.GetType();

        /// <summary>
        /// 当前状态名
        /// </summary>
        public string CurrentStateName => curChainedFsm?.GetType().Name ?? "None";

        /// <summary>
        /// 获取或创建状态
        /// </summary>
        public T GetOrNewState<T>() where T : IChainedFsm, new()
        {
            Type stateType = typeof(T);
            if (!chainedFsmDict.TryGetValue(stateType, out IChainedFsm state))
            {
                state = new T();
                state.Owner = this;
                chainedFsmDict.Add(stateType, state);
            }

            return (T)state;
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        public void ChangeState<T>(bool forceChange = false) where T : IChainedFsm, new()
        {
            Type newStateType = typeof(T);
            IChainedFsm newChainedFsm = GetOrNewState<T>();

            if (curChainedFsm?.GetType() == newStateType && !forceChange)
                return;

            curChainedFsm?.OnExit();
            curChainedFsm = newChainedFsm;
            curChainedFsm.OnEnter();
        }

        /// <summary>
        /// 移除状态
        /// </summary>
        public void RemoveState<T>() where T : IChainedFsm, new()
        {
            chainedFsmDict.Remove(typeof(T));
        }

        /// <summary>
        /// 清空所有状态
        /// </summary>
        public void Clear()
        {
            chainedFsmDict.Clear();
            curChainedFsm = null;
        }

        /// <summary>
        /// 退出并清空所有状态
        /// </summary>
        public void ExitAllChainedFsm()
        {
            foreach (KeyValuePair<Type, IChainedFsm> chainedFsm in chainedFsmDict)
                chainedFsm.Value.OnExit();

            Clear();
        }
    }
}
