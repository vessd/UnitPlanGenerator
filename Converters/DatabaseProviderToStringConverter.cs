using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using UnitPlanGenerator.Models;

namespace UnitPlanGenerator.Converters
{
    class DatabaseProviderToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DatabaseProvider provider)
            {
                switch (provider)
                {
                    case DatabaseProvider.SQLite:
                        return "SQLite";

                    case DatabaseProvider.PostgreSQL:
                        return "PostgreSQL";
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
