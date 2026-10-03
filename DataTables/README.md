# Luban 数据工程

当前工具版本为 `Luban 4.11.0`

- `Data` 保存 Excel Schema 与 Excel 或 JSON 业务数据
- `Defines` 保存 XML Schema
- `check.bat` 只加载并校验全部配置
- `gen.bat` 生成 Newtonsoft JSON C# 代码与 JSON 数据

生成代码固定输出到：

`Assets/MieMieFrameTools/Scripts/Frame/C_Data/Luban/Generated`

生成数据固定输出到：

`Assets/StreamingAssets/DataTables`

这两个目录必须保持纯生成目录 不能放手写文件 因为 Luban 会清理旧生成文件

## 工具初始化

首次缺少 Luban 工具时运行：

```bat
Tools\setup_luban.bat
```

工具源码检出目录不进入主仓库 由初始化脚本固定到官方 `luban_examples` 提交：

`3ddebdc75a67f76cab830608bfaf3b8806e05175`

需要主动跟进新版时先在独立分支更新并重新验证生成结果 不要让团队成员各自拉取不同版本

## 多语言文本

`Data/localization_texts.json` 保存 Locale Key Text 三列形式的 JSON 数组
`Defines/localization.xml` 通过 `*@localization_texts.json` 逐条读取数组记录
生成数据为 `localization_tbtext.json` 生成子表为 `cfg.localization.TbText`
运行时接口 字体工具与接入操作见 [多语言与字体说明](../Assets/MieMieFrameTools/Scripts/Frame/C_Data/Localization/README.md)
