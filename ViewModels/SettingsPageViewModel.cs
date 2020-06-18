using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prism.Commands;
using PropertyChanged;
using System.Threading.Tasks;
using UnitPlanGenerator.Common;
using UnitPlanGenerator.Extensions;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.ViewModels
{
    public class SettingsPageViewModel : BaseViewModel
    {
        private readonly IIdentityService _identityService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IDatabaseService _databaseService;

        public NotifyTask SaveUserTask { get; set; }

        public NotifyTask SavePasswordTask { get; set; }

        public string UserName { get; set; }

        private void OnUserNameChanged()
        {
            UserNameAlreadyExists = false;
        }

        public bool UserNameAlreadyExists { get; private set; }

        private void OnUserNameAlreadyExistsChanged()
        {
            OnPropertyChanged(nameof(UserName));
        }

        public string DisplayName { get; set; }

        public string CurrentPassword { get; set; }

        private void OnCurrentPasswordChanged()
        {
            PasswordIsIncorrect = false;
        }

        public bool PasswordIsIncorrect { get; private set; } = false;

        private void OnPasswordIsIncorrectChanged()
        {
            OnPropertyChanged(nameof(CurrentPassword));
        }

        public string NewPassword { get; set; }

        [DependsOn(nameof(NewPassword))]
        public string ConfirmPassword { get; set; }

        public DelegateCommand SaveUserCommand { get; }

        public DelegateCommand CancelUserCommand { get; }

        public DelegateCommand SavePasswordCommand { get; }

        public DelegateCommand LogOutCommand { get; }

        public SettingsPageViewModel(IIdentityService identityService,
                                     IPasswordHasher passwordHasher,
                                     IDatabaseService databaseService)
        {
            _identityService = identityService;
            _passwordHasher = passwordHasher;
            _databaseService = databaseService;

            _identityService.CurrentUserChanged += IdentityService_CurrentUserChanged;

            UserName = _identityService.CurrentUser.UserName;
            DisplayName = _identityService.CurrentUser.DisplayName;

            SaveUserCommand = new DelegateCommand(SaveUser, CanSaveUser)
                .ObservesProperty(() => UserName)
                .ObservesProperty(() => DisplayName);

            CancelUserCommand = new DelegateCommand(CancelUser, CanCancelUser)
                .ObservesProperty(() => UserName)
                .ObservesProperty(() => DisplayName);

            SavePasswordCommand = new DelegateCommand(SavePasswrod, CanSavePassword)
                .ObservesProperty(() => CurrentPassword)
                .ObservesProperty(() => NewPassword)
                .ObservesProperty(() => ConfirmPassword);

            LogOutCommand = new DelegateCommand(() => _identityService.Logout());
        }

        private void IdentityService_CurrentUserChanged(object sender, CurrentUserChangedEventArgs e)
        {
            UserName = _identityService.CurrentUser.UserName;
            DisplayName = _identityService.CurrentUser.DisplayName;
            CurrentPassword = null;
            NewPassword = null;
            ConfirmPassword = null;
        }

        private void CancelUser()
        {
            UserName = _identityService.CurrentUser.UserName;
            DisplayName = _identityService.CurrentUser.DisplayName;
        }

        private bool CanCancelUser()
        {
            return (SaveUserTask == null || SaveUserTask.IsCompleted) &&
                   (_identityService.CurrentUser.UserName != UserName || _identityService.CurrentUser.DisplayName != DisplayName);
        }

        private void SaveUser()
        {
            SaveUserTask = NotifyTask.Crate(Task.Run(SaveUserAsync)
                                     .ContinueWith(t => {
                                         SaveUserCommand.RaiseCanExecuteChanged();
                                         CancelUserCommand.RaiseCanExecuteChanged();
                                     }));
            SaveUserCommand.RaiseCanExecuteChanged();
            CancelUserCommand.RaiseCanExecuteChanged();
        }

        private async Task SaveUserAsync()
        {
            var context = _databaseService.Context;

            var checkUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == UserName);
            if (checkUser == null)
            {
                _identityService.CurrentUser.UserName = UserName;
                _identityService.CurrentUser.DisplayName = DisplayName;
                await _databaseService.Context.UpsertAsync(_identityService.CurrentUser);
            }
            else
            {
                UserNameAlreadyExists = true;
            }
        }

        private bool CanSaveUser()
        {
            return (SaveUserTask == null || SaveUserTask.IsCompleted) &&
                   (_identityService.CurrentUser.UserName != UserName || _identityService.CurrentUser.DisplayName != DisplayName) &&
                   string.IsNullOrEmpty(_validationTemplate[nameof(UserName)]) &&
                   string.IsNullOrEmpty(_validationTemplate[nameof(DisplayName)]);
        }

        private void SavePasswrod()
        {
            SavePasswordTask = NotifyTask.Crate(Task.Run(SavePasswordAsync));
            SavePasswordCommand.RaiseCanExecuteChanged();
        }

        private async Task SavePasswordAsync()
        {
            var hash = _identityService.CurrentUser.PasswordHash;
            if (!string.IsNullOrEmpty(hash) && _passwordHasher.VerifyPassword(hash, CurrentPassword))
            {
                //var context = _databaseService.Context;
                //context.Users.Attach(_identityService.CurrentUser);
                _identityService.CurrentUser.PasswordHash = _passwordHasher.HashPassword(NewPassword);
                //await context.SaveChangesAsync();
                //context.Entry(_identityService.CurrentUser).State = EntityState.Detached;
                await _databaseService.Context.UpsertAsync(_identityService.CurrentUser);

                CurrentPassword = null;
                NewPassword = null;
                ConfirmPassword = null;
            }
            else
            {
                PasswordIsIncorrect = true;
            }
        }

        private bool CanSavePassword()
        {
            return (SavePasswordTask == null || SavePasswordTask.IsCompleted) &&
                   string.IsNullOrEmpty(_validationTemplate[nameof(SettingsPageViewModel)]) &&
                   string.IsNullOrEmpty(_validationTemplate[nameof(CurrentPassword)]) &&
                   string.IsNullOrEmpty(_validationTemplate[nameof(NewPassword)]) &&
                   string.IsNullOrEmpty(_validationTemplate[nameof(ConfirmPassword)]);
        }
    }

    class SettingsPageViewModelValidator : AbstractValidator<SettingsPageViewModel>
    {
        public SettingsPageViewModelValidator()
        {
            RuleFor(vm => vm)
                .Must(vm => vm.CurrentPassword != null)
                .Must(vm => vm.NewPassword != null)
                .Must(vm => vm.ConfirmPassword != null)
                .OverridePropertyName(nameof(SettingsPageViewModel));

            RuleFor(vm => vm.UserName)
                .NotEmpty()
                .WithMessage("Имя пользователя не может быть пустым.")
                .Must((vm, _) => !vm.UserNameAlreadyExists)
                .WithMessage("Пользователь с именем \"{PropertyValue}\" уже существует.");

            RuleFor(vm => vm.DisplayName)
                .NotEmpty()
                .WithMessage("Ф.И.О не может быть пустым.");

            RuleFor(vm => vm.CurrentPassword)
                .NotEmpty()
                .WithMessage("Пароль не может быть пустым.")
                .Must((vm, _) => !vm.PasswordIsIncorrect)
                .WithMessage("Неверный пароль.")
                .Unless(vm => vm.CurrentPassword == null);

            RuleFor(vm => vm.NewPassword)
                .NotEmpty()
                .WithMessage("Пароль не может быть пустым.")
                .Unless(vm => vm.NewPassword == null);

            RuleFor(vm => vm.ConfirmPassword)
                .NotEmpty()
                .WithMessage("Пароль не может быть пустым.")
                .Must((vm, password) => vm.NewPassword == password)
                .WithMessage("Пароли не совпадают.")
                .Unless(vm => vm.ConfirmPassword == null);
        }
    }
}
