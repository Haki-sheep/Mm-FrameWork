# 审计、恢复、Profile 与自检命令模板

## 操作日志与恢复

```text
<fuc> logs list [目标文件或目录] --project <root> [--limit <N>] [--offset <N>] --json
<fuc> undo [目标文件或operation_id] --project <root> [--force] --json
```

省略 `undo` 目标时按当前状态根执行 LIFO 回滚；优先使用写命令返回的 `operation_id`。

## Unity 日志（只读）

```text
<fuc> logs sources [--project <root>] [--log-file <path>] --json
<fuc> logs editor [--project <root> | --log-file <path>] [--level <csv>] [--since <time>] [--until <time>] [--contains <text>] [--cursor <cursor>] [--limit <N>] [--follow] --json
<fuc> logs player [--project <root> | --log-file <path>] [--pid <PID>] [同上过滤参数] --json
<fuc> logs compile [--project <root> | --log-file <Editor.log>] [--errors-only] [--assembly <name>] [--since-last-compile] --json
<fuc> logs console --project <root> [--follow] --json
<fuc> logs watch --project <root> [--source editor,player,bridge] [--follow] --json
```

`logs list` 仍是 `.fuc/logs/operations.jsonl` 操作审计，不等同于 Unity Console。精确 Console/结构化编译结果要求 Editor Bridge；`Editor.log` 降级结果会明确返回 `structured=false`。`--follow --json` 是一行一个完整 v1 信封的 JSON Lines 流。

## Profile 发布

```text
<fuc> profile verify <Profile目录> --json
<fuc> profile seal <Profile目录> --value <package-version> --json
<fuc> profile pack <Profile目录> --out <目标目录> --json
<fuc> profile golden-add <Profile目录> <样本文件> <id> <category> <origin> <license-id> <redistribution> <approved-by> --value <package-version> --json
```

## Console 自检

全部已实现模板：

```text
<fuc> selfcheck roundtrip [文件或目录 ...] [--project <root>] --json
<fuc> selfcheck fileid --json
<fuc> selfcheck safety --json
<fuc> selfcheck validate --json
<fuc> selfcheck setprop --json
<fuc> selfcheck dryrun --json
<fuc> selfcheck undo --json
<fuc> selfcheck assetops --json
<fuc> selfcheck limitations --json
<fuc> selfcheck texture --json
<fuc> selfcheck idem --json
<fuc> selfcheck golden --json
<fuc> selfcheck node --json
<fuc> selfcheck settings --json
<fuc> selfcheck cache [--project <root>] [--limit <fuzz轮数>] --json
<fuc> selfcheck component --json
<fuc> selfcheck object-selector --json
<fuc> selfcheck assetcreate --json
<fuc> selfcheck serref --json
<fuc> selfcheck material --json
<fuc> selfcheck addressables --json
<fuc> selfcheck heuristic --json
<fuc> selfcheck tx --json
<fuc> selfcheck variant --json
<fuc> selfcheck stripped --json
<fuc> selfcheck variant-write --json
<fuc> selfcheck txdsl --json
<fuc> selfcheck workflow --json
<fuc> selfcheck schema-correction --json
<fuc> selfcheck view --json
<fuc> selfcheck variant-semantic --json
<fuc> selfcheck profile-package --json
<fuc> selfcheck profile-release --json
<fuc> selfcheck profile-2021 --json
<fuc> selfcheck profile-unity6 --json
<fuc> selfcheck editor-unity6-policy --json
<fuc> selfcheck plugin-sdk --json
<fuc> selfcheck profile-versions --json
<fuc> selfcheck performance --json
<fuc> selfcheck docs --json
<fuc> selfcheck emit --json
<fuc> selfcheck prefab-compose --json
<fuc> selfcheck prefab-instance --json
<fuc> selfcheck scene-lifecycle --json
<fuc> selfcheck release-gate --json
<fuc> selfcheck logs --json
<fuc> selfcheck project-refresh --json
<fuc> selfcheck project-refresh-performance --json
```

`selfcheck roundtrip [文件或目录 ...]` 可检查指定文本资产；不带目标时运行语料库门禁。自检用于 CLI 开发和发布验证，不应出现在普通 Unity 对象操作流程中。

`selfcheck release-gate` 同时执行离线端到端事务与精确 Unity 2022.3.62f3 黄金验证。若本机没有精确版本或 batchmode 环境不可用，
命令返回退出码 75，`editor_validation.status=environmental_skip` 且 `all_passed=false`；不得将该结果描述为发布通过。

## 声明式插件

插件清单位于目标状态根的 `.fuc/plugins/<id>/manifest.json`。插件命令由 `schema --json` 动态披露，不存在固定静态模板；插件只生成事务计划，由宿主执行校验、快照、原子写回和回滚。
