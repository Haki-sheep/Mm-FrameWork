# Prefab Variant 命令模板

## 合并与诊断

```text
<fuc> variant merge <Assets/...prefab|绝对路径> --project <root> [--depth <N>] --json
<fuc> variant diagnose <Assets/...prefab|绝对路径> --project <root> --json
```

## 写入属性覆盖

```text
<fuc> variant override-property <Variant Prefab绝对路径> --instance-file-id <本地PrefabInstance fileID> --file-id <源对象fileID> [--guid <源Prefab GUID>] --property <propertyPath> --value <值或引用flow> --project <root> [--force] --json
<fuc> variant override-list <Variant Prefab绝对路径> [--instance-file-id <本地PrefabInstance fileID>] --project <root> --json
<fuc> variant revert-property <Variant Prefab绝对路径> --instance-file-id <本地PrefabInstance fileID> --file-id <源对象fileID> [--guid <源Prefab GUID>] --property <propertyPath> --project <root> --json
<fuc> variant apply-property <Variant Prefab绝对路径> --instance-file-id <本地PrefabInstance fileID> --file-id <源对象fileID> [--guid <源Prefab GUID>] --property <propertyPath> --project <root> (--dry-run|--yes) --json
```

`apply-property --dry-run` 列出所有直接/传递受影响实例和 Variant；`--yes` 在一个批量写事务中修改源
Prefab 并从当前 Variant 移除对应 override。省略两者会拒绝。

## 组件覆盖

```text
<fuc> variant add-component <Variant Prefab绝对路径> --instance-file-id <本地PrefabInstance fileID> --file-id <源GameObject fileID> [--guid <源Prefab GUID>] --type <内置组件类型> [--insert-index <N>] --project <root> [--force] --json
<fuc> variant remove-component <Variant Prefab绝对路径> --instance-file-id <本地PrefabInstance fileID> --file-id <源组件fileID> [--guid <源Prefab GUID>] --project <root> [--force] --json
<fuc> variant revert-removed-component <Variant Prefab绝对路径> --instance-file-id <本地PrefabInstance fileID> --file-id <源组件fileID> [--guid <源Prefab GUID>] --project <root> --json
<fuc> variant delete-added-component <Variant Prefab绝对路径> --instance-file-id <本地PrefabInstance fileID> --file-id <本地组件fileID> --project <root> --json
```

## 实例子对象覆盖

```text
<fuc> variant add-child <Variant Prefab绝对路径> --instance-file-id <本地PrefabInstance fileID> --file-id <源Transform fileID> [--guid <源Prefab GUID>] --name <名称> [--insert-index <N>] --project <root> --json
<fuc> variant remove-child <Variant Prefab绝对路径> --instance-file-id <本地PrefabInstance fileID> --file-id <源GameObject fileID> [--guid <源Prefab GUID>] --project <root> --json
<fuc> variant revert-removed-child <Variant Prefab绝对路径> --instance-file-id <本地PrefabInstance fileID> --file-id <源GameObject fileID> [--guid <源Prefab GUID>] --project <root> --json
<fuc> variant delete-added-child <Variant Prefab绝对路径> --instance-file-id <本地PrefabInstance fileID> --file-id <本地GameObject或Transform fileID> --project <root> --json
```

省略 `--guid` 时从指定 PrefabInstance 的源引用解析。写入前可用 `variant diagnose` 检查脏覆盖和异常源链。
`override-list` 返回 mapping confidence 与 XOR fallback 数量；存在 fallback 时，写结果会要求 Editor 权威回读。
`override-property` 将业务字段变化放在 `semantic_changes`，把 canonical override 排序噪声单独放在
`serialization_only_changes`，审查时优先看前者。

Variant 语义影响查询位于 [queries-and-diagnostics.md](queries-and-diagnostics.md)。
