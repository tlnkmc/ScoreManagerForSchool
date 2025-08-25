using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ScoreManagerForSchool.UI.Converters
{
    /// <summary>
    /// 将布尔值转换为背景色，用于标识关键积分学生
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isCritical && isCritical)
            {
                return new SolidColorBrush(Color.FromRgb(255, 235, 235)); // 浅红色背景
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// 将布尔值转换为文本颜色，用于标识关键积分学生
    /// </summary>
    public class BoolToTextColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isCritical && isCritical)
            {
                return new SolidColorBrush(Color.FromRgb(200, 0, 0)); // 红色文本
            }
            return new SolidColorBrush(Color.FromRgb(0, 0, 0)); // 黑色文本
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// 将布尔值转换为字体粗细，用于突出显示关键积分
    /// </summary>
    public class BoolToFontWeightConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isCritical && isCritical)
            {
                return FontWeight.Bold;
            }
            return FontWeight.Normal;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
