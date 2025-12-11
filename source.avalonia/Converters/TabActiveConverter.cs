using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Ginger.Converters;

public class TabActiveConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive && isActive)
        {
            return Brushes.Transparent;
        }
        return new SolidColorBrush(Color.FromArgb(40, 128, 128, 128));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
