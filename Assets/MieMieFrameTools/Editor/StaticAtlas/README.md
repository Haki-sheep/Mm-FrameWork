# 静态图集编辑器

入口 `Tools / MieMieFrameWork / 工具中枢 / 资源 / 静态图集`

## 使用步骤

1. 查看 Sprite Packer 模式 若 Disabled 请明确点击启用 Sprite Atlas V2 并确认 这是全局项目设置 工具不会自动启用
2. 添加 Assets 下已有来源目录 递归收录已导入为 Sprite 的 Texture2D 重叠目录自动去重 普通纹理不会自动转换
3. 设置尺寸 Padding 压缩与平台覆盖 UI 默认关闭旋转及 Tight Packing 开启选中平台覆盖才修改该平台 其他平台参数保留
4. 点击扫描来源并检查 查看源图列表与重复归属 超尺寸阻断问题 同名文件与同名 Sprite 仅提示风险
5. 创建新图集选择现有输出目录 或选已有图集读取参数 添加来源目录并确认更新 更新将替换全部 Packables 保持图集 GUID 不改源图导入设置与 GUID
6. 点击打包并刷新原生预览 使用当前 Editor 构建平台 原生预览可以查看打包页及 Sprite 数量 这不是 Player 构建验收

选择已有图集不会猜测来源目录 避免把单图所属目录中的其他图片意外加入 更新之前必须重新指定完整来源 原收录遗漏会列为警告
目录规则仅存在于当前打开页面 保存结果是明确的纹理收录列表 不会因后续往目录添加图片自动更新 需再次扫描并更新
关闭页面或重载程序集后重新选来源与目标 不创建额外 Profile 或运行时服务

## 安全边界

- 只支持 Master 图集 不覆盖 Variant 不覆盖已有其他资产 不覆盖 Inspector 中未保存的图集或导入器修改
- 新文件按项目模式生成 spriteatlas 或 spriteatlasv2 V2 使用 SpriteAtlasAsset 和 Unity 导入 API
- 跨其他 Master 图集的同一纹理归属作为错误 即使只收录了该纹理上的部分 Sprite 也要求先处理归属
- 默认关闭 Include In Build 适用于由 YooAsset 显式分发 若需 Player 自动包含由用户自行开启
- 默认关闭 Read/Write 和 MipMap 该工具面向 UI 颜色图 平台格式必须符合目标平台 Unity 导入或打包错误不会自动降级
- 更新前重新扫描检查 不依赖上一次检查结果 不自动改地址 采集器 资源加载器或业务引用

## 与资源管理的关系

图集工具维护构建输入 YooAsset 继续负责采集 构建 下载 加载与释放 不建立第二套资源系统
保存后只读提示覆盖输出路径的现有采集器 包含过滤 打包和寻址规则 路径覆盖不证明过滤通过
默认资源目录为 Assets/MieMieFrameTools/ADefaultRes 输出位置与包组按项目原有采集规则确定 不自动写入配置
按文件名寻址须避免同名源图与图集 同名检查不代替 YooAsset 的最终地址校验
原图是否仍单独打包 图集依赖是否重复须查看 YooAsset 构建报告 平台压缩和真实 UI 效果须在目标平台验收
加载与句柄归属以 [现有 YooAsset 适配层](../../Scripts/Frame/B_Assets/Yooasset/RuntimeAdapter/README.md) 为准

## 验证记录 2026-10-04

- Unity 6000.4.10f1 generation 73 75 与生命周期修复后 76 正式编译 0 错误 0 警告 watermark 与 coverage 完整 history_gap false 并行工程刷新 generation 74 有 10 条既有字体与 UI 警告 非本工具文件
- FakeUnityCLI 请求 4504f5aa6d8445468b4ff5381d1c3897 通过 23 项隔离检查 包含 V2 创建与更新 原 GUID 与 importer 保留 目录去重 跨图集与目录收录重复检查 Variant 脏导入器 超尺寸 无效格式拒绝
- StandaloneWindows64 实际 PackAtlases 得到 2 个 Sprite 窗口参数回读与隐藏实例释放通过 不等于 Android 或 iOS 压缩验收
- 临时资产目录 Assets/__StaticAtlasReview_20261004 已通过 AssetDatabase.DeleteAsset 清理 项目 Sprite Packer 恢复原 Disabled 模式 未切换或保存场景
- FakeUnityCLI 请求 2b6ab5c96d15427b9ba1b474959e7e91 打开中枢静态页面 旧动态图集 MenuItem 数量为 0 中枢两个图集入口各一次
- 独立评审发现中枢切页缺少 OnOpen 已配对调用页面 OnOpen 与原 OnClose 请求 e0eec9b8bccc4af4b4860ad4f69b73bb 补验动态页首次进入 离开与重进 以及对应隐藏窗口创建和释放 绘制后 Console 查询无新增错误
- 首次隔离测试遗漏源图 SpriteImportMode.Single 导致打包数量断言失败 修正测试图片导入设置后按原断言复测通过 首错与历次结果保留于 Temp/FakeUnityCLI 临时证据不作为长期依赖
- 待验收 实际 UI Sprite 引用效果 YooAsset 平台构建与依赖报告 Android 与 iOS 真机 图集原生预览交互 动态图集 Play 实装行为
