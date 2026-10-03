namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        /// 按优先级顺序注册全部管理器 只入表不初始化
        /// </summary>
        internal void RegisterAllManagers()
        {
            BindUIHub();

            // 纯C#相关组件
            var ManagerList = new List<IManagerBase>();
            ManagerList.Add(new ArchiveManager(archiveSubFolder));
            ManagerList.Add(new QualityManager(qualityConfig, MmGlobalEventBus.GlobalBus));
            var InstancePool = new PoolManager(poolManagerConfig, transform);
            ManagerList.Add(InstancePool);
            ManagerList.Add(new AudioManager(audioManagerConfig, transform));
            ManagerList.Add(new AsyncTaskManager());
            ManagerList.Add(new UniTimerManager());

            // Mono 相关组件
            var FrameDriver = this.GetOrAddComponent<MonoManager>();
            ManagerList.Add(FrameDriver);
            ManagerList.Add(new EffectManager(effectManagerConfig, transform, FrameDriver, InstancePool));
            ManagerList.Add(this.GetOrAddComponent<MieMieFrameWork.M_InputSystem.InputManager>());
            ManagerList.Add(this.GetOrAddComponent<MieMieFrameWork.Interaction.InteractionManager>());
            ManagerList.Add(new GameFlowManager(FrameDriver, MmGlobalEventBus.GlobalBus));

            // UI组件通过反射获取
            if (uiCoreMgr != null)
                ManagerList.Add(uiCoreMgr);

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
