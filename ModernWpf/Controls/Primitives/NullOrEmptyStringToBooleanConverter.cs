using System;
using System.Globalization;
using System.Windows.Data;

namespace ModernWpf.Controls.Primitives
{
    [ValueConversion(typeof(object), typeof(bool))]
    public class NullOrEmptyStringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value?.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value?.ToString());
        }
    }
}
