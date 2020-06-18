namespace UnitPlanGenerator.Services.Interfaces
{
    public interface IPasswordService
    {
        void RemoveAll();

        void SavePassword(string userName, string password);

        bool TryGetPassword(string userName, out string password);
    }
}
