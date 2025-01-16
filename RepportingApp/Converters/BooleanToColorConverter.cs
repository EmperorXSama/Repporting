using System.Globalization;
using Avalonia.Data.Converters;

namespace RepportingApp.Converters;

public class BooleanToColorConverter : IValueConverter

{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
            if (value is bool isSelected)
            {
                return isSelected ? "#E0F7FA" : "White"; 
            }
            return "White";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}