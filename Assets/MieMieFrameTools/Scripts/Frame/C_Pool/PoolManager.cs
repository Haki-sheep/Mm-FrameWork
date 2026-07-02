namespace MieMieFrameWork.Pool
{
    using Sirenix.OdinInspector;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using static MieMieFrameWork.ModuleHub;

    /// <summary>
    /// 核心 新创建的实例应该由调用者使用
    /// 当调用者不再需要这个实例时 再调用 PushGameObj 方法将其放回对象池
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
        /// 默认池上限
        /// </summary>
        private const int DefaultMaxSize = 50;

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
        /// GameObject 池字典
        /// </summary>
        private readonly Dictionary<EntityId, GameObjPool> gameObjPoolDict = new();

        /// <summary>
        /// 路径到池 Key 映射
        /// </summary>
        private readonly Dictionary<string, EntityId> pathToPoolKeyDict = new();

        /// <summary>
        /// 普通对象池字典
        /// </summary>
        private readonly Dictionary<string, ObjectPool> objectPoolDic = new();

        public PoolManager(PoolManagerConfig poolManagerConfig, Transform serviceRoot)
        {
            this.serviceRoot = serviceRoot;
            AllGameObjectRoot = poolManagerConfig.AllGameObjectRoot;
        }

        public void Init()
        {
            if (AllGameObjectRoot is null)
                AllGameObjectRoot = serviceRoot.Find("PoolRoot");
        }

        public void Dispose()
        {
            SelectClearPool();
        }


        #region  GameObject池

        /// <summary>
        /// 从对象池获取指定预制体的 GameObject
        /// </summary>
        public T GetGameObj<T>(GameObject prefab, Transform parent = null) where T : UnityEngine.Object
        {
            GameObject obj = GetGameObj(prefab, parent);
            if (obj == null)
            {
                Debug.LogWarning($"{prefab.name} 从对象池加载失败");
                return null;
            }

            if (typeof(T) == typeof(GameObject))
                return obj as T;

            T comp = obj.GetComponent<T>();
            if (comp == null)
                Debug.LogWarning($"{prefab.name} 身上没有指定类型的组件");
            return comp;
        }

        /// <summary>
        /// 从对象池获取 GameObject
        /// </summary>
        public GameObject GetGameObj(GameObject prefab, Transform parent = null, int maxSize = DefaultMaxSize)
        {
            GameObjPool pool = GetOrCreatePool(prefab, maxSize);
            GameObject obj = pool.GetGameObj(parent);
            if (obj != null)
                return obj;

            return pool.CreateNew(parent);
        }

        /// <summary>
        /// 预热对象池
        /// </summary>
        public void Prewarm(GameObject prefab, int count, int maxSize = DefaultMaxSize)
        {
            GetOrCreatePool(prefab, maxSize).PreWarm(count);
        }

        /// <summary>
        /// 注册路径与预制体池的映射
        /// </summary>
        public void RegisterPoolPath(string path, GameObject prefab)
        {
            pathToPoolKeyDict[path] = prefab.GetEntityId();
        }

        /// <summary>
        /// 按路径从池中取对象
        /// </summary>
        public GameObject CheckCacheAndLoadObj(string path, Transform parent = null)
        {
            if (!pathToPoolKeyDict.TryGetValue(path, out EntityId poolKey))
            {
                Debug.LogWarning($"[Pool] 路径未注册 {path}");
                return null;
            }

            if (!gameObjPoolDict.TryGetValue(poolKey, out GameObjPool pool))
                return null;

            return pool.GetGameObj(parent);
        }

        /// <summary>
        /// 将 GameObject 放回对象池
        /// </summary>
        public void PushGameObj(GameObject obj)
        {
            PoolMember member = obj.GetComponent<PoolMember>();
            if (member == null)
            {
                Debug.LogWarning($"[Pool] {obj.name} 非池对象 无法归还");
                return;
            }

            if (!gameObjPoolDict.TryGetValue(member.PoolKey, out GameObjPool pool))
            {
                Debug.LogWarning($"[Pool] 找不到 Key={member.PoolKey} 的池");
                return;
            }

            pool.TryPushGameObj(obj);
        }

        /// <summary>
        /// 收集所有 GameObject 池快照
        /// </summary>
        public void CollectGameObjPoolInfoList(List<GameObjPoolReporter> resultList)
        {
            resultList.Clear();
            foreach (GameObjPool pool in gameObjPoolDict.Values)
                resultList.Add(pool.GetPoolReporter());
        }

        #endregion

        #region  Object池

        /// <summary>
        /// 从对象池获取指定类型的对象
        /// </summary>
        public T GetObject<T>() where T : class, new()
        {
            string name = typeof(T).FullName;
            if (objectPoolDic.TryGetValue(name, out ObjectPool pool))
                return pool.GetObj() as T;

            return new T();
        }

        /// <summary>
        /// 将对象放回对象池
        /// </summary>
        public void PushObject(object obj)
        {
            string name = obj.GetType().FullName;
            if (objectPoolDic.TryGetValue(name, out ObjectPool pool))
                pool.PushObj(obj);
            else
                objectPoolDic.Add(name, new ObjectPool(obj));
        }

        #endregion

        #region  清理

        /// <summary>
        /// 清除对象池
        /// </summary>
        public void SelectClearPool(bool clearGameObject = true, bool clearObject = true)
        {
            if (clearGameObject)
            {
                foreach (GameObjPool pool in gameObjPoolDict.Values)
                    pool.Clear();
                gameObjPoolDict.Clear();
                pathToPoolKeyDict.Clear();
            }

            if (clearObject)
                objectPoolDic.Clear();
        }

        /// <summary>
        /// 清除所有 GameObject 池
        /// </summary>
        public void ClearAllGameObject() => SelectClearPool(true, false);

        /// <summary>
        /// 清除所有 Object 池
        /// </summary>
        public void ClearAllObject() => SelectClearPool(false, true);

        /// <summary>
        /// 清除指定预制体的池
        /// </summary>
        public void ClearGameObject(GameObject prefab)
        {
            EntityId poolKey = prefab.GetEntityId();
            if (!gameObjPoolDict.TryGetValue(poolKey, out GameObjPool pool))
                return;

            pool.Clear();
            gameObjPoolDict.Remove(poolKey);
        }

        /// <summary>
        /// 清除指定类型的 Object 池
        /// </summary>
        public void ClearObject<T>() => objectPoolDic.Remove(typeof(T).FullName);

        /// <summary>
        /// 清除指定类型的 Object 池
        /// </summary>
        public void ClearObject(Type type) => objectPoolDic.Remove(type.FullName);

        #endregion

        #region 私有方法

        /// <summary>
        /// 获取或创建池
        /// </summary>
        private GameObjPool GetOrCreatePool(GameObject prefab, int maxSize)
        {
            EntityId poolKey = prefab.GetEntityId();
            if (!gameObjPoolDict.TryGetValue(poolKey, out GameObjPool pool))
            {
                pool = new GameObjPool(prefab, AllGameObjectRoot, maxSize);
                gameObjPoolDict.Add(poolKey, pool);
            }

            return pool;
        }

        #endregion
    }
}
