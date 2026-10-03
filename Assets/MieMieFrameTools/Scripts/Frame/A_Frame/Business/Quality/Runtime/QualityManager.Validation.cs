namespace MieMieFrameWork.Business.Quality
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public sealed partial class QualityManager
    {
        /// <summary>
        /// 复制档位映射并验证当前构建和平台默认策略
        /// </summary>
        private void PrepareConfig()
        {
            if (config == null || eventBus == null)
                throw new InvalidOperationException("[Quality] 缺少配置或事件总线");
            preferenceKey = config.PreferenceKey;
            if (string.IsNullOrWhiteSpace(preferenceKey))
                throw new InvalidOperationException("[Quality] 偏好键不能为空");
            if (config.PresetList == null || config.PresetList.Count != Enum.GetValues(typeof(EQualityLevel)).Length)
                throw new InvalidOperationException("[Quality] 必须配置完整的极低 低 中 高 极高 Ultra 六档");

            var NameHashList = new HashSet<string>(StringComparer.Ordinal);
            foreach (var Preset in config.PresetList)
            {
                if (Preset == null || !Enum.IsDefined(typeof(EQualityLevel), Preset.Level))
                    throw new InvalidOperationException("[Quality] 档位映射含无效标识");
                if (string.IsNullOrWhiteSpace(Preset.UnityQualityName) || !NameHashList.Add(Preset.UnityQualityName))
                    throw new InvalidOperationException($"[Quality] 档位 {Preset.Level} 的预设名称为空或重复");
                if (presetDict.ContainsKey(Preset.Level))
                    throw new InvalidOperationException($"[Quality] 档位 {Preset.Level} 重复配置");
                ResolveQualityIndex(Preset.UnityQualityName);
                presetDict.Add(Preset.Level, Preset.UnityQualityName);
            }

            CreateState(config.MobileDefault, config.TargetFrameRate, config.VSyncCount);
            CreateState(config.DesktopDefault, config.TargetFrameRate, config.VSyncCount);
            var eDefault = Application.isMobilePlatform ? config.MobileDefault : config.DesktopDefault;
            defaultState = CreateState(eDefault, config.TargetFrameRate, config.VSyncCount);
        }

        /// <summary>
        /// 校验稳定档位与帧率策略 返回待应用快照
        /// </summary>
        private QualityState CreateState(EQualityLevel eLevel, int TargetFrameRate, int VSyncCount)
        {
            if (!presetDict.TryGetValue(eLevel, out string QualityName))
                throw new ArgumentOutOfRangeException(nameof(eLevel), eLevel, "[Quality] 未配置该档位");
            if (TargetFrameRate != -1 && TargetFrameRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(TargetFrameRate), "[Quality] 目标帧率必须为正数或负一");
            if (VSyncCount < 0 || VSyncCount > 4)
                throw new ArgumentOutOfRangeException(nameof(VSyncCount), "[Quality] 垂直同步间隔必须为零至四");
            if (Application.isMobilePlatform && VSyncCount != 0)
                throw new ArgumentException("[Quality] 移动平台使用目标帧率策略 垂直同步间隔必须为零", nameof(VSyncCount));
            return new QualityState(eLevel, QualityName, TargetFrameRate, VSyncCount);
        }

        /// <summary>
        /// 按当前构建的预设名称解析索引 缺失或重名立即失败
        /// </summary>
        private static int ResolveQualityIndex(string QualityName)
        {
            var NameList = QualitySettings.names;
            int ResolvedIndex = -1;
            for (int Index = 0; Index < NameList.Length; Index++)
            {
                if (!string.Equals(NameList[Index], QualityName, StringComparison.Ordinal))
                    continue;
                if (ResolvedIndex >= 0)
                    throw new InvalidOperationException($"[Quality] Unity 预设名称重复 {QualityName}");
                ResolvedIndex = Index;
            }
            if (ResolvedIndex < 0)
                throw new InvalidOperationException($"[Quality] 当前构建缺少预设 {QualityName}");
            return ResolvedIndex;
        }

        /// <summary>
        /// 验证调用线程与管理器生命周期
        /// </summary>
        private void VerifyReady()
        {
            VerifyThread();
            if (isDisposed)
                throw new ObjectDisposedException(nameof(QualityManager));
            if (!isInitialized)
                throw new InvalidOperationException("[Quality] 管理器尚未初始化");
        }

        /// <summary>
        /// 拒绝跨线程调用 避免触碰 Unity API 与主线程事件总线
        /// </summary>
        private void VerifyThread()
        {
            if (Environment.CurrentManagedThreadId != ownerThreadId)
                throw new InvalidOperationException("[Quality] 只允许创建线程访问");
        }
    }
}
