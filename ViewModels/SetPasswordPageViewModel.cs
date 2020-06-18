using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prism.Commands;
using PropertyChanged;
using System;
using System.Threading.Tasks;
using UnitPlanGenerator.Common;
using UnitPlanGenerator.Extensions;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.ViewModels
{
    public class SetPasswordPageViewModel : BaseViewModel
    {
        private readonly IIdentityService _identityService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IDatabaseService _databaseService;

        public NotifyTask SavePasswordTask { get; private set; }

        public string NewPassword { get; set; }

        [DependsOn(nameof(NewPassword))]
        public string ConfirmPassword { get; set; }

        public DelegateCommand SetPasswordCommand { get; }

        public DelegateCommand CancelCommand { get; }

        public SetPasswordPageViewModel(IIdentityService identityService,
                                        IPasswordHasher passwordHasher,
                                        IDatabaseService databaseService)
        {
            _identityService = identityService;
            _passwordHasher = passwordHasher;
            _databaseService = databaseService;

            SetPasswordCommand = new DelegateCommand(SetPassword, CanSetPassword)
                .ObservesProperty(() => NewPassword)
                .ObservesProperty(() => ConfirmPassword);
            CancelCommand = new DelegateCommand(() => _identityService.Logout());
        }

        private void SetPassword()
        {
            SavePasswordTask = NotifyTask.Crate(Task.Run(SetPasswordAsync).ContinueWith(t => SetPasswordCommand.RaiseCanExecuteChanged()));
            SetPasswordCommand.RaiseCanExecuteChanged();
        }

        private async Task SetPasswordAsync()
        {

            _identityService.User.PasswordHash = _passwordHasher.HashPassword(NewPassword);
            _identityService.Password = NewPassword;
            await _databaseService.Context.UpsertAsync(_identityService.User);          
            await _identityService.LoginAsync();
        }

        private bool CanSetPassword()
        {
            return (SavePasswordTask == null || SavePasswordTask.IsCompleted) &&
                   !_validationTemplate.HasErrors;
        }
    }

    class SetPasswordPageViewModelValidator : AbstractValidator<SetPasswordPageViewModel>
    {
        public SetPasswordPageViewModelValidator()
        {
            RuleFor(vm => vm)
                .Must(vm => vm.NewPassword != null)
                .Must(vm => vm.ConfirmPassword != null)
                .OverridePropertyName(nameof(SetPasswordPageViewModel));

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