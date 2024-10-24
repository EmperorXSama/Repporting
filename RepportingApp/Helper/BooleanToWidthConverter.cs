using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace RepportingApp.Helper
{
    public class BooleanToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Check if the input is a boolean
            if (value is bool isOpen)
            {
                // Return the width based on whether the menu is open or closed
                return isOpen ? 250 : 0; // 250 when open, 0 when closed
            }

            // Default fallback
            return 0; // Default when the value is not a boolean
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class AngleToXConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double angle = (double)value;
            // Adjust these values based on the center and radius of your pie chart
            double centerX = 100; 
            double radius = 80; 
            return centerX + radius * Math.Cos(angle * Math.PI / 180);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class AngleToYConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double angle = (double)value;
            double centerY = 100;
            double radius = 80;
            return centerY + radius * Math.Sin(angle * Math.PI / 180);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

}