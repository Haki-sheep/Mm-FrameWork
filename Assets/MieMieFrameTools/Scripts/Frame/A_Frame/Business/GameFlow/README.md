# 游戏流程

轻量封装现有 UpdateFsm 只提供状态注册 同步切换 当前状态查询和变更通知
不提供通用异步生命周期 Loading 故障恢复 状态栈或统一资源释放协议

## 接入

ModuleHub 创建 GameFlowManager 并在管理器初始化阶段注册 IGameFlowService
框架就绪时仍为 None 游戏 Bootstrap 在唯一启动入口等待 ReadyTask 后注册状态并选择初态

```csharp
await ModuleHub.Instance.ReadyTask;
var Flow = ModuleHub.Instance.GetManager<GameFlowManager>();
Flow.RegisterState<MainMenuState>(EGameFlowState.MainMenu);
Flow.RegisterState<GamingState>(EGameFlowState.Gaming);
Flow.RegisterState<SettlementState>(EGameFlowState.Settlement);

var Service = GameHub.Get<IGameFlowService>();
Service.ChangeState(EGameFlowState.MainMenu);
```

三个默认状态只是业务扩展点 实际游戏可派生现有 StateBase 并用自己的类型注册
一个枚举只注册一次 一个状态类型只对应一个枚举 None不作为切换目标
注册时 FSM 创建并初始化实例 后续返回同一状态复用实例 每轮数据重置放 OnEnter

## 查询与切换

```csharp
var Service = GameHub.Get<IGameFlowService>();
bool IsGaming = Service.CurrentState == EGameFlowState.Gaming;
bool Changed = Service.ChangeState(EGameFlowState.Gaming);
```

CurrentState根据内部 FSM 当前实例类型映射 没有第二份独立状态数据
同步切换先调用旧状态 OnExit 再选择目标并调用 OnEnter
OnExit中查询仍是旧状态 OnEnter中查询已是目标状态
Changed为true表示同步进入回调和成功通知都已完成
相同状态或切换回调内再次请求返回false 未注册目标和生命周期异常直接抛出

生命周期异常会停止当前游戏状态 Tick并原样传播 不回滚 不自动重试
此时CurrentState仅反映FSM已选实例 不表示该实例业务准备成功
业务应修复首错后重新建立流程 不把失败状态当作正常Gaming继续使用
通知监听异常向上传播但不撤销已完成的状态切换

## 其他模块接入

```csharp
Subscription = MmGlobalEventBus.GlobalBus.Subscribe(GameFlowEvents.StateChanged, OnFlowChanged);
EGameFlowState eCurrentState = Service.CurrentState;
```

先订阅再立即查询 确保晚加入模块也能取得当前状态
StateChanged携带From和To 在OnEnter正常返回后发布 回调查询与To一致
消费者保存订阅令牌并在自己的销毁入口Dispose 服务获取一次后缓存
通知内不连续切换 需要连跳时在外部按顺序调用ChangeState
接口 事件和状态生命周期均限定Unity主线程

## 状态业务与销毁

直接沿用现有StateBase的OnInit OnEnter OnExit OnUnInit与三类Tick
OnInit仅创建实例时调用 OnEnter负责本轮进入 OnExit负责正常退出 OnUnInit负责实例终止
根节点销毁时现有FSM执行当前状态OnExit与缓存状态UnInit 然后注销流程服务
具体状态自行持有并释放资源与事件订阅 不在状态回调中销毁整个流程管理器

如需异步场景加载 由具体业务先等待资源准备完成再同步切换
本层不把Forget启动的加载完成等同于状态进入完成

当前版本已通过 Unity 导入与编译检查 以及真实源码的外部行为验证 10组43项断言
外部验证仅替换 ModuleHub 与 MonoManager 引擎适配 未执行真实 PlayMode 或 Player 构建
业务模块约定见 [DLS架构与使用规范](../DES/业务模块DLS架构与使用规范.md)
