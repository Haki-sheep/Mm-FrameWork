namespace MieMieFrameWork.Pool
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    internal sealed class GameObjPool : IDisposable
    {
        /// <summary> 关联预制体 </summary>
        private readonly GameObject prefab;

        /// <summary> 池 Key </summary>
        private readonly EntityId poolKey;

        /// <summary> 二级父节点 保持未激活以隔离创建时的 OnEnable </summary>
        private readonly Transform typeFather;

        /// <summary> 闲置队列 </summary>
        private readonly Queue<GameObject> poolQueue = new();

        /// <summary> 借出实例记录 同时承担销毁责任 </summary>
        private readonly Dictionary<EntityId, GameObject> activeInstanceDict = new();

        /// <summary> 闲置实例 ID 防止重复归还 </summary>
        private readonly HashSet<EntityId> pooledInstanceIdHashList = new();

        /// <summary> 本轮租借版本 防止旧异步任务归还新租借 </summary>
        private readonly Dictionary<EntityId, ulong> leaseVersionDict = new();

        /// <summary> 租借版本递增值 </summary>
        private ulong nextLeaseVersion;

        /// <summary> 取出实例回调 </summary>
        private readonly Action<GameObject> onGet;

        /// <summary> 归还实例回调 </summary>
        private readonly Action<GameObject> onRelease;

        /// <summary> 销毁实例前的业务清理回调 </summary>
        private readonly Action<GameObject> onDestroy;

        /// <summary> 生命周期回调执行状态 禁止同池重入 </summary>
        private bool isInvokingCallback;

        public string PrefabName => prefab != null ? prefab.name : "Unknown";
        public EntityId PoolKey => poolKey;
        public int PooledCount => poolQueue.Count;
        public int ActiveCount => activeInstanceDict.Count;
        public int TotalCreated => PooledCount + ActiveCount;
        public int MaxInactive { get; }
        public int MaxTotal { get; }
        public bool IsDisposed { get; private set; }

        #region 创建与租借

        /// <summary>
        /// 构造函数 总量为零表示不限制借出数量
        /// </summary>
        public GameObjPool(GameObject prefab, Transform allGameObjectRoot, int maxInactive, int maxTotal,
            Action<GameObject> onGet = null, Action<GameObject> onRelease = null, Action<GameObject> onDestroy = null)
        {
            if (maxInactive < 0 || maxTotal < 0)
                throw new ArgumentOutOfRangeException(nameof(maxInactive), "对象池容量不能为负数");

            this.prefab = prefab;
            poolKey = prefab.GetEntityId();
            MaxInactive = maxInactive;
            MaxTotal = maxTotal;
            this.onGet = onGet;
            this.onRelease = onRelease;
            this.onDestroy = onDestroy;
            typeFather = new GameObject(prefab.name + "Pool").transform;
            typeFather.gameObject.SetActive(false);
            typeFather.SetParent(allGameObjectRoot, false);
        }

        /// <summary>
        /// 检查重复注册的容量与显式回调是否一致
        /// </summary>
        public bool MatchesConfiguration(int maxInactive, int maxTotal, Action<GameObject> getCallback,
            Action<GameObject> releaseCallback, Action<GameObject> destroyCallback)
        {
            return MaxInactive == maxInactive && MaxTotal == maxTotal
                && (getCallback == null || getCallback == onGet)
                && (releaseCallback == null || releaseCallback == onRelease)
                && (destroyCallback == null || destroyCallback == onDestroy);
        }

        /// <summary>
        /// 预热对象池 新增闲置对象但不触发租借回调
        /// </summary>
        public void PreWarm(int count)
        {
            ThrowIfDisposed();
            for (int Index = 0; Index < count; Index++)
            {
                if (PooledCount >= MaxInactive || MaxTotal > 0 && TotalCreated >= MaxTotal)
                    break;

                var Value = CreateInstance();
                poolQueue.Enqueue(Value);
                pooledInstanceIdHashList.Add(Value.GetEntityId());
            }
        }

        /// <summary>
        /// 从池中取出或新建 未激活状态下完成父节点与取出回调
        /// </summary>
        public GameObject GetGameObj(Transform parent)
        {
            ThrowIfDisposed();
            GameObject Value;
            if (poolQueue.Count > 0)
            {
                Value = poolQueue.Dequeue();
                pooledInstanceIdHashList.Remove(Value.GetEntityId());
            }
            else
            {
                if (MaxTotal > 0 && TotalCreated >= MaxTotal)
                    return null;
                Value = CreateInstance();
            }

            var InstanceId = Value.GetEntityId();
            activeInstanceDict.Add(InstanceId, Value);
            leaseVersionDict.Add(InstanceId, checked(++nextLeaseVersion));
            Value.transform.SetParent(parent, false);
            if (parent == null)
                SceneManager.MoveGameObjectToScene(Value, SceneManager.GetActiveScene());

            Value.transform.localPosition = prefab.transform.localPosition;
            Value.transform.localRotation = prefab.transform.localRotation;
            Value.transform.localScale = prefab.transform.localScale;
            try
            {
                InvokeCallback(onGet, Value);
            }
            catch (Exception Exception)
            {
                DiscardAfterFailedGet(Value, Exception);
            }
            return Value;
        }

        /// <summary>
        /// 执行本轮业务配置 禁止配置回调改变同池所有权
        /// </summary>
        public void Configure<T>(T target, Action<T> configure) where T : Object
        {
            ThrowIfDisposed();
            if (configure == null)
                return;
            isInvokingCallback = true;
            try
            {
                configure(target);
            }
            finally
            {
                isInvokingCallback = false;
            }
        }

        /// <summary>
        /// 完成业务配置后激活当前借出实例
        /// </summary>
        public void Activate(GameObject obj)
        {
            ThrowIfDisposed();
            if (!activeInstanceDict.ContainsKey(obj.GetEntityId()))
                throw new InvalidOperationException($"[Pool] 激活失败 {obj.name} 非当前池借出对象");
            obj.SetActive(true);
        }

        #endregion

        #region 归还与版本

        /// <summary>
        /// 获取当前租借版本 非借出实例立即报错
        /// </summary>
        public ulong GetLeaseVersion(GameObject obj)
        {
            ThrowIfDisposed();
            if (!leaseVersionDict.TryGetValue(obj.GetEntityId(), out ulong Version))
                throw new InvalidOperationException($"[Pool] 获取租借版本失败 {obj.name} 非借出状态");
            return Version;
        }

        /// <summary>
        /// 检查实例是否仍属于指定租借 销毁与过期租借返回失败
        /// </summary>
        public bool IsLeaseValid(GameObject obj, ulong version)
        {
            return !IsDisposed && obj != null
                && leaseVersionDict.TryGetValue(obj.GetEntityId(), out ulong CurrentVersion)
                && CurrentVersion == version;
        }

        /// <summary>
        /// 归还实例 验证所有权并执行业务重置
        /// </summary>
        public bool TryPushGameObj(GameObject obj)
        {
            ThrowIfDisposed();
            var InstanceId = obj.GetEntityId();
            if (pooledInstanceIdHashList.Contains(InstanceId))
            {
                Debug.LogError($"[Pool] 重复归还 {obj.name} id={InstanceId}");
                return false;
            }
            if (!activeInstanceDict.ContainsKey(InstanceId))
            {
                Debug.LogError($"[Pool] 非法归还 {obj.name} 非当前池借出状态");
                return false;
            }

            InvokeCallback(onRelease, obj);
            activeInstanceDict.Remove(InstanceId);
            leaseVersionDict.Remove(InstanceId);
            obj.SetActive(false);
            if (PooledCount >= MaxInactive)
            {
                DestroyInstance(obj);
                return true;
            }

            obj.transform.SetParent(typeFather, false);
            poolQueue.Enqueue(obj);
            pooledInstanceIdHashList.Add(InstanceId);
            return true;
        }

        #endregion

        #region 清理与诊断

        /// <summary>
        /// 销毁前检查同池回调状态 不改变实例归属
        /// </summary>
        public void ValidateDisposal()
        {
            if (!IsDisposed)
                ThrowIfDisposed();
        }

        /// <summary>
        /// 归还回调失败后销毁本轮实例 不再次执行已失败的归还回调
        /// </summary>
        public bool DiscardAfterFailedRelease(GameObject obj, ulong version)
        {
            if (!IsLeaseValid(obj, version))
                return false;
            ThrowIfDisposed();
            var InstanceId = obj.GetEntityId();
            activeInstanceDict.Remove(InstanceId);
            leaseVersionDict.Remove(InstanceId);
            DestroyInstance(obj);
            return true;
        }

        /// <summary>
        /// 清空闲置缓存 保留借出实例与池根节点
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();
            while (poolQueue.Count > 0)
            {
                var Value = poolQueue.Dequeue();
                pooledInstanceIdHashList.Remove(Value.GetEntityId());
                DestroyInstance(Value);
            }
        }

        /// <summary>
        /// 销毁整个池 释放闲置与借出实例并使旧租借失效
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;

            ThrowIfDisposed();
            IsDisposed = true;
            var ErrorList = new List<Exception>();
            foreach (var Value in activeInstanceDict.Values)
            {
                if (Value == null)
                    continue;
                try
                {
                    InvokeCallback(onRelease, Value);
                }
                catch (Exception Exception)
                {
                    ErrorList.Add(Exception);
                }
                DisposeInstance(Value, ErrorList);
            }

            activeInstanceDict.Clear();
            leaseVersionDict.Clear();
            while (poolQueue.Count > 0)
                DisposeInstance(poolQueue.Dequeue(), ErrorList);
            pooledInstanceIdHashList.Clear();
            Object.Destroy(typeFather.gameObject);

            if (ErrorList.Count > 0)
                throw new AggregateException($"[Pool] 销毁对象池失败 {PrefabName}", ErrorList);
        }

        /// <summary>
        /// 获取对象池运行时报告
        /// </summary>
        public GameObjPoolReporter GetPoolReporter()
        {
            return new GameObjPoolReporter
            {
                PoolKey = poolKey,
                PrefabName = PrefabName,
                PooledCount = PooledCount,
                ActiveCount = ActiveCount,
                TotalCreated = TotalCreated,
                MaxSize = MaxInactive,
                MaxTotal = MaxTotal
            };
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取失败时销毁半配置实例 保留原始异常与清理异常
        /// </summary>
        public void DiscardAfterFailedGet(GameObject obj, Exception error)
        {
            var InstanceId = obj.GetEntityId();
            if (!activeInstanceDict.Remove(InstanceId))
                throw new AggregateException($"[Pool] 获取实例失败 {PrefabName}", error);
            leaseVersionDict.Remove(InstanceId);
            var ErrorList = new List<Exception> { error };
            try
            {
                InvokeCallback(onRelease, obj);
            }
            catch (Exception Exception)
            {
                ErrorList.Add(Exception);
            }
            DisposeInstance(obj, ErrorList);
            throw new AggregateException($"[Pool] 获取实例失败 {PrefabName}", ErrorList);
        }

        /// <summary>
        /// 在未激活父节点下创建实例 避免业务配置前触发 OnEnable
        /// </summary>
        private GameObject CreateInstance()
        {
            var Value = Object.Instantiate(prefab, typeFather);
            Value.SetActive(false);
            Value.name = prefab.name;
            return Value;
        }

        /// <summary>
        /// 检查池生命周期 已销毁则立即报错
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException($"GameObjPool<{PrefabName}>");
            if (isInvokingCallback)
                throw new InvalidOperationException($"[Pool] 生命周期回调禁止同池重入 {PrefabName}");
        }

        /// <summary>
        /// 执行业务销毁回调 无论回调是否成功都销毁 Unity 实例
        /// </summary>
        private void DestroyInstance(GameObject obj)
        {
            if (obj == null)
                return;
            obj.SetActive(false);
            try
            {
                InvokeCallback(onDestroy, obj);
            }
            finally
            {
                Object.Destroy(obj);
            }
        }

        /// <summary>
        /// 销毁单个实例 记录异常后继续释放其余实例
        /// </summary>
        private void DisposeInstance(GameObject obj, List<Exception> errorList)
        {
            try
            {
                DestroyInstance(obj);
            }
            catch (Exception Exception)
            {
                errorList.Add(Exception);
            }
        }

        /// <summary>
        /// 执行生命周期回调并保护同池状态转换
        /// </summary>
        private void InvokeCallback(Action<GameObject> callback, GameObject obj)
        {
            if (callback == null)
                return;
            isInvokingCallback = true;
            try
            {
                callback(obj);
            }
            finally
            {
                isInvokingCallback = false;
            }
        }

        #endregion
    }
}
