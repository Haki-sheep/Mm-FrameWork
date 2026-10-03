# UI 管理器接入

## 程序集边界

`MieMieUIFrameWork.UI` 已引用 `MieMieFrameWork.Runtime` `UIHub` 实现 Runtime 中的 `ModuleHub.IManagerBase` 无需新增程序集引用

Runtime 不引用 UI 程序集 不使用 UIHub 具体类型 仅沿用可选类型发现和场景组件定位 然后以 `IManagerBase` 持有和注册组件

移除反射方法调用与 `ReflectionManagerAdapter` 反射只用于发现可选模块 未安装 UI 时不阻止 Runtime 编译

## 初始化与查询

1. `RegisterAllManagers` 定位已有 UIHub 未安装或场景没有组件时保留可选模块警告并跳过
2. 已定位组件必须实现 `IManagerBase` 不符合接口时立即失败
3. UIHub 按自身 `ManagerAttribute` 注册 优先级为十 位于输入之后与交互之前
4. 统一管理器初始化阶段调用一次 `UIHub.Init` 再由该入口调用私有 `InitComponents` 缓存组件并创建 UIStack

不要在 UIHub 的 Awake Start Show Refresh 或业务调用方再次初始化

UI 调用方引用 UI 程序集后可使用 `ModuleHub.GetManager<UIHub>` 原 `GetUI<UIHub>` 与 `HasUI` 保持兼容 两种查询返回同一组件

管理器按组件实际类型注册 `ManagerAttribute` 不继承 若使用 UIHub 子类 子类必须自行标注优先级十 并使用 `GetManager<具体子类>` 查询 `GetUI<UIHub>` 仍可返回该子类实例 不为基类重复注册别名

业务打开窗口前等待 `ModuleHub.ReadyTask` UI 类型未安装或场景没有 UIHub 时不可请求必需 UI 管理器

## 释放责任

UIHub 仍是 Unity 场景组件 未新增 IDisposable 也不改变现有窗口及资源句柄的释放流程 FrameRoot 清理时只解除 UI 注册和缓存引用 不强制销毁独立 UIRoot

完整启动顺序与失败语义见 [框架启动与生命周期](../../A_Frame/Boot/README.md)

## 画质设置功能组件

`QualitySettingsView` 通过 IQualityService 与类型化通知接入 不在 Show 或 Refresh 初始化 不修改 Gen 局部控件由组件自身持有

面板壳一次调用 InitComponents 显示与隐藏分别调用 BindEvents 和 UnbindEvents 生命周期 偏好及失败语义以 [画质模块说明](../../A_Frame/Business/Quality/README.md) 为准
