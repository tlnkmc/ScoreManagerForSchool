using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace ScoreManagerForSchool.UI.Converters
{
    public class EqualsConverter : IValueConverter, IMultiValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var a = value?.ToString();
            var b = parameter?.ToString();

            // Try numeric comparison first when possible
            if (double.TryParse(a, NumberStyles.Any, culture, out var da) && double.TryParse(b, NumberStyles.Any, culture, out var db))
            {
                return Math.Abs(da - db) < double.Epsilon;
            }

            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();

        // MultiBinding: compare first two values; optional parameter "Not" to invert
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Count < 2) return false;
                var a = values[0]?.ToString();
                var b = values[1]?.ToString();
                bool eq = string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
                if (parameter is string p && p.Equals("Not", StringComparison.OrdinalIgnoreCase)) eq = !eq;
                return eq;
            }
            catch { return false; }
        }

        public IList<object?> ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            var arr = new object?[targetTypes?.Length ?? 0];
            for (int i = 0; i < arr.Length; i++) arr[i] = AvaloniaProperty.UnsetValue;
            return arr;
        }
    }
}
