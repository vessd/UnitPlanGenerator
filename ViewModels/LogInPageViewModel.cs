using FluentValidation;
using PropertyChanged;
using Microsoft.Data.Sqlite;
using System.IO;
using UnitPlanGenerator.Services.Interfaces;
using Prism.Commands;
using System.Threading.Tasks;
using UnitPlanGenerator.Models;
using Npgsql;
using UnitPlanGenerator.Common;

namespace UnitPlanGenerator.ViewModels
{
    public class LogInPageViewModel : BaseViewModel
    {
        private readonly IIdentityService _identityService;

        public NotifyTask LoginTask { get; set; }

        internal bool UserDoesNotExist { get; private set; } = false;

        private void OnUserDoesNotExistChanged()
        {
            OnPropertyChanged(nameof(UserName));
        }

        public string UserName { get; set; }

        private void OnUserNameChanged()
        {
            _identityService.UserCredential.UserName = UserName;
            UserDoesNotExist = false;
            PasswordIsIncorrect = false;
        }

        internal bool PasswordIsIncorrect { get; private set; } = false;

        private void OnPasswordIsIncorrectChanged()
        {
            OnPropertyChanged(nameof(Password));
        }

        [DependsOn(nameof(UserName))]
        public string Password { get; set; }

        private void OnPasswordChanged()
        {
            _identityService.Password = Password;
            PasswordIsIncorrect = false;
            OnPropertyChanged(nameof(CanSavePassword));
        }

        public bool SavePassword { get; set; }

        public bool CanSavePassword => !string.IsNullOrEmpty(Password);

        public DatabaseProvider SelectedDatabaseProvider { get; set; }

        private void OnSelectedDatabaseProviderChanged()
        {
            _identityService.UserCredential.DatabaseProvider = SelectedDatabaseProvider;

            if (SelectedDatabaseProvider == DatabaseProvider.PostgreSQL)
            {
                Host = "localhost";
                Port = 5432;
                Database = "postgres";
                DatabaseUserName = "postgres";
                DatabasePassword = null;
            }
        }

        public string FilePath { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public string Database { get; set; }

        public string DatabaseUserName { get; set; }

        public string DatabasePassword { get; set; }

        public DelegateCommand LoginCommand { get; }

        public LogInPageViewModel(IIdentityService identityService)
        {
            _identityService = identityService;
            LoginCommand = new DelegateCommand(Login, CanLogin)
                .ObservesProperty(() => UserName)
                .ObservesProperty(() => Password)
                .ObservesProperty(() => SelectedDatabaseProvider)
                .ObservesProperty(() => FilePath)
                .ObservesProperty(() => Host)
                .ObservesProperty(() => Port)
                .ObservesProperty(() => Database)
                .ObservesProperty(() => DatabaseUserName)
                .ObservesProperty(() => DatabasePassword);

            if (!string.IsNullOrEmpty(_identityService.UserCredential.ConnectionString))
            {
                var provider = _identityService.UserCredential.DatabaseProvider;
                if (provider == DatabaseProvider.SQLite)
                {
                    var sqliteBuilder = new SqliteConnectionStringBuilder(_identityService.UserCredential.ConnectionString);
                    FilePath = sqliteBuilder.DataSource;
                }
                else if (provider == DatabaseProvider.PostgreSQL)
                {
                    SelectedDatabaseProvider = DatabaseProvider.PostgreSQL;

                    var npgsqlBuilder = new NpgsqlConnectionStringBuilder(_identityService.UserCredential.ConnectionString);
                    Host = npgsqlBuilder.Host;
                    Port = npgsqlBuilder.Port;
                    Database = npgsqlBuilder.Database;
                    DatabaseUserName = npgsqlBuilder.Username;
                    DatabasePassword = npgsqlBuilder.Password;
                }
            }

            if (!string.IsNullOrEmpty(_identityService.UserCredential.UserName))
            {
                UserName = _identityService.UserCredential.UserName;
            }
        }

        private void Login()
        {
            LoginTask = NotifyTask.Crate(Task.Run(LoginAsync).ContinueWith(t => LoginCommand.RaiseCanExecuteChanged()));
            LoginCommand.RaiseCanExecuteChanged();
        }

        private async Task LoginAsync()
        {
            switch (_identityService.UserCredential.DatabaseProvider)
            {
                case DatabaseProvider.SQLite:
                    var sqliteBuilder = new SqliteConnectionStringBuilder
                    {
                        DataSource = FilePath,
                    };
                    _identityService.UserCredential.ConnectionString = sqliteBuilder.ToString();
                    break;

                case DatabaseProvider.PostgreSQL:
                    var npgsqlBuilder = new NpgsqlConnectionStringBuilder
                    {
                        Host = Host,
                        Port = Port,
                        Database = Database,
                        Username = DatabaseUserName,
                        Password = DatabasePassword,
                    };
                    _identityService.UserCredential.ConnectionString = npgsqlBuilder.ToString();
                    break;
            }

            var loginResult = await _identityService.LoginAsync(SavePassword);
            UserDoesNotExist = loginResult == LoginResult.UserDoesNotExist;
            PasswordIsIncorrect = loginResult == LoginResult.PasswordIsIncorrect;
        }

        private bool CanLogin()
        {
            return (LoginTask == null || LoginTask.IsCompleted) && !_validationTemplate.HasErrors;
        }
    }

    class LogInPageViewModelValidator : AbstractValidator<LogInPageViewModel>
    {
        public LogInPageViewModelValidator()
        {
            RuleFor(vm => vm)
                .Must(vm => vm.UserName != null)
                .OverridePropertyName(nameof(LogInPageViewModel));

            RuleFor(vm => vm)
                .Must(vm => vm.FilePath != null)
                .When(vm => vm.SelectedDatabaseProvider == DatabaseProvider.SQLite)
                .OverridePropertyName(nameof(LogInPageViewModel));

            RuleFor(vm => vm)
                .Must(vm => vm.Host != null)
                .Must(vm => vm.Database != null)
                .Must(vm => vm.DatabaseUserName != null)
                .Must(vm => vm.DatabasePassword != null)
                .When(vm => vm.SelectedDatabaseProvider == DatabaseProvider.PostgreSQL)
                .OverridePropertyName(nameof(LogInPageViewModel));

            RuleFor(vm => vm.FilePath)
                .NotEmpty()
                .WithMessage("Путь к базе данных не может быть пустым.")
                .Must(path => Path.GetExtension(path) == ".db")
                .WithMessage("Расширение этого файла является недопустимым.")
                .When(vm => vm.SelectedDatabaseProvider == DatabaseProvider.SQLite)
                .Unless(vm => vm.FilePath == null);

            RuleFor(vm => vm.UserName)
                .NotEmpty()
                .WithMessage("Имя пользователя не может быть пустым.")
                .Must((vm, _) => !vm.UserDoesNotExist)
                .WithMessage("Пользователя \"{PropertyValue}\" не существует.")
                .Unless(vm => vm.UserName == null);

            RuleFor(vm => vm.Password)
                .Must((vm, _) => !vm.PasswordIsIncorrect)
                .WithMessage("Неверный пароль.");

            RuleFor(vm => vm.Host)
                .NotEmpty()
                .WithMessage("Адрес сервера не может быть пустым.")
                .When(vm => vm.SelectedDatabaseProvider == DatabaseProvider.PostgreSQL)
                .Unless(vm => vm.Host == null);

            RuleFor(vm => vm.Port)
                .InclusiveBetween(1, 65535)
                .WithMessage("Номер порта должен входить в диапозон от 1 до 65535.")
                .When(vm => vm.SelectedDatabaseProvider == DatabaseProvider.PostgreSQL);

            RuleFor(vm => vm.Database)
                .NotEmpty()
                .WithMessage("Название базы данных не может быть пустым.")
                .When(vm => vm.SelectedDatabaseProvider == DatabaseProvider.PostgreSQL)
                .Unless(vm => vm.Database == null);

            RuleFor(vm => vm.DatabaseUserName)
                .NotEmpty()
                .WithMessage("Имя пользователя базы данных не может быть пустым.")
                .When(vm => vm.SelectedDatabaseProvider == DatabaseProvider.PostgreSQL)
                .Unless(vm => vm.DatabaseUserName == null);

            RuleFor(vm => vm.DatabasePassword)
                .NotEmpty()
                .WithMessage("Пароль для подключения к базе данных не может быть пустым.")
                .When(vm => vm.SelectedDatabaseProvider == DatabaseProvider.PostgreSQL)
                .Unless(vm => vm.DatabasePassword == null);
        }
    }
}
