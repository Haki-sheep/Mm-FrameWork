#if UNITY_5_3_OR_NEWER
using UnityEngine;

namespace MieMieFrameWork.Diagnostics
{
    /// <summary>
    /// 可选启动配置 放到 Resources 下并命名为 MieMieLogProfile
    /// </summary>
    [CreateAssetMenu(menuName = "MieMieFramework/日志配置", fileName = FrameLog.ProfileResourcePath)]
    public sealed class LogProfile : ScriptableObject
    {
        /// <summary> 会话设置 </summary>
        public LogSettings Settings = new LogSettings();
    }
}
#endif
