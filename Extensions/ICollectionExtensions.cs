using System;
using System.Collections.Generic;
using System.Windows;

namespace UnitPlanGenerator.Extensions
{
    public static class ICollectionExtensions
    {
        public static void AddOnUI<T>(this ICollection<T> collection, T item)
        {
            Action<T> addMethod = collection.Add;
            Application.Current.Dispatcher.BeginInvoke(addMethod, item);
        }

        public static void RemoveOnUI<T>(this ICollection<T> collection, T item)
        {
            Func<T, bool> removeMethod = collection.Remove;
            Application.Current.Dispatcher.BeginInvoke(removeMethod, item);
        }
    }
}
