# HakiSheep Framework

Unity 游戏程序框架。共享构建列表以 `Assets/Scenes/TestScene.unity` 为启动场景，其中的 `FrameRoot` 持有 `ModuleHub`，独立的 `UIRoot` 持有 `UIHub`。

接入框架见[框架说明](Assets/MieMieFrameTools/README.md)，定位源码见[框架代码索引](Assets/MieMieFrameTools/框架代码索引.md)。

## 框架启动与生命周期

`ModuleHub.Awake` 创建 `BootManager`。启动器准备 YooAsset 默认包，按本次启动方案执行热更加载、配置表初始化及管理器组装，最后运行已加载的热更入口。业务代码等待 `ModuleHub.Instance.ReadyTask` 成功后再取得服务。启动异常通过同一任务传给等待者。预设与自动补入的依赖见[框架说明](Assets/MieMieFrameTools/README.md)。

管理器在 `ModuleHub.RegisterManagers` 显式组装。通过 `GetManager<T>()` 获取基础设施，通过 `GameHub.Get<T>()` 获取已注册的业务接口。失败或销毁时，框架逆序释放已取得的资源。业务自行建立的订阅和租约由业务持有者释放。

Editor 默认用模拟包。离线 Player 还需把相同目标平台的真实 `DefaultPackage` 放入 `StreamingAssets/yoo`。资源构建、目录和首包配置见 [YooAsset 接入说明](Assets/MieMieFrameTools/Scripts/Frame/B_Assets/Yooasset/RuntimeAdapter/README.md)。

Windows 上使用 Unity `6000.3.26f1` 构建时，如果 Bee 在 `BuildPlayerDataGenerator` 步骤同时缺少 `RuntimeInitializeOnLoads.json` 和 `TypeDb-All.json`，可在纯英文工程路径用相同配置重试。

调用 `Mm_SceneCtrl.LoadSceneAsync` 时，忙碌、空场景名、目标已激活或引擎未创建加载操作都会使任务失败。业务状态应在等待成功后继续切换。

## 画质设置

启动后获取 `GameHub.Get<IQualityService>()`。用 `Current` 读取已提交的状态，`SetLevel` 应用画质档位，`SetFramePolicy` 调整帧率和垂直同步，`ResetToDefaults` 恢复默认偏好。面板显示时订阅 `QualityEvents.Changed`，隐藏时解除订阅。[公共接口](Assets/MieMieFrameTools/Scripts/Frame/A_Frame/Business/Quality/Public/IQualityService.cs)
