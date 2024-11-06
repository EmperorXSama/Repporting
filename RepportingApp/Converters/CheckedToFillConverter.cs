using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace RepportingApp.Helper;

public class CheckedToFillConverter:IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isChecked = (bool)value;
        return isChecked ? Brushes.White : Brushes.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}