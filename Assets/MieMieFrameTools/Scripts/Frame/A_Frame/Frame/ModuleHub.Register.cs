namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using MieMieFrameWork.Business.GameFlow;
    using MieMieFrameWork.Effects;
    using MieMieFrameWork.Business.Quality;
    using MieMieFrameWork.Pool;
    using MieMieFrameWork.Save;

    /// <summary>
    /// 按优先级注册运行时管理器 只入表不初始化
    /// </summary>
    public partial class ModuleHub
    {
        /// <summary>
        /// 只构造本次选用的管理器 按优先级入表
        /// </summary>
        internal void RegisterManagers(EStartupModules eModules, CancellationToken cancellationToken)
        {
            // 纯C#相关组件
            var ManagerList = new List<IManagerBase>();
            if (eModules.HasFlag(EStartupModules.Archive))
                ManagerList.Add(OwnManager(new ArchiveManager(archiveSubFolder)));
            if (eModules.HasFlag(EStartupModules.Quality))
                ManagerList.Add(OwnManager(new QualityManager(qualityConfig, MmGlobalEventBus.GlobalBus)));
            PoolManager InstancePool = null;
            if (eModules.HasFlag(EStartupModules.Pool))
            {
                InstancePool = OwnManager(new PoolManager(poolManagerConfig, transform));
                ManagerList.Add(InstancePool);
            }
            if (eModules.HasFlag(EStartupModules.Audio))
                ManagerList.Add(OwnManager(new AudioManager(audioManagerConfig, transform)));
            if (eModules.HasFlag(EStartupModules.AsyncTasks))
                ManagerList.Add(OwnManager(new AsyncTaskManager(cancellationToken)));
            if (eModules.HasFlag(EStartupModules.Timers))
                ManagerList.Add(OwnManager(new UniTimerManager()));

            // Mono 相关组件
            MonoManager FrameDriver = null;
            if (eModules.HasFlag(EStartupModules.FrameDriver))
            {
                FrameDriver = this.GetOrAddComponent<MonoManager>();
                ManagerList.Add(FrameDriver);
            }
            if (eModules.HasFlag(EStartupModules.Effects))
            {
                var EffectConfig = effectProfile != null ? effectProfile.Config : effectManagerConfig;
                ManagerList.Add(OwnManager(new EffectManager(EffectConfig, transform, FrameDriver, InstancePool)));
            }
            if (eModules.HasFlag(EStartupModules.Input))
                ManagerList.Add(this.GetOrAddComponent<MieMieFrameWork.M_InputSystem.InputManager>());
            if (eModules.HasFlag(EStartupModules.Interaction))
                ManagerList.Add(this.GetOrAddComponent<MieMieFrameWork.Interaction.InteractionManager>());
            if (eModules.HasFlag(EStartupModules.GameFlow))
                ManagerList.Add(OwnManager(new GameFlowManager(FrameDriver, MmGlobalEventBus.GlobalBus)));

            // UI组件通过反射获取
            if (eModules.HasFlag(EStartupModules.UI))
            {
                BindUIHub();
                if (uiCoreMgr != null)
                    ManagerList.Add(uiCoreMgr);
            }

            // 按优先级顺序注册管理器
            foreach (var Manager in ManagerList.OrderBy(GetManagerPriority))
            {
                var ManagerType = Manager.GetType();
                if (managerDict.ContainsKey(ManagerType))
                    throw new InvalidOperationException($"[ModuleHub] 重复管理器类型 {ManagerType.FullName}");

                managerDict.Add(ManagerType, Manager);
                managerOrderList.Add(Manager);
            }
        }

        private T OwnManager<T>(T manager) where T : IManagerBase, IDisposable
        {
            unstartedOwnedManagerList.Add(manager);
            return manager;
        }

        /// <summary>
        /// 获取管理器优先级 获取其自定义特性ManagerAttribute下的int属性
        /// </summary>
        private static int GetManagerPriority(IManagerBase manager)
        {
            var ManagerType = manager.GetType();
            var Attribute = (ManagerAttribute)System.Attribute.GetCustomAttribute(ManagerType, typeof(ManagerAttribute));
            return Attribute?.Priority ?? 0;
        }
    }
}
