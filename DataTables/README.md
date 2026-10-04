# Luban 数据工程

当前工具版本为 `Luban 4.11.0`

- `Data` 保存 Excel Schema 与 Excel 或 JSON 业务数据
- `Defines` 保存 XML Schema
- `check.bat` 从多语言 Excel 更新中间 JSON 并加载校验全部配置 不发布运行时数据
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

策划只维护 `Data/localization_texts.xlsx` 的 `Texts` 工作表 一行一个 Key 一列一种语言
当前示例表包含 `zh-CN` `en` `ja` `ko` 四种语言 五个 Key 日韩示例译文保留原有参数格式 正式文案由策划或译者确认
`gen.bat` 与 `check.bat` 先调用 `export_localization.ps1` 校验并导出 `Data/localization_texts.json`
中间 JSON 为生成产物 不手工维护 `Defines/localization.xml` 继续逐条读取其 Locale Key Text 记录
`gen.bat` 再通过 Luban 发布 `Assets/StreamingAssets/DataTables/localization_tbtext.json` 和 `cfg.localization.TbText`
生成代码和运行时 JSON 的结构不变 字体工作台继续从生成语言表收集文案

### 填写约定

- `Texts` 第一行第一列固定为 `Key` 其余非空表头为 CultureInfo 的标准语言标识 如 `zh-CN` `en` `ja`
- 第二行起填写文案 Key 区分大小写 不重复 不带首尾空白 每种语言必须填写完整译文
- 新增语言只需添加语言列并补齐译文 同时在 Unity 字体映射中补齐对应语言的全部 Style
- 本次仅扩展日韩文案列 未自动创建日韩字体映射或切换按钮 实际启用前须准备覆盖对应字形的字库
- 所有单元格用文本格式 纯数字文案也作为文本输入 不使用公式 不合并单元格
- 支持单元格换行 富文本和 `{0:N0}` 等占位符 保留各语言参数编号 不把实际金币数量写进表
- 全空行与只有格式的空单元格忽略 非空数据没有语言表头时明确报错
- `填写说明` 工作表仅供策划阅读 不参与导出 保存并关闭 Excel 后再生成

### 生成行为

导出器只依赖 Windows PowerShell 5.1 和系统 .NET 压缩 XML 能力 无需 Python Node Excel 安装或额外 NuGet 包
先完整校验工作簿再原子替换中间 JSON 内容不变时不重写 文件损坏 缺译 重复 Key 或公式均使脚本非零退出
导出失败时不会执行 Luban 旧生成数据可能仍在磁盘 不能把旧数据当本次生成成功
`check.bat` 也会更新中间 JSON 但不更新运行时生成目录 正式发布须运行 `gen.bat`
多语言运行时不读取 Excel 只读取 Luban 生成的 JSON 玩家语言选择与游戏存档不受本次改动影响

### 本次接入验证

- 现有五个 Key 简中英文共十条译文已迁入 Excel 中间 JSON 与运行时 JSON 的译文逐项保持一致
- Windows PowerShell 5.1 导出回归二十一项通过 覆盖新增语言 换行 富文本 参数 纯数字文本 单条数组 重复 Key 缺译 非文本单元格 公式 空行与失败不覆盖旧输出
- `check.bat` 与 `gen.bat` 执行成功 运行时 JSON 与 Luban C# 生成文件相对本次接入前无变化
- 工作簿已渲染检查 标题 Key 译文与占位符可见 未验证 Excel 或 WPS 保存后的重新导入以及新增语言实际游戏显示
- 未改变运行时接口 程序集依赖 初始化顺序或字体释放责任 本次不重复验收原有完整 PlayMode 与目标平台流程

运行时接口 字体工具与接入操作见 [多语言与字体说明](../Assets/MieMieFrameTools/Scripts/Frame/C_Data/Localization/README.md)
