namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
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
            if(uICoreMgr is not null)
                managers.Add(uICoreMgr);
                 
            foreach (var manager in managers)
            {
                managerDict[manager.GetType()] = manager;

                if(manager is not null)
                {
                    manager.Init();
                }
                else
                {
                    Debug.LogError($"管理器 {manager.GetType().Name} 不存在");
                }
            }
        }

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
            // 清理事件中心
            EventCenter.ClearAllListeners();

        }

        #endregion

    }
}