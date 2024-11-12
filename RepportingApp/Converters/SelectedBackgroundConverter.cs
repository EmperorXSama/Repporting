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
        return isChecked ? new SolidColorBrush(Color.Parse("#E5E6E4")) : Brushes.White;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}