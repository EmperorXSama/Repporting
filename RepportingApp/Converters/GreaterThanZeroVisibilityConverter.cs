using System.Globalization;
using Avalonia.Data.Converters;

namespace RepportingApp.Helper;

public class TaskInfoTypeToVisibilityConverter:IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Check if TakInfoType is Batch and return Visible, otherwise Collapsed
        if (value is TakInfoType taskInfoType && taskInfoType == TakInfoType.Batch)
        {
            return true;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}