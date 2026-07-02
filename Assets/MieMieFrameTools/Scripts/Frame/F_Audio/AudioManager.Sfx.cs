namespace MieMieFrameWork
{
    using Cysharp.Threading.Tasks;
    using MieMieFrameWork.Pool;
    using System;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// 音频管理器 - 特效音（SFX）分部
    /// </summary>
    public partial class AudioManager
    {
        #region 特效音量刷新

        /// <summary>
        /// 刷新所有正在播放的特效音的实际音量
        /// </summary>
        private void ChangeEffectVolume()
        {
            // 倒序遍历以便安全移除空元素
            for (int i = efAudioList.Count - 1; i >= 0; i--)
            {
                if (efAudioList[i] != null)
                {
                    SetEffectAudioPlay(efAudioList[i]);
                }
                else
                {
                    efAudioList.RemoveAt(i);
                }
            }
        }

        #endregion

        #region 特效音私有辅助

        /// <summary>
        /// 配置单个特效音 AudioSource 的属性
        /// </summary>
        /// <param name="efAudioSource">目标 AudioSource</param>
        /// <param name="spatial">空间 Blend 值（0=2D, 1=3D）</param>
        private void SetEffectAudioPlay(AudioSource efAudioSource, float spatial = 0)
        {
            efAudioSource.mute = isMute;
            // 特效音实际音量 = 特效基准音量 * 全局系数
            efAudioSource.volume = effectVolumeBaseNum * globalVolumeFactor;

            if (spatial != 0)
            {
                efAudioSource.spatialBlend = spatial;
            }

            if (IsPause)
                efAudioSource.Pause();
            else
                efAudioSource.UnPause();
        }

        /// <summary>
        /// 从对象池获取一个特效音 AudioSource
        /// </summary>
        /// <param name="is3d">是否为 3D 声音</param>
        private AudioSource GetEfAudio(bool is3d)
        {
            if (EffectClipRoot == null)
            {
                EffectClipRoot = serviceRoot.Find("EffectRoot");
            }

            AudioSource ef = ModuleHub.Instance.GetManager<PoolManager>().GetGameObj<AudioSource>(efPlayerES, EffectClipRoot);
            try
            {
                if (ef != null)
                {
                    SetEffectAudioPlay(ef, is3d ? 1 : 0);
                    efAudioList.Add(ef);
                    return ef;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("获取特效声音组件失败: " + e.Message);
            }
            return null;
        }

        /// <summary>
        /// 异步回收特效音播放器并触发回调
        /// </summary>
        private async UniTaskVoid DoRecycleAudioPlay(AudioSource audioSource, AudioClip clip, UnityAction callBak, float time)
        {
            // 等待音频播放完成
            await UniTask.WaitForSeconds(clip.length);

            if (audioSource != null)
            {
                if (efAudioList.Contains(audioSource))
                    efAudioList.Remove(audioSource);

                audioSource.PushGameObjectToPool();

                // 等待指定延迟后执行回调
                await UniTask.Delay(
                        TimeSpan.FromSeconds(time), ignoreTimeScale: true).
                            ContinueWith(() => callBak?.Invoke()
                    );
            }
        }

        #endregion

        #region 特效音乐控制

        /// <summary>
        /// 播放一次特效音（支持 AudioClip）
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <param name="volumeScale">音量缩放（相对于特效基准音量）</param>
        /// <param name="is3d">是否 3D</param>
        /// <param name="component">跟随目标组件（为 null 则跟随 AudioManager 本身）</param>
        /// <param name="callBack">播放完成回调</param>
        /// <param name="callBackTime">回调延迟时间</param>
        public void PlayOneShot(AudioClip clip,
            float volumeScale = 1,
            bool is3d = true,
            Component component = null,
            UnityAction callBack = null,
            float callBackTime = 0)
        {
            AudioSource audioSource = GetEfAudio(is3d);
            if (audioSource == null) return;

            if (component != null)
            {
                audioSource.transform.SetParent(component.transform);
                audioSource.transform.localPosition = Vector3.zero;
            }
            else
            {
                audioSource.transform.position = serviceRoot.position;
            }

            audioSource.PlayOneShot(clip, volumeScale);
            DoRecycleAudioPlay(audioSource, clip, callBack, callBackTime).Forget();
        }

        /// <summary>
        /// 播放一次特效音（支持 Addressable 路径同步加载）
        /// </summary>
        public void PlayOneShot(string clipPath, Component component = null,
                    float volumeScale = 1, bool is3d = true, UnityAction callBack = null, float callBacKTime = 0)
        {
            AudioClip audioClip = AddressableMgr.LoadAsset<AudioClip>(clipPath);
            if (audioClip != null) PlayOneShot(audioClip, volumeScale, is3d, component, callBack, callBacKTime);
        }

        /// <summary>
        /// 播放一次特效音（2D UI 专用，自动禁用 3D 传播）
        /// </summary>
        public void PlayOneShotWith2DUI(string clipPath, Component component = null,
                    float volumeScale = 1, UnityAction callBack = null, float callBacKTime = 0)
        {
            PlayOneShot(clipPath, component: component, volumeScale: volumeScale, is3d: false,
                                                        callBack: callBack, callBacKTime: callBacKTime);
        }

        /// <summary>
        /// 播放一次特效音（异步加载）
        /// </summary>
        public async void PlayOneShotAsync(string clipPath,
            Component component = null,
            float volumeScale = 1,
            bool is3d = true,
            UnityAction callBack = null,
            float callBackTime = 0)
        {
            AudioClip audioClip = await AddressableMgr.LoadAssetAsync<AudioClip>(clipPath);
            if (audioClip != null)
            {
                PlayOneShot(audioClip, volumeScale, is3d, component, callBack, callBackTime);
            }
        }

        #endregion
    }
}
