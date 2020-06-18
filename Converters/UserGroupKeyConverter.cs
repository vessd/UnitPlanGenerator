using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UnitPlanGenerator.Converters
{
    public class UserGroupKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return str.Substring(0, 1).ToUpper();
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
