using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace RepportingApp.Converters;

public class ForegroundColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string message)
        {
            return message.Contains("failed", StringComparison.OrdinalIgnoreCase) 
                ? Brushes.Red 
                : Brushes.Green;
        }
        return Brushes.Green; // Default color
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}