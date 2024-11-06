using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace RepportingApp.Helper;

public class GroupNameToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string groupName)
        {
            return groupName == "No Group" ? new SolidColorBrush(Color.Parse("#FF5E2B")) : new SolidColorBrush(Color.Parse("#7fd1ae"));
        }
        return Brushes.Green; // Default color if name is null or not "NO group"
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}