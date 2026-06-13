
using System.Text.RegularExpressions;

namespace EGame
{
    public static partial class StringExtension
    {
        /// <summary>
        /// 匹配驼峰
        /// </summary>
        [GeneratedRegex("([a-z0-9])([A-Z])")]
        private static partial Regex CamelCaseRegex();

        [GeneratedRegex("\\s+")]
        private static partial Regex WhiteSpaceRegex();

        [GeneratedRegex("^A-Za-z0-9")]
        private static partial Regex SpecialRegex();

        public static string Slugify(this string txt)
        {
            var result = txt.Trim();
            result = CamelCaseRegex().Replace(result, "$1_$2");     //在捕获到的第一组和第二组之间插入一个下划线
            result = WhiteSpaceRegex().Replace(result, "_");
            result = SpecialRegex().Replace(result, "");
            return result.ToUpperInvariant();
        }
    }
}