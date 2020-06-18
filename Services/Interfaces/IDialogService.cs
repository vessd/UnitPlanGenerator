namespace UnitPlanGenerator.Services.Interfaces
{
    public interface IDialogService
    {
        string SaveFileDialog(string filter);

        string OpenFileDialog(string filter);
    }
}
