using System;
using System.Threading.Tasks;
using UnitPlanGenerator.Models;

namespace UnitPlanGenerator.Services.Interfaces
{
    public enum LoginResult
    {
        Success,
        UserDoesNotExist,
        PasswordIsIncorrect
    }

    public class CurrentUserChangedEventArgs
    {
        public User PreviousUser { get; }

        public User NewUser { get; }

        public CurrentUserChangedEventArgs(User previousUser, User newUser)
        {
            PreviousUser = previousUser;
            NewUser = newUser;
        }
    }

    public interface IIdentityService
    {
        bool IsLoggedIn { get; }

        UserCredential UserCredential { get; }

        string Password { set; }

        User User { get; }

        User CurrentUser { get; }

        bool SetCurrentUser(User value);

        event EventHandler LoggedIn;

        event EventHandler LoggedOut;

        event EventHandler<CurrentUserChangedEventArgs> CurrentUserChanged;

        Task InitializeAsync();

        Task<LoginResult> LoginAsync(bool savePassword = false);

        Task<bool> LoginSilentAsync();

        void Logout();
    }
}
