# 索引、引用与诊断命令模板

## 资产索引

```text
<fuc> query guid <32位GUID> --project <root> --json
<fuc> query assets [--type <类型>] [--path <Assets目录前缀>] [--limit <N>] [--offset <N>] --project <root> --json
```

## 引用图

`--guid` 与 `--path` 二选一。

```text
<fuc> query refs (--guid <GUID> | --path <资产路径>) [--include-heuristic] [--limit <N>] [--offset <N>] --project <root> --json
<fuc> query referenced-by (--guid <GUID> | --path <资产路径>) [--include-heuristic] [--limit <N>] [--offset <N>] --project <root> --json
```

`refs` 查询目标引用了谁；`referenced-by` 查询谁引用了目标。启发式字符串引用不是完备结果。

## Variant 语义影响查询

```text
<fuc> query variant-overrides --guid <源Prefab GUID> --file-id <源对象fileID> [--property <propertyPath>] --project <root> --json
<fuc> query variant-impact (--guid <Prefab GUID> | --path <Prefab路径>) --project <root> --json
```

## 诊断

```text
<fuc> diag duplicates --project <root> --json
<fuc> diag missing-refs [--path <Assets目录前缀>] --project <root> --json
<fuc> project diagnose --project <root> --json
```

`diag missing-refs` 检测断链 GUID、悬空 fileID、Missing Script 和 meta 对账问题。
