using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ScoreManagerForSchool.Core.Storage;

namespace ScoreManagerForSchool.UI.Converters
{
    public class StringToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string colorString)
            {
                try
                {
                    return Color.Parse(colorString);
                }
                catch
                {
                    return Colors.Gray;
                }
            }
            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return color.ToString();
            }
            return "#808080";
        }
    }

    public class StringToBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string colorString)
            {
                try
                {
                    var color = Color.Parse(colorString);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    return new SolidColorBrush(Colors.Gray);
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color.ToString();
            }
            return "#808080";
        }
    }

    /// <summary>
    /// 将CriticalScoreLevel转换为对应的背景色（带透明度）
    /// </summary>
    public class CriticalLevelToBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is CriticalScoreLevel level && !string.IsNullOrEmpty(level.Color))
            {
                try
                {
                    var color = Color.Parse(level.Color);
                    // 使用较低的透明度作为背景色
                    var backgroundBrush = new SolidColorBrush(Color.FromArgb(60, color.R, color.G, color.B));
                    return backgroundBrush;
                }
                catch
                {
                    return new SolidColorBrush(Colors.Transparent);
                }
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将CriticalScoreLevel转换为对应的前景色
    /// </summary>
    public class CriticalLevelToForegroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is CriticalScoreLevel level && !string.IsNullOrEmpty(level.Color))
            {
                try
                {
                    var color = Color.Parse(level.Color);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    return new SolidColorBrush(Colors.Black);
                }
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
