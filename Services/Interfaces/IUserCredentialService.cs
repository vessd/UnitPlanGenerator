using System.Threading.Tasks;
using UnitPlanGenerator.Models;

namespace UnitPlanGenerator.Services.Interfaces
{
    public interface IUserCredentialService
    {
        Task<UserCredential> LoadAsync();

        Task SaveAsync(UserCredential userCredential);
    }
}
