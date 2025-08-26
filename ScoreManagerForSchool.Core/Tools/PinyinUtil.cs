using System;
using System.Linq;
using System.Text;
using NPinyin;

namespace ScoreManagerForSchool.Core.Tools
{
    /// <summary>
    /// 拼音工具类 - 使用NPinyinPro实现
    /// </summary>
    public static class PinyinUtil
    {
        /// <summary>
        /// 生成拼音键值对
        /// </summary>
        /// <param name="name">姓名</param>
        /// <returns>全拼和首字母缩写</returns>
        public static (string Full, string Initials) MakeKeys(string? name)
        {
            var full = Full(name);
            var init = Initials(name);
            return (full, init);
        }

        /// <summary>
        /// 获取拼音全拼
        /// </summary>
        /// <param name="text">中文文本</param>
        /// <returns>拼音全拼</returns>
        public static string Full(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            
            try
            {
                var pinyin = Pinyin.GetPinyin(text);
                return LettersOnlyLower(pinyin);
            }
            catch
            {
                // 如果拼音转换失败，返回原文本的字母部分
                return LettersOnlyLower(text);
            }
        }

        /// <summary>
        /// 获取拼音首字母
        /// </summary>
        /// <param name="text">中文文本</param>
        /// <returns>拼音首字母</returns>
        public static string Initials(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            
            try
            {
                var result = new StringBuilder();
                foreach (char c in text)
                {
                    if (char.IsWhiteSpace(c)) continue;
                    
                    var pinyin = Pinyin.GetPinyin(c.ToString());
                    if (!string.IsNullOrEmpty(pinyin))
                    {
                        var firstLetter = pinyin.FirstOrDefault(ch => char.IsLetter(ch));
                        if (firstLetter != default)
                        {
                            result.Append(char.ToLowerInvariant(firstLetter));
                        }
                    }
                    else
                    {
                        // 如果不是中文字符，直接使用原字符
                        if (char.IsLetter(c))
                        {
                            result.Append(char.ToLowerInvariant(c));
                        }
                    }
                }
                return result.ToString();
            }
            catch
            {
                // 如果拼音转换失败，使用简化逻辑
                return text.Substring(0, Math.Min(2, text.Length)).ToLowerInvariant();
            }
        }

        /// <summary>
        /// 将拼音转换为仅字母的小写形式
        /// </summary>
        /// <param name="pinyin">拼音字符串</param>
        /// <returns>仅字母的小写字符串</returns>
        public static string LettersOnlyLower(string? pinyin)
        {
            if (string.IsNullOrEmpty(pinyin)) return string.Empty;
            
            var chars = pinyin.Where(c => char.IsLetter(c)).Select(c => char.ToLowerInvariant(c));
            return new string(chars.ToArray());
        }
    }
}
