using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MieMieFrameWork.ChainedFsm
{
    public class ChainedFsm
    {
        private IChainedFsm curChainedFsm;
        public Dictionary<Type, IChainedFsm> chainedFsmDict = new();

        /// <summary>
        /// 当前状态类型（只读）
        /// </summary>
        public Type CurrentStateType => curChainedFsm?.GetType();

        /// <summary>
        /// 获取或创建状态
        /// </summary>
        /// <typeparam name="T">状态类型</typeparam>
        /// <returns>状态实例</returns>
        public T GetOrNewState<T>() where T : IChainedFsm, new()
        {
            var stateType = typeof(T);
            if (!chainedFsmDict.TryGetValue(stateType, out var state))
            {
                state = new T();
                chainedFsmDict.Add(stateType, state);
            }

            return (T)state;
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <typeparam name="T">状态类型</typeparam>
        /// <param name="forceChange">是否强制切换（即使是相同状态）</param>
        public void ChangeState<T>(bool forceChange = false) where T : IChainedFsm, new()
        {
            var newStateType = typeof(T);
            var newChainedFsm = GetOrNewState<T>();

            if (curChainedFsm?.GetType() == newStateType && !forceChange)
                return;

            curChainedFsm?.OnExit();
            curChainedFsm = newChainedFsm;
            curChainedFsm.OnEnter();
        }

        /// <summary>
        /// 移除状态
        /// </summary>
        /// <typeparam name="T">状态类型</typeparam>
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
        /// 快速退出所有状态，并清空所有状态
        /// </summary>
        public void ExitAllChainedFsm()
        {
            foreach (var chainedFsm in chainedFsmDict)
            {
                chainedFsm.Value.OnExit();
            }
            this.Clear();
        }
    }
}