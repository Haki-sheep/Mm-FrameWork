# 统一日志与诊断

## 职责与依赖

`MieMieFrameWork.Diagnostics` 提供双端日志门面 `FrameLog` 与不调用 Unity API 的线程安全 `LogSession`
门面和核心只依赖 C# 标准库 输出通过 `Action<LogRecord, object, Exception>` 注入
Unity 适配文件使用 `UNITY_5_3_OR_NEWER` 编译条件 不依赖 ModuleHub YooAsset UI Firebase 小游戏 SDK

逻辑分为门面与核心两层
- 门面 `FrameLog.cs` 负责唯一会话 生命周期 业务入口与输出委托
- 核心 `LogSession` `LogRecord` `LogSettings` `ELogLevel` `FileLogWriter` 和 `Core` 中的颜色类型负责记录与存储
- `FrameLog.Unity.cs` `FrameLogPump.cs` `LogProfile.cs` 为 Unity 适配 `ConsoleLogOutput` 为纯 C# 终端输出

现有文件路径暂时保留 本机 Unity 6000.4.10f1 的 CLI 离线资产移动返回 `FUC_VERSION_UNSUPPORTED`
未绕过版本限制搬动脚本或 meta 当前完成逻辑分离而非整个 Runtime 的两文件夹搬迁

本阶段完成统一日志入口 过滤 有界近期记录 Unity 日志采集 可选文件轮转与导出
对象池 FSM 动态图集 YooAsset 和 PerformanceTrace 保留原有诊断入口
暂不提供这些模块的统一快照 运行时面板或远程上报

## 启动与释放

- `SubsystemRegistration` 只清理上一运行会话
- Unity 的 `AfterAssembliesLoaded` 唯一自动启动入口 读取可选配置并调用通用 Init 早于事件桥接的 BeforeSceneLoad 和场景 Awake
- 后端由应用启动入口显式调用 `FrameLog.Init` 不执行 Unity 的启动钩子
- 再次 Init 会报错 必须先停止日志生产并 Shutdown 不能覆盖活动会话
- Init 与 Shutdown 必须由宿主同一生命周期线程串行调用 不支持并发初始化或并发重启
- 初始化完成后才能启动日志生产 Shutdown 前须停止并等待所有业务日志线程结束
- LogSession 的线程安全和并发日志写入验证不代表门面生命周期可以并发调用
- 自动创建隐藏的 DontDestroyOnLoad 采样节点 Update 缓存帧号 后台采集不读取 Unity 对象或 Time
- 帧号表示最近一次主线程采样值 不保证等于后台日志发生瞬间的帧号
- 应用暂停刷新文件 Application.quitting 解除订阅并释放会话
- Editor 在重新进入 Edit Mode 和程序集重载前清理 不保存或切换用户场景
- 不在日志函数或属性访问器中初始化 未启动或已退出的业务调用立即抛错
- 业务线程必须在应用退出前停止日志生产 退出期间迟到的 Unity 采集回调允许放弃
- 近期记录只保存字符串与数值 不持有业务对象 object context 与 Exception 仅传入当次输出委托
- Unity 的 context 仍须为 UnityEngine.Object 后端可传自己的对象 门面与缓存均不保留它
- 输出委托在记录线程同步调用 输出异常原样传播 已存入核心的记录不撤销

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

需要自定义时用 Create / MieMieFramework / 日志配置 创建 `LogProfile`
将资产放到任一 Resources 目录下并命名为 `MieMieLogProfile`
资源路径以 `FrameLog.ProfileResourcePath` 为准 配置仅在会话启动时读取一次
工程已提供 `ADefaultRes/Resources/MieMieLogProfile.asset` 显式最低级别为 Info 包括普通 Player
默认资产位置见 [默认资源](../../../ADefaultRes/README.md) 无配置时才采用上表的普通 Player Warning 策略
非法配置或文件权限错误直接暴露 不静默关闭文件 不初始化第三方 SDK

本模块使用 Resources 只加载这个启动配置 不承担业务资源加载 也不需要等待 YooAsset 就绪

## 业务调用

```csharp
using MieMieFrameWork.Diagnostics;

FrameLog.Log("普通日志");
FrameLog.Log("资源就绪", ELogColor.Green, "Asset");
FrameLog.Info("进入主菜单", "GameFlow");
FrameLog.Warning("背景音乐未配置", "Audio");
FrameLog.Error("页面加载失败", "UI", this);
FrameLog.Exception(Exception, "Asset");
FrameLog.SetContext("GameState", "MainMenu");
FrameLog.SetChannelEnabled("Audio", false);
FrameLog.SetMinimumLevel(ELogLevel.Warning);
```

`Log(string message, ELogColor color = ELogColor.White, ...)` 是普通日志入口 与 Info 级别相同
旧 Info Warning Error 渠道与 context 调用方式保留 需要改色时使用命名参数 `color`

```csharp
FrameLog.Warning("需要关注", color: ELogColor.Orange);
FrameLog.Error("指定显示颜色", "UI", color: ELogColor.Violet);
```

### 颜色

| 枚举 | 颜色 | RGB |
| --- | --- | --- |
| Red | 红 | FF0000 |
| Orange | 橙 | FF8000 |
| Yellow | 黄 | FFFF00 |
| Green | 绿 | 00FF00 |
| Blue | 蓝 | 0000FF |
| Indigo | 靛 | 4B0082 |
| Violet | 紫 | 8B00FF |
| Black | 黑 | 000000 |
| White | 白 | FFFFFF |

Log Info Trace 默认白 Warning 默认黄 Error Exception 默认红
颜色只影响显示 不改变级别 渠道或过滤结果 七彩指七种可选颜色 不自动循环或逐字渐变
Unity 普通消息使用 color 富文本 原始异常保持 Debug.LogException 原对象与 Unity 原生异常显示
LogRecord.Message 保持纯文本 Color 单独保存 文件与导出增加颜色枚举名称 不加入富文本或 ANSI
黑色在深色背景以及白色在浅色背景上的可见性由使用者选择颜色时考虑

### C# 后端

将 Runtime 下所有 C# 源文件复制到后端项目 保持未定义 UNITY_5_3_OR_NEWER 即可直接编译
不需要 Unity DLL 不复制 Unity asmdef 与 meta Unity 适配文件会被编译条件排除

```csharp
FrameLog.Init(new LogSettings(), ConsoleLogOutput.Write);
try
{
    FrameLog.Log("服务启动");
    FrameLog.Log("连接建立", ELogColor.Green, "Network");
    FrameLog.Warning("请求超时", "Network");
}
finally
{
    FrameLog.Shutdown();
}
```

ConsoleLogOutput 在支持 ANSI 真彩色的终端输出九种 RGB 颜色 输出重定向时仅输出纯文本
也可在 Init 传入自己的 Action 连接已有后端日志库 空委托只保留缓存与可选文件
EnableFile 开启时 Init 必须传入 fileDirectory 后端不使用 Unity persistentDataPath
OutputToUnity 与 CaptureUnity 仅由 Unity 适配读取 后端输出由注入委托决定

`Exception` 示例参数应传入实际捕获的异常对象 日志不替代 throw
调用位置由 CallerMemberName CallerFilePath CallerLineNumber 自动记录
原始异常对象原样传给 Unity 内层异常与原始堆栈通过 ToString 进入缓存
缓存超过字符预算会明确截断 Unity 异常输出仍使用原始对象

`Trace` 在普通 Player 编译时连调用参数一起裁剪 开发环境还需将最低级别设为 Debug 才会记录
后端需要 Trace 时显式定义 FRAME_LOG_TRACE 并将最低级别设为 Debug 未定义时参数求值同样裁剪
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
- 文件中的常规记录包含 UTC 序号 帧 线程 级别 渠道 颜色 调用位置和异常文本 最新上下文通过手动导出获取
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
