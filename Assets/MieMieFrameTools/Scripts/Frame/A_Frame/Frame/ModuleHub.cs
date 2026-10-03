namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using MiMieSaver;
    using MieMieFrameWork.Boot;
    using MieMieFrameWork.Business;
    using MieMieFrameWork.Save;
    using MieMieFrameWork.Diagnostics;
    using UnityEngine;

    /// <summary>
    /// 游戏根节点管理器 负责框架管理器的组装 查询与释放
    /// Inspector 配置见 ModuleHub.Config.cs 注册见 ModuleHub.Register.cs 反射见 ModuleHub.Reflection.cs
    /// </summary>
    public partial class ModuleHub : SingletonMono<ModuleHub>
    {
        protected override bool DontDestroyOnLoadEnabled => true;

        /// <summary>
        /// 管理器实例字典 键为管理器类型
        /// </summary>
        private readonly Dictionary<Type, IManagerBase> managerDict = new Dictionary<Type, IManagerBase>();

        /// <summary>
        /// 管理器初始化顺序表 按优先级排序
        /// </summary>
        private readonly List<IManagerBase> managerOrderList = new List<IManagerBase>();

        /// <summary>
        /// 启动编排器 不参与普通管理器注册与优先级排序
        /// </summary>
        private BootManager bootManager;

        public bool IsReady => bootManager != null && bootManager.IsReady;

        public UniTask ReadyTask => bootManager.ReadyTask;

        #region Unity 生命周期

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
                return;

            bootManager = new BootManager(this);
            bootManager.InitComponents(resourcePackageName, resourceManifestTimeout, this.GetCancellationTokenOnDestroy()).Forget();
        }

        protected override void OnDestroy()
        {
            try
            {
                if (Instance == this)
                    CleanupFramework();
            }
            finally
            {
                base.OnDestroy();
            }
        }

        #endregion

        #region 框架初始化

        /// <summary>
        /// 是否已初始化存档管理器
        /// </summary>
        public bool HasArchive => GetArchive<IArchiveMgr>() != null;

        /// <summary>
        /// 获取存档管理器 使用 MiMieSaver 命名空间下的 IArchiveMgr 等类型
        /// </summary>
        public T GetArchive<T>() where T : class
        {
            if (managerDict.TryGetValue(typeof(ArchiveManager), out var Manager))
                return ((ArchiveManager)Manager).Archive as T;

            return null;
        }

        /// <summary>
        /// 初始化已注册的运行时管理器 优先级只决定本阶段内部顺序
        /// </summary>
        internal void InitManagers()
        {
            foreach (var Manager in managerOrderList)
            {
                try
                {
                    FrameLog.SetContext("InitializingManager", Manager.GetType().FullName);
                    FrameLog.Info($"初始化管理器 {Manager.GetType().FullName}", "Module");
                    Manager.Init();
                }
                catch (Exception Exception)
                {
                    var ManagerType = Manager.GetType();
                    throw new InvalidOperationException($"[ModuleHub] 管理器 {ManagerType.FullName} 初始化失败", Exception);
                }
            }
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
            GameHub.Clear();
            var FailureList = new List<Exception>();
            for (int Index = managerOrderList.Count - 1; Index >= 0; Index--)
            {
                var Manager = managerOrderList[Index];
                if (Manager is IDisposable DisposableManager)
                {
                    try
                    {
                        DisposableManager.Dispose();
                    }
                    catch (Exception Exception)
                    {
                        FailureList.Add(new InvalidOperationException($"[ModuleHub] 管理器 {Manager.GetType().FullName} 释放失败", Exception));
                    }
                }
            }

            try
            {
                ReleaseConfigModule();
            }
            catch (Exception Exception)
            {
                FailureList.Add(Exception);
            }

            managerDict.Clear();
            managerOrderList.Clear();
            bootManager = null;
            MmGlobalEventBus.GlobalBus.Clear();
            uiCoreMgr = null;

            if (FailureList.Count > 0)
                throw new AggregateException("[ModuleHub] 框架清理失败", FailureList);
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
