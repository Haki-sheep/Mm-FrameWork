namespace MieMieFrameWork.Business.Quality
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 本机偏好快照 读取后校验再转换为运行时状态
    /// </summary>
    [Serializable]
    internal sealed class QualityPreference
    {
        /// <summary> 用户选择的稳定档位 </summary>
        [SerializeField]
        private EQualityLevel level;

        /// <summary> 用户选择的目标帧率 </summary>
        [SerializeField]
        private int targetFrameRate;

        /// <summary> 用户选择的垂直同步间隔 </summary>
        [SerializeField]
        private int vSyncCount;

        public EQualityLevel Level => level;
        public int TargetFrameRate => targetFrameRate;
        public int VSyncCount => vSyncCount;

        /// <summary>
        /// 接收已提交状态 创建不包含 Unity 索引的偏好快照
        /// </summary>
        public QualityPreference(QualityState State)
        {
            level = State.Level;
            targetFrameRate = State.TargetFrameRate;
            vSyncCount = State.VSyncCount;
        }
    }
}
