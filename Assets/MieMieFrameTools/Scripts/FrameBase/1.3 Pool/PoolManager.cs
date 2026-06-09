namespace MieMieFrameWork.Pool
{
    using Sirenix.OdinInspector;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using static MieMieFrameWork.ModuleHub;
    /// <summary>
    /// 核心:新创建的实例应该由调用者使用，当调用者不再需要这个实例时，再调用 PushGameObj 方法将其放回对象池
    /// </summary>
    [ManagerAttribute(2)]
    public class PoolManager : MonoBehaviour, IManagerBase
    {
        [SerializeField]    
        [LabelText("对象池根节点")]
        private Transform AllGameObjectRoot;
        public void Init()
        {
            if (AllGameObjectRoot is null)
                AllGameObjectRoot = this.transform.Find("PoolRoot");
        }
        // GameObject 对象池字典
        public Dictionary<string, GameObjPoolData> gameObjPoolDic = new();

        // 普通对象池字典
        public Dictionary<string, ObjectPoolData> objectPoolDic = new();

        #region GameObject对象池操作

        private bool CheckGameObjectCache(GameObject prefab)
        {
            return gameObjPoolDic.ContainsKey(prefab.name);
        }
        public T GetGameObj<T>(GameObject prefab, Transform parent = null) where T : UnityEngine.Object
        {
            GameObject obj = GetGameObj(prefab, parent);
            
            if (obj != null)
            {
                //如果T类型是GameObject，则直接返回即可
                if (typeof(T) == typeof(GameObject))
                    return obj as T;
                //否则就从其身上获取指定组件
                else
                {
                    if (obj.GetComponent<T>() != null)
                        return obj.GetComponent<T>();
                    else
                    {
                        Debug.LogWarning($"{prefab.name}身上没有指定类型的组件");
                        return null;
                    }
                }
            }
            Debug.LogWarning($"{prefab.name}从对象池加载失败,请检查是否存在该预制体");
            return null;
        }

        /// <summary>
        /// 辅助函数:从对象池获取指定预制体的 GameObject
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="parent">父对象的 Transform</param>
        /// <returns>GameObject 对象</returns>
        private GameObject GetGameObj(GameObject prefab, Transform parent = null)
        {
            GameObject obj = null;
            string name = prefab.name;

            if (CheckGameObjectCache(prefab))
            {
                // 尝试从对象池获取
                obj = gameObjPoolDic[name].GetGameObj(parent);
                
                // 如果对象池中没有可用对象，则实例化新的
                if (obj == null)
                {
                    obj = GameObject.Instantiate(prefab, parent);
                    obj.name = name;
                }
            }
            else
            {
                obj = GameObject.Instantiate(prefab, parent);
                obj.name = name;
            }
            return obj;
        }


        /// <summary>
        /// 将 GameObject 对象放回对象池
        /// </summary>
        /// <param name="obj">要放回的 GameObject 对象</param>
        public void PushGameObj(GameObject obj,bool useFater = true)
        {
            string name = obj.name;
            if (gameObjPoolDic.ContainsKey(name))
            {
                gameObjPoolDic[name].PushGameObj(obj);
            }
            else
            {
                gameObjPoolDic.Add(name, new GameObjPoolData(obj, AllGameObjectRoot, useFater));
            }
        }

        #endregion

        #region 普通对象池操作
        private bool CheckObjectCache<T>()
        {
            string name = typeof(T).FullName;
            return objectPoolDic.ContainsKey(name);
        }

        /// <summary>
        /// 从对象池获取/创建指定类型的对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>指定类型的对象</returns>
        public T GetObject<T>() where T : class, new()
        {
            T obj;
            if (CheckObjectCache<T>())
            {
                string name = typeof(T).FullName;
                obj = objectPoolDic[name].GetObj() as T;
                return obj;
            }
            else
            {
                return new T();
            }
        }
        /// <summary>
        /// 将对象放回对象池
        /// </summary>
        /// <param name="obj">要放回的对象</param>
        public void PushObject(object obj)
        {
            string name = obj.GetType().FullName;

            if (objectPoolDic.ContainsKey(name))
            {
                objectPoolDic[name].PushObj(obj);
            }
            else
            {
                objectPoolDic.Add(name, new ObjectPoolData(obj));
            }
        }

        #endregion

        /// <summary>
        /// 提供给ResourManager调用，用于检查对象池缓存并根据路径加载对象
        /// </summary>
        /// <param name="path">对象路径</param>
        /// <param name="parent">父对象的 Transform</param>
        /// <returns>GameObject 对象</returns>
        public GameObject CheckCacheAndLoadObj(string path, Transform parent = null)
        {
            // 分割路径字符串
            string[] strSplit = path.Split('/');
            //拿到最底层的那个名字来判断是否存在该池
            string pathName = strSplit[strSplit.Length - 1];

            //如果包含该池 返回该池下的对应 GameObject
            if (gameObjPoolDic.ContainsKey(pathName))
            {
                GameObject obj = gameObjPoolDic[pathName].GetGameObj(parent);
                if (obj != null)
                    return obj;
            }

            Debug.LogWarning($"没有找到对象池缓存: {pathName}");
            return null;
        }

        #region 清除对象池操作

        /// <summary>
        /// 清除对象池
        /// </summary>
        /// <param name="clearGameObject">是否清除GameObject对象池</param>
        /// <param name="clearObject">是否清除Object对象池</param>
        public void SelectClearPool(bool clearGameObject = true, bool clearObject = true)
        {
            if (clearGameObject)
            {
                // 修复循环边界问题
                for (int i = AllGameObjectRoot.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = AllGameObjectRoot.transform.GetChild(i);
                    if (child != null)
                        Destroy(child.gameObject);
                }
                gameObjPoolDic.Clear();
            }

            if (clearObject)
            {
                objectPoolDic.Clear();
            }
        }

        /// <summary>
        /// 清除所有游戏对象池
        /// </summary>
        public void ClearAllGameObject()
        {
            SelectClearPool(true, false);
        }
        /// <summary>
        /// 清除所有普通对象池
        /// </summary>
        public void ClearAllObject()
        {
            SelectClearPool(false, true);
        }

        /// <summary>
        /// 清除指定名称的游戏对象池
        /// </summary>
        /// <param name="prefabName">预制体名称</param>
        public void ClearGameObject(string prefabName)
        {
            string poolName = prefabName + "Pool";

            Transform poolTransform = AllGameObjectRoot.transform.Find(poolName);
            if (poolTransform != null)
            {
                Destroy(poolTransform.gameObject);
                gameObjPoolDic.Remove(prefabName); // 使用原始名称作为key
            }
        }

        /// <summary>
        /// 清除指定GameObject的游戏对象池
        /// </summary>
        /// <param name="prefab">预制体</param>
        public void ClearGameObject(GameObject prefab)
        {
            ClearGameObject(prefab.name);
        }



        /// <summary>
        /// 清除指定类型的普通对象池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        public void ClearObject<T>()
        {
            objectPoolDic.Remove(typeof(T).FullName);
        }
        /// <summary>
        /// 清除指定类型的普通对象池
        /// </summary>
        /// <param name="type">对象类型</param>
        public void ClearObject(Type type)
        {
            objectPoolDic.Remove(type.FullName);
        }

        #endregion
    }
}