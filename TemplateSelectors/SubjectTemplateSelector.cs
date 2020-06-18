using System.Windows;
using System.Windows.Controls;
using UnitPlanGenerator.Extensions;
using UnitPlanGenerator.ViewModels;

namespace UnitPlanGenerator.TemplateSelectors
{
    public class SubjectTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is TreeViewItem treeViewItem &&
                treeViewItem.ParentOfType<UserControl>() is UserControl userControl)
            {
                DataTemplate dataTemplate;
                if (item is SubjectSetViewModel)
                {
                    if (treeViewItem.IsSelected)
                    {
                        return userControl.FindResource("EditSubjectSetTemplate") as DataTemplate;
                    }

                    dataTemplate = userControl.FindResource("NormalSubjectSetTemplate") as DataTemplate;
                    return dataTemplate;
                }

                if (treeViewItem.IsSelected)
                    return userControl.FindResource("EditSubjectTemplate") as DataTemplate;

                return userControl.FindResource("NormalSubjectTemplate") as DataTemplate;
            }

            return null;
        }
    }
}
