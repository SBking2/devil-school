
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
        
        public static uint ToDeterMinisticHashCode(this string str)
        {
            if (str == null)
                return 0;

            uint num = 352654597;
            uint num2 = num;
            
            for(int i = 0; i < str.Length; i += 2)
            {
                num = ((num << 5) + num) ^ str[i];
                if (i == str.Length - 1)
                    break;
                num2 = ((num2 << 5) + num2) ^ str[i + 1];
            }

            return num + num2 * 1566083941;
        }
    }
}