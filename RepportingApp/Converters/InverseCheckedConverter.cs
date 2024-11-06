using System.Globalization;
using Avalonia.Data.Converters;

namespace RepportingApp.Helper;

public class InverseCheckedConverter:IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isChecked)
        {
            return !isChecked; // Flips the logic
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}