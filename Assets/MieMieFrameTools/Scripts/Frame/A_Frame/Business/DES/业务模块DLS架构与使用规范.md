# DLS 架构与使用规范

## 1 定位

DLS 是基于 Service Locator 的模块化业务职责约定

| 职责 | 名称 | 边界 |
| --- | --- | --- |
| D | Data | 配置 运行时状态 存档快照与输入输出数据 |
| L | Logic | 业务规则 计算 校验 状态提交与副作用协调 |
| S | Service | 公共接口 内部组装 请求转发与稳定查询 |

GameHub 只定位模块公共服务 不承担业务规则或模块组装

原 DCES 的 Calculator 与 Executor 职责统一归入 Logic 默认不再拆成两套对象
复杂公式需要复用 替换策略或独立测试时 再从 Logic 提取可选 Calculator

DLS 保留 Functional Core Imperative Shell 的职责边界
纯计算方法与状态提交方法可以放在同一个 Logic 类中 但不能混成无法单独验证的大函数

DLS 是职责约束 不是文件数量约束 不是每个请求都必须走完三类对象
Data 偏数据容器 Logic 承载业务规则 不以 DDD 或完整 Clean Architecture 的分层要求套用全部模块

本规范定义业务模块的组织方式 本次文档调整不表示已有模块已经完成代码重构

## 2 各类职责

### Data

Data 保存业务数据以及公共输入和结果

- ConfigData 初始化后只读
- RuntimeData 是运行期间唯一业务真值 不向模块外暴露
- SaveData 只是序列化快照 加载后转换成 RuntimeData
- Request 和 Result 不是必选项 简单参数直接写在接口方法上
- 只有入参或回执明显变多时才在 Public/Data 增加 DTO
- 外部只能获得只读结果或 DTO 不能取得内部可变对象及其集合引用
- 正常业务命令中的数据修改由负责该业务的 Logic 完成
- 创建初始数据与读档恢复由业务所有者显式编排 不通过公共接口开放任意写入
- Data 不定位服务 不发布事件 不保存存档 不驱动场景对象

配置 Runtime 与 Save 的创建 读档 注销 释放以及失败清理约定统一见 [数据模块生命周期](../../../C_Data/README.md)

### Logic

Logic 是模块内部的业务规则与状态提交边界 按业务能力命名和拆分

推荐 PlayerDamageLogic InventoryItemLogic RewardGrantLogic
业务能力增加后按职责拆分 不把整个模块全部规则堆进巨型 PlayerLogic

允许

- 读取注入的配置和依赖
- 计算结果并校验业务条件
- 修改模块私有 RuntimeData
- 按明确保存策略调用存档端口
- 发布事件 请求表现并返回明确业务结果
- 调用可选 Calculator 复用纯计算规则

禁止

- 向模块外暴露自身或内部 RuntimeData
- 在深层代码中临时调用 GameHub 获取隐藏依赖
- 默认通过同级 Logic 相互调用组成依赖环
- 在多个 Logic 中重复实现同一业务规则
- 将计算 写状态 保存和通知混成无法分别验证的大函数

Logic 内部区分两类方法

| 方法 | 输入与输出 | 约束 |
| --- | --- | --- |
| 纯计算 | 明确参数 不可变配置或只读快照 返回计算结果 | 不修改 Runtime 不发事件 不保存 不访问场景对象 |
| 业务执行 | 请求参数及注入依赖 返回业务结果 | 协调校验 状态提交与实际需要的副作用 |

纯计算方法不读取 Unity 时间或未注入的随机数
时间和随机样本由执行方法采集后作为明确输入传入 相同输入必须产生相同结果
无状态公式可以使用 private static 方法 有只读配置依赖时可以使用实例方法
纯计算方法不能持有可变业务状态或更新缓存

默认在同一个 Logic 内分别实现计算方法和执行方法 不为两个方法再创建两套类

业务执行通常先生成结果 再验证 再一次性应用 RuntimeData
需要保存时按明确策略执行保存 最后发布已提交状态的事件并请求表现
不需要计算或保存的命令跳过对应步骤 不为步骤齐全制造空方法

复杂业务应返回明确 Result 不要只依赖异常或 bool
上述顺序不提供跨 Runtime 存档 外部服务和表现的原子事务保证
写状态后保存或通知失败必须暴露原始异常与已完成阶段 不能返回成功或声称自动回滚
业务需要重试 补偿或原子提交时须单独定义恢复策略

多个同模块 Logic 需要共同完成流程时才增加 FlowLogic
例如 BattleFlowLogic 编排 CombatLogic RewardLogic 和 GrowthLogic
跨模块流程使用独立 FlowService 不绕过其他模块的公共接口

### 可选 Calculator

Calculator 是从 Logic 提取的纯计算协作者 不是 DLS 的第四个必选层

| 情况 | 实现方式 |
| --- | --- |
| 简单公式 只在一个 Logic 使用 | 保留为 Logic 内的纯计算方法 |
| 公式需要多处复用 无字段 | 提取无状态静态 Calculator |
| 计算依赖 Config 数值表或公式策略 | 提取实例 Calculator 构造注入只读依赖 |
| 需要业务写入 缓存更新 资源加载或通知 | 保留在 Logic 的执行职责内 |

提取后的 Calculator 只读取方法参数与构造注入的不可变配置或只读快照 并返回结果
不修改 RuntimeData 不访问 GameHub 不发布事件 不保存存档 不访问场景或 UI
不读取 Unity 时间或未注入的随机数 不保存可变业务状态
实例 Calculator 构造后不可变 可以调用同模块静态纯函数复用公式

Service 或业务组装入口创建 Calculator 后注入 Logic
不要把大量无边界规则堆进巨型静态工具类 以复用 可替换性和独立验证需要决定是否提取

### Service

Service 是模块公共入口 实现公共接口并组装模块内部对象

允许

- 持有模块内部 Data 和 Logic
- 将公共请求转发给对应 Logic
- 提供稳定查询
- 将内部结果转换成接口返回值或事件参数

禁止

- 把 Logic Calculator 或内部 RuntimeData 返回给外部
- 在 Service 中复制业务规则或直接提交正常业务状态
- 让其他模块直接依赖具体 Service 实现类
- 用 Action 回调向 Logic 注入跨模块通知出口
- 在公共服务接口上堆 C# event 作为跨模块广播

跨模块命令和查询通过 GameHub 获取 IXxxService
跨模块状态通知由业务执行方法通过 MmGlobalEventBus.GlobalBus.Publish 发布
消费者通过 MmGlobalEventBus.GlobalBus.Subscribe 订阅

Public/Interface 定义能做什么 Public/Event 定义会发生什么
不要把跨模块通知做成构造函数里的 Action 委托

事件接口 消息类型与订阅生命周期见 [事件中心使用规范](../../../../Tools/CodingTools/EventCenter/README.md)

## 3 接口与职责标记

D 和 L 默认不增加 IData ILogic 或原 IExecutor 一类空标记接口
目录 类名 访问修饰符与代码审查负责划分职责 空接口不能阻止写状态或副作用 也不能保证纯计算

| 对象 | 默认形式 | 何时引入接口 |
| --- | --- | --- |
| Data | 模块内部具体数据类型 | 框架有实际读写契约时实现对应接口 例如存档适配器实现 IArchiveModule |
| Logic | 模块内部具体逻辑类 | 同一业务能力需要多种实现 可替换策略或测试替身时 |
| Calculator | 纯函数或只读依赖的具体类 | 计算策略需要替换时定义 IDamageFormula 等具体契约 |
| Service | 实现继承 IGameService 的公共接口 | 跨模块公开命令与查询的固定边界 |

需要接口时按业务能力命名 例如 IRewardGrantLogic IDamageFormula
接口必须声明调用方实际需要的方法或属性 不为所有 Data 和 Logic 创建一一对应接口
跨模块需要查询数据时默认返回只读结果 不因为定义了数据接口就暴露内部 Runtime
只读接口也不自动保证其实现和返回集合不可变 必须检查返回值与实际持有对象

IGameService 是当前 GameHub 注册约束需要的标记接口
不将这一用途推广成所有层都必须拥有标记接口
具体接口是否有价值以消费方的真实依赖与替换需要判断 不是以层名判断

## 4 查询和命令路径

命令由 Service 转发给 Logic Logic 完成业务规则与 RuntimeData 提交
提取 Calculator 后由 Logic 调用 Calculator 取得计算结果 再由 Logic 执行提交

查询可以由 Service 直接读取只读状态
需要业务规则的查询交给 Logic 的纯计算方法 或可选 Calculator
查询不修改业务状态 不保存存档 不发布状态变更事件

读取一个属性不需要创建空壳 Logic
模块对外仍只有公共接口与只读结果 不因短路径暴露内部对象

## 5 GameHub 规则

GameHub 是单脚本服务注册表 内部就是一张接口到实例的字典
IGameService 与 GameHub 放在同一文件

正确注册

```csharp
GameHub.Register<IPlayerService>(PlayerService);
```

注册键必须是具体模块的公共接口
不能省略接口类型让类型推断得到实现类 不能注册 PlayerService 或 IGameService
同接口重复注册会覆盖旧实例 业务组装入口负责明确注册与注销时机

必需依赖使用 Get

```csharp
IPlayerService PlayerService = GameHub.Get<IPlayerService>();
```

当前实现中服务未注册时 Get 返回 null 调用方应视为组装错误
不以备用实现或静默跳过掩盖必需依赖缺失

可选依赖使用 TryGet

```csharp
if (GameHub.TryGet(out IPhotoModeService PhotoModeService))
{
    PhotoModeService.Open();
}
```

业务所有者释放模块时调用 GameHub.Unregister<IPlayerService>()
注销顺序与资源所有权统一遵守数据模块生命周期约定

GameHub 面向 Unity 主线程使用 不作为多线程依赖注入容器
高频逻辑不要每帧反复 Get 应获取一次后缓存

GameHub 不编排框架启动 普通业务服务由具体游戏的 Bootstrap 显式创建和注册
基础游戏流程服务由框架管理器阶段注册 Bootstrap 只在就绪后组装具体状态
查询与通知约定见 [游戏级流程编排](../GameFlow/README.md)

业务入口在 Start 中先 await ModuleHub.Instance.ReadyTask 再组装服务
有效 ModuleHub 销毁时清空 GameHub 注册表 普通场景切换不清空全局服务
清空注册表不会释放业务服务的资源 业务入口仍负责注销服务并释放事件订阅

启动实现以 [BootManager](../../Boot/BootManager.cs) 为准
根节点管理器与销毁实现以 [ModuleHub](../../Frame/ModuleHub.cs) 为准
数据所有权与失败清理统一见 [数据模块生命周期](../../../C_Data/README.md)

## 6 跨模块依赖

GameHub 只能用于模块边界和组装入口

推荐位置

- Bootstrap
- Service 组装入口
- 跨模块 FlowService
- UI 或 Gameplay 的外部消费入口

禁止位置

- Data
- Calculator
- 普通业务实体
- 普通 Logic 内部临时获取
- 高频 Update 内重复获取服务

Logic 依赖其他模块时由上层获取公共接口后通过构造函数注入

```csharp
IInventoryService InventoryService = GameHub.Get<IInventoryService>();
var RewardLogic = new RewardLogic(InventoryService);
```

跨模块复杂流程放入独立 FlowService 避免模块间循环依赖
例如 BattleFlowService 只依赖 IPlayerService IInventoryService 和 IRewardService
FlowService 只能调用其他模块的公共服务接口 不访问模块内部 Logic 或 Data

## 7 按需裁剪与命名

推荐目录

| 位置 | 内容 |
| --- | --- |
| Public/Interface | IPlayerService 等公共接口 |
| Public/Event | PlayerEvents 等公共事件键 |
| Public/Data | 参数或回执明显变多时才增加的 DTO |
| Data/Config | 可选配置数据 |
| Data/Runtime | 模块私有运行时状态 |
| Data/Save | 可选序列化快照 |
| Logic | PlayerDamageLogic 等业务逻辑 |
| Logic/Calculator | 按需提取的纯计算协作者 |
| PlayerService.cs | 模块公共服务实现 |

目录按实际职责创建 不以已有框架目录名称替代模块职责命名

- 没有配置时不创建 Config
- 没有存档时不创建 Save
- 纯计算没有独立复用或验证需求时不创建 Calculator
- 没有复合流程时不创建 FlowLogic
- 只有一个简单 Logic 时不增加额外 FlowLogic
- Data 类型不要为了满足目录结构而创建空类
- 简单查询模块没有实际业务规则时不创建空 Logic

类名以具体业务能力加 Logic 后缀 表明负责哪一项规则与提交
默认不使用 Executor 作为新的业务类命名 Calculator 仅用于可选纯计算协作者
DLS 的三类职责不要求每类恰好一个文件 也不要求每个模块创建全部目录

## 8 主要缺陷与规避

### 隐藏依赖

任何类都能写 GameHub.Get 会导致构造函数看不出真实依赖
规避 限制 GameHub 出现范围 Data 和 Calculator 禁止访问 普通 Logic 通过构造函数接收依赖

### Logic 职责膨胀

合并计算与执行后容易把所有能力和副作用集中到一个类
规避 按业务能力拆 Logic 类 类内分开纯计算与提交方法 复杂公式按需提取 Calculator

### 强制分层产生空壳

读取一个属性也创建 Logic 或每个公式都提取 Calculator 会增加无意义转发
规避 查询允许短路径 只为实际业务职责创建对象

### Data 退化成公共数据袋

多个 Logic 随意修改同一 RuntimeData 会使约束散落
规避 RuntimeData 模块私有 每项业务状态明确对应提交责任 外部只获取只读结果

### 跨模块循环依赖

模块 Service 互相 Get 会造成初始化顺序和递归问题
规避 复杂跨模块流程独立成 FlowService 只编排公共接口

### 副作用中途失败

先改数据再请求表现再存档再发事件 中途失败会出现半完成状态
规避 分开计算 校验与提交 按业务策略安排保存和通知 明确异常发生时哪些阶段已完成
需要恢复的业务单独定义恢复策略 不把执行顺序当作事务或自动回滚

## 9 最终约束

- Data 是模块私有状态与明确输入输出
- Logic 是业务规则 状态写入与副作用协调边界
- Service 是模块公共入口
- 纯计算与提交方法分开 复杂计算按需提取 Calculator
- GameHub 只定位公共 Service
- 跨模块命令走 Interface 状态通知走 EventKey
- 每个请求不强制走完全部职责对象
- 跨模块复杂流程由独立 FlowService 编排

## 10 业务接入

原 Business/Samples/Player 示例已移除 本文不再依赖示例程序集或场景组件

1. 游戏自己的 Bootstrap 在 Start 中等待框架 ReadyTask 完成
2. 显式创建模块 Data Logic 和 Service 有独立计算需要时才组装 Calculator
3. 需要读档时先按数据模块生命周期约定恢复 Runtime 全部就绪后按公共接口向 GameHub 注册 Service
4. 服务消费者在业务组装完成后获取接口并订阅事件 不依赖不同组件的 Start 隐式顺序
5. Bootstrap 退出时按所有权约定注销服务并释放资源 失败清理不发布就绪状态
6. 消费者销毁时释放自己持有的事件订阅令牌

## 11 代码审查清单

提交新的 DLS 模块前检查

- 公共接口是否继承 IGameService GameHub 是否按公共接口类型注册
- 外部是否只依赖公共接口 公共 EventKey 与只读结果
- 简单参数是否直接写在接口上 没有多余 DTO
- 跨模块通知是否走 Public/Event 而不是 Action 回调或接口上的 C# event
- RuntimeData 是否仍然是模块私有 没有泄露可变集合引用
- Logic 是否按业务能力拆分 避免巨型业务类和相互调用环
- 纯计算方法是否与状态提交分开 相同明确输入是否得到相同结果
- Calculator 是否按需提取 是否只持有不可变配置或只读快照
- 正常业务状态修改是否集中到对应 Logic
- 保存策略与通知时机是否明确 失败阶段是否可观察
- 是否将执行顺序误当作跨系统事务或自动回滚
- Service 是否只负责组装 转发和稳定查询
- 模块释放时是否 Unregister 对应服务 EventBus 订阅是否存在对应 Dispose
- 高频路径是否缓存服务引用 跨模块流程是否只调用公共 Service
- 是否创建了没有实际职责的空层或仅用于分类的空标记接口
