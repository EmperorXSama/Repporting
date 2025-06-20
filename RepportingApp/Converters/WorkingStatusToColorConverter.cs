using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using TaskStatus = RepportingApp.Models.UI.TaskStatus;

namespace RepportingApp.Converters;

public class WorkingStatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Check if the text is "Working..."
        if (value is TaskStatus status && status == TaskStatus.Waiting )
        {
            return Brushes.Red; // Return green for "Working..."
        }
        return Brushes.Green; // Return red for any other status
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}public class IntToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int selected && parameter is int i)
            return selected == i;
        if (value is int selectedStr && parameter is string currentStr && int.TryParse(currentStr, out var current))
            return selectedStr == current;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
