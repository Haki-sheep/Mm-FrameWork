# 项目 Agent 指令

## Unity 操作入口

- 涉及 Unity 工程检查 资产操作或 Editor 自动化时 默认使用 Codex 全局安装的 fakeunity-cli Skill
  先按当前会话 Skill 列表读取全局 SKILL.md 不再依赖项目内的 .agents 目录
  再按任务读取必要 reference 不重复维护 Skill 的详细操作规则
- CLI 默认使用全局 fakeunity-cli Skill 声明的平台程序路径
  每次显式传入 `--project <本项目根目录>` 与 `--json`
  若项目提供统一启动入口则按 Skill 使用该入口
  全局 Skill 或程序不可发现时报告原因 不自动安装或恢复项目副本
- 场景 Prefab 组件 材质和 Unity 序列化设置的检查与修改 必须通过 FakeUnityCLI 完成
  不直接读写或搜索目标 YAML 不手写 meta 不手改桥接队列或 receipt
- 普通 C# 源码 文档 Git 与外部开发工具允许使用常规工具
  影响 Editor 状态的源码或配置修改前 按 Skill 执行对应门禁
  不通过普通文件操作或 Roslyn 文件操作绕过 Unity 操作约束
- 能力不足 版本不支持 现场保护或 Editor 锁阻止操作时 报告原因
  不通过其他工具或 `--force` 绕过 不擅自关闭 Editor 或解除现场保护
- 写后按任务影响回读并验证 保留操作标识和真实恢复边界
  区分连接成功 数据正确 导入成功 编译通过与行为通过 未验证项如实报告
