using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using UnitPlanGenerator.Models;

namespace UnitPlanGenerator.Converters
{
    public class UserRoleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Role role)
            {
                switch (role)
                {
                    case Role.Administrator:
                        return "Администратор";

                    case Role.CurriculumDeveloper:
                        return "Методист";

                    case Role.Lecturer:
                        return "Преподаватель";
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
