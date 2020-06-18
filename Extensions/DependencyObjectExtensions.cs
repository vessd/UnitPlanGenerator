using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace UnitPlanGenerator.Extensions
{
    public static class DependencyObjectExtensions
    {
        public static T ParentOfType<T>(this DependencyObject element) where T : DependencyObject
        {
            if (element == null)
                return default;
            else
                return GetParents(element).OfType<T>().FirstOrDefault();
        }

        private static IEnumerable<DependencyObject> GetParents(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            while ((element = GetParent(element)) != null)
                yield return element;
        }

        private static DependencyObject GetParent(DependencyObject element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            if (parent == null)
            {
                if (element is FrameworkElement frameworkElement)
                {
                    parent = frameworkElement.Parent;
                }
            }
            return parent;
        }
    }
}
