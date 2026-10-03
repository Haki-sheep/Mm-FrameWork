# 统一日志与诊断

## 职责与依赖

`MieMieFrameWork.Diagnostics` 提供游戏日志门面 `FrameLog` 与不调用 Unity API 的线程安全 `LogSession`
只依赖 Unity 原生运行时 不依赖 ModuleHub YooAsset UI Firebase 小游戏 SDK 或服务器后端

本阶段完成统一日志入口 过滤 有界近期记录 Unity 日志采集 可选文件轮转与导出
对象池 FSM 动态图集 YooAsset 和 PerformanceTrace 保留原有诊断入口
暂不提供这些模块的统一快照 运行时面板或远程上报

## 启动与释放

- `SubsystemRegistration` 只清理上一运行会话
- `AfterAssembliesLoaded` 唯一启动入口 读取可选配置并创建会话 早于事件桥接的 BeforeSceneLoad 和场景 Awake
- 自动创建隐藏的 DontDestroyOnLoad 采样节点 Update 缓存帧号 后台采集不读取 Unity 对象或 Time
- 帧号表示最近一次主线程采样值 不保证等于后台日志发生瞬间的帧号
- 应用暂停刷新文件 Application.quitting 解除订阅并释放会话
- Editor 在重新进入 Edit Mode 和程序集重载前清理 不保存或切换用户场景
- 不在日志函数或属性访问器中初始化 未启动或已退出的业务调用立即抛错
- 业务线程必须在应用退出前停止日志生产 退出期间迟到的 Unity 采集回调允许放弃
- 近期记录只保存字符串与数值 不持有 Unity 对象 Object context 仅用于当次控制台输出

## 默认配置

无需创建资产即可运行

| 配置 | 默认值 | 语义 |
| --- | --- | --- |
| MinimumLevel | Editor 与 Development 为 Info 普通 Player 为 Warning | 控制记录与门面控制台输出 |
| DisabledChannelList | 空 | 启动时禁用的模块渠道 对所有级别生效 |
| RecentCapacity | 512 | 环形缓存条数 满后覆盖最旧记录 |
| MaximumTextLength | 32768 | 消息 堆栈 渠道与调用位置分别限长 超长附 TRUNCATED 标记 |
| EnableFile | false | 可选项目独立文件记录 |
| MaximumFileBytes | 4194304 | 当前写入文件的字节预算 最少 1 MiB |
| MaximumFileCount | 4 | 固定轮转槽位数 |
| OutputToUnity | true | 门面日志是否同步输出 Unity 控制台 |
| CaptureUnity | true | 是否采集 Unity 与第三方原始日志 |

需要自定义时用 Create / MieMie / 日志配置 创建 `LogProfile`
将资产放到任一 Resources 目录下并命名为 `MieMieLogProfile`
资源路径以 `FrameLog.ProfileResourcePath` 为准 配置仅在会话启动时读取一次
非法配置或文件权限错误直接暴露 不静默关闭文件 不初始化第三方 SDK

本模块使用 Resources 只加载这个启动配置 不承担业务资源加载 也不需要等待 YooAsset 就绪

## 业务调用

```csharp
using MieMieFrameWork.Diagnostics;

FrameLog.Info("进入主菜单", "GameFlow");
FrameLog.Warning("背景音乐未配置", "Audio");
FrameLog.Error("页面加载失败", "UI", this);
FrameLog.Exception(Exception, "Asset");
FrameLog.SetContext("GameState", "MainMenu");
FrameLog.SetChannelEnabled("Audio", false);
FrameLog.SetMinimumLevel(ELogLevel.Warning);
```

`Exception` 示例参数应传入实际捕获的异常对象 日志不替代 throw
调用位置由 CallerMemberName CallerFilePath CallerLineNumber 自动记录
原始异常对象原样传给 Unity 内层异常与原始堆栈通过 ToString 进入缓存
缓存超过字符预算会明确截断 Unity 异常输出仍使用原始对象

`Trace` 在普通 Player 编译时连调用参数一起裁剪 开发环境还需将最低级别设为 Debug 才会记录
运行时关闭渠道不能撤销调用前已经发生的字符串插值 昂贵消息应先查询

```csharp
if (FrameLog.IsEnabled(ELogLevel.Debug, "UI"))
    FrameLog.Trace(BuildExpensiveDebugText(), "UI");
```

建议渠道为 Boot Module Asset UI Audio Save Event GameFlow
渠道代表模块分类 不是发行渠道

## Unity 原始日志采集

`Application.logMessageReceivedThreaded` 的消息统一进入 Unity 渠道
采集仅写缓存与可选文件 不再次调用 Debug 防止递归
门面同步输出使用线程局部标记防止重复采集 标记通过 finally 恢复
级别与渠道过滤覆盖所有记录来源 包括 Error 和 Exception

过滤第三方原始日志仅影响本模块的记录 不拦截它们原本的 Unity 控制台输出
不修改 Debug.unityLogger 全局配置 不改写第三方源码

## 导出与文件预算

- Editor 菜单 `Tools / MieMieFrameWork / 工具中枢` 选择 `日志与诊断 / 日志导出` 仅在会话存在时允许导出
- 运行时使用 `FrameLog.ExportRecent(outputPath)` 指定一个不存在的新文件 父目录须已经存在
- 导出含会话 ID 已接受与保留条数 应用版本 Unity 版本 平台 当前上下文以及近期记录
- 导出使用 CreateNew 现有文件不会被覆盖 保存对话框选择已有文件也会报错
- 可选实时文件目录为 `Application.persistentDataPath/MieMieLogs`
- 仅覆盖 `runtime-0.log` 至 `runtime-N.log` 固定槽位 不枚举或批量删除用户文件
- 新会话从槽位 0 开始 每个文件头包含会话 ID 轮转顺序不能只按槽位名称判断
- 文件中的常规记录包含 UTC 序号 帧 线程 渠道 调用位置和异常文本 最新上下文通过手动导出获取
- Error 与 Exception 立即刷新 应用暂停和退出也刷新 不承诺系统强制结束时零丢失
- 配置减少槽位数或文件预算时 不自动删除或缩小旧会话遗留槽位 新写入文件遵守新预算
- 不记录账号令牌等敏感数据 导出仅发生在明确调用或菜单操作时 不自动上传

文件 IO 在记录线程同步执行 默认关闭 高频使用应依据目标设备实测决定是否启用
不承诺日志热路径零分配 不在业务 Update 中逐帧导出或复制近期快照

## 本项目接入边界

BootManager 更新 BootStage 并记录启动开始与就绪
ModuleHub 记录当前初始化管理器并统一可选 UI 告警
YooAsset RuntimeAdapter 记录包初始化与资源版本 不改原生资源句柄责任
UIHub 加载错误 AudioManager 与 ArchiveManager 初始化记录分别使用自己的渠道
EventBusUnityBootstrap 只注入错误日志委托 事件核心仍不依赖 Unity 或本日志程序集
未修改存档格式 存档写入规则 管理器排序或 ReadyTask 成功失败语义

验证范围与未完成项见 [验证记录](验证记录.md)
