# 兼容范围与验证边界

## 单一事实源

- 运行中的 `project capabilities` 与 `project preflight` 决定当前工程前置条件 不按文档版本号猜权限
- Bridge 1.20.0 可安装于 Unity 2022.3 与 6000 系列 U6 为 experimental-editor 不是所有 API 已兼容
- Scene Prefab Material Importer 与 ProjectSettings 离线写入仍须正式可写 Profile
- unity-6000-draft 保持 research-only 未验证系列不借用 2022.3 默认值
- U6 Bridge 安装只允许完整内置包文件与字节 不允许额外目标 删除或符号链接
  有遗留清理需求时拒绝并报告 不用 force 扩大授权
- 限定授权同样覆盖 .fuc 日志 快照目录与具体快照文件 这些辅助路径链接也必须拒写
- Windows 日志还必须是单硬链接文件 当前 U6 特例未实现其他平台链接计数 因此非 Windows 拒写
- 领域工作流与 Editor delegate adapter 仍按独立版本策略判断 不继承在线 Bridge 权限
- Unity 6000.4 及以上在线对象标识使用 EntityId 与 SceneHandle 原始 64 位整数
  instance_id component_instance_id 与 scene_handle 不得收窄为 Int32 或浮点数
  继续绑定 Editor session 与对象路径 不持久化为跨会话身份

## 已验证范围

2026-10-02 Windows x64 隔离工程实测 6000.4.10f1 6000.5.1f1 6000.6.4f1
覆盖 Bridge 安装 ready 结构化 context Roslyn 对象身份往返 64 位标识 Prefab 保存导入
新 generation 的编译门禁与受控退出 未覆盖所有 Inspector UI 测试运行器与第三方 Provider
其他 6000 系列只允许实验性安装 不推定 API 全通过 6000.3 本机安装不完整 未做 Editor 实测
2022.3.62f3 同范围回归通过 使用 Editor 自带 Newtonsoft 3.2.1 原 3.0.2 下载失败不记为通过
初始编译存在过时 API 与 Unity analyzer 警告 警告不是编译错误 也没有被隐藏
此版本未做 U6 live-update 现场保护全矩阵 Linux macOS 或 Player 验证

## 预检

```text
<fuc> project capabilities --project <root> --json
<fuc> project preflight asset clone --project <root> --json
<fuc> project preflight editor bridge-install --project <root> --json
<fuc> project preflight editor exec --project <root> --json
```

`supported=true` 只表示已检查的前置条件成立 `validation_scope` 公布检查范围
未检查目标字段 包 API 兼容性和执行结果 `requires_target_validation=true` 不可忽略
拒绝返回非零退出码与 failure_code 不自动强制重试
Template list/describe 从本地 catalog 读取 不要求 Editor 在线
在线命令按实际 capability 判断 TestRunner 与 GameView 分辨率不能只检查 online=true

## 分层结果

1. 文件层 语义回读 diff 引用与事务撤销
2. 导入层 Unity 能导入修改的资产 并回读组件或序列化对象
3. 编译层 新 generation 完整 coverage 零 errors 最终 ready 同时报 warning
4. 行为层 对本任务的最小可观察结果检查 非全部游戏正确性声明

按任务影响选择层级 不重复全量检查 不以空日志或旧 generation 判通过
Online 任意代码不会自动获得文件层 byte undo

## 发布完整性

独立包 manifest 记录 Runtime SHA256 大小 构建身份 全部 Profile 与其他 payload 哈希
`release_channel=preview` 不是正式 release `source_commit=unknown` 表示源码无 Git 身份
`execution_validation=not_run` 不是通过 平台没有当前构建则不可用
包验证用 `scripts/verify-package.ps1` 同时检查完整文件集合 不只验证可执行文件
打包必须包含 profile-unity6 自检依赖的研究文档 自检失败不能靠删断言或补造证据修复
