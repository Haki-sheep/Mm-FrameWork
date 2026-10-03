# 可选 YooAsset LRU 缓存

## 定位与预算

YooAssetLruCache 只实现上层资源保留与最近最少使用淘汰 不重写加载器 包依赖 引用计数或磁盘 BundleCache

容量由调用方显式指定 单位是资源条目数 不是内存字节数 不保证运行时内存总量 同一资源包内的原生资产路径和加载类型共同确定一个条目 地址与路径别名归一到相同条目

字典与双向链表负责常数复杂度的命中和顺序更新 超额时从最久未使用的条目开始淘汰 缩容和清空按实际释放的条目数执行

零容量允许加载但不保留缓存 不设置隐藏全局默认容量 不自动改变现有 UI 音频 对象池或资源门面的加载行为

## 使用方式

业务先等待 ModuleHub.ReadyTask 然后调用 YooAssetMgr.CreateCache(capacity) 创建自己的缓存 非默认资源包可在显式完成该包初始化与清单加载后调用 new YooAssetLruCache(package, capacity)

```csharp
await ModuleHub.Instance.ReadyTask;
using var Cache = YooAssetMgr.CreateCache(capacity);
var Handle = await Cache.LoadAssetAsync<UnityEngine.AudioClip>(audioAddress, cancellationToken);
try
{
    await PlayUntilStopped(Handle.GetAssetObject<UnityEngine.AudioClip>());
}
finally
{
    Handle.Release();
}
```

capacity audioAddress cancellationToken 与 PlayUntilStopped 是业务提供的容量 地址 取消令牌和播放结束等待 不是框架新增接口 实际播放器停止并清空 clip 后才释放业务句柄

常驻业务可用字段持有 Cache 在唯一初始化入口创建 在自身 Dispose 或 OnDestroy 中释放 所有资源和缓存操作都必须在 Unity 主线程执行

## 独立持有责任

- LoadAsset 和 LoadAssetAsync 每次成功返回调用方独立拥有的原生 AssetHandle 无论是否命中都必须由调用方 Release
- 缓存为成功资源额外取得一份私有原生句柄 底层资源加载与依赖引用仍由 YooAsset 处理 不在缓存层计算业务引用数量
- 淘汰 Remove Clear SetCapacity 或 Dispose 只释放缓存自己的句柄 已交付业务的句柄继续有效 实际卸载时机取决于原生依赖及其他持有
- 预制体实例和对象池活跃或闲置实例仍须各自持有对应资源直到实例销毁 不能用缓存容量替代实例生命周期
- 多个缓存实例相互独立 清理一个实例不释放另一个缓存或业务的持有
- 缓存持有不默认随 FrameRoot 创建或销毁 业务负责释放自己创建的实例 不把缓存挂成新的全局单例

## 操作与异步语义

- Capacity 和 Count 分别表示条目容量和已保留条目数 PendingCount 表示尚未交付业务或完成缓存登记的原生句柄数 同步等待中的句柄也会登记
- HitCount 和 MissCount 按请求进入时是否已缓存记录 解析地址失败和预取消请求不计数 加载中同地址请求仍可能各记一次未命中
- Contains 只查询 不更新 LRU 顺序 加载命中与成功保留更新顺序
- SetCapacity 立即淘汰超额条目 不取消请求 后续请求完成时使用当时的容量
- Remove 移除指定类型的缓存条目 并禁止操作之前已经进入的异步请求重新填充缓存 不取消这些业务请求
- Clear 清空全部缓存并使此前加载失去填充资格 这些请求成功后仍能交付自己的业务句柄 包括同步等待原生 Completed 回调中发生的 Clear
- 同步等待若经原生 Completed 回调重入 Dispose 尚未交付的句柄立即释放 同步调用抛出 ObjectDisposedException 不交付失效结果
- Dispose 立即取消请求并释放缓存和尚未交付的句柄 调用方须正常等待正在运行的 UniTask 并处理取消 后续加载或修改操作抛出 ObjectDisposedException
- 加载失败或取消不会留下新缓存条目 也不会释放其他请求的持有
- 包清单切换或业务资源版本切换前由缓存持有者 Clear 缓存不会跨版本猜测资源兼容性

本缓存是资源引用保留策略 不是磁盘下载缓存 不能承诺每个条目淘汰都立即释放一个独占 Bundle 更不能强制清空全局使用中的资源
