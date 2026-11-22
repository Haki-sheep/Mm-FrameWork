# 哈基咩咩框架

Unity 游戏运行时框架，提供启动与服务组装、业务模块和配套编辑器工具。

接入时，在启动场景的框架根节点挂载 `ModuleHub`，按[资源接入说明](Scripts/Frame/B_Assets/Yooasset/RuntimeAdapter/README.md)配置默认资源包。游戏自己的启动入口等待 `ModuleHub.Instance.ReadyTask` 完成后，再组装业务模块并取得服务。

在 `ModuleHub` 的 Inspector 里选择「启动方案」。已有根节点默认「完整启动」。「资源和存档」只准备默认资源包并创建存档管理器。「自定义」可逐项勾选模块，默认资源包始终启动。勾选音频或特效会补入对象池与帧驱动，勾选交互会补入输入与帧驱动，勾选游戏流程会补入帧驱动。Inspector 会显示自动补入的依赖。代码热更还需打开热更配置中的启用开关。

方案在根节点启动时读取一次，控制框架的创建、注册与初始化。业务入口可用 `HasManager<T>()` 判断可选管理器。独立放在场景里的 UI 根节点、多语言宿主、动态图集宿主等仍按各自的 Unity 生命周期运行，精简场景只放实际需要的宿主。

框架和配套窗口使用 Unity 原生 Inspector 与编辑器控件，Odin Inspector 可独立选装。字段中文标签使用 [`InspectorLabel`](Scripts/Tools/Inspector/InspectorLabelAttribute.cs)，枚举选项使用 Unity 的 `InspectorName`。`InspectorShowIf` 在框架的 UI Toolkit 检查器中按同对象的枚举值或空对象引用切换显示。跳字组件用 `InspectorFoldout` 保留分组。绘制与共享 USS 位于 [Inspector 编辑器程序集](Editor/Inspector/MieMieFrameWork.Inspector.Editor.asmdef)。

需要阅读或修改源码时，从[框架代码索引](框架代码索引.md)按问题定位模块。
