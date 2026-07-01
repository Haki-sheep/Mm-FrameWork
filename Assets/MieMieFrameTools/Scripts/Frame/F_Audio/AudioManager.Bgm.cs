namespace MieMieFrameWork
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// 音频管理器 - BGM 与环境音（Ambience）分部
    /// </summary>
    public partial class AudioManager
    {
        #region 背景音乐音量刷新

        /// <summary>
        /// 刷新背景音乐通道的实际音量
        /// </summary>
        private void ChangeBgVolume()
        {
            if (BgmSource != null) BgmSource.volume = bgVolumeBaseNum * globalVolumeFactor;
            if (AmbienceSource != null) AmbienceSource.volume = ambienceVolumeBaseNum * globalVolumeFactor;
        }

        /// <summary>
        /// 同步 BGM 通道的循环设置
        /// </summary>
        private void OnSelectLoop()
        {
            if (BgmSource != null) BgmSource.loop = IsLoop;
        }

        #endregion

        #region 背景音乐私有辅助

        /// <summary>
        /// 根据类型获取对应的背景音乐 AudioSource
        /// </summary>
        private AudioSource GetBgAudioSource(BgAudioType type)
        {
            return type == BgAudioType.BGM ? BgmSource : AmbienceSource;
        }

        #endregion

        #region 背景音乐控制

        /// <summary>
        /// 播放背景音乐（支持 AudioClip）
        /// </summary>
        /// <param name="audioClip">音频片段</param>
        /// <param name="type">背景音乐类型（BGM/Ambience）</param>
        /// <param name="needLoop">是否循环</param>
        /// <param name="volume">指定音量（-1 使用基准音量）</param>
        public void PlayerBgAudio(AudioClip audioClip, BgAudioType type = BgAudioType.BGM, bool needLoop = true, float volume = 1)
        {
            AudioSource source = GetBgAudioSource(type);

            if (source == null)
            {
                Debug.LogError($"[AudioManager] {type} 对应的 AudioSource 未赋值! 请检查 Inspector.");
                return;
            }

            source.clip = audioClip;
            source.loop = needLoop;

            if (volume != -1)
            {
                if (type == BgAudioType.BGM) BgVolumeBaseNum = volume;
                else AmbienceVolumeBaseNum = volume;
            }

            source.Play();
        }

        /// <summary>
        /// 播放背景音乐（支持 Addressable 路径同步加载）
        /// </summary>
        public void PlayerBgAudio(string path, BgAudioType type = BgAudioType.BGM, bool needLoop = true, float volume = -1)
        {
            AudioClip clip = AddressableMgr.LoadAsset<AudioClip>(path);
            PlayerBgAudio(clip, type, needLoop, volume);
        }

        /// <summary>
        /// 异步加载并播放背景音乐
        /// </summary>
        public async UniTask PlayerBgAudioAsync(string path, BgAudioType type = BgAudioType.BGM, bool needLoop = true, float volume = 1)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            AudioClip clip = await AddressableMgr.LoadAssetAsync<AudioClip>(path);
            sw.Stop();
            Debug.Log($"[AudioManager] 异步加载音频耗时: {sw.ElapsedMilliseconds}ms | Path: {path}");

            if (clip != null)
            {
                PlayerBgAudio(clip, type, needLoop, volume);
            }
            else
            {
                Debug.LogError($"[AudioManager] 音频加载失败，路径: {path}");
            }
        }

        /// <summary>
        /// 异步加载背景音乐 AudioClip
        /// </summary>
        public async UniTask<AudioClip> LoadBgClipAsync(string path)
        {
            return await AddressableMgr.LoadAssetAsync<AudioClip>(path);
        }

        /// <summary>
        /// 停止指定类型的背景音乐
        /// </summary>
        public void StopBgAudio(BgAudioType type = BgAudioType.BGM)
        {
            AudioSource source = GetBgAudioSource(type);
            if (source != null) source.Stop();
        }

        /// <summary>
        /// 暂停指定类型的背景音乐
        /// </summary>
        public void PauseBgAudio(BgAudioType type = BgAudioType.BGM)
        {
            AudioSource source = GetBgAudioSource(type);
            if (source != null && source.isPlaying) source.Pause();
        }

        /// <summary>
        /// 恢复指定类型的背景音乐
        /// </summary>
        public void UnPauseBgAudio(BgAudioType type = BgAudioType.BGM)
        {
            AudioSource source = GetBgAudioSource(type);
            if (source != null) source.UnPause();
        }

        #endregion
    }
}
