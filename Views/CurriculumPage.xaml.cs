using System.Windows;
using System.Windows.Controls;
using UnitPlanGenerator.ViewModels;

namespace UnitPlanGenerator.Views
{
    /// <summary>
    /// Логика взаимодействия для CurriculumPage.xaml
    /// </summary>
    public partial class CurriculumPage : Page
    {
        public CurriculumPage()
        {
            InitializeComponent();
        }

        private void CurriculaTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is ProgressViewModel progressViewModel)
            {
                if (DataContext is CurriculumPageViewModel viewModel)
                {
                    viewModel.SelectedProgressViewModel = progressViewModel;

                }
            }
        }
    }
}
