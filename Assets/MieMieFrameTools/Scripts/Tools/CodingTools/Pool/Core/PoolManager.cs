namespace MieMieFrameWork.Pool
{
    using Sirenix.OdinInspector;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using static MieMieFrameWork.ModuleHub;

    /// <summary>
    /// 对象池注册与句柄管理器
    /// </summary>
    [ManagerAttribute(2)]
    public class PoolManager : IManagerBase, IDisposable
    {
        [Serializable]
        public sealed class PoolManagerConfig
        {
            /// <summary>
            /// 对象池根节点
            /// </summary>
            [SerializeField]
            [LabelText("对象池根节点")]
            private Transform allGameObjectRoot;

            public Transform AllGameObjectRoot => allGameObjectRoot;
        }

        /// <summary>
        /// 全局实例 由 ModuleHub 创建时赋值 调用处直接访问免查注册表
        /// </summary>
        public static PoolManager Instance { get; internal set; }

        /// <summary>
        /// 默认闲置缓存上限
        /// </summary>
        private const int DefaultMaxSize = ObjectPool.DefaultMaxInactive;

        /// <summary>
        /// 服务根节点
        /// </summary>
        private readonly Transform serviceRoot;

        /// <summary>
        /// 对象池根节点
        /// </summary>
        private Transform AllGameObjectRoot;

        public Transform PoolRoot => AllGameObjectRoot;

        /// <summary>
        /// GameObject 池句柄字典
        /// </summary>
        private readonly Dictionary<EntityId, PoolHandle> poolHandleDict = new();

        /// <summary>
        /// 普通对象池字典
        /// </summary>
        private readonly Dictionary<Type, ObjectPool> objectPoolDict = new();

        /// <summary> 管理器是否已销毁 </summary>
        private bool isDisposed;

        /// <summary> 批量销毁状态 禁止清理回调重新注册资源 </summary>
        private bool isReleasingPools;

        #region 生命周期

        /// <summary>
        /// 接收对象池配置与服务根节点
        /// </summary>
        public PoolManager(PoolManagerConfig poolManagerConfig, Transform serviceRoot)
        {
            this.serviceRoot = serviceRoot;
            AllGameObjectRoot = poolManagerConfig.AllGameObjectRoot;
            Instance = this;
        }

        /// <summary>
        /// 由 ModuleHub 初始化对象池根节点
        /// </summary>
        public void Init()
        {
            ThrowIfDisposed();
            if (AllGameObjectRoot is null)
                AllGameObjectRoot = serviceRoot.Find("PoolRoot");
        }

        /// <summary>
        /// 销毁注册池并释放全局实例
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;
            ThrowIfDisposed();
            ValidateRegisteredPoolsForDisposal(true, true);
            isDisposed = true;
            try
            {
                DestroyRegisteredPools(true, true);
            }
            finally
            {
                if (Instance == this)
                    Instance = null;
            }
        }

        #endregion

        #region GameObject 池

        /// <summary>
        /// 获取指定预制体的对象池句柄 保留旧接口的总量硬上限
        /// </summary>
        public PoolHandle GetPool(GameObject prefab, int maxSize = DefaultMaxSize)
        {
            ThrowIfDisposed();
            if (TryGetPool(prefab, out var Handle))
                return Handle;
            return GetPool(prefab, maxSize, maxSize);
        }

        /// <summary>
        /// 注册独立缓存与总量上限 总量为零表示不限制借出数量
        /// </summary>
        public PoolHandle GetPool(GameObject prefab, int maxInactive, int maxTotal,
            Action<GameObject> onGet = null, Action<GameObject> onRelease = null, Action<GameObject> onDestroy = null)
        {
            ThrowIfDisposed();
            var PoolKey = prefab.GetEntityId();
            if (TryGetPool(prefab, out var Handle))
            {
                if (!Handle.MatchesConfiguration(maxInactive, maxTotal, onGet, onRelease, onDestroy))
                    throw new InvalidOperationException($"[Pool] 重复注册配置不一致 {prefab.name}");
                return Handle;
            }

            var Pool = new GameObjPool(prefab, AllGameObjectRoot, maxInactive, maxTotal, onGet, onRelease, onDestroy);
            Handle = new PoolHandle(Pool);
            poolHandleDict.Add(PoolKey, Handle);
            return Handle;
        }

        /// <summary>
        /// 查找已注册池 不创建新池
        /// </summary>
        public bool TryGetPool(GameObject prefab, out PoolHandle handle)
        {
            ThrowIfDisposed();
            var PoolKey = prefab.GetEntityId();
            if (!poolHandleDict.TryGetValue(PoolKey, out handle))
                return false;
            if (!handle.IsDisposed)
                return true;
            poolHandleDict.Remove(PoolKey);
            handle = null;
            return false;
        }

        /// <summary>
        /// 收集所有 GameObject 池快照
        /// </summary>
        public void CollectGameObjPoolInfoList(List<GameObjPoolReporter> resultList)
        {
            ThrowIfDisposed();
            resultList.Clear();
            foreach (PoolHandle poolHandle in poolHandleDict.Values)
                resultList.Add(poolHandle.GetReporter());
        }

        #endregion

        #region  Object池

        /// <summary>
        /// 从对象池获取指定类型的对象
        /// </summary>
        public T GetObject<T>() where T : class, new()
        {
            ThrowIfDisposed();
            var ObjectType = typeof(T);
            if (!objectPoolDict.TryGetValue(ObjectType, out var Pool) || Pool.IsDisposed)
            {
                Pool = new ObjectPool<T>(() => new T());
                objectPoolDict[ObjectType] = Pool;
            }
            return (T)Pool.GetObj();
        }

        /// <summary>
        /// 注册泛型对象池 配置创建与业务重置回调
        /// </summary>
        public ObjectPool<T> RegisterObjectPool<T>(Func<T> createFunc, Action<T> onGet = null,
            Action<T> onRelease = null, Action<T> onDestroy = null,
            int maxInactive = DefaultMaxSize, int maxTotal = 0) where T : class
        {
            ThrowIfDisposed();
            var ObjectType = typeof(T);
            if (objectPoolDict.TryGetValue(ObjectType, out var ExistingPool) && !ExistingPool.IsDisposed)
                throw new InvalidOperationException($"[Pool] 对象类型已注册 {ObjectType.FullName}");
            var Pool = new ObjectPool<T>(createFunc, onGet, onRelease, onDestroy, maxInactive, maxTotal);
            objectPoolDict[ObjectType] = Pool;
            return Pool;
        }

        /// <summary>
        /// 将对象放回对象池
        /// </summary>
        public void PushObject(object obj)
        {
            ThrowIfDisposed();
            var ObjectType = obj.GetType();
            if (objectPoolDict.TryGetValue(ObjectType, out var Pool) && !Pool.IsDisposed)
                Pool.PushObj(obj);
            else
                objectPoolDict[ObjectType] = new ObjectPool(obj);
        }

        #endregion

        #region  清理

        /// <summary>
        /// 销毁并注销对象池 包含仍在借出的对象
        /// </summary>
        public void SelectClearPool(bool clearGameObject = true, bool clearObject = true)
        {
            ThrowIfDisposed();
            DestroyRegisteredPools(clearGameObject, clearObject);
        }

        /// <summary>
        /// 批量销毁注册池 清理期间禁止管理器重入
        /// </summary>
        private void DestroyRegisteredPools(bool clearGameObject, bool clearObject)
        {
            ValidateRegisteredPoolsForDisposal(clearGameObject, clearObject);
            isReleasingPools = true;
            var ErrorList = new List<Exception>();
            try
            {
                if (clearGameObject)
                {
                    foreach (var Handle in poolHandleDict.Values)
                        DisposeRegisteredPool(Handle, ErrorList);
                    poolHandleDict.Clear();
                }
                if (clearObject)
                {
                    foreach (var Pool in objectPoolDict.Values)
                        DisposeRegisteredPool(Pool, ErrorList);
                    objectPoolDict.Clear();
                }
            }
            finally
            {
                isReleasingPools = false;
            }
            if (ErrorList.Count > 0)
                throw new AggregateException("[Pool] 销毁注册池失败", ErrorList);
        }

        /// <summary>
        /// 清空所有闲置缓存 保留注册关系与借出记录
        /// </summary>
        public void ClearInactive()
        {
            ThrowIfDisposed();
            isReleasingPools = true;
            try
            {
                foreach (var Handle in poolHandleDict.Values)
                {
                    if (!Handle.IsDisposed)
                        Handle.Clear();
                }
                foreach (var Pool in objectPoolDict.Values)
                {
                    if (!Pool.IsDisposed)
                        Pool.Clear();
                }
            }
            finally
            {
                isReleasingPools = false;
            }
        }

        /// <summary>
        /// 销毁并注销所有 GameObject 池
        /// </summary>
        public void ClearAllGameObject() => SelectClearPool(true, false);

        /// <summary>
        /// 销毁并注销所有 Object 池
        /// </summary>
        public void ClearAllObject() => SelectClearPool(false, true);

        /// <summary>
        /// 销毁并注销指定预制体的池
        /// </summary>
        public void ClearGameObject(GameObject prefab)
        {
            ThrowIfDisposed();
            var PoolKey = prefab.GetEntityId();
            if (!poolHandleDict.TryGetValue(PoolKey, out var Handle))
                return;

            isReleasingPools = true;
            try
            {
                Handle.Dispose();
            }
            finally
            {
                if (Handle.IsDisposed)
                    poolHandleDict.Remove(PoolKey);
                isReleasingPools = false;
            }
        }

        /// <summary>
        /// 销毁并注销指定类型的 Object 池
        /// </summary>
        public void ClearObject<T>() => ClearObject(typeof(T));

        /// <summary>
        /// 销毁并注销指定类型的 Object 池
        /// </summary>
        public void ClearObject(Type type)
        {
            ThrowIfDisposed();
            if (!objectPoolDict.TryGetValue(type, out var Pool))
                return;
            isReleasingPools = true;
            try
            {
                Pool.Dispose();
            }
            finally
            {
                if (Pool.IsDisposed)
                    objectPoolDict.Remove(type);
                isReleasingPools = false;
            }
        }

        #endregion

        #region 内部约束

        /// <summary>
        /// 批量注销前确认所有池允许销毁 防止拒绝重入后丢失注册关系
        /// </summary>
        private void ValidateRegisteredPoolsForDisposal(bool clearGameObject, bool clearObject)
        {
            if (clearGameObject)
            {
                foreach (var Handle in poolHandleDict.Values)
                    Handle.ValidateDisposal();
            }
            if (clearObject)
            {
                foreach (var Pool in objectPoolDict.Values)
                    Pool.ValidateDisposal();
            }
        }

        /// <summary>
        /// 检查管理器生命周期 禁止销毁后重新持有资源
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(nameof(PoolManager));
            if (isReleasingPools)
                throw new InvalidOperationException("[Pool] 批量清理期间禁止重新访问管理器");
        }

        /// <summary>
        /// 销毁注册池 记录异常后继续释放其余池
        /// </summary>
        private static void DisposeRegisteredPool(IDisposable pool, List<Exception> errorList)
        {
            try
            {
                pool.Dispose();
            }
            catch (Exception Exception)
            {
                errorList.Add(Exception);
            }
        }

        #endregion

    }
}
