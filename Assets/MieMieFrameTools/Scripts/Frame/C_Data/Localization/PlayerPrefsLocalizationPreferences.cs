using UnityEngine;

namespace MieMieFrameWork.Localization
{
    /// <summary>
    /// 仅持久化语言选择 不改变游戏存档格式
    /// </summary>
    public sealed class PlayerPrefsLocalizationPreferences : ILocalizationPreferences
    {
        /// <summary> 语言设置存储 Key </summary>
        private readonly string preferenceKey;

        /// <summary>
        /// 接收宿主配置的语言设置 Key
        /// </summary>
        public PlayerPrefsLocalizationPreferences(string PreferenceKey)
        {
            preferenceKey = PreferenceKey;
        }

        /// <summary>
        /// 读取用户选择 未保存时使用指定默认语言
        /// </summary>
        public string GetLocale(string DefaultLocale)
        {
            return PlayerPrefs.GetString(preferenceKey, DefaultLocale);
        }

        /// <summary>
        /// 保存已准备完成的语言选择
        /// </summary>
        public void SetLocale(string Locale)
        {
            PlayerPrefs.SetString(preferenceKey, Locale);
            PlayerPrefs.Save();
        }
    }
}
