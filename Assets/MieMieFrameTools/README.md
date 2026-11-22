# 哈基咩咩框架

Unity 游戏运行时框架，提供启动与服务组装、业务模块和配套编辑器工具。

接入时，在启动场景的框架根节点挂载 `ModuleHub`，按[资源接入说明](Scripts/Frame/B_Assets/Yooasset/RuntimeAdapter/README.md)配置默认资源包。游戏自己的启动入口等待 `ModuleHub.Instance.ReadyTask` 完成后，再组装业务模块并取得服务。

需要阅读或修改源码时，从[框架代码索引](框架代码索引.md)按问题定位模块。
