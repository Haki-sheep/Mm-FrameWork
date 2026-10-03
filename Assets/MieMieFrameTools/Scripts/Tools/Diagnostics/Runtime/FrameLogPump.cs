#if UNITY_5_3_OR_NEWER
using UnityEngine;

namespace MieMieFrameWork.Diagnostics
{
    /// <summary>
    /// 日志帧采样组件 由唯一启动入口创建并随应用退出
    /// </summary>
    internal sealed class FrameLogPump : MonoBehaviour
    {
        /// <summary>
        /// 主线程刷新日志帧号
        /// </summary>
        private void Update()
        {
            FrameLog.RefreshFrame();
        }

        /// <summary>
        /// 暂停时刷新文件缓冲
        /// </summary>
        private void OnApplicationPause(bool paused)
        {
            if (paused)
                FrameLog.FlushSession();
        }
    }
}
#endif
