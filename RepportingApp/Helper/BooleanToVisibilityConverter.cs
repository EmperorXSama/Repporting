using System;
using System.Globalization;
using Avalonia.Controls; // This is the correct namespace
using Avalonia.Data.Converters;
using ExCSS;

namespace RepportingApp.Helper
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the input is a boolean
            if (value is bool isVisible)
            {
                // Return Visible or Collapsed based on the boolean value
                return isVisible ? Visibility.Visible : Visibility.Collapse;
            }

            // Default fallback
            return Visibility.Collapse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}