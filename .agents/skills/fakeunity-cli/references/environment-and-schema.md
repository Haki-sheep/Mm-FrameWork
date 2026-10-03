# 工程、环境与契约命令模板

使用 `<fuc>` 代表当前平台的 Skill 内置程序。读取命令统一添加 `--json`。

## 工程与 Unity 环境

首次写任务先 `project capabilities` 与目标 `project preflight` 预检不是目标执行验证
详细边界见 [兼容与验证](compatibility-and-validation.md)

```text
<fuc> project info --project <Unity工程根目录> --json
<fuc> project diagnose --project <Unity工程根目录> --json
<fuc> unity probe --project <Unity工程根目录> --json
<fuc> unity lock --project <Unity工程根目录> --json
<fuc> unity locate --project <Unity工程根目录> --json
```

`unity locate` 依赖本机 Unity 安装；其余命令可离线执行。

## 契约与底层解析

```text
<fuc> --version --json
<fuc> schema --json
<fuc> schema describe <类型全名或短名> --project <Unity工程根目录> --json
<fuc> raw parse <Unity文本资产路径> --project <Unity工程根目录> --json
<fuc> <domain> --help --json
<fuc> <domain> <verb> --help --json
```

只在模块模板未覆盖命令、插件动态增加命令或 CLI 返回 `FUC_USAGE`/未知命令时调用 `schema` 或帮助。

## 通用参数

所有参数均可使用 `--flag=value` 形式。

```text
--json
--project <工程根目录>
--dry-run
--idempotency-key <键>
--limit <数量> --offset <偏移> --cursor <游标>
--max-tokens <预算>
--no-input --yes --continue-on-error
--force
```

写命令常用参数：

```text
--file-id <int64> --property <propertyPath> --value <值>
--type <类型> --path <路径> --guid <32位GUID>
```
