using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MieMieFrameWork.Inspector.Editor")]

namespace MieMieFrameWork
{
    using System;
    using MieMieFrameWork.Tools.Inspector;
    using UnityEngine;

    public partial class ModuleHub
    {
        public enum EStartupPreset
        {
            [InspectorName("完整启动")]
            Full = 0,
            [InspectorName("资源和存档")]
            ResourcesAndArchive = 1,
            [InspectorName("自定义")]
            Custom = 2
        }

        [Flags]
        public enum EStartupModules
        {
            [InspectorName("无")]
            None = 0,
            [InspectorName("配置")]
            Config = 1 << 0,
            [InspectorName("代码热更新")]
            HotUpdate = 1 << 1,
            [InspectorName("存档")]
            Archive = 1 << 2,
            [InspectorName("画质")]
            Quality = 1 << 3,
            [InspectorName("对象池")]
            Pool = 1 << 4,
            [InspectorName("音频")]
            Audio = 1 << 5,
            [InspectorName("异步任务")]
            AsyncTasks = 1 << 6,
            [InspectorName("定时器")]
            Timers = 1 << 7,
            [InspectorName("帧驱动")]
            FrameDriver = 1 << 8,
            [InspectorName("特效")]
            Effects = 1 << 9,
            [InspectorName("输入")]
            Input = 1 << 10,
            [InspectorName("交互")]
            Interaction = 1 << 11,
            [InspectorName("游戏流程")]
            GameFlow = 1 << 12,
            [InspectorName("UI")]
            UI = 1 << 13,
            [InspectorName("全部")]
            All = Config | HotUpdate | Archive | Quality | Pool | Audio | AsyncTasks | Timers |
                  FrameDriver | Effects | Input | Interaction | GameFlow | UI
        }

        [SerializeField]
        [InspectorLabel("启动方案")]
        private EStartupPreset eStartupPreset = EStartupPreset.Full;

        [SerializeField]
        [InspectorLabel("选用模块")]
        [InspectorShowIf(nameof(eStartupPreset), (int)EStartupPreset.Custom)]
        private EStartupModules eSelectedModules = EStartupModules.Archive;

        internal EStartupModules AddedDependencies => ResolveStartupModules() & ~eSelectedModules;

        internal EStartupModules ResolveStartupModules()
        {
            switch (eStartupPreset)
            {
                case EStartupPreset.Full:
                    return EStartupModules.All;
                case EStartupPreset.ResourcesAndArchive:
                    return EStartupModules.Archive;
                case EStartupPreset.Custom:
                    break;
                default:
                    throw new InvalidOperationException($"[ModuleHub] 无效的启动方案 {eStartupPreset}");
            }

            // Unity 的多选框可以用全位掩码表示 Everything
            var eModules = (int)eSelectedModules == -1 ? EStartupModules.All : eSelectedModules;
            if ((eModules & ~EStartupModules.All) != 0)
                throw new InvalidOperationException($"[ModuleHub] 无效的启动模块掩码 {(int)eModules}");

            if ((eModules & (EStartupModules.Effects | EStartupModules.Audio)) != 0)
                eModules |= EStartupModules.Pool | EStartupModules.FrameDriver;
            if (eModules.HasFlag(EStartupModules.Interaction))
                eModules |= EStartupModules.Input | EStartupModules.FrameDriver;
            if (eModules.HasFlag(EStartupModules.GameFlow))
                eModules |= EStartupModules.FrameDriver;
            return eModules;
        }
    }
}
