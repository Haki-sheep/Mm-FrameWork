using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace MieMieFrameWork.Asset.DynamicAtlas
{
    /// <summary>
    /// GPU 转换源格式并扩展边缘 全程不读回 CPU 像素
    /// </summary>
    internal sealed class DynamicAtlasGpuWriter : IDisposable
    {
        /// <summary>
        /// 边缘扩展材质
        /// </summary>
        private readonly Material material;

        /// <summary>
        /// 校验 GPU 能力并建立显式引用的材质
        /// </summary>
        public DynamicAtlasGpuWriter(Shader shader, bool srgb)
        {
            if (shader == null || !shader.isSupported)
                throw new InvalidOperationException("动态图集必须配置受支持的 DynamicAtlasPadding Shader");
            if ((SystemInfo.copyTextureSupport & CopyTextureSupport.RTToTexture) == 0)
                throw new NotSupportedException($"动态图集需要 RTToTexture GPU 复制 当前设备 {SystemInfo.graphicsDeviceType}");
            var eFormat = GraphicsFormatUtility.GetGraphicsFormat(TextureFormat.RGBA32, srgb);
            if (!SystemInfo.IsFormatSupported(eFormat, GraphicsFormatUsage.Render | GraphicsFormatUsage.Sample))
                throw new NotSupportedException($"动态图集不支持目标格式 {eFormat}");
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        /// <summary>
        /// 输入源纹理与已分配区域 转换后只复制该区域到图集
        /// </summary>
        public void Write(Texture2D source, Texture2D atlas, RectInt area, int padding)
        {
            var Descriptor = new RenderTextureDescriptor(area.width, area.height)
            {
                graphicsFormat = atlas.graphicsFormat,
                depthBufferBits = 0,
                msaaSamples = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            var Temporary = RenderTexture.GetTemporary(Descriptor);
            var Previous = RenderTexture.active;
            bool PreviousSrgb = GL.sRGBWrite;
            try
            {
                if (Temporary.graphicsFormat != atlas.graphicsFormat)
                    throw new NotSupportedException($"动态图集临时 RT 格式 {Temporary.graphicsFormat} 与目标 {atlas.graphicsFormat} 不一致");
                material.SetVector("AtlasLayout", new Vector4(area.width, area.height, padding, 0));
                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear && Temporary.sRGB;
                Graphics.Blit(source, Temporary, material, 0);
                // GPU 纹理写入后禁止调用 Apply 避免旧 CPU 数据覆盖
                Graphics.CopyTexture(Temporary, 0, 0, 0, 0, area.width, area.height, atlas, 0, 0, area.x, area.y);
            }
            finally
            {
                GL.sRGBWrite = PreviousSrgb;
                RenderTexture.active = Previous;
                RenderTexture.ReleaseTemporary(Temporary);
            }
        }

        /// <summary>
        /// 释放本写入器持有的材质
        /// </summary>
        public void Dispose()
        {
            DynamicAtlasPage.DestroyObject(material);
        }
    }
}
