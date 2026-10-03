# 存档管理器接入

## 生命周期

`MieMieFrameWork.Save.ArchiveManager` 是框架生命周期适配器 实现 `ModuleHub.IManagerBase` 与 `IDisposable`

构造时只接收 `archiveSubFolder` 不创建目录 不读取索引 `RegisterAllManagers` 将适配器与音频等管理器一起入表

统一 `InitManagers` 阶段按优先级调用 `Init` 存档优先级为零 早于帧代理和其他运行时管理器 此时创建原有 `MiMieSaver.ArchiveMgr`

存档路径保持原语义 `Application.persistentDataPath` 加序列化子目录 空白子目录使用 `Archives` 非空子目录去除首尾空白

## 查询与兼容

- 原有 `ModuleHub.GetArchive<T>` 和 `HasArchive` 保持兼容 从已注册的适配器查询实际存档核心
- 可通过 `ModuleHub.GetManager<ArchiveManager>` 获取适配器 `Archive` 返回 `IArchiveMgr` `IsReady` 表示核心是否已创建
- 注册不等于初始化 创建前查询存档返回 null `HasArchive` 为 false 业务访问前等待 `ReadyTask`
- 不修改 `IArchiveMgr` `IArchiveModule` 存档格式 槽位操作或迁移规则

## 释放责任

有效 FrameRoot 销毁时按框架管理器逆序调用 `Dispose` 适配器只解除存档核心持有 不隐式保存 不删除存档 不替业务所有者释放模块

业务入口仍负责注册与注销自身的 `IArchiveModule` 并释放自己持有的运行时资源

完整启动顺序与失败语义见 [框架启动与生命周期](../A_Frame/Boot/README.md)
