namespace MieMieFrameWork.Pool
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class GameObjPool
    {
        /// <summary>
        /// 关联预制体
        /// </summary>
        private readonly GameObject prefab;

        /// <summary>
        /// 池 Key
        /// </summary>
        private readonly EntityId poolKey;

        /// <summary>
        /// 池内闲置上限
        /// </summary>
        private readonly int maxSize;

        /// <summary>
        /// 二级父节点
        /// </summary>
        private Transform typeFather;

        /// <summary>
        /// 闲置队列
        /// </summary>
        private readonly Queue<GameObject> poolQueue = new();

        /// <summary>
        /// 借出正在使用的实例ID
        /// </summary>
        private readonly HashSet<EntityId> activeInstanceIdHashList = new();

        /// <summary>
        /// 闲置实例 ID 防止重复归还
        /// </summary>
        private readonly HashSet<EntityId> pooledInstanceIdHashList = new();

        /// <summary>
        /// 累计创建数量 = 借出的 + 池内闲置的
        /// </summary>
        private int totalCreatedCount;

        public string PrefabName => prefab != null ? prefab.name : "Unknown";
        public EntityId PoolKey => poolKey;
        public int PooledCount => poolQueue.Count;
        public int ActiveCount => activeInstanceIdHashList.Count;
        public int TotalCreated => totalCreatedCount;
        public int MaxSize => maxSize;

        #region 公共方法

        /// <summary>
        /// 构造函数
        /// </summary>
        public GameObjPool(GameObject prefab, Transform allGameObjectRoot, int maxSize = 50, bool useFather = true)
        {
            this.prefab = prefab;
            poolKey = prefab.GetEntityId();
            this.maxSize = maxSize;

            if (useFather)
            {
                typeFather = new GameObject(prefab.name + "Pool").transform;
                typeFather.SetParent(allGameObjectRoot);
            }
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
                TotalCreated = totalCreatedCount,
                MaxSize = maxSize
            };
        }

        /// <summary>
        /// 预热对象池
        /// </summary>
        public void PreWarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (totalCreatedCount >= maxSize || poolQueue.Count >= maxSize)
                    break;

                GameObject obj = GameObject.Instantiate(prefab, typeFather);
                obj.name = prefab.name;
                totalCreatedCount++;
                PushPreWarmInstance(obj);
            }
        }

        /// <summary>
        /// 从池中取出
        /// </summary>
        public GameObject GetGameObj(Transform parent = null)
        {
            if (poolQueue.Count == 0)
                return null;

            GameObject obj = poolQueue.Dequeue();
            EntityId instanceId = obj.GetEntityId();
            pooledInstanceIdHashList.Remove(instanceId);
            activeInstanceIdHashList.Add(instanceId);

            obj.SetActive(true);
            obj.transform.SetParent(parent);
            if (parent == null)
                SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());

            NotifyPoolable(obj, true);
            return obj;
        }

        /// <summary>
        /// 新建实例
        /// </summary>
        public GameObject CreateNew(Transform parent)
        {
            if (totalCreatedCount >= maxSize)
                return null;

            GameObject obj = GameObject.Instantiate(prefab, parent);
            obj.name = prefab.name;
            totalCreatedCount++;
            activeInstanceIdHashList.Add(obj.GetEntityId());
            BindPoolKey(obj);
            if (parent == null)
                SceneManager.MoveGameObjectToScene(obj, SceneManager.GetActiveScene());
            NotifyPoolable(obj, true);
            return obj;
        }

        /// <summary>
        /// 归还实例
        /// </summary>
        public bool TryPushGameObj(GameObject obj)
        {
            EntityId instanceId = obj.GetEntityId();

            if (pooledInstanceIdHashList.Contains(instanceId))
            {
                Debug.LogError($"[Pool] 重复归还 {obj.name} id={instanceId}");
                return false;
            }

            if (!activeInstanceIdHashList.Contains(instanceId))
            {
                Debug.LogError($"[Pool] 非法归还 {obj.name} 非借出状态");
                return false;
            }

            activeInstanceIdHashList.Remove(instanceId);

            if (poolQueue.Count >= maxSize)
            {
                totalCreatedCount--;
                Object.Destroy(obj);
                return true;
            }

            NotifyPoolable(obj, false);
            BindPoolKey(obj);

            if (typeFather != null)
                obj.transform.SetParent(typeFather);

            poolQueue.Enqueue(obj);
            pooledInstanceIdHashList.Add(instanceId);
            obj.SetActive(false);
            return true;
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public void Clear()
        {
            while (poolQueue.Count > 0)
            {
                GameObject obj = poolQueue.Dequeue();
                if (obj != null)
                    Object.Destroy(obj);
            }

            activeInstanceIdHashList.Clear();
            pooledInstanceIdHashList.Clear();
            totalCreatedCount = 0;

            if (typeFather != null)
                Object.Destroy(typeFather.gameObject);
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 预热专用入池 跳过借出检查
        /// </summary>
        private void PushPreWarmInstance(GameObject obj)
        {
            BindPoolKey(obj);
            if (typeFather != null)
                obj.transform.SetParent(typeFather);

            EntityId instanceId = obj.GetEntityId();
            poolQueue.Enqueue(obj);
            pooledInstanceIdHashList.Add(instanceId);
            obj.SetActive(false);
        }

        /// <summary>
        /// 绑定池标记
        /// </summary>
        private void BindPoolKey(GameObject obj)
        {
            PoolMember member = obj.GetComponent<PoolMember>();
            if (member == null)
                member = obj.AddComponent<PoolMember>();
            member.PoolKey = poolKey;
        }

        /// <summary>
        /// 通知 IPoolable
        /// </summary>
        private static void NotifyPoolable(GameObject obj, bool isSpawn)
        {
            IPoolable[] poolableList = obj.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolableList.Length; i++)
            {
                if (isSpawn)
                    poolableList[i].OnSpawn();
                else
                    poolableList[i].OnDespawn();
            }
        }

        #endregion
    }
}
