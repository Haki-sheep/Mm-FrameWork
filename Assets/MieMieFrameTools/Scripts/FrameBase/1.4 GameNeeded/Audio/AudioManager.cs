namespace MieMieFrameWork
{
    using Cysharp.Threading.Tasks;
    using MieMieFrameWork.Pool;
    using Sirenix.OdinInspector;
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Events;

    /// <summary>
    /// 音频管理器
    /// 支持 BGM、环境音（Ambience）、特效音三种通道独立播放与控制
    /// </summary>
    [ManagerAttribute(3)]
    public class AudioManager : MonoBehaviour, IManagerBase
    {
        /// <summary>
        /// 背景音乐类型枚举
        /// </summary>
        public enum BgAudioType
        {
            /// <summary>主背景音乐（如主题曲）</summary>
            BGM,
            /// <summary>环境音（如雨声、风声）</summary>
            Ambience
        }

        public void Init()
        {
            ChangeGlobalVolume();
        }

        #region 组件

        /// <summary>特效声音预制体（对象池用）</summary>
        [TitleGroup("组件配置")]
        [SerializeField, LabelText("特效声音播放器")]
        private GameObject efPlayerES;

        /// <summary>主背景音乐播放器（对应 BGM 混音组）</summary>
        [SerializeField, LabelText("BGM播放器")]
        private AudioSource BgmSource;

        /// <summary>环境音播放器（对应 Ambience 混音组）</summary>
        [SerializeField, LabelText("环境音播放器")]
        private AudioSource AmbienceSource;
        
        #endregion

        #region 全局音量
        /// <summary>全局音量乘法系数（影响所有通道：BGM、环境音、特效音）</summary>
        [TitleGroup("音量设置")]
        [SerializeField, Range(0, 1), OnValueChanged("ChangeGlobalVolume"), LabelText("全局音量系数")]
        private float globalVolumeFactor;
        
        public float GlobalVolumeFactor
        {
            get => globalVolumeFactor;
            set
            {
                if (globalVolumeFactor == value) return;
                globalVolumeFactor = value;
                ChangeGlobalVolume();
            }
        }

        /// <summary>
        /// 刷新所有通道的实际音量
        /// 计算公式：实际音量 = 通道基准音量 * 全局系数
        /// </summary>
        private void ChangeGlobalVolume()
        {
            ChangeBgVolume();
            ChangeEffectVolume();
        }

        #endregion

        #region 背景音乐音量

        /// <summary>主背景音乐（BGM）基准音量（0-1）</summary>
        [Header("背景音乐音量")]
        [SerializeField, Range(0, 1), OnValueChanged("ChangeBgVolume"), LabelText("BGM基准音量")]
        private float bgVolumeBaseNum;
        
        public float BgVolumeBaseNum
        {
            get => bgVolumeBaseNum; set
            {
                if (bgVolumeBaseNum == value) return;
                bgVolumeBaseNum = value;
                ChangeBgVolume();
            }
        }

        /// <summary>环境音（Ambience）基准音量（0-1）</summary>
        [SerializeField, Range(0, 1), OnValueChanged("ChangeBgVolume"), LabelText("环境音基准音量")]
        private float ambienceVolumeBaseNum;
        
        public float AmbienceVolumeBaseNum
        {
            get => ambienceVolumeBaseNum; set
            {
                if (ambienceVolumeBaseNum == value) return;
                ambienceVolumeBaseNum = value;
                ChangeBgVolume();
            }
        }

        /// <summary>
        /// 刷新背景音乐通道的实际音量
        /// </summary>
        private void ChangeBgVolume()
        {
            if (BgmSource != null) BgmSource.volume = bgVolumeBaseNum * globalVolumeFactor;
            if (AmbienceSource != null) AmbienceSource.volume = ambienceVolumeBaseNum * globalVolumeFactor;
        }

        #endregion

        #region 特效音量
        /// <summary>特效音（Effect）基准音量（0-1）</summary>
        [Header("特效音量")]
        [SerializeField, Range(0, 1), OnValueChanged("ChangeEffectVolume"), LabelText("特效音基准音量")]
        private float effectVolumeBaseNum;
        
        public float EffectVolumeBaseNum
        {
            get => effectVolumeBaseNum; set
            {
                if (effectVolumeBaseNum == value) return;
                effectVolumeBaseNum = value;
                ChangeEffectVolume();
            }
        }

        /// <summary>当前正在播放的特效音 AudioSource 列表（用于统一刷新音量）</summary>
        private List<AudioSource> efAudioList = new();

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

        #region 全局控制

        /// <summary>是否静音（影响所有通道）</summary>
        [Header("全局控制")]
        [SerializeField, OnValueChanged("OnSelectMute"), LabelText("静音")]
        private bool isMute;
        
        public bool IsMute
        {
            get => isMute;
            set
            {
                if (isMute == value) return;
                isMute = value;
                OnSelectMute();
            }
        }

        /// <summary>
        /// 同步所有通道的静音状态
        /// </summary>
        private void OnSelectMute()
        {
            if (BgmSource != null) BgmSource.mute = IsMute;
            if (AmbienceSource != null) AmbienceSource.mute = IsMute;
            ChangeEffectVolume();
        }

        /// <summary>是否循环播放（仅影响 BGM 通道）</summary>
        [SerializeField, OnValueChanged("OnSelectLoop"), LabelText("循环播放")]
        private bool isLoop;
        
        public bool IsLoop
        {
            get => isLoop; set
            {
                if (isLoop == value) return;
                isLoop = value;
                OnSelectLoop();
            }
        }

        /// <summary>
        /// 同步 BGM 通道的循环设置
        /// </summary>
        private void OnSelectLoop()
        {
            if (BgmSource != null) BgmSource.loop = IsLoop;
        }

        /// <summary>是否暂停（影响所有通道）</summary>
        [SerializeField, OnValueChanged("OnIsPause"), LabelText("暂停所有")]
        private bool isPause;
        
        public bool IsPause
        {
            get => isPause; set
            {
                if (isPause == value) return;
                isPause = value;
                OnIsPause();
            }
        }

        /// <summary>
        /// 同步所有通道的暂停/恢复状态
        /// </summary>
        private void OnIsPause()
        {
            if (BgmSource != null)
            {
                if (isPause == true) BgmSource.Pause();
                else BgmSource.UnPause();
            }
            if (AmbienceSource != null)
            {
                if (isPause == true) AmbienceSource.Pause();
                else AmbienceSource.UnPause();
            }
            ChangeEffectVolume();
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 配置单个特效音 AudioSource 的属性
        /// </summary>
        /// <param name="efAudioSource">目标 AudioSource</param>
        /// <param name="spatial">空间Blend值（0=2D, 1=3D）</param>
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
        /// <param name="volume">指定音量（-1使用基准音量）</param>
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


        #region 特效音乐控制
        
        /// <summary>特效音对象池根节点</summary>
        [Header("特效音配置")]
        [SerializeField, LabelText("特效音乐根节点")]
        private Transform EffectClipRoot;

        /// <summary>
        /// 从对象池获取一个特效音 AudioSource
        /// </summary>
        /// <param name="is3d">是否为3D声音</param>
        private AudioSource GetEfAudio(bool is3d)
        {
            if (EffectClipRoot == null)
            {
                EffectClipRoot = this.transform.Find("EffectRoot");
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
            catch (System.Exception e)
            {
                Debug.LogError("获取特效声音组件失败: " + e.Message);
            }
            return null;
        }

        /// <summary>
        /// 播放一次特效音（支持 AudioClip）
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <param name="volumeScale">音量缩放（相对于特效基准音量）</param>
        /// <param name="is3d">是否3D</param>
        /// <param name="component">跟随目标组件（为null则跟随AudioManager本身）</param>
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
                audioSource.transform.position = this.transform.position;
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
        /// 播放一次特效音（2D UI专用，自动禁用3D传播）
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
    }
}
