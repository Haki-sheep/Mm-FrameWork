namespace MieMieFrameWork
{
    using System;
    using Sirenix.OdinInspector;
    using UnityEngine;

    public partial class ModuleHub
    {
        public enum EStartupPreset
        {
            [LabelText("完整启动")] Full = 0,
            [LabelText("资源和存档")] ResourcesAndArchive = 1,
            [LabelText("自定义")] Custom = 2
        }

        [Flags]
        public enum EStartupModules
        {
            None = 0,
            Config = 1 << 0,
            HotUpdate = 1 << 1,
            Archive = 1 << 2,
            Quality = 1 << 3,
            Pool = 1 << 4,
            Audio = 1 << 5,
            AsyncTasks = 1 << 6,
            Timers = 1 << 7,
            FrameDriver = 1 << 8,
            Effects = 1 << 9,
            Input = 1 << 10,
            Interaction = 1 << 11,
            GameFlow = 1 << 12,
            UI = 1 << 13,
            All = Config | HotUpdate | Archive | Quality | Pool | Audio | AsyncTasks | Timers |
                  FrameDriver | Effects | Input | Interaction | GameFlow | UI
        }

        [SerializeField, LabelText("启动方案"), PropertyOrder(-3)]
        private EStartupPreset eStartupPreset = EStartupPreset.Full;

        [SerializeField, LabelText("选用模块"), ShowIf(nameof(IsCustomStartup)), PropertyOrder(-2)]
        private EStartupModules eSelectedModules = EStartupModules.Archive;

        private bool IsCustomStartup => eStartupPreset == EStartupPreset.Custom;

        [ShowInInspector, ReadOnly, LabelText("自动补入的依赖"), ShowIf(nameof(IsCustomStartup)), PropertyOrder(-1)]
        private EStartupModules AddedDependencies => ResolveStartupModules() & ~eSelectedModules;

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
