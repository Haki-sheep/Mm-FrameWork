# 事件中心使用规范

本文是事件中心接口 派发行为与订阅生命周期的权威说明

## 接口与消息

- 无参事件使用 `EventKey` 与 `Action`
- 有参事件使用 `EventKey<TEvent>` 与 `Action<TEvent>`
- 多个数据字段放入一个明确的消息类型 不再提供二至五参数重载
- 消息无需实现框架接口 简单事件仍可直接使用 `int` `float` 等类型
- 事件槽位以名称与消息类型区分 同名不同消息类型不会互相派发
- 引用类型消息支持 `Action` 逆变 按消息类型统一派发委托 并保留原订阅委托用于注销
- 事件用于通知已发生的事实 命令 查询与必须完成的业务步骤走公共服务接口

```csharp
public readonly struct HealthChangedEvent
{
    public int PlayerId { get; }
    public int OldHealth { get; }
    public int NewHealth { get; }

    /// <summary>
    /// 创建血量变化消息 保存角色与变化前后的血量
    /// </summary>
    public HealthChangedEvent(int playerId, int oldHealth, int newHealth)
    {
        PlayerId = playerId;
        OldHealth = oldHealth;
        NewHealth = newHealth;
    }
}

public static class PlayerEvents
{
    /// <summary>
    /// 玩家血量变化通知
    /// </summary>
    public static readonly EventKey<HealthChangedEvent> HealthChanged = new("Player.HealthChanged");
}
```

```csharp
MmGlobalEventBus.GlobalBus.Publish(
    PlayerEvents.HealthChanged,
    new HealthChangedEvent(playerId, oldHealth, newHealth));
```

消息应表达发布时的数据 不直接暴露模块内部可变 RuntimeData
只读结构体内的引用字段仍可能被修改 不等于完整的数据快照

## 同步派发契约

| 场景 | 行为 |
| --- | --- |
| 线程 | 所有订阅 发布 注销与容器操作仅限主线程 |
| 普通派发 | `Publish` 在当前调用栈同步执行 按订阅顺序调用 |
| 没有监听器 | 不执行回调 不更新最近触发记录 |
| 派发中新增订阅 | 不参与本轮 从下一次 `Publish` 生效 |
| 派发中注销或清空 | 本轮已经取得的委托快照继续执行 下一次 `Publish` 使用新状态 |
| 嵌套发布 | 立即同步执行 使用嵌套发布时的最新订阅状态 不自动排队 |
| 重复订阅 | 允许重复 每个返回令牌只取消自己对应的那次注册 |
| 显式注销 | `Unsubscribe` 取消最后一次委托相等的注册 不按组合委托的子回调拆分注销 |
| 监听异常 | 原异常立即向上传播 本轮后续监听不再执行 不记录后继续或自动恢复 |
| 总线清空 | 清除注册并解除旧令牌持有的引用 旧令牌后续释放不会影响新注册 |

取消订阅应优先使用返回的 `IDisposable` 令牌
手动调用 `Unsubscribe` 时保存原订阅委托 不使用新建 Lambda
清空总线不能撤回正在执行的回调快照

`RemoveAll(EventKey)` `RemoveAll<TEvent>` 与 `RemoveAllByName` 沿用按名称清理的语义
它们会移除该名称的全部消息类型槽位 不仅清理传入 Key 的消息类型

## 订阅容器与生命周期

`EventBusSubscriptionGroup` 收集订阅令牌 由消费者承担统一释放责任

| 接口 | 语义 |
| --- | --- |
| `Add` | 收集一个有效的订阅令牌 容器释放后拒绝新增 |
| `Count` | 尚未从容器清理的令牌数量 不代表总线当前有效监听数量 |
| `Clear` | 释放本轮已收集的令牌并清空 容器可继续收集下一轮订阅 |
| `Dispose` | 永久释放容器 重复调用不重复释放 |

```csharp
/// <summary>
/// 当前显示周期的事件订阅
/// </summary>
private readonly EventBusSubscriptionGroup eventSubscriptions = new();

/// <summary>
/// 显示时注册血量通知并交给容器管理
/// </summary>
private void BindEvents()
{
    eventSubscriptions.Add(
        MmGlobalEventBus.GlobalBus.Subscribe(PlayerEvents.HealthChanged, OnHealthChanged));
}

/// <summary>
/// 隐藏时解除当前显示周期的通知
/// </summary>
private void UnbindEvents()
{
    eventSubscriptions.Clear();
}
```

`OnHealthChanged` 由业务实现 参数类型为 `HealthChangedEvent`
显示期间消费事件的 UI 每个显示周期只调用一次 `BindEvents` 隐藏时调用 `UnbindEvents`
反复 `Refresh` 不重新订阅 对象最终销毁时调用容器 `Dispose`
对象整个存活周期消费事件时 在唯一初始化入口订阅 销毁时释放
业务模块在启动时订阅 退出时释放 不通过 Unity 对象判空替代注销

`Clear` 先分离本轮令牌 因释放回调重入收集的新令牌不属于本轮
某个令牌释放失败时仍尝试释放本轮其余令牌 最后以 `AggregateException` 抛出全部失败
这只用于资源释放收尾 不代表监听回调异常也会被隔离
调用方不得将释放失败当作资源全部释放成功

## 核心与 Unity 接入

- `EventBusCore` `EventKey` 订阅令牌与容器不依赖 Unity
- `EventBusUnityBootstrap` 沿用已有 Unity 日志与时间桥接 业务不需要另写适配层
- `MmGlobalEventBus.GlobalBus` 是全局入口 由框架退出清理
- `MmLocalEventBusMono.LocalBus` 在挂载对象销毁时清空
- 局部总线销毁不会替消费者取消其在其他总线上的订阅
- `EventBusTrace` 与现有编辑器监控接口保留 日志桥接不承担吞掉监听异常的责任

## 迁移与范围

- 原无参和单参调用保持 API 形状不变
- 原 `EventKey<T0, T1...>` 迁移为 `EventKey<XxxEvent>`
- 原多参数发布改为构造并发布一个消息对象
- 原逐监听器记录异常后继续派发改为异常原样传播 依赖容错通知的业务须显式设计恢复策略
- 此次不增加事件队列 优先级 后台线程或自动 Unity 生命周期组件
- 派发复用不可变多播委托 不创建逐监听器数组和捕获消息的 Lambda
- 订阅变更会重建派发委托并读取调用列表 诊断计数直接读取槽位统计

## 验证要点

1. 无参和消息事件同步按注册顺序执行 同名不同消息类型互不干扰
2. 派发中增删监听不改变本轮快照 嵌套发布读取最新状态
3. 重复订阅的不同令牌分别取消自己的注册 不误删其他位置
4. 清空总线后重订阅 再释放旧令牌不影响新监听
5. UI 隐藏清空订阅容器 再次显示重新订阅不重复触发
6. 监听异常原样传播且停止本轮 释放异常汇总且尝试所有令牌
