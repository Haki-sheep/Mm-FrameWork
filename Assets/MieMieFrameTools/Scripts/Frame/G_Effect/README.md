# 视觉特效管理

本页是视觉特效播放 档位 预算与资源释放责任的权威说明

## 范围与入口

- 首版支持 ParticleSystem 预制体 不管理音频 Animator Timeline 或 VFX Graph
- EffectManager 由 ModuleHub 创建并唯一 Init 不创建静态游戏实例 不自动加载资源
- FrameRoot 的视觉特效配置中填写 DefinitionList 每项包含业务 Id 与 YooAsset Location
- 同一 Id 和地址不能重复 同一预制体池由本模块独占 不与其他业务共用 PoolHandle
- 预制体根节点必须挂 EffectInstance 子发射器必须位于该预制体内
- Looping 必须与整组粒子是否存在循环系统一致 非循环特效整组粒子结束后自动归还
- EffectInstance 组件由池首次取出回调唯一 InitComponents 不依赖尚未执行的 Awake
- 实例始终位于全局 FrameRoot 下 Owner 只是姿态锚点 不是实例父节点
- 所有接口在 Unity 主线程调用 不允许在池回调中递归获取归还清理或销毁同一池

## 使用

```csharp
await ModuleHub.Instance.ReadyTask;
var Effects = ModuleHub.Instance.GetManager<EffectManager>();
await Effects.PrewarmAsync("Hit");
var Result = await Effects.PlayAsync("Hit",
    new EffectSpawnOptions(HitPosition, Quaternion.identity), CancellationToken);
if (Result.Status == EEffectPlayStatus.Accepted)
{
    var Handle = Result.Handle;
    Handle.Pause();
    Handle.Resume();
    Handle.Stop();
}
```

示例需引用 MieMieFrameWork 与 MieMieFrameWork.Effects 变量由游戏业务提供

- 无 Owner 时 Position 与 Rotation 是世界姿态 有 Owner 时是相对 Owner 的局部偏移
- FollowOwner 为 true 时每帧刷新姿态 Owner 销毁后停止本轮 无论是否跟随都检查 Owner 生命周期
- 默认绑定请求时的活跃场景 有 Owner 时默认绑定其场景 可通过 Scene 显式指定
- 场景卸载会停止非 Persistent 请求 Persistent 仅跳过场景清理 不忽略 Owner 销毁
- UseUnscaledTime 控制粒子时间缩放 默认跟随 Time.timeScale
- 循环粒子配置保持预制体原始语义 必须调用 Handle.Stop 或 StopAll
- CancellationToken 控制加载 排队与已启动播放 运行中取消最迟在下一次帧更新回收
- StopAll 取消所有播放请求 保留资源缓存 不取消独立 PrewarmAsync
- 单轮暂停与全局暂停叠加 任一暂停仍生效 不在恢复时重启已结束的粒子
- EffectHandle 绑定管理器会话和单轮请求 并校验池租借版本 默认与过期句柄不操作新播放
- 外部整池销毁后下一次 Tick 根据已取得的租借版本清理请求 不将已销毁实例误判为加载等待

## 预算与预热

- MaxConcurrent 默认 64 限制全部加载中 排队中与播放中请求
- MaxPerFrame 默认 16 同时限制每帧新接受请求和实际启动或显式预热实例数量
- 单定义 MaxConcurrent 默认 8 对应其池总量上限 MaxInactive 默认 8 可以为零
- 预算拒绝返回 GlobalLimit DefinitionLimit 或 FrameLimit 不加载资源且不返回有效句柄
- 加载集中完成超过启动预算时等待后续帧 不突破预算 不将排队伪装为成功播放
- 停止请求释放并发预算 不返还本帧已接受或启动预算
- PrewarmAsync 显式加载并分帧预热 PrewarmCount 是目标闲置数量 默认零
- 预热不占播放请求预算 但计入资源使用中请求 不能被 ClearUnused 淘汰
- 有活跃实例占用池总量时预热受剩余容量限制 不销毁活跃实例腾出预热空间
- 容量 地址 未知 Id 循环声明与组件配置错误直接抛出 不用预算拒绝掩盖错误

## 档位与实例刷新

- SetQuality 显式设置 Low Medium High 与全局画质模块解耦 游戏可以从画质服务映射档位
- 默认粒子容量倍率为 0.35 0.65 1 默认发射倍率为 0.4 0.7 1 可在管理器配置修改
- 每次从原始参数设置 maxParticles 发射曲线倍率与 Burst 数量 不累计缩减 不把曲线改为常量
- OptionalNodeList 只绑定特效内部子节点 根据 MinimumQuality 启用 原本禁用的节点不会被强行启用
- 激活前清空上轮粒子和 TrailRenderer 拖尾 设置姿态 缩放 时间模式与档位
- playOnAwake 在实例中关闭 stopAction 统一改为 None 播放与释放由管理器负责
- 子发射器不独立 Play 由预制体事件触发 自然结束检测包含其粒子
- 不修改共享材质 不提供透明度衰减或自动 FPS 调档 不将透明度变化当作性能优化
- 独立 TrailRenderer 只负责复用清空 不作为自然结束计时依据 使用 ParticleSystem 自带 Trails 表达需等待的粒子拖尾
- 不重置任意自定义脚本 材质动画或子节点动画状态 这些行为不属于首版粒子播放契约

## 资源与退出责任

- 单定义持有一份 YooAsset 原生 AssetHandle 多个播放共享加载 取消一个请求不取消其他请求
- 原生句柄由 EffectResourceEntry 持有 不另建全局资源引用计数 不接入资源 LRU
- 归还实例后保留资源句柄 闲置池实例依然依赖预制体资源
- ClearUnused 只淘汰无播放和预热请求的条目 同时注销独占池
- Dispose 取消加载和排队请求 停止播放 注销帧与场景监听 销毁实例池
- 池销毁回调记录待销毁对象 等 Unity 实际 Destroy 后才释放原生资源 包含从未激活的预热实例
- 延迟释放不依赖已经退出的 EffectManager Tick 可通过 WaitForReleaseAsync 等待并观察释放异常
- WaitForReleaseAsync 只等待调用时已经退休的条目 不主动清理当前资源缓存
- 归还失败会销毁该轮实例 保留原异常 多条退出清理失败集中抛出 AggregateException
- 资源管线通用契约见 [YooAsset 接入](../B_Assets/Yooasset/RuntimeAdapter/README.md) 池契约见 [对象池](../../Tools/CodingTools/Pool/README.md)

## 诊断与验证

- 菜单 Tools/MieMieFrameWork/工具中枢 选择运行监控/视觉特效 显示请求数 资源持有 拒绝次数 借出与闲置数量
- CopyDiagnostics 填充调用方复用的列表 不在运行时 Tick 自动分配诊断快照
- 实际验证证据及未验证项见 [特效管理验收记录](../../../../../AIPlan/特效管理-验收记录.md)
- 真机 GPU 粒子成本 透明过绘制 IL2CPP 与长期 GC 预算须由目标平台 Profiler 验证
