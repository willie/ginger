using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Ginger.Converters;

public class BoolToOpacityConverter : IValueConverter
{
    public double TrueValue { get; set; } = 0.4;
    public double FalseValue { get; set; } = 1.0;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            return b ? TrueValue : FalseValue;
        }
        return FalseValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
