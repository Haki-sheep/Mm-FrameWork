using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.StaticAtlas
{
    /// <summary>
    /// UI 图集写入参数 仅修改明确选择的平台覆盖
    /// </summary>
    public sealed class StaticAtlasOptions
    {
        /// <summary>
        /// 最大纹理尺寸
        /// </summary>
        public int MaxSize = 2048;
        /// <summary>
        /// 图块间隔
        /// </summary>
        public int Padding = 4;
        /// <summary>
        /// 允许旋转 UI 默认关闭
        /// </summary>
        public bool Rotation;
        /// <summary>
        /// 紧密网格 UI 默认关闭
        /// </summary>
        public bool TightPacking;
        /// <summary>
        /// 是否随 Player 构建自动包含
        /// </summary>
        public bool IncludeInBuild;
        /// <summary>
        /// 默认平台压缩
        /// </summary>
        public TextureImporterCompression Compression = TextureImporterCompression.Compressed;
        /// <summary>
        /// 输出使用 sRGB
        /// </summary>
        public bool Srgb = true;
        /// <summary>
        /// 输出过滤模式
        /// </summary>
        public FilterMode Filter = FilterMode.Bilinear;
        /// <summary>
        /// 是否写入当前选择的平台覆盖
        /// </summary>
        public bool WritePlatformOverride;
        /// <summary>
        /// 当前平台覆盖是否启用
        /// </summary>
        public bool PlatformOverrideEnabled = true;
        /// <summary>
        /// 平台名称
        /// </summary>
        public string PlatformName = "Android";
        /// <summary>
        /// 平台纹理格式
        /// </summary>
        public TextureImporterFormat PlatformFormat = TextureImporterFormat.Automatic;
    }
}
