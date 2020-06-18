using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using UnitPlanGenerator.Models;

namespace UnitPlanGenerator.Converters
{
    public class SubjectTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SubjectType type)
            {
                switch (type)
                {
                    case SubjectType.Elective:
                        return "Вариативная часть";

                    case SubjectType.Lecture:
                        return "Теоретическое занятие";

                    case SubjectType.Practice:
                        return "Практическое занятие";

                    case SubjectType.Laboratory:
                        return "Лабораторная работа";

                    case SubjectType.Independent:
                        return "Самостоятельная работа";

                    case SubjectType.CourseProject:
                        return "Курсовой проект";

                    case SubjectType.AcademicTraining:
                        return "Учебная практика";

                    case SubjectType.PracticalTraining:
                        return "Производственная практика";

                    case SubjectType.InterimAssessment:
                        return "Промежуточная аттестация";

                    case SubjectType.Consultation:
                        return "Консультация";
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
