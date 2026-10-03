# 开发模式与 Play 下持续开发

Bridge 1.18。用户选择持续适用于当前任务；未选择时不主动传auto，让CLI读取工程本机默认。
优先级为共享guard > 显式 `--development-mode auto|preserve|free` > `FUC_DEVELOPMENT_MODE`任务环境 >
工程 `.fuc/development-settings.json` 默认 > 内置auto。子进程继承任务环境；没有覆盖参数的新session自动读取已保存默认。

Unity菜单 **Window/FakeUnityCLI** 提供当前工程默认模式/继承、来源和共享保护状态。
设置仅存于 `<UnityRoot>/.fuc/development-settings.json`，schema_version=2.0，default_mode为auto/preserve/free，
空字符串表示继承内置auto。工程应忽略 `.fuc/`；mining已有 `UnityProject/.fuc/`。
不再读取或写入用户目录，也不保留用户级fallback；不同工作副本的设置互相独立。
保存只影响后续CLI调用，不切Play、不编译、不修改Unity脚本偏好、不建立或释放guard。
未保存选择遇到外部修改时拒绝覆盖，重新加载后再保存。

CLI `editor preferences-get` 查询本机默认及最终策略；
`editor preferences-set <auto|preserve|free|inherit> --yes` 保存或清除当前工程默认，
target仅支持project（默认），--target user返回迁移说明。没有可验证工程根时不写配置。
状态和操作结果显示development_policy的effective_mode/source与selected_mode/selected_source。
损坏或未知版本设置采用preserve并报告settings-error，禁止悄悄重置；显式任务模式仍按优先级处理。

| 模式 | 用户意图 | 必须 Edit 的步骤 |
|---|---|---|
| auto 自动 | 尽量保持运行，继续独立开发 | 延后，继续不依赖此步骤的工作 |
| preserve 现场保护 | 保留当前现场 | 拒绝相关改动 |
| free 自由开发 | 能在当前模式执行就直接执行 | 只有明确需要Edit才停止Play；不根据一般失败重发，不自动恢复Play |

用户明确要求保留现场时，用 `editor guard-begin --owner ... --reason ... --yes` 建立共享记录。
本机默认preserve或单次preserve参数不建立共享guard；另一任务的free不解除已经建立的共享保护。
只有用户明确释放，才对原 guard ID 执行 guard-release。能力限制、对象身份、事务与未知结果规则在所有模式保留。

## 外部文件和生成器

`editor operation-check <scope>` 只读预检；`editor operation-prepare <scope>` 在实际执行前检查，
free的edit-required准备可停止Play；import/disk-generate等已支持范围保持当前模式。只把 `data.allowed=true` 当成通过；deferred/denied 均为非零退出码。
这是瞬时检查，不是跨进程文件锁；调用方在发布前再次检查并保留原有 Policy/CAS/Validator/事务。

| scope | 使用边界 |
|---|---|
| read | 已确认只读的观察，不授权任意代码 |
| source-write | 普通 C# 源码保存，不包括DLL、Package、asmdef/asmref或Bridge替换 |
| disk-generate | 项目适配器已核实的磁盘生成操作 |
| edit-required | 明确要求Edit的操作、部署、程序集结构调整及尚未细分的生成链 |
| runtime-write | 预检已支持的运行时实例修改；实际执行用 runtime-inspector-set |
| import | free支持稳定Play中的导入准备，auto仍保持原有Edit门槛；实际导入走正式命令 |

auto 在稳定Play下保存普通源码，要求当前Editor明确报告支持读取的 `recompile_after_finished_playing` 设置。
设置未知只影响该放行路径。free 保存源码接受中断风险，不根据偏好未知阻塞；不自动改变Unity偏好。
生成器不能仅凭“没启动Unity”自称纯磁盘。auto的disk-generate放行需要受控项目适配器提供
`verified_pure_disk=true,runtime_consumed=false,triggers_import=false` 的 `--values` 声明；声明必须反映实际步骤。
没有证据时使用edit-required，不拆开必须一致提交的输出。free也不豁免项目生成/发布事务。

## 运行控制与待验收

自由开发授权CLI在目标任务确需Edit时自行停止Play，无需另问一次。停止请求绑定目标Editor实例/session，
停止后等待编译、重载、导入和其他任务请求完成，重新检查身份与保护。超时或未知只查询原stop request，
不重复发送，不杀进程、不自动重启、不自动恢复Play。已完成或待处理的原请求不因重查再次停止Play。
Prefab资产修改用结构化editor prefab-edit/inspector-set；free在稳定Play中直接隔离编辑并保存/撤销，
仍检查源hash/身份/用户PrefabStage。运行场景dirty不导致停止或保存用户Scene。
free的inspector-set遇到带本轮Play身份的场景目标时转runtime-inspector-set，不自动持久化运行时实例。
Prefab及持久Inspector写入显式request-id须32位GUID，CLI在任何模式变更前校验。
free的refresh、无await-compile的import、外部普通文件import/undo可保持稳定Play；
import --await-compile、显式编译、EditMode测试及Bridge替换仍包含Edit前置。
未知、部分写入、hash不符、保护和普通异常不触发停止后重试；结果未知只查询原request。
离线component/game-object资产命令的Editor文件锁规则不因free自动绕过；不要把free等同于--force。
Dry-run不停止Play；Play场景对象身份在退出Play后可能失效，应重新检查，不能自动把运行时对象映射成源资产。

返回 `validation_pending` 后继续独立工作，明确“磁盘代码已更新，当前Play尚未加载，编译/运行验证待完成”。
用户任务要求的正式编译与运行验收仍须完成：使用本轮baseline之后的新generation、身份、完整日志与零错误。
单次mode错误只延后该步骤和依赖步骤；不能把旧编译成功或落盘成功报告成验收成功。
旧 `editor guard-check` 无模式上下文，仍只允许无保护且空闲Edit或可靠离线，用于尚未迁移的调用方。

## 运行时实例属性

先在本轮Play用 inspector-get取得完整target，再 `editor runtime-inspector-set --values-file ... --dry-run`，
确认后用新request ID及 `--yes`执行。target包含scene、object/component ID、session、source和
`play_mode_observed_since`；不要将旧回执的epoch替换成当前值。可带expected_values验证写前值。

支持简单数值、布尔、字符串、枚举、颜色、向量、Rect、Bounds，精确字段以当前Inspector支持为准。
Pause允许实例属性写入，UI点击仍拒绝Pause。禁止asset目标、对象引用和ManagedReference子字段。
不保存Scene、不Apply Prefab；退出Play不承诺保留，组件回调仍可能影响运行。检查applied和verified及after，
回读不匹配不能视为成功；outcome_unknown只查询原request，不换ID重发。
