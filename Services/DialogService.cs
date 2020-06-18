using Microsoft.Win32;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.Services
{
    public class DialogService : IDialogService
    {
        public string OpenFileDialog(string filter)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }

        public string SaveFileDialog(string filter)
        {
            var dialog = new SaveFileDialog
            {
                Filter = filter,
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }
    }
}
