using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MieMieFrameWork.Localization
{
    /// <summary>
    /// 显式配置语言与样式的主字体 不按文件名推断路径
    /// </summary>
    [CreateAssetMenu(menuName = "MieMieFramework/本地化/字体映射", fileName = "LocalizationFonts")]
    public sealed class LocalizationFontCatalog : ScriptableObject
    {
        /// <summary> 语言与字体样式配置 </summary>
        [SerializeField]
        private List<LocaleFontEntry> entryList = new List<LocaleFontEntry>();

        public IReadOnlyList<LocaleFontEntry> EntryList => entryList;
    }

    /// <summary>
    /// 单个语言与样式的字体来源 直接引用与资源地址二选一
    /// </summary>
    [Serializable]
    public sealed class LocaleFontEntry
    {
        /// <summary> 语言文化标识 </summary>
        [SerializeField]
        private string locale = "zh-CN";
        /// <summary> UI 字体样式标识 </summary>
        [SerializeField]
        private string style = "Body";
        /// <summary> 随配置直接引用的字体 不由加载器卸载 </summary>
        [SerializeField]
        private TMP_FontAsset font;
        /// <summary> YooAsset 资源地址 使用时配置字体引用须为空 </summary>
        [SerializeField]
        private string address;
        /// <summary> 字体样式材质 留空使用主字体默认材质 </summary>
        [SerializeField]
        private Material material;
        /// <summary> 样式材质的 YooAsset 地址 与直接材质引用二选一 </summary>
        [SerializeField]
        private string materialAddress;

        public string Locale => locale;
        public string Style => style;
        public TMP_FontAsset Font => font;
        public string Address => address;
        public Material Material => material;
        public string MaterialAddress => materialAddress;
    }
}
