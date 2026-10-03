namespace MieMieFrameWork.Effects
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using MieMieFrameWork.Pool;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using static MieMieFrameWork.ModuleHub;

    /// <summary>
    /// 视觉特效管理器 统一请求预算 播放生命周期与资源责任
    /// </summary>
    [ManagerAttribute(3)]
    public sealed partial class EffectManager : IManagerBase, IDisposable
    {
        /// <summary> 启动配置 仅 Init 读取 </summary>
        private readonly EffectManagerConfig config;

        /// <summary> 全局实例父节点 不使用业务对象作为池实例父节点 </summary>
        private readonly Transform serviceRoot;

        /// <summary> 帧生命周期代理 </summary>
        private readonly MonoManager frameDriver;

        /// <summary> 统一实例池所有者 </summary>
        private readonly PoolManager poolManager;

        /// <summary> 管理器生命周期取消源 </summary>
        private readonly CancellationTokenSource lifetimeCancellation = new();

        /// <summary> 特效定义快照 </summary>
        private readonly Dictionary<string, EffectDefinition> definitionDict = new();

        /// <summary> 当前持有的资源与实例池 </summary>
        private readonly Dictionary<string, EffectResourceEntry> resourceDict = new();

        /// <summary> 已退休且可能仍等待 Unity 销毁的资源条目 </summary>
        private readonly List<EffectResourceEntry> retiredList = new();

        /// <summary> 活跃与加载中的播放记录 </summary>
        private readonly List<EffectPlayback> playbackList = new();

        /// <summary> 按本轮标识查询播放记录 </summary>
        private readonly Dictionary<ulong, EffectPlayback> playbackDict = new();

        /// <summary> 低中高档位快照 </summary>
        private EffectQualityProfile[] profileList;

        /// <summary> 单调递增的请求标识 </summary>
        private ulong nextId;

        /// <summary> 当前帧预算对应的帧序号 </summary>
        private int requestFrame = -1;

        /// <summary> 本帧已接受的播放请求数量 </summary>
        private int frameRequests;

        /// <summary> 当前帧创建或启动实例的帧序号 </summary>
        private int spawnFrame = -1;

        /// <summary> 本帧已创建或启动的实例数量 </summary>
        private int frameSpawns;

        /// <summary> 全局并发请求上限快照 </summary>
        private int maxConcurrent;

        /// <summary> 每帧接受请求上限快照 </summary>
        private int maxPerFrame;

        /// <summary> 唯一初始化入口是否已经执行 </summary>
        private bool initialized;

        /// <summary> 管理器是否已经退出 </summary>
        private bool disposed;

        public EEffectQuality Quality { get; private set; }
        public bool IsPaused { get; private set; }
        public int RequestCount => playbackList.Count;
        public int ResourceCount => resourceDict.Count;
        public long RejectedRequests { get; private set; }

        #region 生命周期

        /// <summary>
        /// 绑定框架依赖 不在构造阶段初始化或加载资源
        /// </summary>
        public EffectManager(EffectManagerConfig config, Transform serviceRoot, MonoManager frameDriver,
            PoolManager poolManager)
        {
            this.config = config;
            this.serviceRoot = serviceRoot;
            this.frameDriver = frameDriver;
            this.poolManager = poolManager;
        }

        /// <summary>
        /// 由 ModuleHub 唯一调用 验证配置后注册帧和场景生命周期
        /// </summary>
        public void Init()
        {
            if (initialized || disposed)
                throw new InvalidOperationException("[EffectManager] 不允许重复初始化或退出后初始化");
            if (config == null || serviceRoot == null || frameDriver == null || poolManager == null)
                throw new InvalidOperationException("[EffectManager] 缺少启动配置或框架依赖");
            profileList = config.SnapshotProfiles();
            var LocationHashList = new HashSet<string>();
            foreach (var Definition in config.DefinitionList)
            {
                if (Definition == null)
                    throw new InvalidOperationException("[EffectManager] 特效定义不能为空");
                var Snapshot = Definition.Snapshot();
                if (!definitionDict.TryAdd(Snapshot.Id, Snapshot) || !LocationHashList.Add(Snapshot.Location))
                    throw new InvalidOperationException($"[EffectManager] 重复特效标识或地址 {Snapshot.Id} {Snapshot.Location}");
            }
            maxConcurrent = config.MaxConcurrent;
            maxPerFrame = config.MaxPerFrame;
            Quality = config.Quality;
            initialized = true;
            frameDriver.AddUpdateListener(Tick);
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        /// <summary>
        /// 取消请求并注销独占池 资源句柄在实际销毁后释放
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            if (initialized)
            {
                frameDriver.RemoveUpdateListener(Tick);
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
            }
            var ErrorList = new List<Exception>();
            try { lifetimeCancellation.Cancel(); }
            catch (Exception Error) { ErrorList.Add(Error); }
            while (playbackList.Count > 0)
            {
                try { ReleasePlayback(playbackList[playbackList.Count - 1]); }
                catch (Exception Error) { ErrorList.Add(Error); }
            }
            foreach (var Entry in resourceDict.Values)
            {
                retiredList.Add(Entry);
                try { Entry.Retire(poolManager); }
                catch (Exception Error) { ErrorList.Add(Error); }
            }
            resourceDict.Clear();
            definitionDict.Clear();
            lifetimeCancellation.Dispose();
            if (ErrorList.Count > 0)
                throw new AggregateException("[EffectManager] 特效退出清理失败", ErrorList);
        }

        /// <summary>
        /// 等待本管理器已退休资源完成实际销毁和句柄释放
        /// </summary>
        public async UniTask WaitForReleaseAsync(CancellationToken cancellationToken = default)
        {
            foreach (var Entry in retiredList.ToArray())
                await Entry.ReleaseTask.AttachExternalCancellation(cancellationToken);
        }

        /// <summary>
        /// 非播放句柄入口必须处于已初始化的有效生命周期
        /// </summary>
        private void CheckReady()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(EffectManager));
            if (!initialized)
                throw new InvalidOperationException("[EffectManager] 使用前必须等待 ModuleHub.ReadyTask");
        }

        #endregion
    }
}
