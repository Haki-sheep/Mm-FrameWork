using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MieMieFrameWork.Localization;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace MieMieFrameWork.Data.Luban.Localization
{
    /// <summary>
    /// Luban 语言表适配器 文本服务不依赖生成类型
    /// </summary>
    public sealed class LubanLocalizationTextSource : ILocalizationTextSource
    {
        /// <summary> Luban 生成语言表的唯一数据文件名 </summary>
        public const string DataFileName = "localization_tbtext.json";
        /// <summary> StreamingAssets 配置数据目录 </summary>
        public const string DataFolder = "DataTables";
        /// <summary> 语言表加载入口 支持验证时注入表快照 </summary>
        private readonly Func<CancellationToken, UniTask<cfg.localization.TbText>> loadTable;
        /// <summary> 按语言和 Key 索引的只读文案 </summary>
        private readonly Dictionary<string, Dictionary<string, string>> localeDict =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        /// <summary> 当前语言表支持的语言标识 </summary>
        private string[] localeList = Array.Empty<string>();
        /// <summary> 是否已经进入初始化 </summary>
        private bool initStarted;

        public IReadOnlyList<string> LocaleList => localeList;
        public static string DataPath => Path.Combine(Application.streamingAssetsPath, DataFolder, DataFileName);

        /// <summary>
        /// 使用跨平台 StreamingAssets 加载入口
        /// </summary>
        public LubanLocalizationTextSource() : this(LoadTableAsync)
        {
        }

        /// <summary>
        /// 接收已有的表加载入口 避免绑定业务总表生命周期
        /// </summary>
        public LubanLocalizationTextSource(Func<CancellationToken, UniTask<cfg.localization.TbText>> LoadTable)
        {
            loadTable = LoadTable;
        }

        /// <summary>
        /// 加载语言表并检查重复 Key 与缺失翻译
        /// </summary>
        public async UniTask InitAsync(CancellationToken CancellationToken)
        {
            if (initStarted)
                throw new InvalidOperationException("语言文本源只能初始化一次");
            initStarted = true;
            var Table = await loadTable(CancellationToken);
            CancellationToken.ThrowIfCancellationRequested();
            var KeyHashList = new HashSet<string>(StringComparer.Ordinal);
            foreach (var Entry in Table.DataList)
            {
                string Locale = CultureInfo.GetCultureInfo(Entry.Locale).Name;
                if (string.IsNullOrWhiteSpace(Entry.Key) || string.IsNullOrWhiteSpace(Entry.Text))
                    throw new InvalidDataException($"语言 {Locale} 文本 {Entry.Key} 的 Key 或翻译为空");
                if (!localeDict.TryGetValue(Locale, out var TextDict))
                {
                    TextDict = new Dictionary<string, string>(StringComparer.Ordinal);
                    localeDict.Add(Locale, TextDict);
                }
                if (TextDict.ContainsKey(Entry.Key))
                    throw new InvalidDataException($"重复语言文本 {Locale} {Entry.Key}");
                TextDict.Add(Entry.Key, Entry.Text);
                KeyHashList.Add(Entry.Key);
            }
            if (localeDict.Count == 0)
                throw new InvalidDataException("语言表不能为空");
            foreach (var Pair in localeDict)
            {
                var MissingList = KeyHashList.Where(Key => !Pair.Value.ContainsKey(Key)).ToArray();
                if (MissingList.Length > 0)
                    throw new InvalidDataException($"语言 {Pair.Key} 缺失翻译 {string.Join(" ", MissingList)}");
            }
            localeList = localeDict.Keys.OrderBy(Locale => Locale, StringComparer.Ordinal).ToArray();
        }

        /// <summary>
        /// 按语言与 Key 查询 不静默返回默认语言或空文本
        /// </summary>
        public string GetText(string Locale, string Key)
        {
            if (!localeDict.TryGetValue(Locale, out var TextDict) || !TextDict.TryGetValue(Key, out string Text))
                throw new KeyNotFoundException($"缺少语言文本 {Locale} {Key}");
            return Text;
        }

        /// <summary>
        /// 通过 UnityWebRequest 读取桌面 Android 与 WebGL 的语言数据
        /// </summary>
        private static async UniTask<cfg.localization.TbText> LoadTableAsync(CancellationToken CancellationToken)
        {
            string PathValue = DataPath.Replace('\\', '/');
            string Url = PathValue.Contains("://") || PathValue.StartsWith("jar:", StringComparison.Ordinal)
                ? PathValue : new Uri(PathValue).AbsoluteUri;
            using (var Request = UnityWebRequest.Get(Url))
            {
                try
                {
                    await Request.SendWebRequest().ToUniTask(cancellationToken: CancellationToken);
                    return new cfg.localization.TbText(JArray.Parse(Request.downloadHandler.text));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception Error)
                {
                    throw new InvalidDataException($"加载 Luban 语言表失败 {Url}", Error);
                }
            }
        }
    }
}
