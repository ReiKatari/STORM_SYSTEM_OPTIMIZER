using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StormSystemOptimizer.Converters
{
    public class BoolNegationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }
    }

    public class BoolToStatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFixed && isFixed)
            {
                return new SolidColorBrush(Color.FromArgb(255, 5, 46, 22)); // emerald bg
            }
            return new SolidColorBrush(Color.FromArgb(255, 32, 41, 58)); // slate bg
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
