using System.Threading.Tasks;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.Services
{
    public class UserCredentialService : IUserCredentialService
    {
        public Task<UserCredential> LoadAsync()
        {
            return Task.Run(() => new UserCredential
            {
                DatabaseProvider = (DatabaseProvider)Properties.Settings.Default.DatabaseProvider,
                ConnectionString = Properties.Settings.Default.ConnectionString,
                UserName = Properties.Settings.Default.UserName,
            });
        }

        public Task SaveAsync(UserCredential userCredential)
        {
            return Task.Run(() =>
            {
                Properties.Settings.Default.DatabaseProvider = (ushort)userCredential.DatabaseProvider;
                Properties.Settings.Default.ConnectionString = userCredential.ConnectionString;
                Properties.Settings.Default.UserName = userCredential.UserName;
                Properties.Settings.Default.Save();
            });
        }
    }
}
