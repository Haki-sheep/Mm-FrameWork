namespace MieMieFrameWork.Business.Quality
{
    using System;
    using UnityEngine;

    public sealed partial class QualityManager
    {
        /// <summary>
        /// 读取本机偏好 无记录时采用平台默认 损坏记录保留原文并抛错
        /// </summary>
        private QualityState ReadPreference()
        {
            if (!PlayerPrefs.HasKey(preferenceKey))
                return defaultState;
            try
            {
                var Preference = JsonUtility.FromJson<QualityPreference>(PlayerPrefs.GetString(preferenceKey));
                if (Preference == null)
                    throw new InvalidOperationException("偏好记录为空");
                return CreateState(Preference.Level, Preference.TargetFrameRate, Preference.VSyncCount);
            }
            catch (Exception Error)
            {
                throw new InvalidOperationException($"[Quality] 偏好记录无效 键 {preferenceKey}", Error);
            }
        }

        /// <summary>
        /// 应用预设后覆盖独立帧率策略 确认 Unity 档位确实生效
        /// </summary>
        private static void ApplyUnitySettings(QualityState State)
        {
            int QualityIndex = ResolveQualityIndex(State.UnityQualityName);
            // 画质预设先应用 帧率策略后应用 防止预设覆盖用户选择
            QualitySettings.SetQualityLevel(QualityIndex, true);
            if (QualitySettings.GetQualityLevel() != QualityIndex)
                throw new InvalidOperationException($"[Quality] Unity 未应用预设 {State.UnityQualityName}");
            QualitySettings.vSyncCount = State.VSyncCount;
            Application.targetFrameRate = State.TargetFrameRate;
        }

        /// <summary>
        /// 应用设置后提交状态与偏好 最后同步发布已提交快照
        /// </summary>
        private void Commit(QualityState State, bool DeletePreference)
        {
            if (isApplying)
                throw new InvalidOperationException("[Quality] 设置通知期间不能重入提交");
            bool HasChanged = State.Level != current.Level || State.TargetFrameRate != current.TargetFrameRate || State.VSyncCount != current.VSyncCount;
            isApplying = true;
            try
            {
                if (HasChanged)
                    ApplyUnitySettings(State);
                current = State;
                if (DeletePreference)
                    PlayerPrefs.DeleteKey(preferenceKey);
                else
                    PlayerPrefs.SetString(preferenceKey, JsonUtility.ToJson(new QualityPreference(State)));
                PlayerPrefs.Save();
                if (HasChanged)
                    eventBus.Publish(QualityEvents.Changed, current);
            }
            finally
            {
                isApplying = false;
            }
        }
    }
}
