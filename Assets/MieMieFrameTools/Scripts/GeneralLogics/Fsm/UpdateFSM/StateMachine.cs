namespace MieMieFrameWork.FSM
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 有限状态机核心类 - 管理状态的切换和生命周期
    /// </summary>
    [Serializable]
    public class StateMachine 
    {
        private IFsmBlackboard blackboard;
        private Dictionary<Type, StateBase> statesDic = new();

        /// 状态机启动事件
        public Action<Type> OnStateMachineStarted;
        /// 状态机停止事件
        public Action OnStateMachineStopped;
        /// 当前状态类型（只读）
        public Type CurrentStateType => currentStateBase?.GetType();
        /// 当前状态实例（只读）
        public StateBase CurrentState => currentStateBase;
        [SerializeField] private StateBase currentStateBase;
        [SerializeField] private MonoManager monoManager;
        /// <summary>
        /// 初始化状态机
        /// </summary>
        /// <param name="owner">状态机拥有者</param>
        /// <param name="blackboard">黑板数据（可选）</param>
        /// <param name="statesList">预设状态列表（可选）</param>
        public void Init(IFsmBlackboard blackboard = null)
        {
            this.blackboard = blackboard;
            monoManager = ModuleHub.Instance.GetManager<MonoManager>();
            if (monoManager is null)
            {
                Debug.LogError("MonoManager is null");
                return;
            }
        }

        /// <summary>
        /// 切换到指定状态
        /// </summary>
        /// <typeparam name="T">目标状态类型</typeparam>
        /// <param name="forceChange">是否强制切换（即使是相同状态）</param>
        /// <returns>是否成功切换</returns>
        public bool ChangeState<T>(bool forceChange = false) where T : StateBase, new()
        {
            Type targetType = typeof(T);

            // 检查是否需要切换
            if (CurrentStateType == targetType && !forceChange)
            {
                return false;
            }

            // 退出当前状态
            ExitCurrentState();

            // 进入新状态
            currentStateBase = GetNewState<T>();
            EnterNewState();

            return true;
        }

        /// <summary>
        /// 获取或创建指定类型的状态
        /// </summary>
        /// <typeparam name="T">状态类型</typeparam>
        /// <returns>状态实例</returns>
        public T GetNewState<T>() where T : StateBase, new()
        {
            Type stateType = typeof(T);

            if (!statesDic.TryGetValue(stateType, out StateBase state))
            {
                // 创建新状态实例
                state = new T();
                state.Init(blackboard);
                statesDic.Add(stateType, state);
            }

            return state as T;
        }

        /// <summary>
        /// 停止状态机
        /// </summary>
        public void Stop()
        {
            // 退出当前状态
            ExitCurrentState();
            currentStateBase = null;

            // 清理所有状态
            foreach (var state in statesDic.Values)
            {
                if (state != null)
                {
                    state.UnInit();
                }
            }

            statesDic.Clear();

            // 触发停止事件
            OnStateMachineStopped?.Invoke();
        }

        /// <summary>
        /// 暂停状态机（保留状态，但停止Update调用）
        /// </summary>
        public void Pause()
        {
            if (currentStateBase != null)
            {
                RemoveUpdateListeners();
            }
        }

        /// <summary>
        /// 恢复状态机
        /// </summary>
        public void Resume()
        {
            if (currentStateBase != null)
            {
                AddUpdateListeners();
            }
        }

        
        /// <summary>
        /// 调试方法 获取当前状态名
        /// </summary>
        public string Debug_GetCurrentState()
        {
             return currentStateBase.GetDebugInfo();
        }

        #region 私有方法

        private void ExitCurrentState()
        {
            if (currentStateBase != null)
            {
                currentStateBase.OnExit();
                RemoveUpdateListeners();
            }
        }

        private void EnterNewState()
        {
            if (currentStateBase != null)
            {
                currentStateBase.OnEnter();
                AddUpdateListeners();
            }
        }
    
        private void AddUpdateListeners()
        {
            if (currentStateBase is not null)
            {
                monoManager.AddUpdateListener(currentStateBase.OnUpdate);
                monoManager.AddLaterUpdateListener(currentStateBase.OnLateUpdate);
                monoManager.AddFixedUpdateListener(currentStateBase.OnFixedUpdate);
            }
        }

        private void RemoveUpdateListeners()
        {
            if (currentStateBase is not null)
            {
                monoManager.RemoveUpdateListener(currentStateBase.OnUpdate);

                monoManager.RemoveLaterUpdateListener(currentStateBase.OnLateUpdate);
                monoManager.RemoveFixedUpdateListener(currentStateBase.OnFixedUpdate);
            }
        }

        #endregion
    }
}