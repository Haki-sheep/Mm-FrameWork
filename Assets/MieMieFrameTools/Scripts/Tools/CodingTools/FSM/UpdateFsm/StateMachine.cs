namespace MiMieFSM.UpdateFsm
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Update 驱动有限状态机
    /// </summary>
    public class StateMachine
    {
        /// <summary>
        /// 黑板数据
        /// </summary>
        private IFsmBlackboard blackboard;

        /// <summary>
        /// Tick 调度器
        /// </summary>
        private IFsmTickScheduler tickScheduler;

        /// <summary>
        /// 状态字典
        /// </summary>
        private Dictionary<Type, StateBase> statesDic = new();

        /// <summary>
        /// 当前状态实例
        /// </summary>
        private StateBase currentStateBase;

        /// <summary>
        /// 已向调度器注册帧回调的状态 防止嵌套切换或重复恢复导致重复注册
        /// </summary>
        private StateBase tickingStateBase;

        /// <summary>
        /// 当前状态类型
        /// </summary>
        public Type CurrentStateType => currentStateBase?.GetType();

        /// <summary>
        /// 当前状态实例
        /// </summary>
        public StateBase CurrentState => currentStateBase;

        /// <summary>
        /// 当前状态名
        /// </summary>
        public string CurrentStateName => currentStateBase?.StateName ?? "None";

        /// <summary>
        /// 黑板数据
        /// </summary>
        public IFsmBlackboard Blackboard => blackboard;

        /// <summary>
        /// 初始化状态机
        /// </summary>
        public void Init(IFsmTickScheduler tickScheduler, IFsmBlackboard blackboard = null)
        {
            this.tickScheduler = tickScheduler;
            this.blackboard = blackboard;
        }

        /// <summary>
        /// 切换到指定状态
        /// </summary>
        public bool ChangeState<T>(bool forceChange = false) where T : StateBase, new()
        {
            Type targetType = typeof(T);

            if (CurrentStateType == targetType && !forceChange)
                return false;

            ExitCurrentState();
            currentStateBase = GetNewState<T>();
            EnterNewState();
            return true;
        }

        /// <summary>
        /// 获取或创建指定类型的状态
        /// </summary>
        public T GetNewState<T>() where T : StateBase, new()
        {
            Type stateType = typeof(T);

            if (!statesDic.TryGetValue(stateType, out StateBase state))
            {
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
            ExitCurrentState();
            currentStateBase = null;

            foreach (StateBase state in statesDic.Values)
            {
                if (state != null)
                    state.UnInit();
            }

            statesDic.Clear();
        }

        /// <summary>
        /// 暂停状态机
        /// </summary>
        public void Pause()
        {
            if (currentStateBase != null)
                RemoveUpdateListeners();
        }

        /// <summary>
        /// 恢复状态机
        /// </summary>
        public void Resume()
        {
            if (currentStateBase != null)
                AddUpdateListeners();
        }

        /// <summary>
        /// 获取当前状态调试名
        /// </summary>
        public string Debug_GetCurrentState()
        {
            return currentStateBase?.GetDebugInfo() ?? "None";
        }

        private void ExitCurrentState()
        {
            if (currentStateBase == null)
                return;

            currentStateBase.OnExit();
            RemoveUpdateListeners();
        }

        private void EnterNewState()
        {
            if (currentStateBase == null)
                return;

            currentStateBase.OnEnter();
            AddUpdateListeners();
        }

        private void AddUpdateListeners()
        {
            if (currentStateBase is null || tickScheduler is null || tickingStateBase == currentStateBase)
                return;

            RemoveUpdateListeners();
            tickScheduler.AddUpdateListener(currentStateBase.OnUpdate);
            tickScheduler.AddLateUpdateListener(currentStateBase.OnLateUpdate);
            tickScheduler.AddFixedUpdateListener(currentStateBase.OnFixedUpdate);
            tickingStateBase = currentStateBase;
        }

        private void RemoveUpdateListeners()
        {
            if (tickingStateBase is null || tickScheduler is null)
                return;

            tickScheduler.RemoveUpdateListener(tickingStateBase.OnUpdate);
            tickScheduler.RemoveLateUpdateListener(tickingStateBase.OnLateUpdate);
            tickScheduler.RemoveFixedUpdateListener(tickingStateBase.OnFixedUpdate);
            tickingStateBase = null;
        }
    }
}
