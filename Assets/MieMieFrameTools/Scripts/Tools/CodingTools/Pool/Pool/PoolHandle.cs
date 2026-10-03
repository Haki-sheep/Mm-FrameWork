namespace MieMieFrameWork.Pool
{
    using System;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary>
    /// GameObject 对象池句柄
    /// </summary>
    public sealed class PoolHandle : IDisposable
    {
        /// <summary>
        /// 实际对象池
        /// </summary>
        private readonly GameObjPool gameObjPool;

        /// <summary>
        /// 对象池句柄
        /// </summary>
        internal PoolHandle(GameObjPool gameObjPool)
        {
            this.gameObjPool = gameObjPool;
        }

        /// <summary>
        /// 对象池 Key
        /// </summary>
        public EntityId PoolKey => gameObjPool.PoolKey;

        /// <summary>
        /// 预制体名称
        /// </summary>
        public string PrefabName => gameObjPool.PrefabName;

        /// <summary>
        /// 池内闲置数量
        /// </summary>
        public int PooledCount => gameObjPool.PooledCount;

        /// <summary>
        /// 当前借出数量
        /// </summary>
        public int ActiveCount => gameObjPool.ActiveCount;

        /// <summary>
        /// 当前存活数量 包含闲置与借出
        /// </summary>
        public int TotalCreated => gameObjPool.TotalCreated;

        /// <summary>
        /// 池内闲置上限
        /// </summary>
        public int MaxSize => gameObjPool.MaxInactive;

        public int MaxInactive => gameObjPool.MaxInactive;
        public int MaxTotal => gameObjPool.MaxTotal;
        public bool IsDisposed => gameObjPool.IsDisposed;

        #region 获取与归还

        /// <summary>
        /// 从对象池获取 GameObject 先配置再按需激活
        /// </summary>
        public GameObject Get(Transform parent = null, bool activate = true, Action<GameObject> configure = null)
        {
            var Value = gameObjPool.GetGameObj(parent);
            if (Value == null)
                return null;
            try
            {
                gameObjPool.Configure(Value, configure);
                if (activate)
                    gameObjPool.Activate(Value);
                return Value;
            }
            catch (Exception Exception)
            {
                gameObjPool.DiscardAfterFailedGet(Value, Exception);
                throw;
            }
        }

        /// <summary>
        /// 从对象池获取指定组件 业务配置完成后再激活
        /// </summary>
        public T Get<T>(Transform parent = null, bool activate = true, Action<T> configure = null) where T : Object
        {
            var Value = gameObjPool.GetGameObj(parent);
            if (Value == null)
                return null;
            try
            {
                var Component = typeof(T) == typeof(GameObject)
                    ? Value as T : Value.GetComponent(typeof(T)) as T;
                if (Component == null)
                    throw new InvalidOperationException($"[Pool] 预制体 {PrefabName} 缺少组件 {typeof(T).FullName}");
                gameObjPool.Configure(Component, configure);
                if (activate)
                    gameObjPool.Activate(Value);
                return Component;
            }
            catch (Exception Exception)
            {
                gameObjPool.DiscardAfterFailedGet(Value, Exception);
                throw;
            }
        }

        /// <summary>
        /// 将 GameObject 归还到当前对象池
        /// </summary>
        public bool Release(GameObject obj)
        {
            return gameObjPool.TryPushGameObj(obj);
        }

        /// <summary>
        /// 将 Component 对应的 GameObject 归还到当前对象池
        /// </summary>
        public bool Release(Component component)
        {
            return Release(component.gameObject);
        }

        /// <summary>
        /// 获取本轮租借版本 供异步任务保存
        /// </summary>
        public ulong GetLeaseVersion(GameObject obj) => gameObjPool.GetLeaseVersion(obj);

        /// <summary>
        /// 获取组件对应实例的租借版本
        /// </summary>
        public ulong GetLeaseVersion(Component component) => GetLeaseVersion(component.gameObject);

        /// <summary>
        /// 检查异步任务对应租借是否仍有效
        /// </summary>
        public bool IsLeaseValid(GameObject obj, ulong version) => gameObjPool.IsLeaseValid(obj, version);

        /// <summary>
        /// 检查组件对应租借是否仍有效
        /// </summary>
        public bool IsLeaseValid(Component component, ulong version)
        {
            return component != null && IsLeaseValid(component.gameObject, version);
        }

        /// <summary>
        /// 仅归还指定轮次的租借 过期或销毁后返回失败
        /// </summary>
        public bool TryRelease(GameObject obj, ulong version)
        {
            return IsLeaseValid(obj, version) && Release(obj);
        }

        /// <summary>
        /// 仅归还组件对应的指定轮次租借
        /// </summary>
        public bool TryRelease(Component component, ulong version)
        {
            return component != null && TryRelease(component.gameObject, version);
        }

        /// <summary>
        /// 归还回调失败后销毁指定轮次实例 不重复执行归还回调
        /// </summary>
        public bool DiscardAfterFailedRelease(GameObject obj, ulong version)
        {
            return gameObjPool.DiscardAfterFailedRelease(obj, version);
        }

        #endregion

        #region 生命周期与诊断

        /// <summary>
        /// 预热对象池
        /// </summary>
        public void Prewarm(int count)
        {
            gameObjPool.PreWarm(count);
        }

        /// <summary>
        /// 获取对象池运行时快照
        /// </summary>
        public GameObjPoolReporter GetReporter()
        {
            return gameObjPool.GetPoolReporter();
        }

        /// <summary>
        /// 清空闲置缓存 保留借出实例与句柄
        /// </summary>
        public void Clear()
        {
            gameObjPool.Clear();
        }

        /// <summary>
        /// 销毁整个池 包括借出实例 并使当前句柄失效
        /// </summary>
        public void Dispose() => gameObjPool.Dispose();

        /// <summary>
        /// 验证当前池允许销毁 供管理器批量注销前检查
        /// </summary>
        internal void ValidateDisposal() => gameObjPool.ValidateDisposal();

        /// <summary>
        /// 检查重复注册配置
        /// </summary>
        internal bool MatchesConfiguration(int maxInactive, int maxTotal, Action<GameObject> onGet,
            Action<GameObject> onRelease, Action<GameObject> onDestroy)
        {
            return gameObjPool.MatchesConfiguration(maxInactive, maxTotal, onGet, onRelease, onDestroy);
        }

        #endregion
    }
}
