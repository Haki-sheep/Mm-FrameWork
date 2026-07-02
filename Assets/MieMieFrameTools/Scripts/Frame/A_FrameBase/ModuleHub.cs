namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using MieMieFrameWork.UI;
    using Sirenix.OdinInspector;
    using UnityEngine;

    /// <summary>
    /// 游戏根节点管理器 - 负责框架核心系统的初始化和管理
    /// </summary> 
    public class ModuleHub : SingletonMono<ModuleHub>
    {
        private readonly Dictionary<Type, IManagerBase> managerDict = new Dictionary<Type, IManagerBase>();

        [SerializeField, LabelText("UI管理器")]
        private UICoreMgr uICoreMgr;

        [SerializeField, LabelText("存档子目录")]
        private string archiveSubFolder = "Archives";

        /// <summary>
        /// 存档管理器实例 未安装 com.hakisheep.mm-saver 时为 null
        /// </summary>
        private object archiveMgr;


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
                InitArchiveMgr();
                GetAllManager();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameRoot] 框架初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 初始化存档管理器
        /// </summary>
        private void InitArchiveMgr()
        {
            Type archiveType = Type.GetType("MiMieSaver.ArchiveMgr, MiMieSaver");
            if (archiveType == null)
            {
                Debug.LogWarning("[ModuleHub] 存档模块未安装 跳过 ArchiveMgr 初始化");
                return;
            }

            string folder = string.IsNullOrWhiteSpace(archiveSubFolder) ? "Archives" : archiveSubFolder.Trim();
            string rootPath = Path.Combine(Application.persistentDataPath, folder);
            archiveMgr = Activator.CreateInstance(archiveType, rootPath);
        }

        /// <summary>
        /// 是否已安装并初始化存档模块
        /// </summary>
        public bool HasArchive => archiveMgr != null;

        /// <summary>
        /// 获取存档管理器 需引用 MiMieSaver 后使用 IArchiveMgr 等类型
        /// </summary>
        public T GetArchive<T>() where T : class => archiveMgr as T;

        private void GetAllManager()
        {
            var managers = new List<IManagerBase>();
            managers.Add(new AsyncTaskManager());
            managers.Add(new UniTimerManager());
            managers.AddRange(this.transform.GetComponents<IManagerBase>());
            //获取特殊的UIManager
            if (uICoreMgr is not null && !managers.Contains((IManagerBase)uICoreMgr))
                managers.Add((IManagerBase)uICoreMgr);

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
            foreach (var manager in managerDict.Values)
            {
                if (manager is IDisposable disposableManager)
                {
                    disposableManager.Dispose();
                }
            }

            managerDict.Clear();
            EventCenter.ClearAllListeners();
            archiveMgr = null;
        }

        #endregion

        #region 管理器特性与接口
        public interface IManagerBase
        {
            public void Init();
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public sealed class ManagerAttribute : Attribute
        {
            public int Priority { get; }

            public ManagerAttribute(int priority = 0)
            {
                Priority = priority;
            }
        }
        #endregion

    }
}
