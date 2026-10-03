# ProjectSettings 命令模板

## 读取字段

```text
<fuc> settings get <ProjectSettings资产路径> --property <propertyPath> --project <root> --json
```

示例：

```text
<fuc> settings get <root>/ProjectSettings/TimeManager.asset --property m_TimeScale --project <root> --json
```

## 修改字段

```text
<fuc> settings set <ProjectSettings资产绝对路径> --property <propertyPath> --value <值> --project <root> [--force] --json
```

仅支持白名单内且已经验证形态的 TagManager、Physics、Quality、Graphics 和 PlayerSettings 等字段。数组、枚举和范围由 CLI 校验。

`TimeManager.m_TimeScale` 是序列化项目值，不代表 Editor Play Mode 内存中的当前 `Time.timeScale`。
