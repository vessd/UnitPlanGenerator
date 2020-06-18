using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly IPasswordService _passwordService;
        private readonly IPasswordHasher _passwordHasherService;
        private readonly IUserCredentialService _userCredentialService;
        private readonly IDatabaseService _databaseService;

        private User _user;

        public User User
        {
            get { return _user; }
            private set
            {
                _user = value;
                CurrentUser = _user;
            }
        }

        public User CurrentUser { get; private set; }

        public bool SetCurrentUser(User value)
        {
            if (_user?.Role == Role.Administrator && CurrentUser != value)
            {
                var previousUser = CurrentUser;
                CurrentUser = value;
                CurrentUserChanged?.Invoke(this, new CurrentUserChangedEventArgs(previousUser, CurrentUser));
                return true;
            }
            return false;
        }



        public bool IsLoggedIn => User != null;

        public UserCredential UserCredential { get; private set; }

        public string Password { private get; set; }


        public event EventHandler LoggedIn;

        public event EventHandler LoggedOut;

        public event EventHandler<CurrentUserChangedEventArgs> CurrentUserChanged;

        public IdentityService(IPasswordService passwordService,
                               IPasswordHasher passwordHasherService,
                               IUserCredentialService userCredentialService,
                               IDatabaseService databaseService)
        {
            _passwordService = passwordService;
            _passwordHasherService = passwordHasherService;
            _userCredentialService = userCredentialService;
            _databaseService = databaseService;
        }

        public async Task InitializeAsync()
        {
            UserCredential = await _userCredentialService.LoadAsync();
            if (UserCredential == null)
            {
                UserCredential = new UserCredential();
            }
        }

        private bool TryGetPassword(out string password)
        {
            password = null;

            if (!string.IsNullOrEmpty(UserCredential.UserName))
            {
                return _passwordService.TryGetPassword(UserCredential.UserName, out password);
            }

            return false;
        }

        private bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            if (!string.IsNullOrEmpty(hashedPassword) && !string.IsNullOrEmpty(providedPassword))
            {
                return _passwordHasherService.VerifyPassword(hashedPassword, providedPassword);
            }
            return false;
        }

        private async Task<User> GetUser()
        {
            if (!string.IsNullOrEmpty(UserCredential.ConnectionString) && !string.IsNullOrEmpty(UserCredential.UserName))
            {
                var dbOptionBuilder = new DbContextOptionsBuilder<UnitPlanContext>().UseLazyLoadingProxies();
                DbContextOptions<UnitPlanContext> dbOptions;
                
                switch (UserCredential.DatabaseProvider)
                {
                    case DatabaseProvider.SQLite:
                        dbOptions = dbOptionBuilder.UseSqlite(UserCredential.ConnectionString).Options;
                        break;
                    case DatabaseProvider.PostgreSQL:
                        dbOptions = dbOptionBuilder.UseNpgsql(UserCredential.ConnectionString).Options;
                        break;
                    default:
                        return null;
                }

                _databaseService.SetOptions(dbOptions);
                return await _databaseService.Context.Users.FirstOrDefaultAsync(u => u.UserName == UserCredential.UserName);
            }

            return null;
        }

        public async Task<bool> LoginSilentAsync()
        {
            if (TryGetPassword(out var password) && await GetUser() is User user)
            {
                if (VerifyPassword(user.PasswordHash, password))
                {
                    User = user;
                    LoggedIn?.Invoke(this, EventArgs.Empty);

                    return true;
                }
                _passwordService.RemoveAll();
            }

            return false;
        }

        public async Task<LoginResult> LoginAsync(bool savePassword = false)
        {
            if (await GetUser() is User user)
            {
                if (user.PasswordHash == null || VerifyPassword(user.PasswordHash, Password))
                {
                    await _userCredentialService.SaveAsync(UserCredential);

                    User = user;

                    if (savePassword && !string.IsNullOrEmpty(Password))
                    {
                        _passwordService.SavePassword(UserCredential.UserName, Password);
                    }
                    Password = null;
                    
                    LoggedIn?.Invoke(this, EventArgs.Empty);

                    return LoginResult.Success;
                }

                return LoginResult.PasswordIsIncorrect;
            }

            return LoginResult.UserDoesNotExist;
        }

        public void Logout()
        {
            User = null;
            _passwordService.RemoveAll();
            LoggedOut?.Invoke(this, EventArgs.Empty);
        }
    }
}
