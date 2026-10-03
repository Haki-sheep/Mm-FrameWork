using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MieMieFrameWork.Localization
{
    /// <summary>
    /// 语言字体引用租约 所有消费者归还后才释放实际资源
    /// </summary>
    public sealed class LocaleFontLease : IDisposable
    {
        /// <summary> 共享字体资源与引用计数 </summary>
        private readonly SharedFonts sharedFonts;
        /// <summary> 当前消费者是否已经归还租约 </summary>
        private bool disposed;

        /// <summary>
        /// 接收已加载字体与实际资源释放操作 创建首份租约
        /// </summary>
        public LocaleFontLease(IReadOnlyDictionary<string, TMP_FontAsset> FontDict, Action Release,
            IReadOnlyDictionary<string, Material> MaterialDict = null)
        {
            sharedFonts = new SharedFonts(FontDict, MaterialDict, Release);
        }

        /// <summary>
        /// 为另一个消费者创建独立且可重复安全归还的租约
        /// </summary>
        private LocaleFontLease(SharedFonts SharedFonts)
        {
            sharedFonts = SharedFonts;
            sharedFonts.Retain();
        }

        /// <summary>
        /// 持有同一语言资源 返回属于调用方的新租约
        /// </summary>
        public LocaleFontLease Retain()
        {
            ThrowIfDisposed();
            return new LocaleFontLease(sharedFonts);
        }

        /// <summary>
        /// 返回必需的字体样式 缺失时立即报错
        /// </summary>
        public TMP_FontAsset GetFont(string Style)
        {
            ThrowIfDisposed();
            return sharedFonts.GetFont(Style);
        }

        /// <summary>
        /// 返回指定样式的材质
        /// </summary>
        public Material GetMaterial(string Style)
        {
            ThrowIfDisposed();
            return sharedFonts.GetMaterial(Style);
        }

        /// <summary>
        /// 检查当前消费者是否已经归还资源
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(LocaleFontLease));
        }

        /// <summary>
        /// 归还当前消费者引用 最后一个引用归还时释放资源句柄
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            sharedFonts.Release();
        }

        /// <summary>
        /// 主线程共享资源状态 不持有业务对象或 UI 引用
        /// </summary>
        private sealed class SharedFonts
        {
            /// <summary> 字体样式与主字体映射 </summary>
            private readonly IReadOnlyDictionary<string, TMP_FontAsset> fontDict;
            /// <summary> 字体样式与文字材质映射 </summary>
            private readonly IReadOnlyDictionary<string, Material> materialDict;
            /// <summary> 实际资源句柄释放操作 </summary>
            private readonly Action release;
            /// <summary> 尚未归还的消费者引用数量 </summary>
            private int referenceCount = 1;

            /// <summary>
            /// 保存不可变字体映射与释放操作
            /// </summary>
            public SharedFonts(IReadOnlyDictionary<string, TMP_FontAsset> FontDict,
                IReadOnlyDictionary<string, Material> MaterialDict, Action Release)
            {
                fontDict = FontDict;
                materialDict = MaterialDict;
                release = Release;
            }

            /// <summary>
            /// 增加一份消费者引用
            /// </summary>
            public void Retain()
            {
                referenceCount++;
            }

            /// <summary>
            /// 根据样式查询共享字体
            /// </summary>
            public TMP_FontAsset GetFont(string Style)
            {
                if (!fontDict.TryGetValue(Style, out var Font))
                    throw new KeyNotFoundException($"当前语言没有配置字体样式 {Style}");
                return Font;
            }

            /// <summary>
            /// 返回样式材质 未提供材质映射时使用主字体默认材质
            /// </summary>
            public Material GetMaterial(string Style)
            {
                var Font = GetFont(Style);
                return materialDict == null ? Font.material : materialDict[Style];
            }

            /// <summary>
            /// 最后一个消费者归还时执行唯一资源释放操作
            /// </summary>
            public void Release()
            {
                referenceCount--;
                if (referenceCount == 0)
                    release();
            }
        }
    }
}
