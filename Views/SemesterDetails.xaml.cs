using NPOI.SS.Formula.Functions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UnitPlanGenerator.Extensions;
using UnitPlanGenerator.ViewModels;

namespace UnitPlanGenerator.Views
{
    /// <summary>
    /// Логика взаимодействия для CoursePlanDetails.xaml
    /// </summary>
    public partial class SemesterDetails : UserControl
    {
        public SemesterDetails()
        {
            InitializeComponent();
        }

        private void SubjectTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is SemesterDetailsViewModel semesterDetailsViewModel)
            {
                semesterDetailsViewModel.SelectedNode = e.NewValue;
            }
        }

        private void DependencyObject_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DependencyObject dependencyObject)
            {
                if (dependencyObject.ParentOfType<TreeViewItem>() is TreeViewItem treeViewItem)
                {
                    treeViewItem.IsSelected = true;
                }
            }
        }
    }
}
