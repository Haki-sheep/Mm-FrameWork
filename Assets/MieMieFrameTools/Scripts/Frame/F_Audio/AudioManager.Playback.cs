namespace MieMieFrameWork
{
    /// <summary>
    /// 单次音效句柄控制 不允许旧会话和旧租借影响新播放
    /// </summary>
    public partial class AudioManager
    {
        public int ActiveEffectCount => effectPlaybackList.Count;

        /// <summary>
        /// 查询请求是否仍然有效 加载中与暂停中均视为有效
        /// </summary>
        public bool IsEffectPlaying(AudioPlaybackHandle handle) => TryGetEffect(handle, out var Playback);

        /// <summary>
        /// 停止单次音效或加载请求 不触发自然完成回调
        /// </summary>
        public bool StopEffect(AudioPlaybackHandle handle)
        {
            if (!TryGetEffect(handle, out var Playback)) return false;
            ReleaseEffect(Playback);
            return true;
        }

        /// <summary>
        /// 暂停单次音效 加载中的暂停意图在播放开始时生效
        /// </summary>
        public bool PauseEffect(AudioPlaybackHandle handle)
        {
            if (!TryGetEffect(handle, out var Playback)) return false;
            Playback.Paused = true;
            RefreshEffectPause(Playback);
            return true;
        }

        /// <summary>
        /// 恢复单次音效 全局暂停仍然生效
        /// </summary>
        public bool UnPauseEffect(AudioPlaybackHandle handle)
        {
            if (!TryGetEffect(handle, out var Playback)) return false;
            Playback.Paused = false;
            RefreshEffectPause(Playback);
            return true;
        }

        /// <summary>
        /// 校验会话 请求编号与池租借版本 无效句柄只返回失败
        /// </summary>
        private bool TryGetEffect(AudioPlaybackHandle handle, out EffectPlayback playback)
        {
            playback = null;
            return !disposed && handle.Owner == this && effectPlaybackDict.TryGetValue(handle.Id, out playback) &&
                (!playback.Started || effectPoolHandle.IsLeaseValid(playback.Source, playback.LeaseVersion));
        }
    }
}
