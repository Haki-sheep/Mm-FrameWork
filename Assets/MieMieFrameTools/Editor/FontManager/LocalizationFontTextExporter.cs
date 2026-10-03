using System;
using System.Globalization;
using System.IO;
using System.Linq;
using MieMieFrameWork.Data.Luban.Localization;
using Newtonsoft.Json.Linq;

namespace MieMieFrameWork.Localization.FontManagement
{
    /// <summary>
    /// 从生成语言表收集实际文案 不把 JSON 字段名当作字库内容
    /// </summary>
    public static class LocalizationFontTextExporter
    {
        /// <summary>
        /// 返回指定语言全部文案 交给工作台过滤标签与占位符
        /// </summary>
        public static string GetText(string Locale)
        {
            string CanonicalLocale = CultureInfo.GetCultureInfo(Locale).Name;
            var Table = new cfg.localization.TbText(JArray.Parse(File.ReadAllText(LubanLocalizationTextSource.DataPath)));
            var TextList = Table.DataList.Where(Entry => CultureInfo.GetCultureInfo(Entry.Locale).Name == CanonicalLocale)
                .Select(Entry => Entry.Text).ToArray();
            if (TextList.Length == 0)
                throw new InvalidDataException($"Luban 语言表没有语言 {CanonicalLocale}");
            return string.Join("\n", TextList);
        }
    }
}
