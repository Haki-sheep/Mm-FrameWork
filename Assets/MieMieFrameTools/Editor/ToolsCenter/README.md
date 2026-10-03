# 工具中枢

唯一菜单入口 `Tools / MieMieFrameWork / 工具中枢`
左侧按目录选择工具 右侧显示嵌入页面或明确的操作按钮
关闭页面时释放中枢创建的临时窗口 不释放运行时管理器
中枢使用 OnInspectorUpdate 定期重绘宿主 嵌入监控不依赖隐藏子窗口自行重绘

## 日志与诊断

选择 `日志与诊断 / 日志导出`

- 有运行会话时可点击导出近期日志 无会话时按钮禁用
- 打开日志目录始终可用 不创建日志文件或启动运行会话
- 导出格式 配置与生命周期以 [日志说明](../../Scripts/Tools/Diagnostics/README.md) 为准

## 运行监控

选择 `运行监控 / 视觉特效` 直接在中枢查看特效监控
未进入 Play 或框架未就绪时仅显示提示
运行时可以查看预算 资源数量 修改档位与暂停状态 停止播放或清理闲置资源
业务规则以 [特效说明](../../Scripts/Frame/G_Effect/README.md) 为准

## 多语言与字体

| 页面 | 操作 |
| --- | --- |
| 字体管理器 | 点击执行操作打开现有完整 Odin 字体工作台 |
| 创建默认字体映射 | 点击执行操作创建或选中已有字体映射 |
| 接入选中的框架根节点 | 先选择场景 ModuleHub 再执行接入 |
| 绑定选中的 TMP 文本 | 先选择场景 TextMeshProUGUI 再执行绑定 |
| 验证字体工具迁移 | 明确点击后才执行原有验证 不在打开中枢时自动执行 |

接入条件与字体行为以 [多语言说明](../../Scripts/Frame/C_Data/Localization/README.md) 为准

原顶层 MieMie 日志与运行监控菜单 原 Tools/MieMie 字体与多语言菜单不再注册
Tools/HakiSheep/聊天布局 菜单入口已移除 保留示例生成器源码与既有场景 未自动删除或生成场景

## 本次验证 2026-10-03

- FakeUnityCLI 正式 Unity 6000.4.10f1 编译 generation 31 与 32 均通过 watermark 与 coverage 完整 history_gap false 本代错误和警告均为 0
- 运行中的 Editor 程序集只读回读 7 个中枢页面各注册一次 原菜单匹配数量 0 原中枢菜单保留
- Edit Mode 无日志会话时导出校验为禁用 未进入 Play 未切换或保存用户场景
- 字体创建 组件绑定 导出对话框及运行时特效操作未自动执行 不将菜单注册验证当作这些业务操作全部验收
- 首轮缺字体程序集引用产生 CS0234 已补显式引用 之后导入 asmdef 再通过门禁 首错与导入证据保存在 Logs/MenuConsolidation
- 本机证据 compile-first-failure.json compile-before-asmdef-import.json import.json compile-final.json menu-readback.json

独立评审未发现 P0/P1 指出嵌入监控的宿主刷新为 P2 未验证风险
已补宿主 OnInspectorUpdate 重绘 不添加新的运行时状态或 Player 更新回调
只复核这次修正的编译与 Editor 消息入口 不重复无变化部分的业务验证

修正后 generation 34 编译门禁通过 watermark 与 coverage 完整 history_gap false 本代错误与警告均为 0
证据 compile-repaint-fix.json request a337b9264f1f44e084db315fd2dfdd86
repaint-readback.json 确认宿主无参 OnInspectorUpdate 入口存在 7 个页面仍各注册一次
原评审者复核关闭 P2 未发现 P0/P1 或规范违规
静置视觉动态刷新与跨 Play 的日志按钮变化仍未实测 不将消息入口回读当作完整视觉验收
