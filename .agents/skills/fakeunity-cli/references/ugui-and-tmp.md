# UGUI 与 TextMeshPro

本模块用于创建、语义修改和分层验证 Unity 2022.3 UGUI/TMP Scene 与 Prefab。只通过 FakeUnityCLI
操作 UnityProject；默认离线，不连接用户已打开的 Editor、Gateway、设备或外部服务。显式 Editor/Play Mode
验证只会启动隔离临时工程的精确版本 Unity batchmode。

## 权威流程

1. 从用户需求提取精确层级、控件、行为和引用；不补充未要求的业务元素。
2. 对每种所需组件运行 `schema describe`，确认 canonical 类型、语义字段、默认值、范围、枚举和引用约束。
3. 用 `game-object get` 与 `component get` 检查目标。目标路径扩展名会自动识别 Scene 或 Prefab。
4. 单个明确变更直接使用语义 `game-object`/`component` 操作；多个相互依赖的变更先生成 transaction plan。
5. 只发送用户要求且不同于 schema 默认值的语义字段。让 CLI 创建必要默认字段和序列化结构。
6. 检查 `tx plan` 返回的 `asset_validation`；按 operation/object/component/field 诊断修正，不猜测字段。
7. apply 后只执行一次 `game-object get` 或 `component get` 语义回读，并保存 `operation_id` 与 `undo`。
8. 按验收需要运行 `asset validate --level editor|playmode`，分别报告三态；环境跳过和未覆盖业务回调都不是通过。

UGUI/TMP 与物理、音频等组件共享 `GameObject + Component` 公共契约。旧 `ui` 及分域对象命令已移出
公共契约；`prefab compose --manifest` 也不是 Agent 的 UI 创作流程。完整业务 manifest 不再作为中间格式。

## Schema 查询

先查询当前工程的真实契约：

```text
<fuc> schema describe <组件类型> --project <root> --json
```

以返回的 canonical 类型和语义字段为准。schema 未公开的字段视为不可写；不要从 Unity YAML、旧示例、
托管类名或记忆中补全。CLI 负责把语义值映射为 Unity 2022.3 的内部序列化字段及默认值。

`m_*` 只属于 raw serialization plumbing。Agent 不得：

- 将语义值和同义的 `m_*` 值同时发送。
- 为了“完整”而复制默认颜色、渐变、导航、过渡或空引用。
- 从 read 输出中复制 `m_*` 再写回。
- 在语义命令失败后退回 `set-property` 或通用 raw component patch。

这会避免把一个文本或颜色变更膨胀为数百行易过期的序列化快照。

当前资产的分层验证命令为：

```text
<fuc> asset validate <目标.prefab|目标.unity> --level offline|editor|playmode --project <root> --json
```

## 语义节点与组件操作

目标位置参数唯一，CLI 根据 `.prefab` 或 `.unity` 自动选择资产域：

```text
<fuc> game-object get <目标.prefab|目标.unity> --object <selector> --project <root> --json
<fuc> game-object add <目标.prefab|目标.unity> --name <名称> --type rect [--file-id <父级>] --project <root> --json
<fuc> game-object set <目标.prefab|目标.unity> --object <selector> --name <新名称> --project <root> --json
<fuc> game-object remove <目标.prefab|目标.unity> --object <selector> [--yes] --project <root> --json
<fuc> game-object reparent <目标.prefab|目标.unity> --file-id <节点> --value <父级> --project <root> --json

<fuc> component get <目标.prefab|目标.unity> --component <selector> --project <root> --json
<fuc> component add <目标.prefab|目标.unity> --object <selector> --type <schema类型> [--values <语义JSON>] --project <root> --json
<fuc> component set <目标.prefab|目标.unity> --component <selector> --values <语义JSON> --project <root> --json
<fuc> component remove <目标.prefab|目标.unity> --component <selector> --project <root> --json
```

只使用当前 `schema describe` 确认过的参数和值。一次操作只修改一个明确职责；不要为了减少命令次数
生成整个页面的组件快照。

## 多变更事务

创建控件通常涉及节点、RectTransform、Graphic、交互组件和相互引用。存在前后依赖或必须共同成功时，
将这些语义操作放入 transaction plan，而不是编写完整 Prefab manifest：

1. plan 阶段解析全部目标和 schema，建立临时身份与引用。
2. `tx plan` 自身检查计划结果的结构、schema、引用闭包和静态布局约束，并返回问题差分。
3. apply 阶段复核 baseline 后原子写入；任一步失败必须零写入。
4. apply 后只做一次语义回读，不解析底层 YAML。

transaction manifest 的稳定对象 DSL 结构、操作和 `$ref` 规则见
[transactions-workflows-views.md](transactions-workflows-views.md)。不要将 transaction plan 扩展成包含所有默认字段的完整业务页面答案。

字体和事件必须保持可证明：TMP 组件只有在项目中可发现 TMP Settings/default font 时才依赖自动默认值；否则
使用已由 CLI 查询并证明的字体引用，无法证明 subasset fileID 时报告能力缺口。普通 `Text` 的空字体默认值不代表
可渲染。用户未给出业务回调目标和方法时保持 UnityEvent 默认空数组，只声明视觉控件和结构已创建，不声称登录等
业务行为已实现。

## 布局与引用约束

- 分别确定水平轴和垂直轴的控制者。RectTransform、LayoutGroup、LayoutElement、ContentSizeFitter 与
  AspectRatioFitter 不得在同一轴形成冲突或循环。
- stretch 子节点的实际尺寸取决于父尺寸与 inset；不要用孤立的 `sizeDelta` 推断最终像素尺寸。
- LayoutGroup 控制某轴时，子节点应通过 schema 支持的 min/preferred/flexible 语义提供尺寸意图。
- InputField 的 viewport、text、placeholder，ScrollRect 的 viewport、content，以及 Button 的
  targetGraphic 必须引用 schema 允许的目标类型。
- UGUI/TMP Behaviour 仅在 schema 公布 `enabled` 时允许切换启用状态；`CanvasRenderer` 和固有
  RectTransform 不使用该字段。
- 控件 UnityEvent 是监听列表的完整替换语义，例如 `Button.onClick`、`Toggle.onValueChanged`、
  `InputField.onEndEdit` 与 TMP 对应事件。每项必须包含 `target`、`targetAssemblyType` 和 `method`；
  `mode` 为 `0..6`，`callState` 为 `0..2`，可选 `arguments` 包含 object、objectAssemblyType、int、float、
  string、bool。事务中的 target 与 object 参数可使用 `{$ref:"<operation>.gameObject"}` 或
  `{$ref:"<operation>.component"}`。始终以 `schema describe Button` 当前返回的 validation rule 为准。
- 任意已计算负尺寸都属于错误；零尺寸只在对象含视觉、控件或布局 Component 时属于错误。
  EventSystem/InputModule 等非视觉基础设施可保持零尺寸。
- schema 或 `asset validate` 无法证明必要约束时停止并报告能力缺口，不写 raw 字段兜底。

## 验证层级

| 层级 | 能证明 | 不能证明 |
|---|---|---|
| 离线结构/schema 验证 | YAML 可解析、类型和语义字段合法、引用闭包与静态约束成立 | Unity 实际导入、布局重建、脚本行为 |
| Editor/import 验证 | 隔离副本的精确 Unity 导入、Missing Script/引用、布局/TMP 重建和实际 Rect | Play Mode、设备差异、业务流程 |
| runtime/Play Mode 验证 | 隔离场景中的通用控件引用和事件发射探针 | 业务回调、完整输入流程、动画、设备表现 |

默认只完成第一层。显式运行后两层时，只有 `status=passed` 才能声明通过；`environmental_skip` 不是通过。
