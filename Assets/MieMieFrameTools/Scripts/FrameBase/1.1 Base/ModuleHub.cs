namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MieMieFrameWork.UI;
    using Sirenix.OdinInspector;
    using UnityEngine; 

    /// <summary>
    /// 游戏根节点管理器 - 负责框架核心系统的初始化和管理
    /// </summary> 
    public class ModuleHub : SingletonMono<ModuleHub>  
    {
        private Dictionary<Type, IManagerBase> managerDict = new Dictionary<Type, IManagerBase>();
        
        [SerializeField,LabelText("UI管理器")] 
        private UICoreMgr uICoreMgr;


        #region Unity 生命周期

        protected override void Awake()
        {
            base.Awake();
            InitializeFramework();
        }

        private void OnDestroy()
        {
            CleanupFramework();
        }

        #endregion

        #region 框架初始化

        /// <summary>
        /// 初始化整个框架系统
        /// </summary>
        private void InitializeFramework()
        {
            try
            {
                GetAllManager();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameRoot] 框架初始化失败: {ex.Message}");
            }
        }

        private void GetAllManager()
        {
            var managers = new List<IManagerBase>();
            managers.AddRange(this.transform.GetComponents<IManagerBase>());
            //获取特殊的UIManager
            if (uICoreMgr is not null && !managers.Contains(uICoreMgr))
                managers.Add(uICoreMgr);

            foreach (var manager in managers.Where(m => m is not null)
                                                            .OrderBy(GetManagerPriority))
            {
                var managerType = manager.GetType();
                if (managerDict.ContainsKey(managerType))
                {
                    Debug.LogError($"[ModuleHub] 发现重复管理器类型: {managerType.Name}，后续实例将被忽略。");
                    continue;
                }

                managerDict.Add(managerType, manager);
                manager.Init();
            }
        }

        /// <summary>
        /// 获取管理器优先级
        /// </summary>
        /// <param name="manager"></param>
        /// <returns></returns>
        private static int GetManagerPriority(IManagerBase manager)
        {
            var managerType = manager.GetType();
            // managerType = 查谁的 , typeof(ManagerAttribute) = 查哪个Attribute
            var attr = (ManagerAttribute)Attribute.GetCustomAttribute(managerType, typeof(ManagerAttribute));
            return attr?.Priority ?? 0;
        }

        /// <summary>
        /// 获取管理器
        /// </summary>
        /// <typeparam name="T">管理器类型</typeparam>
        /// <returns>管理器实例</returns>
        /// <exception cref="Exception"></exception>
        public T GetManager<T>() where T : IManagerBase
        {
            if (managerDict.TryGetValue(typeof(T), out var manager))
            {
                if (manager is T typedManager)
                {
                    return typedManager;
                }
                else
                {
                    throw new Exception($"管理器 {typeof(T).Name} 类型不匹配");
                }
            }
            else
            {
                throw new Exception($"管理器 {typeof(T).Name} 不存在");
            }
        }

        
        /// <summary>
        /// 清理框架资源
        /// </summary>
        private void CleanupFramework()
        {
            EventCenter.ClearAllListeners();
        }

        #endregion

    }
}