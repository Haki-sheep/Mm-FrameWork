using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MieMieFrameWork.Asset;
using TMPro;
using YooAsset;

namespace MieMieFrameWork.Localization
{
    /// <summary>
    /// 复用现有 YooAsset 门面加载语言字体 租约负责释放全部句柄
    /// </summary>
    public sealed class YooAssetLocalizationFontLoader : ILocalizationFontLoader
    {
        /// <summary> 语言与字体配置索引 </summary>
        private readonly Dictionary<string, Dictionary<string, LocaleFontEntry>> localeDict =
            new Dictionary<string, Dictionary<string, LocaleFontEntry>>(StringComparer.Ordinal);

        /// <summary>
        /// 检查配置并创建语言索引
        /// </summary>
        public YooAssetLocalizationFontLoader(LocalizationFontCatalog Catalog)
        {
            if (Catalog == null)
                throw new ArgumentException("多语言宿主必须配置字体映射", nameof(Catalog));
            foreach (var Entry in Catalog.EntryList)
            {
                string Locale = CultureInfo.GetCultureInfo(Entry.Locale).Name;
                if (string.IsNullOrWhiteSpace(Entry.Style))
                    throw new ArgumentException($"语言 {Locale} 字体样式不能为空");
                bool HasAddress = !string.IsNullOrWhiteSpace(Entry.Address);
                if ((Entry.Font != null) == HasAddress)
                    throw new ArgumentException($"语言 {Locale} 字体 {Entry.Style} 必须在直接引用与 YooAsset 地址中选择一种");
                if (Entry.Material != null && !string.IsNullOrWhiteSpace(Entry.MaterialAddress))
                    throw new ArgumentException($"语言 {Locale} 样式 {Entry.Style} 的材质引用与材质地址不能同时配置");
                if (!localeDict.TryGetValue(Locale, out var StyleDict))
                {
                    StyleDict = new Dictionary<string, LocaleFontEntry>(StringComparer.Ordinal);
                    localeDict.Add(Locale, StyleDict);
                }
                if (StyleDict.ContainsKey(Entry.Style))
                    throw new ArgumentException($"重复字体配置 {Locale} {Entry.Style}");
                StyleDict.Add(Entry.Style, Entry);
            }
            var StyleHashList = new HashSet<string>(localeDict.Values.SelectMany(StyleDict => StyleDict.Keys), StringComparer.Ordinal);
            foreach (var Pair in localeDict)
            {
                var MissingList = StyleHashList.Where(Style => !Pair.Value.ContainsKey(Style)).ToArray();
                if (MissingList.Length > 0)
                    throw new ArgumentException($"语言 {Pair.Key} 缺少字体样式 {string.Join(" ", MissingList)}");
            }
        }

        /// <summary>
        /// 准备整个语言的字体 任一失败释放本次全部资源
        /// </summary>
        public async UniTask<LocaleFontLease> LoadAsync(string Locale, CancellationToken CancellationToken)
        {
            if (!localeDict.TryGetValue(Locale, out var StyleDict))
                throw new KeyNotFoundException($"字体映射没有配置语言 {Locale}");
            var HandleList = new List<AssetHandle>();
            var FontDict = new Dictionary<string, TMP_FontAsset>(StringComparer.Ordinal);
            var MaterialDict = new Dictionary<string, UnityEngine.Material>(StringComparer.Ordinal);
            try
            {
                foreach (var Pair in StyleDict)
                {
                    CancellationToken.ThrowIfCancellationRequested();
                    var Entry = Pair.Value;
                    var Font = Entry.Font;
                    if (Font == null)
                        Font = await LoadAssetAsync<TMP_FontAsset>(Locale, Entry.Style, Entry.Address, HandleList, CancellationToken);
                    var Material = Entry.Material;
                    if (!string.IsNullOrWhiteSpace(Entry.MaterialAddress))
                        Material = await LoadAssetAsync<UnityEngine.Material>(Locale, Entry.Style, Entry.MaterialAddress, HandleList, CancellationToken);
                    if (Material == null)
                        Material = Font.material;
                    if (Material.mainTexture != Font.atlasTextures[0])
                        throw new InvalidOperationException($"语言 {Locale} 样式 {Entry.Style} 的材质图集与主字体不匹配");
                    FontDict.Add(Pair.Key, Font);
                    MaterialDict.Add(Pair.Key, Material);
                }
                return new LocaleFontLease(FontDict, () => ReleaseHandles(HandleList), MaterialDict);
            }
            catch
            {
                ReleaseHandles(HandleList);
                throw;
            }
        }

        /// <summary>
        /// 持有资源句柄直至整个语言租约释放 失败携带地址与样式上下文
        /// </summary>
        private static async UniTask<T> LoadAssetAsync<T>(string Locale, string Style, string Address,
            List<AssetHandle> HandleList, CancellationToken CancellationToken) where T : UnityEngine.Object
        {
            var Handle = YooAssetMgr.LoadAssetAsync<T>(Address);
            HandleList.Add(Handle);
            await UniTask.WaitUntil(() => Handle.IsDone, cancellationToken: CancellationToken);
            if (Handle.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException($"语言 {Locale} 样式 {Style} 资源加载失败 {Address} {Handle.Error}");
            var Asset = Handle.AssetObject as T;
            if (Asset == null)
                throw new InvalidOperationException($"资源类型不是 {typeof(T).Name} {Address}");
            return Asset;
        }

        /// <summary>
        /// 释放加载器持有的资源引用 不卸载直接引用字体
        /// </summary>
        private static void ReleaseHandles(List<AssetHandle> HandleList)
        {
            foreach (var Handle in HandleList)
                YooAssetMgr.Release(Handle);
        }
    }
}
