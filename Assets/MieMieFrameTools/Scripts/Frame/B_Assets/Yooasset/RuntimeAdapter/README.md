# YooAsset 资源接入

## 唯一后端

HakiSheep 只使用工程内嵌的 YooAsset 资源管线 不安装 Addressables 不恢复旧 MmAssetMgr

YooAssetMgr 是薄门面 负责默认资源包绑定和启动就绪校验 加载返回原生 AssetHandle 不自研打包 下载 依赖计数或全局资源缓存

可选的业务级 LRU 通过 YooAssetMgr.CreateCache 创建独立实例 按条目容量保留缓存自己的原生句柄 不改变现有直接加载接口 详细协议见 [LRU 资源缓存](LRU.md)

## 配置与启动

- FrameRoot 上 ModuleHub 的 resourcePackageName 默认是 DefaultPackage 必须对应 BundleCollectorSetting 的包名
- resourceManifestTimeout 是版本和清单请求的超时秒数
- DefaultPackage 默认采集 Assets/MieMieFrameTools/ADefaultRes 按文件名生成地址 支持省略扩展名 同包资源文件名必须唯一
- 游戏 UI 音频等资源应通过 YooAsset 采集器加入默认包 UI 地址必须与 UIDataBase 类型名一致
- 编辑器使用模拟文件系统 进入 Play 时生成模拟清单
- 原生平台构建使用 OfflinePlayModeOptions 构建默认包后必须把版本文件 清单和 Bundle 复制到 YooAsset 内置目录 StreamingAssets/yoo/DefaultPackage
- 当前不配置远端服务 WebGL 和小游戏文件系统 这些平台须新增对应的显式启动配置 不能把当前离线模式视为已验证的跨平台方案
- 启动顺序与取消语义的权威说明见 [框架启动与生命周期](../../../A_Frame/Boot/README.md)

业务入口必须先等待 ModuleHub.Instance.ReadyTask 同步入口不会偷偷初始化资源系统

## 原生句柄责任

- YooAssetMgr.LoadAsset 和 LoadAssetAsync 返回 AssetHandle 每次取得的句柄由调用方持有并释放
- 异步句柄须等待完成并检查 Status 失败也必须释放 不能仅取 AssetObject 后丢弃句柄
- 预制体资源必须保留到所有对应实例销毁 不能实例化后立即释放
- 视觉特效按定义持有原生句柄 活跃和闲置池实例共用 只在独占池注销且实例实际销毁后释放 详细责任见 [视觉特效管理](../../../G_Effect/README.md)
- UILoad.Load 和 LoadAsync 将句柄移交给实例的 YooAssetInstanceOwner UILoad.Release 等待实例实际销毁后释放句柄 活跃实例也会在 OnDestroy 释放 从未激活的实例须通过 UILoad.Release 销毁
- AudioManager 每次路径加载独立持有原生句柄 BGM 和环境音由各自通道持有 特效音持有到播放结束 由 YooAsset 合并底层资源加载和引用
- LoadBgClipAsync 每次返回一份调用方资源持有 使用结束必须调用同一 AudioManager 的 ReleaseBgClip 管理器 Dispose 同时回收尚未释放的持有 不能跨管理器生命周期使用返回片段
- 停止通道会取消该通道未完成请求 替换片段先停止旧播放再释放旧句柄 加载失败和取消均释放本次独立句柄
- Dispose 先取消异步请求 停止背景通道和特效播放器 归还对象池后释放全部音频持有 延迟回调不会在退出后触发
- 音频配置与预算的权威说明见 [音频管理](../../../F_Audio/README.md)
- 原生包开启 AutoUnloadBundleWhenUnused 不另建全局引用计数字典 不提供强制清空所有使用中资源的接口
- YooAsset 包与驱动器由原生运行会话管理 不在 ModuleHub 销毁时强制销毁全局系统 避免影响独立资源包及仍存活的 UI 实例

资源工具中枢直接打开 YooAsset 原生 Collector Builder 和 Debugger 旧 MmAsset 构建器 配置资产和对象池资源池页已移除 普通对象池预热与监控保留

## 资源对象与集合引用结构

以下以通过 LRU 成功加载 Monster 怪物预制体为例 加载类型为 GameObject 仅有一份业务句柄和一份缓存句柄

缩进表示成员或集合条目 箭头表示对象引用 相同编号始终表示同一个对象 不是每层复制一份资源 以下只展示相关成员

### YooAsset 底层管理结构

```text
ResourcePackage "DefaultPackage"
└── ResourceManager
    ├── _providerDict
    │   └── [怪物资源的加载标识] → AssetProvider①
    │       ├── AssetObject → 怪物预制体 GameObject①
    │       ├── _handles = HashSet<HandleBase>
    │       │   ├── AssetHandle①  业务持有
    │       │   └── AssetHandle②  缓存持有
    │       └── RefCount = 2
    │
    └── _bundleLoaderDict
        └── 对应 Bundle 及依赖的加载对象
```

同包内同一资源身份与加载方式可复用 Provider 每次取得独立的 AssetHandle Provider 记录有效句柄并维护引用计数 ResourceManager 管理 Provider 和 Bundle 加载对象

### 业务与缓存的引用结构

```text
怪物业务对象
└── 持有字段 → AssetHandle①
                  └── Provider → AssetProvider①

YooAssetLruCache
├── cacheEntryDict
│   └── Key = (怪物预制体路径, typeof(GameObject))
│       Value → LinkedListNode①
│
└── leastRecentList
    └── … ↔ LinkedListNode① ↔ …
             └── Value = CacheEntry①
                 ├── Key = 同一个资源身份
                 └── Handle → AssetHandle②
                                  └── Provider → AssetProvider①
```

- 业务句柄和缓存句柄都是原生 AssetHandle 不同实例由不同持有者负责释放 业务持有字段只是示例 也可按业务需要使用集合
- 字典与双向链表引用同一个节点 字典负责定位 链表负责从最久未使用到最近使用排序 CacheEntry 保存资源身份和缓存自己的句柄
- 两个句柄引用同一个 Provider 访问同一份预制体资源 不代表复制或加载两份预制体
- 实例化后得到的场景怪物是独立 GameObject 实例 不属于上述缓存条目 业务仍须持有对应资源直到所有实例销毁 不能依赖 LRU 暂时保留来代替实例生命周期
- RefCount 为零不等于所有依赖立即卸载 实际卸载仍受 Bundle 和其他资源持有影响 清理 取消与释放的详细协议见 [LRU 资源缓存](LRU.md)

## 验证步骤

1. 有 FrameRoot 的场景进入 Play 等待 ReadyTask 确认默认包清单加载成功且管理器只初始化一次
2. 使用默认采集地址 UITemple 调用 UILoad.LoadAsync 再调用 Release 确认实例销毁后句柄释放
3. 加入真实音频采集项 测试同步异步加载和同地址复用 销毁根节点后确认音频停止且句柄释放
4. 使用不存在的资源地址 确认异常包含地址并且失败句柄已释放
5. 启动或音频加载中销毁根节点 确认不再创建管理器或播放音频
6. 禁用 Domain Reload 后连续两次进入 Play 确认默认包门面状态按运行会话重置
7. 构建内置资源包并在目标平台验证离线加载 联网更新与平台文件系统尚未接入
