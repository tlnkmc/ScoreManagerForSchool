using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ScoreManagerForSchool.Core.Storage;

namespace ScoreManagerForSchool.UI.Converters
{
    /// <summary>
    /// 将积分转换为关键等级颜色的转换器
    /// </summary>
    public class ScoreToLevelColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double score)
            {
                var level = CriticalScoreLevels.GetCriticalLevel(score);
                if (level != null)
                {
                    try
                    {
                        // 解析十六进制颜色
                        var colorStr = level.Color.TrimStart('#');
                        if (colorStr.Length == 6)
                        {
                            byte r = byte.Parse(colorStr[0..2], NumberStyles.HexNumber);
                            byte g = byte.Parse(colorStr[2..4], NumberStyles.HexNumber);
                            byte b = byte.Parse(colorStr[4..6], NumberStyles.HexNumber);
                            
                            // 根据parameter决定返回背景色还是前景色
                            if (parameter?.ToString() == "Background")
                            {
                                // 背景色使用较淡的版本
                                return new SolidColorBrush(Color.FromArgb(60, r, g, b));
                            }
                            else
                            {
                                // 前景色使用原色
                                return new SolidColorBrush(Color.FromRgb(r, g, b));
                            }
                        }
                    }
                    catch
                    {
                        // 解析失败时使用默认红色
                        return parameter?.ToString() == "Background" 
                            ? new SolidColorBrush(Color.FromArgb(60, 244, 67, 54))
                            : new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    }
                }
            }
            
            // 非关键状态返回透明或黑色
            return parameter?.ToString() == "Background" 
                ? new SolidColorBrush(Colors.Transparent)
                : new SolidColorBrush(Colors.Black);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// 将积分转换为关键等级名称的转换器
    /// </summary>
    public class ScoreToLevelNameConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double score)
            {
                var level = CriticalScoreLevels.GetCriticalLevel(score);
                return level?.Name ?? "正常";
            }
            return "正常";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// 将积分转换为字体粗细的转换器（关键等级使用粗体）
    /// </summary>
    public class ScoreToFontWeightConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double score && CriticalScoreLevels.IsCritical(score))
            {
                return FontWeight.Bold;
            }
            return FontWeight.Normal;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
