# 数据模块生命周期

本页是配置与业务数据生命周期的权威约定 不引入通用 DataModuleManager

## 配置模块

- `LubanTablesDataModule` 从指定本地目录加载 `cfg.Tables` 全部表并解析跨表引用 直接负责一次初始化 整份配置替换与释放 不持有全局静态状态 不依赖通用数据基类
- `LubanConfigModule` 是根节点配置组件 不在 Awake Start 属性或 Reload 中自行初始化 保留类型供反射发现
- `ModuleHub` 通过 `IConfigModule` 持有可选配置组件 不反向引用 Luban 程序集
- ModuleHub 在唯一配置初始化入口优先复用同物体 `IConfigModule` 没有组件时发现已安装的 Luban 类型 并通过 `MmUnityExtension.GetOrAddComponent<IConfigModule>(Type)` 补挂 只持有接口 不需要 Inspector 拖拽槽位
- 配置组件必须位于同一个 FrameRoot 可以预挂或由配置阶段自动补挂 同一根节点按一个配置入口使用
- 自建根节点没有预挂组件时 已安装 Luban 模块会自动添加 `LubanConfigModule` 并使用其默认目录 `DataTables` 只有既无同物体配置组件也未安装 Luban 模块时才跳过配置阶段
- 自定义目录仍通过预挂组件设置 已有配置实现优先使用 不覆盖其设置 缺少配置文件会使启动失败 不自动创建空表
- 唯一调用顺序由 BootManager 编排 资源就绪 加载配置 注册管理器 初始化管理器 全部成功后完成 ReadyTask
- 配置加载不能依赖尚未注册的管理器 配置加载失败时不会进入管理器创建与注册阶段

业务入口先等待框架就绪 再获取配置 不依赖组件的 Start 隐式顺序

```csharp
await ModuleHub.Instance.ReadyTask;
var Config = ModuleHub.Instance.GetConfig<LubanConfigModule>();
var Tables = Config.Tables;
```

配置消费者需要引用 `MieMieFrameWork.Data.Luban` 并导入 `MieMieFrameWork.Data.Luban` 命名空间

当前配置来源为 `StreamingAssets/DataTables` 下的 JSON 子目录可在组件上修改

当前组件使用同步本地文件加载 支持 Editor 与能直接读取该目录的原生平台 不宣称跨平台通用 Android 与 WebGL 启动时明确报不支持 后续须接入真正的异步配置来源并更新启动完成与取消契约

生成规则与查询 API 见 [Luban 结构与使用说明](Luban结构与使用说明.md)

## 配置替换与异常

当前配置就是一次加载得到的完整 `cfg.Tables` 内存对象 原称配置快照 不是存档文件或额外备份

- Init 只能成功调用一次 初始化前读取或重复初始化立即抛出异常
- Reload 先构建完整候选表 再替换当前表 并递增 Revision
- 候选表构建失败时旧表与 Revision 不变 不采用空表或默认值掩盖错误
- Dispose 清除当前表引用并进入终止状态 之后不允许访问或再次初始化 重复释放不产生额外行为
- 当前 Luban 表只有托管配置对象 不持有资源句柄 不需要额外释放钩子 外部保留的旧表引用不会自动指向新表
- Logic 可选 Calculator 或 Service 缓存了旧配置时 Reload 后由业务所有者重建依赖 或查询时读取当前配置 不自动修改已经运行的战斗状态

## 所有权与退出

| 数据 | 创建与使用范围 | 清理责任 |
| --- | --- | --- |
| Config | 应用启动后供业务只读使用 普通切场景或退出账号不重载 | ModuleHub 销毁时释放 |
| Runtime | 业务会话开始后作为模块私有真值 | 创建它的业务入口退出时释放 |
| Save | 读档时转换成 Runtime 保存时从 Runtime 导出 | 现有 IArchiveModule 适配层 不充当第二份业务真值 |

没有当前账号或会话业务实现时只记录契约 不新增空 Service 空登录模块或第二套存档管理器

业务入口按以下顺序执行

1. 等待 ReadyTask 并关联自身销毁取消 再显式组装 Runtime Logic Service
2. 需要存档时由业务编排入口显式选择或创建槽位 并先向 ArchiveMgr 注册本会话存档适配器 此时不向 GameHub 发布业务服务
3. 已有存档调用 ArchiveMgr.Load 由已注册适配器的 FromArchive 写入模块 Runtime
4. 新档由业务编排入口创建 SaveData 对本会话适配器依次调用 CreateArchive 填充默认快照 再调用 FromArchive 建立 Runtime CreatSlot 只创建槽位 不自动执行这两个回调
5. 全部数据就绪后注册 GameHub 公共接口并绑定模块所需事件 任一加载失败立即注销本次注册的存档适配器并清理已组装对象 不发布半初始化服务
6. 退出时先停止接收命令并取消本模块异步任务 按反向依赖顺序注销服务和存档适配器
7. 模块释放自己持有的订阅令牌与资源 最后释放 Runtime 不在初始化或 Dispose 中隐式保存 需要落盘时由业务显式调用 Save

同一业务对象的所有者同时负责部分组装失败后的清理 清理失败须保留初始异常 不发布就绪或成功通知

GameHub 只定位服务 Clear 只解除引用 不负责 Dispose 跨模块命令与数据边界见 [DLS 架构与使用规范](../A_Frame/Business/DES/业务模块DLS架构与使用规范.md)

框架启动失败时立即解除并释放已持有的配置 不等到根节点销毁 若清理也失败则同时保留两项异常 ReadyTask 不会成功

有效根节点销毁时框架管理器逆序释放 再释放配置 任一释放失败继续清理其余对象并在最后抛出聚合异常 清空根节点单例引用不依赖释放成功

## 验证步骤

1. 使用标准 FrameRoot 进入 Play 等待 ReadyTask 确认 IsReady 与 HasConfig 为 true 配置 Revision 为 1
2. 查询生成表并执行一次 Reload 确认内容可读 Revision 递增
3. 将某个配置文件暂时改为无效 JSON Reload 应抛出原始异常链 旧快照与版本保持不变 恢复原文件
4. 使用错误配置目录启动 ReadyTask 应失败 IsReady 与 HasConfig 为 false 后续管理器不执行初始化
5. 重复放置 FrameRoot 仅有效根节点加载配置 销毁重复根节点不影响有效配置
6. 销毁有效根节点 已缓存配置组件再查询应抛出释放异常 新根节点不得沿用旧快照

以上是可重复验证步骤 不等于已在 Play 或目标设备执行通过

## 2026-10-03 通用组件扩展与配置补挂验证

- 新增 `MmUnityExtension` 泛型 GetOrAdd 支持 GameObject 与 Component 调用 具体类型直接获取 接口调用提供具体组件 Type 不执行业务初始化
- ModuleHub 注册中的私有 GetOrAdd 已删除 帧代理 输入与交互使用公共扩展 配置阶段优先复用已有接口 缺失时补挂已安装 Luban 类型
- Luban 配置类型使用 Preserve 保留反射入口 Runtime 未新增 Luban 程序集引用
- Unity 编译请求 `49b578b3434c4b7083ca7261a4cb7e08` 基线 generation 46 新代 47 覆盖完整 无 history_gap 零 errors 零 warnings 最终 ready
- 首轮行为检查请求 `910c5521244540aeab4847c414886c89` 因测试假设标准 Prefab 已预挂 Luban 而失败 未改断言为通过 修正为在隔离副本显式建立已有组件场景
- 复验请求 `6a9daf0898be4640a62b0ddb5fac03ab` 26 项通过 覆盖具体泛型和接口重载复用 缺组件补挂 不兼容类型拒绝 扩展不初始化 配置阶段自动补挂并加载真实表 退出释放
- 行为检查只操作 LoadPrefabContents 的隔离副本 finally 卸载 未保存 Prefab 未进入 Play 核对源 Prefab 依赖 hash 与框架单例未变
- Play 完整资源及管理器启动 IL2CPP 裁剪与目标设备行为未验证 泛型扩展用法见 [Unity 通用扩展](../../Tools/CodingTools/UnityExtension/UnityExt/README.md)
- 独立增量评审条件通过 未发现新增 P0/P1 或项目规范违例 保留 Play 集成 IL2CPP 与设备验证缺口 AddComponent 实际创建失败路径仅静态核对

## 2026-10-03 配置组件发现验证

- 移除配置拖拽槽位 ModuleHub 在唯一配置阶段按同物体 `IConfigModule` 获取并持有组件 保持 `GetConfig<T>` 与退出释放方式
- Unity 编译请求 `67816d753884467298d371ff2fb51b19` 基线 generation 39 新代 40 覆盖完整 无 history_gap 零 errors 零 warnings 最终 ready
- 标准 FrameRoot Prefab 通过 FakeUnityCLI 结构化查询确认根组件与 Luban 配置组件同物体 查询标识 `90be1c0728154a9abe2ebaf85906b873`
- 使用 Unity 实际接口查询完成 7 项只读检查 验证接口与具体组件一致 唯一配置持有字段 旧源码及 Inspector 字段已移除 同物体查询边界 请求标识 `643d3209c2a448c89b608039386643a4`
- 只读取既有 Prefab 未创建实例 未初始化配置 未保存 Scene 或 Prefab 未进入 Play 完整启动 失败传播与销毁集成行为尚未验证
- 本轮独立增量评审条件通过 未发现新增 P0/P1 或项目规范违例 保留上述集成验证缺口 不将只读查询扩大为实际启动通过

## 2026-10-03 简化加载器验证

- 移除通用数据基类 Luban 加载器直接管理当前表 业务仍通过 `GetConfig<LubanConfigModule>().Tables` 查询
- 配置初始化提前到管理器注册前 资源就绪与 ReadyTask 完成位置保持原职责
- 使用 FakeUnityCLI 在线 AssetDatabase 删除基类源码及配套 meta 操作标识 `a7bd0ce8324b4b7a826cbe7e1f442e56` 只备份源码 不声明完整资产撤销
- Unity 编译 generation 39 相对请求基线 38 为新代 覆盖完整 无 history_gap 零 errors 零 warnings 最终 ready 请求标识 `76340f35f40f4ba195ea0da70c2fd24d`
- 使用真实加载器与当前生成表执行 21 项行为检查 通过首次加载 重复初始化 成功重载 无效 JSON 缺失文件 失败保留旧表与版本 释放后访问及重复释放 请求标识 `912b8c48a4c94f279d588293e7d2d661`
- 行为检查只修改 `Temp/FakeUnityCLI` 中复制的 JSON 没有修改正式表 没有进入 Play
- 源码核对配置先于注册 注册先于初始化 模块中心探测路径有效 且不存在已删除基类的源码引用
- 独立评审条件通过 未发现本次增量引入的 P0/P1 或项目规范违例 保留 P2 验证缺口为真实根节点 Play 启动 失败回滚 重复根与销毁集成行为
- Play 下完整框架启动与目标平台行为未验证 下列历史记录不充当本轮验证证据

## 2026-10-02 历史验证记录

- 验证日期 2026-10-02
- 使用当前 Unity 6000.4.10f1 自带 Roslyn 与真实 Newtonsoft Luban DLL 定向编译数据基类 接口 配置组件 加载器与当前全部生成表
- 使用 Mono 执行 28 项快照与异常状态检查通过 仅使用 Temp 中复制的配置 没有改写工程配置
- 定向编译出现 Newtonsoft 的 netstandard 2.0 与 Unity netstandard 2.1 引用兼容警告 不隐瞒该警告
- Runtime 全量源码首轮因同期操作移动 ModuleHub.cs 导致旧编辑器响应文件 CS2001 更新实际源码后出现 AudioManager 的 6 项缺引用与缺类型错误 原始失败日志均保留
- 同期音频任务修正后使用同一 Unity Roslyn 和真实引用复验 Runtime 全程序集源码编译通过 未通过添加虚假引用或跳过源码掩盖前述错误
- 全 Luban 源码检查另发现同期 Localization 代码依赖 UniTask 而该程序集尚未声明引用 6 项错误记录在 luban-full-compile.log 不属于本次数据模块增量且未修改无关代码
- 独立评审发现根组件被同期拆分改名导致挂载文件不再与类同名 已恢复 ModuleHub.cs 并保留原 GUID 标准 Prefab 仍引用同一根组件
- 定向与 Runtime 源码通过不代表全工程编译或框架启动通过 Unity Play 标准 Prefab 初始化 失败回滚 重复根节点与目标设备未验证
- 临时验证源码 编译响应文件和首错日志保留在工程 Temp/HakiDataLifecycleValidation 不加入运行时程序集
