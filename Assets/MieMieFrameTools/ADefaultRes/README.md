# 框架默认资源

Project 右键 Create → MieMieFramework 提供中文配置入口 创建新资产不会自动绑定所有业务组件
本工程按用途保存一份默认资产 现有字体烘焙与本地化配置保留原位置 不重复创建

以下路径均相对于本目录

| 创建入口 | 默认资产位置 | 使用方式 |
| --- | --- | --- |
| 视觉特效配置 | Effects/Profiles/EffectProfile.asset | 默认 FrameRoot 已引用 在 SO 配置特效列表和预算 |
| 资源/动态图集配置 | DynamicAtlas/Profiles/DynamicAtlasConfig.asset | 已复制模块默认参数与 Shader 引用 按业务需要赋给 DynamicAtlasHost |
| 跳字字符映射 | FloatingText/Profiles/FloatingTextCharMap.asset | 空映射模板 不是已烘焙产物 不直接当作可用跳字资源 |
| 日志配置 | Resources/MieMieLogProfile.asset | 会话启动时按 Resources 路径自动读取 无需挂到 FrameRoot |
| 字体/字体烘焙配置 | Fonts/Profiles/FontProfile.asset | 使用已有字体烘焙工具 |
| 本地化/字体映射 | Fonts/Profiles/LocalizationFonts.asset | 使用已有本地化字体映射 |

## 跳字映射与烘焙

跳字映射保存字符与图集格子的对应关系 不能仅添加字符而不生成对应纹理
本目录的默认映射字符数为零 需要字体与图集烘焙才能得到有效映射
当前 FloatingTextAtlasBaker 仍将匹配的图集 材质 CharMap 与 Prefab 输出到跳字模块的 Art/Generated
应使用该次烘焙输出的匹配资源 不将空模板替换到已烘焙 Prefab 中
本次未烘焙跳字 未移动模块烘焙输出目录或重绑已有 UI

## 配置责任

- [视觉特效配置与生命周期](../Scripts/Frame/G_Effect/README.md)
- [动态图集接入](../Scripts/Frame/B_Assets/DynamicAtlas/Docs/使用说明.md)
- [日志启动配置](../Scripts/Tools/Diagnostics/README.md)
- [本地化配置](../Scripts/Frame/C_Data/Localization/README.md)

默认日志资产 MinimumLevel 为 Info 文件记录关闭 普通 Player 也会使用该显式级别
如正式包只需要 Warning 在发布前修改该资产 无配置时的默认策略不替代已配置值
运行中的配置变更生效时点以各模块权威说明为准 不通过重复初始化刷新配置
