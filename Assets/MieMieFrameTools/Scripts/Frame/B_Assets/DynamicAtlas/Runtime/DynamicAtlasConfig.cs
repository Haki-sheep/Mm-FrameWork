using UnityEngine;

namespace MieMieFrameWork.Asset.DynamicAtlas
{
    /// <summary>
    /// 配置图集尺寸 预算与 GPU 写入 Shader
    /// </summary>
    [CreateAssetMenu(menuName = "MieMieFramework/资源/动态图集配置", fileName = "DynamicAtlasConfig")]
    public sealed class DynamicAtlasConfig : ScriptableObject
    {
        /// <summary>
        /// 单页宽度
        /// </summary>
        [SerializeField, Min(1)]
        private int pageWidth = 1024;

        /// <summary>
        /// 单页高度
        /// </summary>
        [SerializeField, Min(1)]
        private int pageHeight = 1024;

        /// <summary>
        /// 每侧边缘扩展像素
        /// </summary>
        [SerializeField, Min(1)]
        private int padding = 2;

        /// <summary>
        /// 最大常驻页数
        /// </summary>
        [SerializeField, Min(1)]
        private int maxPageCount = 8;

        /// <summary>
        /// 图集纹理常驻预算 MiB 不含源纹理与临时 RT
        /// </summary>
        [SerializeField, Min(1)]
        private int memoryBudgetMiB = 32;

        /// <summary>
        /// 按 sRGB 存储颜色纹理
        /// </summary>
        [SerializeField]
        private bool srgb = true;

        /// <summary>
        /// 所有生成 Sprite 共用的轴心
        /// </summary>
        [SerializeField]
        private Vector2 pivot = new Vector2(0.5f, 0.5f);

        /// <summary>
        /// 所有生成 Sprite 共用的每单位像素
        /// </summary>
        [SerializeField, Min(1)]
        private float pixelsPerUnit = 100f;

        /// <summary>
        /// 显式引用边缘扩展 Shader 防止构建剥离
        /// </summary>
        [SerializeField]
        private Shader paddingShader;

        public int PageWidth => pageWidth;
        public int PageHeight => pageHeight;
        public int Padding => padding;
        public int MaxPageCount => maxPageCount;
        public long MemoryBudgetBytes => (long)memoryBudgetMiB * 1024 * 1024;
        public bool Srgb => srgb;
        public Vector2 Pivot => pivot;
        public float PixelsPerUnit => pixelsPerUnit;
        public Shader PaddingShader => paddingShader;
    }
}
