using System;
using NPinyin;

namespace ScoreManagerForSchool.Core.Storage
{
    public static class PinyinUtil
    {
        public static (string Full, string Initials) MakeKeys(string? name)
        {
            var full = Full(name);
            var init = Initials(name);
            return (full, init);
        }

        public static string Full(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var py = Pinyin.GetPinyin(name);
            return LettersOnlyLower(py);
        }

        public static string Initials(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;
            var py = Pinyin.GetPinyin(name);
            var parts = py.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return string.Empty;
            var sb = new System.Text.StringBuilder(parts.Length);
            foreach (var p in parts)
            {
                sb.Append(char.ToLowerInvariant(p[0]));
            }
            return sb.ToString();
        }

        public static string LettersOnlyLower(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var arr = s.Where(ch => char.IsLetter(ch)).Select(ch => char.ToLowerInvariant(ch)).ToArray();
            return new string(arr);
        }
    }
}
