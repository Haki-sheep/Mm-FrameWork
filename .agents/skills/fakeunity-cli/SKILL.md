---
name: fakeunity-cli
description: 使用 FakeUnityCLI 检查 Unity 工程 修改正式 Profile 资产 或通过 2022.3 与实验性 U6 Bridge 操作 Editor 内存态 用于资产编辑 Bridge 初始化 引用诊断 编译与日志 不替代项目业务规则
---

# FakeUnityCLI

把 CLI 作为目标 Unity 文本资产与 Editor 自动化的操作入口。按任务选择线路，只读取对应 reference；
精确参数、字段支持性和动态命令以当前 schema、组件查询、Provider/Template describe 为准，不默认加载完整目录。

## 入口

- 工程提供统一启动脚本时使用它，由本机配置选择开发版或 Release；不把维护者源码路径写进共享文档。
- 独立 Skill 默认程序为 `runtime/windows-x64/fuc.exe`、`runtime/linux-x64/fuc` 或 `runtime/macos-arm64/fuc`。
  用户明确要求开发版时使用源码构建。不自动发布或下载其他二进制替换工程入口。
- 发布包核对 `runtime/build-manifest.json` 未列出的平台不可用 写前查 capabilities/preflight
  U6 Bridge 可安装不等于离线资产可写
- 每次加 `--json`。没有启动脚本的明确绑定时，加 `--project <Unity根目录>`；显式项目优先于部署推断。
- 用户未选开发模式时不主动传 auto；CLI读取本机默认，Unity菜单 Window/FakeUnityCLI 可调整。
- “初始化 FakeUnityCLI”表示安装/更新 Editor Bridge：先 status/guard-check，再 bridge-update dry-run；
  支持 live-update 时用 --yes 受控 Reload。首次或旧 Bridge 才在安全关闭 Editor 后 bootstrap，详见在线 reference。

## 安全底线

1. 不直接读写或搜索目标 .unity/.prefab/.asset/.mat 与 ProjectSettings YAML，不手写 meta 或桥接队列/receipt。
2. 资产创作用 schema 公布的语义字段；`m_*` 不是 Agent 创作接口。只发送需求涉及的最小字段，
   不照抄序列化对象。单对象用统一 game-object/component，多步依赖用 tx plan/apply。
3. 层级完整性看 depth 和 depth_truncated_children，不为了完整层级开 --full；收窄查询并检查截断状态。
4. 离线资产与在线内存态分开报告。在线先 status，结构化能力优先，其次项目 Provider/锁定 Template，
   最后才受约束 Roslyn；能力缺口不能用 YAML、旧命令或另一传输绕过。
5. 用户要求保留现场时使用 guard-begin/status；取得结果、超时或 Reload 都不解除保护。
   只有用户明确释放时按原 guard ID 解除。开发用 auto/preserve/free 三模式及 operation-check/prepare；
   详见 [开发模式](references/editor-development-modes.md)。旧 guard-check 保留保守语义，free/--force 不绕过共享保护。
6. 遵守写入口的 --yes/dry-run 合同；批量、跨文件或用户要求预览时先计划。写后做一次对应回读，
   保留 request/operation ID、receipt 和真实恢复边界。UI/业务动作不可逆，不伪造 byte undo 或全局 Undo。
7. expired 表示未执行；processing_timeout/outcome_unknown 表示未知。只查原 request ID，停止自动重发写入。
   不因原请求未知就换 ID、清文件、换工具或关闭 Unity。计划外副作用停该写入并保留证据。
8. Agent 临时 JSON/C#/截图放 `<UnityRoot>/Temp/FakeUnityCLI/`。任务自有 Unity fixture 只有明确测试授权下
   才由 Unity API 创建和清理；不擅自保存用户 Scene、接管 Prefab Stage 或运行测试；free可按任务需要自动停Play。
9. 编译通过须有新 generation、完整 watermark/coverage、无 history_gap、零 errors 和最终 ready；
   空日志、not_observed、旧代或普通 dotnet build 不能代替 Unity 编译。警告如实报告。
10. 回读仅证明数据 涉及导入 编译或行为时补对应验证 未执行层级如实标注

## 按需路由

| 本次任务 | 首选 reference |
|---|---|
| 工程、版本、schema、锁与基础契约 | [environment-and-schema](references/environment-and-schema.md) |
| 兼容范围 发布包身份与分层验证 | [compatibility-and-validation](references/compatibility-and-validation.md) |
| Scene/Prefab 层级和统一对象创作 | [scene-prefab-structure](references/scene-prefab-structure.md) |
| 离线 Component 与属性 | [properties-and-components](references/properties-and-components.md) |
| 在线 Inspector、引用、Curve/Gradient、受控 Prefab | [editor-inspector-prefab](references/editor-inspector-prefab.md) |
| UGUI/TMP 资产布局与引用创作 | [ugui-and-tmp](references/ugui-and-tmp.md) |
| 运行中 UGUI 快照、射线、点击和坐标 | [editor-ui](references/editor-ui.md) |
| Editor、导入、Provider、Template、Roslyn、截图、测试 | [editor-online-roslyn](references/editor-online-roslyn.md) |
| 编译证据、Console cursor、文件日志 | [logs-and-project-refresh](references/logs-and-project-refresh.md) |
| 资产导入、移动、重命名和贴图 | [asset-management](references/asset-management.md) |
| GUID、引用和 Missing 诊断 | [queries-and-diagnostics](references/queries-and-diagnostics.md) |
| Material / ScriptableObject | [material-and-scriptableobject](references/material-and-scriptableobject.md) |
| SerializeReference | [serialize-reference](references/serialize-reference.md) |
| ProjectSettings | [project-settings](references/project-settings.md) |
| Addressables | [addressables](references/addressables.md) |
| Variant 与实例覆盖链 | [prefab-variant](references/prefab-variant.md) |
| 事务、工作流和可逆视图 | [transactions-workflows-views](references/transactions-workflows-views.md) |
| 审计、undo、Profile、插件与自检 | [audit-profile-selfcheck](references/audit-profile-selfcheck.md) |

整体能力才读 [feature-directory](references/feature-directory.md) 业务规则查项目文档
普通 Editor 任务不加载网络 真机或上游目录 Editor 验证不证明 Player 行为
