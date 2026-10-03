# Material 与 ScriptableObject 命令模板

## Material

```text
<fuc> material set-shader <材质绝对路径> --value <standard|particles/additive|工程内Shader GUID> --project <root> [--force] --json
<fuc> material set-color <材质绝对路径> --property <Shader属性名> --value <r,g,b[,a]> --project <root> [--force] --json
<fuc> material set-texture <材质绝对路径> --property <Shader属性名> --value <贴图GUID或路径> --project <root> [--force] --json
<fuc> material set-float <材质绝对路径> --property <Shader属性名> --value <浮点值> --project <root> [--force] --json
```

颜色、贴图和浮点命令会修改已有属性条目或追加新条目，并校验 Shader 属性名。

## ScriptableObject

```text
<fuc> so set-field <ScriptableObject绝对路径> --property <字段名> --value <值> --project <root> [--force] --json
```

该命令通过脚本元数据进行字段类型、枚举值和拼写校验。需要查看字段时使用：

```text
<fuc> schema describe <ScriptableObject类型> --project <root> --json
```
