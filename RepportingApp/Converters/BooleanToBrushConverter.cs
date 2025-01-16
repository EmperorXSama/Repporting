using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace RepportingApp.Converters;

public class BooleanToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSelected)
        {
            return isSelected ? Brushes.LightBlue : Brushes.White;
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}