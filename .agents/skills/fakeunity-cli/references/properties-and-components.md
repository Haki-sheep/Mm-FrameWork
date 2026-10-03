# 属性与组件参考

## 先查询语义契约

修改组件前先查询当前工程 schema，不从 YAML 或旧示例猜字段：

```text
<fuc> schema describe <组件类型> --project <root> --json
```

所有 Scene/Prefab Component 只使用查询返回的 canonical 类型、语义字段和默认值。目标路径由
`game-object`/`component` 命令自动识别为 Scene 或 Prefab；单个明确变更直接执行，多个依赖变更使用
`tx plan|apply`。

## Raw serialization plumbing

```text
<fuc> asset set-property <资产绝对路径> --file-id <文档fileID> --property <propertyPath> --value <值> --project <root> [--force] --json
```

该入口仅是没有专用契约的非对象资产底层 plumbing。Scene/Prefab 的 GameObject、Transform、Component、
层级和引用不得通过 raw property path 创作。任何 `m_*` 都不是 Agent 对象创作输入；不得读取后照抄、主动
填写或在语义写入失败后用 raw 命令兜底。

## 统一对象与组件契约

```text
<fuc> component coverage --project <root> --json
<fuc> component add <目标.prefab|目标.unity> --object <selector> --type <schema类型> [--values <语义JSON>] --project <root> --json
<fuc> component get <目标.prefab|目标.unity> --component <selector> --project <root> --json
<fuc> component get <目标.prefab|目标.unity> --component <selector> --property <语义字段> --project <root> --json
<fuc> component set <目标.prefab|目标.unity> --component <selector> --values <语义JSON> --project <root> --json
<fuc> component remove <目标.prefab|目标.unity> --component <selector> --project <root> --json
```

读取时优先选择最小必要粒度：读取整个组件时省略 `--property`，只需要一个字段时显式指定 `--property`。
`component get` 一次读取一个有 authoring contract 的组件。`game-object get --object <selector>` 的
`components[]` 会直接给出每个组件的 `file_id`、`class_id`、`type_name`、`managed_type` 和脚本身份，但不会
一次展开所有组件的语义字段。需要全部组件数据时，根据这些身份逐个执行 `component get`；名称明确时优先使用
`/对象路径::组件类型`，不必先转换为 fileID。

工程自定义 MonoBehaviour 会从 `Library/ScriptAssemblies` 和脚本元数据生成项目级动态 contract。先执行：

```text
<fuc> schema describe SkillCard --project <root> --json
<fuc> component get <目标.prefab|目标.unity> --component /Root/Card::SkillCard --project <root> --json
```

动态 contract 当前公开 `enabled`、标量、字符串、整数形式枚举和 Unity Object 引用。`schema describe` 与
`component get` 会披露 `dynamic_contract`、`schema_stale` 和 `unsupported_fields`；数组、复杂结构与
`SerializeReference` 字段继续 fail closed。脚本源码比已编译程序集新时允许诊断读取，但所有动态 contract
写入都会拒绝，先让 Unity 完成编译。只有静态和动态 contract 都无法提供所需诊断字段时，才使用带 `--full`
和输出预算的 `scene read`/`prefab read`；不得用其输出进行 raw 对象写入。

Unity Object 引用字段使用稳定选择器，CLI 在写入前解析并校验目标类型与唯一性：

```json
{
  "evolutionLevelBackground": {"object": "/Root/EvoBg"},
  "evolutionBaseSkillIcon": {"component": "/Root/EvoBg/SkillIcon::Image"}
}
```

跨 Prefab Asset 的组件引用使用：

```json
{"relatedCard":{"asset":"Assets/UI/Related.prefab","component":"/Related::SkillCard"}}
```

同资产和跨 Prefab 引用会由 `component get` 以相同的 `object`/`component`/`asset` shape 回读；写结果的
`resolved_references` 给出字段、目标、资产、GUID、fileID 和期望类型。仍兼容底层 `{"fileID":123}`；`null`
会清空引用。跨 Scene 引用、缺失、歧义或类型不匹配会在 WritePipeline 前零写入拒绝。

`component coverage` 只用于发现 contract 覆盖范围；字段、范围、引用角色和默认值仍以各类型的
`schema describe` 为准。Transform 与 RectTransform 是固有组件，不使用 `component add/remove`；通过
`component get/set` 修改其语义字段。欧拉角由 CLI 映射为合法旋转，Agent 不计算四元数。

数组、UnityEvent、布局、物理、音频和 UI 不是独立命令域；只有相应 contract 公布语义字段时才能通过
`component set` 表达。缺少字段或操作时报告 coverage gap，不调用旧分域命令。

对 Unity Behaviour，只有 schema 返回 `enabled` 时才可启用或禁用；不要假定所有 Component 都有该字段。
UnityEvent 和 options 也是普通 Component 递归语义字段。Button、Toggle、Slider、Scrollbar、Dropdown、
InputField、ScrollRect 与 TMP 控件只通过各自 schema 公布的事件数组整体替换监听列表；Dropdown options 使用
`{text,image}` 数组整体替换。嵌套必填字段、范围、引用角色和事务 `$ref` 形式以 `value_schema` 为准。

多个相关变更使用 `tx plan` 与 `tx apply`。plan 会验证内存中的计划后资产并返回问题差分；apply 会再次验证
封存结果后才写盘。apply 后只执行一次语义回读并保留 operation ID/undo。需要 Unity 权威证据时再运行
`asset validate <目标> --level editor|playmode --project <root> --json`；环境跳过不是通过，业务回调不在通用探针覆盖内。
