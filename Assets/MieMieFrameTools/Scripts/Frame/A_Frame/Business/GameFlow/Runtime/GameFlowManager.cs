namespace MieMieFrameWork.Business.GameFlow
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MiMieEventBus;
    using MiMieFSM.UpdateFsm;

    /// <summary>
    /// 游戏级流程入口 复用现有 FSM 执行状态生命周期与 Tick
    /// </summary>
    [ModuleHub.ManagerAttribute(int.MaxValue)]
    public sealed class GameFlowManager : ModuleHub.IManagerBase, IGameFlowService, IFsmTickScheduler, IDisposable
    {
        /// <summary> 框架帧代理 由 ModuleHub 注入 </summary>
        private readonly MonoManager monoManager;

        /// <summary> 游戏状态通知使用的事件总线 </summary>
        private readonly EventBusCore eventBus;

        /// <summary>  Update 状态机 </summary>
        private readonly StateMachine stateMachine = new StateMachine();

        /// <summary> 游戏状态标识到 FSM 类型与切换动作的注册表 </summary>
        private readonly Dictionary<EGameFlowState, GameFlowStructInfo> stateDict = new Dictionary<EGameFlowState, GameFlowStructInfo>();

        /// <summary> 同步生命周期与通知派发期间禁止切换重入 </summary>
        private bool isChanging;

        /// <summary> 管理器终止标记 </summary>
        private bool isDisposed;

        #region 状态注册信息

        /// <summary>
        /// 保存 FSM 状态类型与对应切换动作
        /// </summary>
        private readonly struct GameFlowStructInfo
        {
            public Type Type { get; }
            public Func<bool> Change { get; }

            /// <summary>
            /// 接收状态类型与切换动作 保存注册信息
            /// </summary>
            public GameFlowStructInfo(Type Type, Func<bool> Change)
            {
                this.Type = Type;
                this.Change = Change;
            }
        }

        #endregion

        public EGameFlowState CurrentState
        {
            get
            {
                foreach (var Entry in stateDict)
                {
                    if (Entry.Value.Type == stateMachine.CurrentStateType)
                        return Entry.Key;
                }

                return EGameFlowState.None;
            }
        }

        /// <summary>
        /// 接收帧代理与总线 不在构造阶段初始化或进入状态
        /// </summary>
        public GameFlowManager(MonoManager MonoManager, EventBusCore EventBus)
        {
            monoManager = MonoManager;
            eventBus = EventBus;
        }

        /// <summary>
        /// 由 ModuleHub 管理器阶段唯一调用组件初始化入口
        /// </summary>
        void ModuleHub.IManagerBase.Init() => InitComponents();

        /// <summary>
        /// 初始化 FSM 帧驱动 注册唯一流程公共接口
        /// </summary>
        private void InitComponents()
        {
            stateMachine.Init(this);
            GameHub.Register<IGameFlowService>(this);
        }

        /// <summary>
        /// 由业务 Bootstrap 注册标识与具体状态 缓存实例但不进入状态
        /// </summary>
        public void RegisterState<T>(EGameFlowState eState) where T : StateBase, new()
        {
            ThrowIfDisposed();
            if (isChanging)
                throw new InvalidOperationException("[GameFlow] 切换回调中不能注册状态");
            if (eState == EGameFlowState.None || stateDict.ContainsKey(eState) || stateDict.Values.Any(Entry => Entry.Type == typeof(T)))
                throw new InvalidOperationException($"[GameFlow] 无效或重复状态注册 {eState} 类型 {typeof(T).FullName}");

            stateMachine.GetNewState<T>();
            stateDict.Add(eState, new GameFlowStructInfo(typeof(T), () => stateMachine.ChangeState<T>()));
        }

        /// <summary>
        /// 同步调用现有 FSM 切换成功后发布状态通知 重复状态与重入返回 false
        /// </summary>
        public bool ChangeState(EGameFlowState eState)
        {
            ThrowIfDisposed();
            if (!stateDict.TryGetValue(eState, out var Target))
                throw new InvalidOperationException($"[GameFlow] 目标状态 {eState} 未注册");
            if (isChanging)
                return false;

            var ePrevious = CurrentState;
            isChanging = true;
            try
            {
                bool Changed;
                try
                {
                    Changed = Target.Change();
                }
                catch
                {
                    stateMachine.Pause();
                    throw;
                }

                if (!Changed)
                    return false;

                eventBus.Publish(GameFlowEvents.StateChanged, new GameFlowTransition(ePrevious, eState));
                return true;
            }
            finally
            {
                isChanging = false;
            }
        }

        /// <summary>
        /// 根节点销毁时停止帧驱动 由现有 FSM 执行退出与反初始化并注销服务
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;
            if (isChanging)
                throw new InvalidOperationException("[GameFlow] 同步切换回调中不能销毁流程管理器");

            isDisposed = true;
            stateMachine.Pause();
            try
            {
                stateMachine.Stop();
            }
            finally
            {
                if (GameHub.TryGet<IGameFlowService>(out var Service) && ReferenceEquals(Service, this))
                    GameHub.Unregister<IGameFlowService>();
                stateDict.Clear();
            }
        }

        /// <summary>
        /// 已销毁入口立即报错 不继续驱动状态
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(GameFlowManager));
        }

        /// <summary>
        /// FSM 注册当前状态 Update
        /// </summary>
        void IFsmTickScheduler.AddUpdateListener(Action Action) => monoManager.AddUpdateListener(Action);

        /// <summary>
        /// FSM 移除当前状态 Update
        /// </summary>
        void IFsmTickScheduler.RemoveUpdateListener(Action Action) => monoManager.RemoveUpdateListener(Action);

        /// <summary>
        /// FSM 注册当前状态 LateUpdate
        /// </summary>
        void IFsmTickScheduler.AddLateUpdateListener(Action Action) => monoManager.AddLaterUpdateListener(Action);

        /// <summary>
        /// FSM 移除当前状态 LateUpdate
        /// </summary>
        void IFsmTickScheduler.RemoveLateUpdateListener(Action Action) => monoManager.RemoveLaterUpdateListener(Action);

        /// <summary>
        /// FSM 注册当前状态 FixedUpdate
        /// </summary>
        void IFsmTickScheduler.AddFixedUpdateListener(Action Action) => monoManager.AddFixedUpdateListener(Action);

        /// <summary>
        /// FSM 移除当前状态 FixedUpdate
        /// </summary>
        void IFsmTickScheduler.RemoveFixedUpdateListener(Action Action) => monoManager.RemoveFixedUpdateListener(Action);
    }
}
