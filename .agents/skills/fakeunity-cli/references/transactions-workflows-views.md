# 事务、工作流与可逆视图命令模板

## 事务 DSL

先创建 `<root>/Temp/FakeUnityCLI/`，将 manifest、sealed plan、workflow state 和临时 view 全部放入该目录；不要写入 Skill 目录、工作区根目录、`Assets` 或 `Packages`。

```text
<fuc> tx plan <root>/Temp/FakeUnityCLI/transaction-manifest.json --project <root> --out <root>/Temp/FakeUnityCLI/sealed-plan.json --json
<fuc> tx apply <root>/Temp/FakeUnityCLI/sealed-plan.json --project <root> [--force] --json
```

`tx plan` 在内存模拟后运行资产验证，返回 `baseline`、`resolved`、`introduced`、`remaining` 与 `worsened`；
新增或恶化 error 会在目标零写入时拒绝。`tx apply` 只接受 sealed plan，并会复核 baseline、validator version、
计划内容哈希、验证结果绑定和自定义 MonoBehaviour contract 输入；脚本或程序集内容在 plan 后变化会以
`FUC_TX_STALE` 拒绝。

对象事务 manifest 使用以下稳定结构；不要从 `--help` 猜测，也不要改写为完整 Prefab 快照：

```json
{
  "tx_version": "1.0",
  "idempotency_key": "stable-task-key",
  "preconditions": [],
  "operations": [
    {
      "id": "root",
      "depends_on": [],
      "op": "gameObject.add",
      "args": {"file": "Assets/Generated.prefab", "name": "Root", "transform": "rect"}
    },
    {
      "id": "child",
      "depends_on": ["root"],
      "op": "gameObject.add",
      "args": {
        "file": "Assets/Generated.prefab",
        "name": "Child",
        "transform": "rect",
        "parent": {"$ref": "root.transform"}
      }
    },
    {
      "id": "layout",
      "depends_on": ["child"],
      "op": "component.set",
      "args": {
        "file": "Assets/Generated.prefab",
        "target": {"$ref": "child.transform"},
        "values": {"preset": "middle-center", "sizeDelta": {"x": 160, "y": 40}}
      }
    },
    {
      "id": "image",
      "depends_on": ["child"],
      "op": "component.add",
      "args": {
        "file": "Assets/Generated.prefab",
        "gameObject": {"$ref": "child.gameObject"},
        "type": "Image",
        "values": {"raycastTarget": true}
      }
    }
  ],
  "options": {"atomic": true, "validate_after_write": true}
}
```

支持的对象操作为 `gameObject.add|set|remove|reparent` 与 `component.add|set|remove`。每个 operation 必须有唯一
`id`、`depends_on`、`op` 和 `args`。引用事务中新建结果时使用
`{"$ref":"<operation-id>.gameObject|transform|component"}`。`component.set` 的既有同资产 Unity Object 引用字段
也可使用 `{"object":"/对象路径"}`、`{"component":"/对象路径::组件类型"}`、`{"fileID":123}` 或 `null`；
解析基于该 operation 执行时的内存事务状态。字段值只使用对应 `schema describe` 公布的语义名。
首个 `gameObject.add` 可以原子创建尚不存在的 `.prefab` 及 meta，但不能隐式创建新 Scene；新 Scene 先用
`scene create`。`asset.set-property` 不能用于 `.prefab` 或 `.unity`。

## 离线工作流

```text
<fuc> workflow plan <root>/Temp/FakeUnityCLI/workflow-manifest.json --project <root> --out <root>/Temp/FakeUnityCLI/workflow-state.json --json
<fuc> workflow apply <root>/Temp/FakeUnityCLI/workflow-state.json --project <root> [--force] --json
<fuc> workflow status <root>/Temp/FakeUnityCLI/workflow-state.json --project <root> --json
<fuc> workflow resume <root>/Temp/FakeUnityCLI/workflow-state.json --project <root> [--force] --json
```

工作流用于 DAG、条件、跨步骤依赖和失败续跑；单对象操作不使用工作流。

## 可逆简化视图

```text
<fuc> view export <Unity文本资产> [--full] --out <root>/Temp/FakeUnityCLI/view.json --json
<fuc> view apply <root>/Temp/FakeUnityCLI/view.json [--path <目标资产>] --project <root> [--dry-run] [--force] --json
```

`view apply` 只能定点修改既有字段和引用，不能增删 YAML 文档或改变结构身份。源 SHA 或字段 baseline 变化时会拒绝写回。

## 对象创作语义事务

Scene/Prefab 对象创作不以完整 Prefab manifest 作为 Agent 中间格式。先用 `schema describe` 获取语义字段与默认值，
目标由 `game-object`/`component` 自动识别为 Scene 或 Prefab。单个明确变更直接执行；多个相互依赖的节点、
组件和引用变更生成 transaction plan，检查 plan 返回的 `asset_validation` 后再 apply。计划只包含最小语义意图，
不包含 schema 默认值或 `m_*` raw plumbing。apply 后只做一次语义回读，并保留 operation ID/undo；需要
Unity 权威验证时对已写入资产运行 `asset validate --level editor|playmode`。

旧 `prefab compose` 只作为历史兼容实现存在，不是 Agent 对象创作工作流，也不得承载固定业务对象答案。
离线 plan/validate/apply 只证明结构、schema、引用和静态约束；Editor/import 与 runtime/Play Mode/interaction
必须分别执行和报告。

`selfcheck release-gate` 使用私有测试 fixture 验证 manifest、PNG、Scene、脚本、事件、事务和 undo，不向调用方提供可复用的业务界面结构。只有离线门禁与精确 Unity batchmode 黄金验证均通过时才是完整产品通过；退出码 75 是环境跳过，不是通过。
