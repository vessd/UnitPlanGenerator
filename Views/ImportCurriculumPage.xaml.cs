using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UnitPlanGenerator.Views
{
    /// <summary>
    /// Логика взаимодействия для ImportCurriculumPage.xaml
    /// </summary>
    public partial class ImportCurriculumPage : Page
    {
        public ImportCurriculumPage()
        {
            InitializeComponent();
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Книга Excel 97-2003 (*.xls)|*.xls",
            };

            if (dialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = dialog.FileName;
            }
        }

        private void ComboBoxSpecialty_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var combobox = sender as ComboBox;
            if (Regex.IsMatch(e.Text, "^[а-яА-Я]*$"))
            {
                var cmbTextBox = (TextBox)combobox.Template.FindName("PART_EditableTextBox", combobox);
                if (cmbTextBox.Text.Length + e.Text.Length - cmbTextBox.SelectionLength <= 5)
                {
                    int index = cmbTextBox.CaretIndex;
                    cmbTextBox.Text = cmbTextBox.Text.Remove(index, cmbTextBox.SelectionLength).Insert(index, e.Text.ToUpper());
                    cmbTextBox.CaretIndex = index + e.Text.Length;
                }
            }
            e.Handled = true;
        }

        private void ComboBoxYear_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var cmbTextBox = (TextBox)(sender as ComboBox).Template.FindName("PART_EditableTextBox", (sender as ComboBox));
            e.Handled = cmbTextBox.Text.Length + e.Text.Length - cmbTextBox.SelectionLength > 2 || !Regex.IsMatch(e.Text, "^[0-9]*$");
        }

        private void ComboBoxSpecialty_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = e.Key == Key.Space;
        }

        private void ComboBoxYear_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = e.Key == Key.Space;
        }
    }
}
