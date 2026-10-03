# 框架对比 原子化建设 vs HakiSheep-Framework

- 生成时间 2026-10-02
- A 侧 公司商业框架原子化提取 `C:\Users\fmz\Desktop\AiWrok\GithubWork\原子化建设`
- B 侧 本项目 `E:\AAAA学习资料\Demo\哈基咩咩框架专用项目\HakiSheep-Framework\Assets\MieMieFrameTools`
- 对比口径 只读比对 未修改任何一侧源码
- 对象池增量 2026-10-02 本地实现已独立补强 详细接口与生命周期见 [对象池文档](../Assets/MieMieFrameTools/Scripts/Tools/CodingTools/Pool/README.md)

> 更新说明 2026-10-02 本项目后续已统一到 YooAsset 下文资源相关结论已同步 初始文件数量和重合度为原始快照 不代表当前工作区统计 资源接入权威说明见 [YooAsset 资源接入](../Assets/MieMieFrameTools/Scripts/Frame/B_Assets/Yooasset/RuntimeAdapter/README.md)

## 一 结论

A 与 B **不是同一套框架的缺件关系** 而是两套独立世代

| 判据 | 数据 |
| --- | --- |
| A 侧模块闭包去重 C# 文件 | 398 |
| B 侧框架 C# 文件 不含 Samples~ | 765 |
| 两边文件名交集 | 3 个 ObjectPool.cs Singleton.cs StateMachine.cs |
| 同名文件实现是否同源 | 否 例如 Singleton 本地为 lock 单例 A 侧为另一实现 |
| 按完整相对路径命中 | 0 路径体系完全不同 |

**重合度约 0.8% 因此不存在 补齐 A 侧缺件 的做法 只存在 移植 A 侧能力 的做法**

## 二 根本差异对照表

| 维度 | A 原子化建设 | B 本项目 |
| --- | --- | --- |
| 命名空间 | Framework HotfixFramework AtomicFramework | MieMieFrameWork MiMieSaver MmUIFrameWork |
| 路径体系 | Assets\Framework + Assets\HotfixFramework | Assets\MieMieFrameTools\Scripts\Frame\{A_Frame B_Assets C_Data D_UI E_Input F_Audio J_Save} |
| 程序集 | Framework.asmdef HotFixFramework.asmdef | MieMieFrameWork.UI MieMieUIFrameWork.UI 等 |
| 发行SDK | Gorilla 全套 Ad Base GM Hub Purchase 及 SRDebugger | 无 |
| 资源主方案 | Addressables 为主 附 YooAsset 编译期替换分支 | YooAsset 唯一后端 业务使用原生资源句柄 |
| 序列化 | Protobuf 源码内嵌 | MessagePack |
| 配置表 | ITableLoader 读取层 | Luban 生成 加 加载 |
| 存档 | 无独立模块 | J_Save 本地独有 |
| 业务服务定位 | 无 直接静态单例 | GameHub 接口注册表 本地独有 |
| 启动入口 | GameLauncher 加 GameApp 加 AppStartUpViewModule 状态机 | ModuleHub 加 BootManager 默认包就绪后初始化管理器 |

## 三 A 侧 22 模块在 B 侧的归属判定

判定口径 完全缺失 指 B 侧无对应能力 部分具备 指 B 侧有同类但能力更弱 本地更强 指 B 侧已有更完整实现 已具备 指后续已补齐对应能力

| 模块 | A 侧核心文件数 | B 侧判定 | 说明 |
| --- | ---: | --- | --- |
| 01 对象与集合池 | 2 | 已具备 | B 保留 PoolManager 与 PoolHandle 补泛型对象池 重置回调 双容量 清理销毁与租借版本 集合池直接使用 Unity 原生能力 |
| 02 资源加载 | 4 | 部分具备 | B 使用 YooAsset 薄门面 UI 与音频已接入 不计划引入双后端 IResourcesLoader |
| 03 资源缓存 | 7 | 已具备 | B 有 YooAsset 原生引用管理与可选实例级 LRU 条目预算 淘汰只释放缓存自身句柄 不强制清理业务持有 详见 [LRU 资源缓存](../Assets/MieMieFrameTools/Scripts/Frame/B_Assets/Yooasset/RuntimeAdapter/LRU.md) |
| 04 音效与背景音乐 | 7 | 部分具备 | B 有播放句柄 三类播放预算 Mixer 分组 BGM 渐变与 YooAsset 独立持有 缺设备音频恢复与通用多槽位BGM 详见 [音频管理](../Assets/MieMieFrameTools/Scripts/Frame/F_Audio/README.md) |
| 05 事件中心 | 2 | 部分具备 | B 有类型化同步派发 独立订阅令牌 订阅容器与 Bridge 队列与优先级按需扩展 不作为能力更弱的依据 |
| 06 定时调度 | 1 | 已具备 | B 已改造 UniTimerManager 集中驱动 缩放时间 循环次数 ID取消 暂停恢复与卡顿策略 见定时调度说明 |
| 07 基础状态机 | 4 | 部分具备 | B 有 UpdateFsm 与 ChainedFsm 缺嵌套State与StateTimer |
| 08 游戏状态管理 | 3 | 已具备 | B 的 GameFlow 轻量封装 UpdateFsm 提供同步流程切换 当前状态查询与变更通知 具体状态业务仍由项目实现 |
| 09 管理器与帧生命周期 | 7 | 部分具备 | B 有 ModuleHub 加 ManagerAttribute 缺初始化进度 统一帧分发 登录通知 |
| 10 数据模块生命周期 | 3 | 部分具备 | B 已有 DataModule 快照生命周期并接入 Luban 加载 缺真实账号会话业务编排 |
| 11 UI页面框架 | 38 | 部分具备 | B 有 UIHub UIStack UIWindowBase 缺 SubModule 上下文状态机 自动事件释放 |
| 12 本地存储 | 4 | 部分具备 | B 存档走 J_Save 缺通用 KV 抽象 ILocalData |
| 13 多语言与字体 | 6 | 完全缺失 | B 无多语言体系 无 TMP 按语言切换字库 |
| 14 动态图集 | 7 | 部分具备 | B 已新增独立运行时合图 多页预算 引用句柄 整页回收与 Editor 调试 [使用说明](../Assets/MieMieFrameTools/Scripts/Frame/B_Assets/DynamicAtlas/Docs/使用说明.md) |
| 15 网络与协议基础 | 22 | 完全缺失 | B 无通用网络层 无 IM 状态机 无 WebSocket 封装 |
| 16 日志与诊断 | 12 | 部分具备 | B 已新增独立 FrameLog 级别与渠道过滤 Unity 采集 有界缓存 可选文件轮转与导出 保留模块诊断 未聚合全模块快照与远程上报 见 [日志与诊断](../Assets/MieMieFrameTools/Scripts/Tools/Diagnostics/README.md) |
| 17 启动热更与资源更新 | 75 | 部分具备 | B 已有 BootManager 异步包初始化与就绪任务 仅接编辑器模拟和原生离线模式 缺联网更新编排 |
| 18 特效管理 | 3 | 已具备 | B 已新增 ParticleSystem 播放句柄 请求与启动预算 档位 独占池和延迟资源释放 首版不含 VFX Graph 与自动 FPS 调档 见 [视觉特效管理](../Assets/MieMieFrameTools/Scripts/Frame/G_Effect/README.md) |
| 19 画质管理 | 5 | 完全缺失 | B 无画质分级 |
| 20 平台与屏幕适配 | 6 | 完全缺失 | B 无震动 无平台能力查询 无统一屏幕适配 |
| 21 基础工具 | 10 | 部分具备 | B 有 MmTween 与 MathExtensions 缺 SequencePool 复用与安全工具 |
| 22 配置表读取 | 2 | 本地更强 | B 走 Luban 全链路 A 侧仅给读取层 |

统计按当前表格 完全缺失 4 项 部分具备 12 项 本地更强 1 项 已具备 5 项

特效管理增量 2026-10-03 本地按 ParticleSystem 独立实现 原子模块的 EffectManager 本身为空类 不将其说明中的创建释放与缓存视为可直接移植实现 详细职责见 [视觉特效管理](../Assets/MieMieFrameTools/Scripts/Frame/G_Effect/README.md)

游戏流程增量 2026-10-03 详细职责 接口与验证范围见 [游戏级流程编排](../Assets/MieMieFrameTools/Scripts/Frame/A_Frame/Business/GameFlow/README.md)

## 四 B 侧独有能力 A 侧没有

| 能力 | B 侧位置 | 价值 |
| --- | --- | --- |
| YooAsset 完整版 | Scripts\Frame\B_Assets\Yooasset | BundleCache BundleHandle DiagnosticSystem 新架构 A 侧只有薄适配 |
| Luban 配置表全链路 | Scripts\Frame\C_Data\Luban | 生成 加 加载 A 侧明确不带业务生成表 |
| J_Save 存档框架 | Scripts\Frame\J_Save | ArchiveMgr SlotsIndexMgr 多槽位 A 侧 22 模块无存档 |
| GameHub 业务服务注册表 | A_Frame\Business\GameHub | 接口定位服务 避免跨模块直接依赖实现 |
| MmTween 与动画扩展 | Tools\Display\AnimationExtension | A 侧无 |
| InteractionDetector 交互系统 | E_Input\InputInteract | A 侧无 |
| ItemWheel 与 FloatingText | D_UI\MmUIFrameWork\Runtime\Widgets | 本地独有 UI 组件 |
| YooAsset 默认包接入 | B_Assets\Yooasset\RuntimeAdapter | 原生句柄持有 UI 实例销毁释放 音频管理器退出释放 |

## 五 移植路径建议

14 动态图集已按 B 侧资源边界重新实现核心能力 不复制 A 侧 BaseGameManager 与 ResourcesManager 体系 详细契约和验证范围以模块使用说明为准

### 第一档 可直接移植 低耦合

无 Unity 场景依赖 或依赖可替换 改命名空间即可

| 模块 | 改造点 |
| --- | --- |
| 16 日志与诊断 | 已按本项目边界独立补齐第一阶段 不直接导入 ZLog 平台 SDK 或 GM 体系 后续按需聚合现有诊断 |
| 06 定时调度 | 已完成本地能力改造 不直接导入 A 的 ScheduleManager 以每阶段集中驱动保留全部 PlayerLoopTiming 详见 [定时调度说明](../Assets/MieMieFrameTools/Scripts/Tools/CodingTools/UniTask/Docs/定时调度.md) |
| 21 基础工具 | Easing JsonManager Utility.File Math SystemSafety |
| 03 资源缓存 | 已补实例级 YooAsset LRU 与业务持有和实例池分离 不移植 A 侧缓存系统 不提供虚假的内存字节预算 |
| 01 对象与集合池 | 本地已独立补强 不移植 A 侧 ObjectPool 与 CollectionPool 详见对象池文档 |
| 12 本地存储 | ILocalData 与 UnityLocalData |

### 第二档 需连体系一起移植

单独搬无法编译 因为 A 侧模块说明已声明沿用 BaseGameManager 生命周期

| 模块 | 必须连带引入 |
| --- | --- |
| 11 UI页面框架 | 09 BaseGameManager 体系 加 05 事件中心 加 04 音效 |
| 17 启动热更 | 09 体系 加 15 网络 加 11 UI 加 02 资源 加 SDK 层 |
| 15 网络 | 09 体系 加 Protobuf 运行时 加 Best HTTP 或改写为原生 |
| 13 多语言 | 11 UI 加 19 画质 加 20 屏幕适配 加 TMP |
| 05 事件中心 | 原实现沿用 09 体系 本项目保留 EventBusCore 不移植该管理器体系 规则见 [事件中心使用规范](../Assets/MieMieFrameTools/Scripts/Tools/CodingTools/EventCenter/README.md) |
| 07 09 | A 侧属于管理器族 B 侧应按现有启动体系评估 不默认整体引入 08 已由本地 GameFlow 提供 |
| 10 数据模块生命周期 | 保留 B 的快照模块 启动所有者与 GameHub 边界 不移植 A 的单例数据管理器 |

### 第三档 不建议移植

| 模块 | 原因 |
| --- | --- |
| 08 游戏状态管理 | 原实现 306 文件闭包 深度耦合 Gorilla SDK 加 UI 加平台 本地已按现有 FSM 独立实现 无须移植原闭包 |
| 19 画质管理 | 308 文件闭包 同上 |
| 04 音效 | B 已有 AudioManager 两套冲突 建议只补缺失能力 |

## 六 必须先澄清的授权问题

A 侧大模块依赖以下商业 SDK B 侧一个都没有

- GorillaBase GorillaGM GorillaHub GorillaAd GorillaPurchase
- StompyRobot SRDebugger
- com.Tivadar.Best.HTTP 与 WebSockets
- Newtonsoft.Json Sirenix Odin

**移植 08 11 15 17 19 时 B 侧要么补齐这些 SDK 并遵守其授权 要么改写掉全部 Gorilla 调用点 这是工作量级差异 不是复制粘贴**

## 七 未验证项

- 未在 Unity 中实际导入 A 侧模块包验证编译
- 未执行 B 侧项目整体编译
- 未验证 A 侧模块在 B 侧命名空间冲突情况 例如 Framework 与 MieMieFrameWork 是否存在同名类型
- 未核对 A 侧 module 清单中 conditionalDependencies 在 B 侧宏配置下的实际取值
- A 侧验证记录自述只做过离线源码编译 未做 Unity 导入与真机验证

## 八 后续落地进展

上文统计与归属为生成时的只读快照 后续实施不回写原始对比统计

- 19 画质管理 2026-10-03 已按本地架构实现三档预设 IQualityService 本机偏好与独立帧率策略 不移植原 308 文件闭包 具体界面绑定 PlayMode 与真机验收仍待完成 以 [画质模块说明](../Assets/MieMieFrameTools/Scripts/Frame/A_Frame/Business/Quality/README.md) 为权威入口

- 13 多语言与字体 已按本地架构实现独立语言服务 Luban 文本适配 TMP 绑定与语言主字体映射
- 字体工作台从顶点数工程迁入独立 Editor 程序集 不整体引入 A 侧的 UI 画质与屏幕适配依赖
- 原模块的源码依赖闭包不等于该功能的必要依赖
- 接入与资源责任以 [多语言与字体说明](../Assets/MieMieFrameTools/Scripts/Frame/C_Data/Localization/README.md) 为权威入口
- 当前四程序集离线编译与三十四项核心逻辑检查通过 十二项 Unity 原生字体烘焙与 TMP 渲染检查通过
- 追加运行时绑定原生检查被其他任务的日志脚本编译错误阻断 未运行 PlayMode 与目标平台验收 详细范围见权威说明的验证记录
