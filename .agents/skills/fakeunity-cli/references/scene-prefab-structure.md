# Scene 与 Prefab 结构参考

## 读取结构

```text
<fuc> scene read <场景路径> --project <root> [--depth <N>] [--full] [--max-tokens <N>] --json
<fuc> prefab read <Prefab路径> --project <root> [--depth <N>] [--full] [--max-tokens <N>] --json
```

结构读取默认用于检查完整层级、节点名称、父子关系、激活状态和挂载组件。`--depth` 控制层级读取深度；
结果中的 `stats.depth_truncated_children == 0` 表示没有因深度限制省略子节点。`--full` 不控制层级完整性，
而是为每个组件附加最多 200 条扁平化序列化字段；它可能产生很大的输出，数组仍会折叠为数量，不能视为
原始 YAML 的等价表示。

Prefab Instance 节点会返回合并后的 `effective_active`、`active_source`、`override_count`、`override_summary`、
`has_missing_reference` 和 `effective_state_confidence`。判断实例显隐直接使用这些字段，不再手工扫描 `m_IsActive`。
所有 `Assets/...` 输入相对于 `--project`，用响应中的 `resolved_asset_path` 核对最终资产路径。

默认不要添加 `--full`。只有公共 `component get` 语义契约无法提供诊断所需字段时才使用它，并同时设置
合理的 `--max-tokens`。推荐按以下粒度逐步读取：

1. 整体结构：不带 `--full` 的 `scene read`/`prefab read`。
2. 单个 GameObject：`game-object get`。
3. 单个 Component 的公开语义数据：`component get`，必要时用 `--property` 只读取一个字段。
4. 未被公共语义契约覆盖的底层字段诊断：最后才使用 `--full`。

正常对象定位和写后验证使用统一语义 get：

```text
<fuc> game-object get <目标.prefab|目标.unity> --object <selector> --project <root> --json
<fuc> component get <目标.prefab|目标.unity> --component <selector> --project <root> --json
```

对象选择器接受绝对层级路径、`go:<fileID>`、`fileid:<fileID>` 或唯一名称；组件选择器接受
`/层级/路径::组件类型`、`component:<fileID>` 或唯一组件类型。写命令要求选择器唯一，多匹配会在写入前
拒绝并返回候选。

`game-object get` 返回对象元数据以及包含 `file_id`、`class_id`、`type_name`、`managed_type` 和脚本身份的
`components[]`，不会一次返回该对象所有组件的语义值。需要读取全部组件数据时，根据列表中的类型直接逐个读取：

```text
<fuc> component get <目标.prefab|目标.unity> --component /对象路径::组件类型 --project <root> --json
```

自定义 MonoBehaviour 的组件类型选择器同时匹配脚本类名和脚本路径。若静态契约与项目动态契约都不可用，
报告 coverage gap；不要因此直接读取 YAML。

## GameObject 公共契约

CLI 根据目标的 `.prefab` 或 `.unity` 扩展名自动识别资产域，不使用 `scene`/`prefab` 对象命令分支：

```text
<fuc> game-object add <目标.prefab|目标.unity> --name <名称> --type standard|rect [--file-id <父级>] --project <root> --json
<fuc> game-object get <目标.prefab|目标.unity> --object <selector> --project <root> --json
<fuc> game-object set <目标.prefab|目标.unity> --object <selector> [--name <名称>] [--active true|false] [--tag <tag>] [--layer <N>] --project <root> --json
<fuc> game-object remove <目标.prefab|目标.unity> --object <selector> [--yes] --project <root> --json
<fuc> game-object reparent <目标.prefab|目标.unity> --file-id <节点> [--value <父级>] --project <root> --json
```

`--type standard` 创建固有 Transform，`--type rect` 创建固有 RectTransform；二者都不是随后通过
`component add` 附加的普通组件。省略父级时创建根对象，Prefab 保持单根规则。删除含子节点的对象时显式
使用 `--yes`；换父级省略 `--value` 时移动到根级。写入后只执行一次统一 get 验证。
直接 `game-object add` 修改已有目标；需要从零创建 Prefab 时，使用事务中首个无 parent 的 `gameObject.add`，
它会原子创建 `.prefab` 与 meta。它不会隐式创建 Scene。

对象图有多个依赖变更时，不逐条留下中间态，也不编写完整 Prefab manifest；使用 `tx plan` 验证计划后状态，
检查 `asset_validation` 后执行 `tx apply`，再做一次语义回读。需要 Unity 权威验证时对已写入资产运行
`asset validate --level editor|playmode`。

## Scene 资产操作

```text
<fuc> scene create <新Scene.unity> --type empty --project <root> [--dry-run] --json
<fuc> scene manager-get <Scene> --type render|lighting|navmesh|occlusion --property <路径> --project <root> --json
<fuc> scene manager-set <Scene> --type render|lighting|navmesh|occlusion --property <路径> --value <值> --project <root> --json
<fuc> scene merge <目标Scene> --value <来源Scene> --project <root> (--dry-run|--yes) --json
```

`scene create --type empty` 原子创建没有业务对象模板的 Scene 与 meta；随后只通过统一对象契约添加用户要求的
GameObject 和 Component。manager 和 merge 是 Scene 资产级操作，不替代对象创作接口。SceneRoots 与旧式
无 SceneRoots 场景均可读取和编辑；跨 Scene 对象引用必须 fail closed。

## Prefab 关系操作

Prefab 实例、Variant、unpack 和 source replacement 属于 Prefab 资产关系，不是普通 GameObject + Component
创作。需要这些操作时读取 [prefab-variant.md](prefab-variant.md)，不要把其专用身份字段用于普通对象创作。

将宿主 Prefab 的内嵌对象提取成独立 Prefab 时，使用结构化 Editor 命令：

```text
<fuc> prefab extract-child Assets/UI/Host.prefab --object /Host/Container/Template \
  --path Assets/UI/Extracted.prefab --project <root> --dry-run --json
<fuc> prefab extract-child Assets/UI/Host.prefab --object /Host/Container/Template \
  --path Assets/UI/Extracted.prefab --component /Host::HostWidget \
  --property itemTemplate --value itemContainer --project <root> --yes --json
```

该命令需要 Bridge capability `prefab_extract_child`，通过 `PrefabUtility.SaveAsPrefabAssetAndConnect` 创建并连接
实例，可选把宿主 `--property` 绑定到提取对象、把 `--value` 指定的字段绑定到原容器。正式执行前先 dry-run；
保留结果中的 `operation_id` 和 `prefab extract-child-undo`。输出 Prefab 已存在、路径歧义、字段不是已序列化
Unity Object 引用或类型不匹配时均在写入前拒绝。

## Missing Prefab 清理

源 GUID 已断链的 PrefabInstance 不是普通 GameObject。先按查询/诊断模块确认 source 不存在，再使用：

```text
<fuc> scene missing-prefab-remove <场景绝对路径> --file-id <PrefabInstance fileID> --project <root> [--force] --json
```

普通 GameObject 一律使用 `game-object remove`。

## 兼容实现边界

旧 `scene/prefab node-*`、`object-*`、`component-*`、`rect-transform-*`、`unity-event-*`、`primitive-create` 与
`prefab compose/append/compose-apply` 可能仍存在于历史代码和 evidence，但已从公共 schema/help 隐藏。
它们不是 Agent 可调用的创作接口，也不得作为统一命令失败后的 fallback。raw `m_*` 字段同样只属于 CLI
内部 serialization plumbing。

没有 `SceneRoots` 文档的旧式 Scene 仍允许多个根级 GameObject；不要为了绕过限制擅自改变用户要求的层级。
