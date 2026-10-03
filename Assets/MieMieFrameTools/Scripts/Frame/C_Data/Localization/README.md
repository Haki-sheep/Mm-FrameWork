# 多语言与字体

本模块采用自研文本服务与 TMP 主字体映射
不引入 Unity Localization 不搬入原子化框架的管理器 UI 画质与屏幕适配依赖

## 接入

1. 修改 `DataTables/Data/localization_texts.json` 的 Locale Key Text 并运行 `DataTables/gen.bat`
2. Unity 菜单 `Tools/MieMieFrameWork/工具中枢` 选择 `多语言与字体/创建默认字体映射` 并执行操作 创建简中与英文的 Body 字体映射
3. 在场景选择 ModuleHub 根节点 在中枢选择 `多语言与字体/接入选中的框架根节点` 并执行操作
4. 选择 TextMeshProUGUI 文本 在中枢选择 `多语言与字体/绑定选中的 TMP 文本` 并执行操作 修改组件的文本 Key 与字体样式
5. 手动保存场景或按工程流程应用 Prefab 修改 工具不自动覆盖框架 Prefab

游戏内按钮可绑定 `LubanLocalizationHost.SetLocale(string)` 参数为 `zh-CN` 或 `en`
代码通过 `GameHub.Get<ILocalizationService>()` 获取服务 先等待 `ReadyTask` 再调用 `SetLocaleAsync`
业务按钮应在 `IsSwitching` 时禁用 重复并发请求明确报错 不排队或静默覆盖
动态文案调用 `LocalizedTMPText.SetArguments` 原始查询使用 `GetText` 参数格式化使用 `FormatText`

## 生命周期与依赖

- LocalizationHost 在 Start 确认 ModuleHub Awake 已完成根节点排重 再组装服务并等待 ReadyTask 加载语言数据
- 不修改 ModuleHub 的管理器注册与启动顺序 宿主应挂在同一个框架根节点
- 文本与字体接口位于 Runtime 文本表适配器位于 Data.Luban UI 绑定组件位于 UI 程序集
- GameHub 只定位公共服务 不拥有资源 宿主销毁时注销自身服务并取消加载 归还服务持有的字体租约
- LocalizedTMPText 在 Awake 缓存组件 Start 从 GameHub 绑定服务 不序列化场景宿主引用 可用于独立 UI Prefab
- 首次 Start 与后续 OnEnable 订阅 LocalizationEvents.LocaleChanged 在 OnDisable 取消等待并解除订阅
- 服务先加载目标语言全部字体 再保存选择并提交语言 通过 MmGlobalEventBus 发布事实通知 最后归还旧语言的服务租约
- 消费者通过 AcquireFontLease 持有独立字体租约 换字库成功后归还旧租约 全部消费者归还后才释放实际句柄
- 仅停用绑定组件时 TMP 可能仍在显示 字体租约继续保留 重新启用刷新后归还旧租约
- 单独销毁绑定组件时先恢复 TMP 原始字体与材质并重建 再释放本组件租约
- 加载失败或取消保留旧语言 字体和设置 取消服务生命周期后返回的未提交租约也会释放
- 通知采用事件中心的主线程同步 Fail fast 契约 监听错误原样传播 后续监听不执行 不聚合或吞异常
- 通知失败时语言已经提交 未更新的文本仍持有旧租约 不会因此提前卸载旧字体 请修正错误后显式刷新这些文本

## 语言数据

语言标识使用 CultureInfo 名称 文本 Key 区分大小写
同一 Locale 与 Key 不允许重复 每个语言必须覆盖全部 Key 翻译不能为空
语言表默认全量加载 字体按语言加载 不包含自动翻译 复数规则 RTL 排版或语言自动识别
缺语言 缺 Key 参数格式错误均明确报错 不静默返回空白或默认语言
用户选择使用宿主配置的 PlayerPrefs Key 不改变游戏存档结构 失效的已保存语言需要显式迁移或清理该 Key
默认数据通过 UnityWebRequest 读取 StreamingAssets 以兼容 Android 与 WebGL 的路径形式

## 字体资源

LocalizationFontCatalog 每条配置为 Locale Style 与字体来源
Font 与 Address 必须二选一 不根据字体名称拼接资源路径
Font 直接引用适合首版验证 所有被配置直接引用的字体可能随配置驻留 租约不销毁它们
Address 使用已初始化的 YooAsset 默认包 须自行配置资源采集与地址 租约只释放自己持有的句柄
UI 切换的是主字体 fallback 只用于补字 不把 fallback 插入当作主字体切换
租约与事件操作仅限主线程 不在公共服务接口提供跨模块 C# event
每条样式可额外配置 Material 或 MaterialAddress 二选一 两者均留空时使用主字体默认材质
描边阴影等工作台生成的样式材质应加入对应语言样式映射 切换时保持其效果
材质必须属于该主字体的图集 地址模式的字体和材质句柄由同一个语言租约持有并释放
动态字体必须保留源 TTF 固定文案优先使用按语言收集后烘焙的静态字库

## 字体工作台

菜单 `Tools/MieMieFrameWork/工具中枢` 选择 `多语言与字体/字体管理器` 并执行操作打开完整字体工作台
从顶点数工程的 FontManager 移植 字体烘焙 诊断 预览 优化和原位更新保持原实现
工具位于 Editor 独立程序集 复用宿主的 Odin TMP UGUI UniTask 与 Newtonsoft 不携带第三方副本
烘焙配置中选择 Locale 并启用收集 Luban 语言表 可配合富文本过滤与忽略占位符收集真实文案
占位符提供的运行时字符不在固定文案里 数字通过常用字符补充 任意用户输入应配置动态字库或明确的字符来源
输出位于 `Assets/MieMieFrameTools/ADefaultRes/Fonts` 下的 Profiles Generated Backups
原位更新只允许兼容的图集子资源数量 不兼容时明确要求另存新字库
移植工具保留原有公开字段与序列化字段名称 不为样式统一重命名

## 首版验收

生成语言表 编译 Editor 与 Player 程序集
验证简中英文查询 参数格式化 切换失败取消 并发请求与资源释放
验证动态 UI Prefab 启用停用期间的事件与字体租约 字库文案收集 缺字诊断与烘焙原位更新
Android WebGL 读取与 YooAsset 地址模式仍需在对应目标平台验收 不以桌面验证代替

## 本次验证记录

- DataTables/gen.bat 成功生成简中与英文共十条翻译
- 使用本机 Unity 6000.4.10f1 的 Roslyn 与真实依赖离线编译 Runtime Data.Luban UI FontManager.Editor 四个程序集成功 不能替代 Unity 全量编译
- 核心服务离线回归三十四项通过 覆盖切换 取消 并发 拒绝重复初始化 失败保留状态 缺翻译与消费者租约责任
- Unity CLI 批处理执行 FontManagerVerification.RunBatch 退出码零 十二项通过 含真实字体烘焙与 TMP 渲染 输出 Logs/FontManager-verification.txt 和 Logs/FontManager-preview.png
- 追加 LocalizationRuntimeVerification.RunBatch 用于真实数据读取与动态绑定生命周期契约检查 该次 Unity 编译受其他任务的 FrameLogMenu.cs 中 PlayModeStateChange.ExitedPlayMode 错误阻断 退出码一 验证主体未运行 不计通过
- 独立评审发现的动态 Prefab 场景引用 旧字体提前释放 根节点排重时序与跨模块 event 均已修复 受影响项复核未发现残留 P0/P1
- 最终结论为条件通过 仍未验收 PlayMode 全流程 动态面板实际加载 重复根节点场景切换 YooAsset 地址卸载 Android 与 WebGL
- 本次未修改用户框架 Prefab 或场景 通过接入菜单完成挂载后须按项目流程保存
- Luban 生成 Tables.cs 含工具原生尾部空格 git diff --check 会报告该处 未手改生成代码消除报告
