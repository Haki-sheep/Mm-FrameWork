# 能力发现与归属

Bridge 1.16 新增自动/现场保护/自由开发模式、按操作预检与准备、受控停止Play、运行时实例简单字段写入。
详见 [开发模式](editor-development-modes.md)，实时参数以 CLI help/schema 为准。

仅在用户询问整体能力、工具选择或迁移边界时读本页。执行任务时从顶层 Skill 进入对应 reference，
不要先加载全部模块、静态命令表或上游文档。Unity 2022.3 为离线写入边界 U6 Bridge 为实验性在线支持 详见 [兼容与验证](compatibility-and-validation.md)。

## 权威来源

- 当前命令与参数：CLI schema；对象语义字段：schema describe 与组件查询。
- 在线能力与状态：editor status/context；编译：generation/watermark/coverage；日志：Console cursor。
- 动态扩展：editor provider-list/describe、template-list/describe。未安装项目扩展不改变内建 Core。
- 发布物、接口来源和历史验证记录在产品 docs；历史通过不能替代本次 Editor/资产/版本检查。

## 通用 Core

离线覆盖 Scene/Prefab/GameObject/Component、语义字段与引用、Material/ScriptableObject、
SerializeReference、Variant/实例覆盖链、资产/GUID/引用诊断、受支持的 ProjectSettings/Addressables、
事务/工作流/可逆视图，以及操作审计/恢复/Profile。字段覆盖以实时契约为准，不猜未知 Unity 序列化结构。

在线覆盖当前 Editor 身份/模式、context/层级/定位、结构化 refresh/import、编译/日志、
类型化 Inspector/对象引用/Curve/Gradient、受控 Prefab 编辑、UGUI 观察/射线/目标点击/坐标、
截图/分辨率和受控 EditMode TestRunner。详见对应 reference；不将在线回调称为离线事务沙箱。

## 项目扩展

项目 Plugin/Provider/Template 是业务入口：Scenario、构筑和命中预期、View/对象池契约、Importer/Addressables
规则、音频分类、屏幕适配策略等保留在项目。Provider C# 放项目自己的 Assets/Editor 目录，避免进入 Player；
SDK manifest 位于 deployment manifest 声明的 plugins/templates 根，不覆盖其他项目的扩展。

项目 Provider 只复用受验证的业务 API，不包装另一套上游传输或任意二进制。Provider/Template 声明的权限、
参数与 source hash 通过 describe 核对；写入需 --yes，未知结果不能自动重发。

## 不应推出的结论

- Offline 不观察 Play 内存；Editor 不等于 Player/真机。
- 组件 active、几何和射线不等于像素可见；截图不证明业务回调。
- UI click dispatched 不等于业务 postcondition；View activation 不等于视觉品质或对象池复用。
- 工具日志/receipt 只证明声明的范围，不能替代项目构建和运行验收。
- Runtime 网络、远程代码、自动发现、构建注入未在本轮实现。Profiler、Code Index、GIF、drag/long-press 等
  不能仅凭上游目录存在就报告为 Core 已支持。
- UnitySkills 的保留/退役由项目迁移门禁决定；不能自动安装它或在 FakeUnityCLI 失败后静默切换。
