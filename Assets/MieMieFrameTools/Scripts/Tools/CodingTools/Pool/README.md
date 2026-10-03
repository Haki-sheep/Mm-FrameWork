# 对象池使用与生命周期

本页是对象池公共接口 容量与释放责任的权威说明

## 一 对象池归属

- `PoolManager` 由 `ModuleHub` 创建并唯一初始化 管理器负责注册池和最终销毁
- `PoolHandle` 管理 GameObject 实例 同一预制体在管理器内对应一个池
- `ObjectPool<T>` 管理普通 C# 对象 实例引用用于归还校验 不使用业务 `Equals`
- 池持有借出记录 使用者必须归还 不得直接销毁 GameObject 或让其所属场景先卸载
- 场景池应在卸载场景前销毁 全局池的借出对象应放在全局父节点下
- 组件引用只初始化一次 每轮业务状态通过取出与归还回调刷新
- 所有池均按单线程使用 创建函数与生命周期及 configure 回调中不得递归获取归还清理或销毁同一个池
- 管理器销毁注册池期间禁止经管理器重新注册或访问其他池 避免清理回调改变注册关系

## 二 GameObject 获取与容量

```csharp
var Handle = PoolManager.Instance.GetPool(Prefab, maxInactive: 20, maxTotal: 100);
var Instance = Handle.Get(Parent, configure: Value =>
{
    Value.transform.localPosition = SpawnPosition;
});
```

- `maxInactive` 只限制闲置缓存 超出缓存容量的归还实例会销毁 可以为零
- `maxTotal` 限制借出与闲置总量 零表示不限制 达到上限时获取返回 null
- 原接口 `GetPool(prefab, maxSize)` 保留旧语义 缓存与总量上限都为 maxSize 默认 50
- 原接口获取已注册池时直接复用 原有容量参数不会修改池
- 新接口重复注册时检查容量与显式传入的回调 不一致立即报错 不静默覆盖
- 池回调由首次注册确定 后续传 null 表示只查找而不是清空已有回调
- `TryGetPool` 只查找已注册池 已销毁的池可重新注册 旧句柄仍然失效
- 创建与预热在未激活的池节点下执行 避免配置前触发 OnEnable
- 获取顺序为登记租借 设置父节点与场景 恢复预制体局部 Transform 执行 onGet 执行 configure 最后激活
- `Get(parent, activate: false)` 与泛型组件获取均支持暂不激活 由使用者完成配置后自行激活
- `Get<T>` 缺少所需组件会销毁本次实例并报错 不返回空组件留下借出对象
- onGet 或 configure 失败时销毁半配置实例 原始异常与清理异常一起向上传递
- 预热只创建闲置实例 不执行 onGet 与 onRelease
- 回调不得自行提前激活实例 初始化组件不能依赖尚未执行的 Awake

## 三 清缓存与销毁

| 接口 | 行为 | 借出对象 | 旧句柄 |
| --- | --- | --- | --- |
| `PoolHandle.Clear` | 只销毁闲置缓存 | 保留记录 允许归还 | 继续有效 |
| `ObjectPool<T>.Clear` | 只清闲置缓存并执行 onDestroy | 保留记录 允许归还 | 继续有效 |
| `PoolManager.ClearInactive` | 清理全部已注册池的闲置缓存 | 保留记录 允许归还 | 继续有效 |
| `PoolHandle.Dispose` | 销毁整个 GameObject 池 | 执行 onRelease 后销毁 | 失效 |
| `ObjectPool<T>.Dispose` | 销毁普通对象池 | 执行 onRelease 与 onDestroy | 失效 |
| `ClearGameObject` 与 `ClearObject` | 销毁指定池并注销 | 按池销毁规则处理 | 失效 |
| `ClearAllGameObject` 与 `ClearAllObject` 与 `SelectClearPool` | 销毁对应注册池并注销 | 按池销毁规则处理 | 失效 |
| `PoolManager.Dispose` | 销毁所有注册池并释放全局实例 | 按池销毁规则处理 | 管理器与句柄都失效 |

- 普通对象自身资源由 onDestroy 释放 不自动假定所有 IDisposable 对象都应 Dispose
- GameObject 的 onDestroy 只负责业务清理 Unity 实例始终由池调用 Destroy
- 指定池注销只在池已进入销毁状态后进行 批量注销先预检查所有目标池 重入拒绝不会丢失原注册关系
- 销毁多个对象时收集清理异常并继续释放 最后抛出 AggregateException 不伪造成功
- 销毁后的 Get Release Clear Prewarm 会报错 Dispose 可重复调用
- `TotalCreated` 与 `CountAll` 表示当前持有的借出加闲置数量 不是历史累计创建次数

## 四 普通对象池

```csharp
var Handle = PoolManager.Instance.RegisterObjectPool(
    createFunc: () => new DamageContext(),
    onRelease: Value => Value.Reset(),
    maxInactive: 32);

using (Handle.Get(out var Context))
{
    Context.Damage = 10;
}
```

- 工厂必须返回注册的具体类型 新对象不能与已借出对象重复
- `GetObject<T>` 仍可直接使用 首次获取即注册默认泛型池 空池用 new T 补充
- 使用自定义工厂和回调时必须先 RegisterObjectPool 重复注册未销毁类型会报错
- 类型注册键使用 System.Type 不通过 FullName 字符串匹配
- `PushObject` 与 `PushObj` 归还当前池借出对象 重复归还和非本池归还会报错
- 兼容先归还后获取的旧用法 类型尚未注册时首次 PushObject 接管该对象
- 类型已有注册池时不再接受任意外部对象 需要外部初始对象时在首次注册前接管
- `ObjectPool<T>.Release` 始终严格校验归属
- 原公开 poolQueue 改为私有 避免绕过归还校验 数量读取使用 CountInactive
- 作用域 Get 遇到总量上限会报错 不创建持有 null 的归还句柄
- 示例 DamageContext 与 Reset 为业务自定义类型和重置函数 不属于对象池模块

## 五 异步任务与租借版本

```csharp
var Instance = Handle.Get(Parent);
ulong Version = Handle.GetLeaseVersion(Instance);
await UniTask.WaitForSeconds(Duration, cancellationToken: LifetimeToken);
bool Released = Handle.TryRelease(Instance, Version);
```

- 每次租借产生新版本 异步任务保存本轮版本
- 归还 再借出 销毁池或销毁对象后旧版本均无效
- `TryRelease` 遇到过期租借或已销毁池返回 false 不归还新一轮对象
- `IsLeaseValid` 可用于旧任务更新业务数据前的检查
- 普通 Release 不区分任务轮次 异步回收应使用版本接口
- DiscardAfterFailedRelease 用于归还回调已抛异常后的强制清理 只销毁指定轮次 不重复调用 onRelease 仍执行 onDestroy
- 版本校验不代替任务取消 任务所属业务仍负责取消源与订阅清理
- 既有调用若仍使用普通 Release 不会自动获得轮次保护 需要显式接入租借版本接口

## 六 集合池

直接使用 Unity 原生 ListPool DictionaryPool HashSetPool 不新增同职责包装层

```csharp
using (UnityEngine.Pool.ListPool<GameObject>.Get(out var InstanceList))
{
    InstanceList.Add(Instance);
}
```

- 归还会清空集合元素 不表示释放底层容量
- using 仅用于集合使用已经结束的作用域
- 不可提前归还仍被异步任务或其他调用者使用的集合
- 不可在归还后继续访问集合 不跨线程共享原生集合池
- 对象池编辑器压力操作使用原生 ListPool 保存本次借出实例
- 压力操作不激活实例 在 finally 中归还所有成功借出的实例 不残留压力对象
- 压力归还回调失败时销毁该轮次实例并继续处理其余实例 获取与清理异常统一向上传递且保留首错
- 编辑器预热预设沿用旧接口 缓存与总量采用相同上限 已注册池以首次配置为准

## 七 验证范围

验证证据记录在本任务交付中 不将模块隔离验证视为完整项目通过

- 普通对象 空池创建 重复归还 跨池归还 引用相等 归还重置 容量 清缓存与销毁
- GameObject 预热 激活时序 配置回调 清缓存后归还 总量限制 缓存溢出 销毁借出对象 旧句柄失效
- 租借版本 归还后再借出 旧回调不影响新租借 销毁后的 TryRelease
- 集合池 using 归还后集合清空
- 完整工程还需在其他并行改动结束并排除既有编译问题后验证
