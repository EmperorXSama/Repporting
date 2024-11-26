using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace RepportingApp.Helper;

public class SelectedBackgroundConverter:IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isChecked = (bool)value;
        return isChecked ? new SolidColorBrush(Color.Parse("#1570ef")) : new SolidColorBrush(Color.Parse("#E0E0E0"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
public class SelectedForgroundConverter:IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isChecked = (bool)value;
        return isChecked ? new SolidColorBrush(Color.Parse("#1570ef")) : new SolidColorBrush(Color.Parse("#000814"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}