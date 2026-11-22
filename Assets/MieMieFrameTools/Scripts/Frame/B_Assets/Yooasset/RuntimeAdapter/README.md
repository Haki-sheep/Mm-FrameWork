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
- resourceUpdateConfig 默认 Offline 且 simulateInEditor 为 true 编辑器进入 Play 时生成模拟清单
- Offline 原生平台使用内置文件系统 构建默认包后把版本文件 清单和 Bundle 复制到 StreamingAssets/yoo/DefaultPackage
- Host 使用内置与 Sandbox 文件系统 remoteUrl 指向资源包发布目录 fallbackUrl 仅在显式配置时使用 下载并发默认 8 重试默认 2
- 编辑器测试真实下载须关闭 simulateInEditor Host 按远端版本加载清单并完成全部下载后才开放业务加载
- 当前适配器面向原生平台 WebGL 和小游戏须新增对应的显式文件系统配置
- 启动顺序与取消语义见本文 [启动与代码热更](#启动与代码热更)

业务入口必须先等待 ModuleHub.Instance.ReadyTask 同步入口不会偷偷初始化资源系统

## 启动与代码热更

启动次序是资源包就绪 → 按方案和热更开关加载热更程序集 → 按方案初始化配置 → 注册并初始化选用的管理器 → 启动已加载的热更入口 → ReadyTask 完成 任一阶段失败会传播异常并中止后续阶段

ModuleHub 的 hotUpdateConfig 默认关闭 启用时配置 playerBuildId 与 manifestLocation 默认清单地址 MmHotUpdateManifest 入口实现 IHotUpdateEntry 入口 StartAsync 执行时管理器已初始化 不能等待同一个 ModuleHub.ReadyTask

热更模块位于 A_Frame/HotUpdate 使用开源 HybridCLR v9.0.0 依赖固定到官方 Gitee 标签 原生运行库由官方安装器使用 Gitee 源安装到工程 Library

部署者需在自己的 Unity 和目标平台环境中验证 HybridCLR 安装 构建及 IL2CPP Player 热更流程 并处理遇到的兼容问题

运行时先校验清单格式 首包身份 平台 地址唯一性与全部二进制 SHA256 再补充 AOT 元数据并按依赖顺序 Assembly.Load 创建入口 SHA256 用于完整性检查 不提供发布方身份认证 对外发布应使用受信任的 HTTPS 服务

AOT 程序集只引用公共入口协议 不引用业务热更程序集 业务 asmdef 应关闭 autoReferenced 且进入 HybridCLR 剔除列表 Editor 使用已经编译的同名程序集时仅验证下载和校验 解释执行必须在安装 HybridCLR 的 IL2CPP Player 中验证

下载取消在 Unity 主线程调用原生 CancelDownload 版本和清单操作没有公共取消接口 会等待操作收尾再抛取消异常并禁止下一阶段 资源句柄在成功 失败和取消后均释放 ModuleHub 退出先释放热更入口再清理业务管理器 程序集与已补充元数据无法卸载或回滚 更新代码与加载失败后均须重新启动 Player

## 发布与独立验证

1. 在独立 Unity 工程导入 HotUpdate YooAsset Runtime/Editor/RuntimeAdapter 与 Diagnostics Runtime 并安装 UniTask 和 Scriptable Build Pipeline
2. 通过 HotUpdateDemoTools.InstallSource 导入示例源码 等待编译后调用 CreateFixture 创建测试场景与 DemoProfile
3. 打开 Tools/MieMieFrameWork/工具中枢 选择 资源/热更新 页面 点击 创建发布配置 SO 或选择已有 Profile 配置 HybridCLR 并安装本机原生运行库 Project 创建菜单统一为 Create/MieMieFramework/资源/热更新发布配置 独立入口 Tools/MmFramework/热更新 保留
4. 准备首包基线 执行官方 GenerateAll 归档裁剪后的 AOT 元数据与 Unity 版本 基线目录禁止覆盖 失败的 staging 目录不作为有效基线
5. 用未占用的首包资源版本（例如 0）构建 YooAsset 并复制内置资源 再构建 IL2CPP Player 构建后核对实际裁剪 DLL 与首包归档哈希 不一致时禁止发布 以下示例假定服务器资源版本 1、2、3 均未占用
6. 在热更新窗口填写资源版本 1 点击 导出代码并发布资源到本机服务器 默认发布到 Builds/HotUpdateServer/当前平台/包名 先写入不可变 Bundle 与清单 最后原子切换 version 文件 同版本不同内容会拒绝发布 旧资源保留
7. 示例 Player 目录双击 Start-Server.cmd 保持本机服务运行 再双击 MmHotUpdateDemo.exe 应显示 CODE_V1|RESOURCE_V1 默认地址 http://127.0.0.1:18780 也可用 Start-LocalDemo.cmd 同时启动服务与 Player 退出 Player 时自动停止它创建的服务 本机需安装 Python
8. 只修改 Assets/MmHotUpdateValidation/Data/ResourceMarker.txt 为 RESOURCE_V2 导入后填写资源版本 2 发布到同一个服务器目录 关闭并重启同一个 EXE 应显示 CODE_V1|RESOURCE_V2
9. 只修改 Assets/MmHotUpdateValidation/HotLogic/DemoEntry.cs 的 CodeMarker 为 CODE_V2 更新 Profile.codeVersion 为 2 资源版本为 3 发布后重启同一个 EXE 应显示 CODE_V2|RESOURCE_V2 补丁发布按钮不会重建 Player 也不会复制内置资源

serverOutputDirectory 是本机发布根目录 HTTP 服务须指向其平台与包名子目录 示例启动脚本按默认路径定位 改目录或端口时用 Start-LocalDemo.ps1 的 PackageDirectory 与 Port 参数 Player 地址可用 --hotupdate-url 显式覆盖 示例输出参数 --hotupdate-result=<结果文件> 与 --hotupdate-quit 用于自动化验证

热更导出会将工具生成的旧 CollectAll 采集规则升级为清单过滤。输出目录顶层的 MmHot_ 与 MmAot_ 程序集字节文件按本次清单采集，移出清单的文件保留在磁盘，后续版本包会排除它们。自定义采集规则由规则所有者维护。

代码与普通资源可以同一次修改并发布 codeVersion 只是代码发布标记 不直接决定 DemoEntry.CodeMarker 的显示值 资源发布版本标记包含 DLL 清单与普通资源的整包 每次构建须使用未占用的新版本 版本目录已存在时在编译前拒绝并显示本次未发布 服务器继续提供上一次成功版本 改完内容后不能重复使用旧资源版本 codeVersion 可保持不变 不需要清理客户端缓存

独立工程 HotLoad 完成同一 IL2CPP Player 的 CODE_V1|RESOURCE_V1 → CODE_V1|RESOURCE_V2 → CODE_V2|RESOURCE_V2 验证 实际资源版本为 101 102 103 EXE 与 GameAssembly.dll 的 SHA256 保持不变 错误基线和损坏 DLL 均以退出码 1 拒绝且未执行业务入口 当前正常服务器版本为 103 后续发布使用未占用的新资源版本

窗口的长构建任务通过 HotUpdateBuildJob 执行 启动时先保存 Profile 正常重载 Editor 再由已编译的工具运行 在窗口或返回的 resultPath 查看 WaitingForReload Running Succeeded Failed 状态 运行中被重载中断会明确记为失败 不自动重放 该入口同时隔离临时内存程序集 CodeBase 反射不兼容的问题 自动化构建应调用该任务入口并检查终态

Windows IL2CPP Player 构建还需要系统安装 Visual Studio 2022 或 Build Tools 2022 的 MSVC C++ x86/x64 编译器及 Windows SDK 10.0.19041 以上版本 在 Unity Hub 勾选 Windows Build Support IL2CPP 不能代替这些系统组件

补丁导出使用本次新编译 DLL 并沿用首包归档的 AOT 元数据 Unity 精确版本与 Development 模式必须一致 Player 新增 AOT API 泛型实例或裁剪保留需求时须重新评估首包覆盖范围 不能只用 Editor 运行证明兼容

可使用仓库 Tools/HotUpdate/Run-HotUpdateProbe.ps1 自动启动本地服务与示例 Player 按预期结果断言并归档日志 需要 Python 参数 PlayerPath 指向同一个 exe PackageDirectory 指向当前资源发布目录 ExpectedResult 分别填写上述三个结果 EvidenceDirectory 指定日志目录 脚本完成后仅结束自己创建的测试进程

测试错误发布包时以 ExpectedError 替代 ExpectedResult 脚本要求 Player 非零退出 无业务结果文件 且日志包含指定错误原因 不能把任意构建或启动错误当作预期拒绝

异常验证包括错误首包身份 错误平台 损坏 DLL 哈希与远端不可用 失败时不得执行热更入口或将启动标记为完成 本文描述验证步骤 实际通过范围以当次验证结果为准

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
7. 构建内置资源包验证离线加载 Host 与代码更新按本文独立验证步骤测试
