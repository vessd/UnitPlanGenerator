using FluentValidation;
using Prism.Commands;
using Prism.Events;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using UnitPlanGenerator.Common;
using UnitPlanGenerator.Events;
using UnitPlanGenerator.Extensions;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.ViewModels
{
    public class UserDetailsViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly IEventAggregator _eventAggregator;

        private NotifyTask _saveTask;

        public User User { get; }

        public HashSet<string> UserNames { get; }

        public string DisplayName { get; set; }

        public string UserName { get; set; }

        public Role Role { get; set; }

        public bool IsSelected { get; set; }

        private void OnIsSelectedChanged()
        {
            if (!IsSelected && !_validationTemplate.HasErrors)
            {
                if (User.DisplayName != DisplayName ||
                    User.UserName != UserName ||
                    User.Role != Role)
                {
                    UserNames.Remove(User.UserName);
                    UserNames.Add(UserName);

                    User.DisplayName = DisplayName;
                    User.UserName = UserName;
                    User.Role = Role;

                    Save();
                }
            }
        }

        public DelegateCommand ResetPasswordCommand { get; }

        public UserDetailsViewModel(IDatabaseService databaseService,
                                    IEventAggregator eventAggregator,
                                    HashSet<string> userNames,
                                    User user)
        {
            _databaseService = databaseService;
            _eventAggregator = eventAggregator;

            User = user;
            UserNames = userNames;

            DisplayName = User.DisplayName;
            UserName = User.UserName;
            Role = User.Role;

            ResetPasswordCommand = new DelegateCommand(ResetPassword);
        }

        private void ResetPassword()
        {
            User.PasswordHash = null;
            Save();
        }

        private void Save()
        {
            if (_saveTask == null || _saveTask.IsCompleted)
            {
                var action = UserCollectionChangedAction.Add;
                if (User.Id != 0) action = UserCollectionChangedAction.Replace;
                _saveTask = NotifyTask.Crate(Task.Run(SaveAsync).ContinueWith(t => OnUserUpdated(action)));
            }
            else
            {
                _saveTask.PropertyChanged += SaveTask_PropertyChanged;
            }
        }

        private async Task SaveAsync()
        {
            await _databaseService.Context.UpsertAsync(User);
        }

        private void OnUserUpdated(UserCollectionChangedAction action)
        {
            _eventAggregator
                .GetEvent<UserCollectionChangedEvent>()
                .Publish(new UserCollectionChangedEventArgs(User.Id, action));
        }

        private void SaveTask_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_saveTask.IsCompleted)
            {
                _saveTask.PropertyChanged -= SaveTask_PropertyChanged;
                Save();
            }
        }
    }

    class UserDetailsViewModelValidator : AbstractValidator<UserDetailsViewModel>
    {
        public UserDetailsViewModelValidator()
        {
            RuleFor(vm => vm)
                .Must(vm => vm.DisplayName != null)
                .Must(vm => vm.UserName != null)
                .OverridePropertyName(nameof(UserDetailsViewModel));

            RuleFor(vm => vm.DisplayName)
                .NotEmpty()
                .WithMessage("Ф.И.О не может быть пустым.")
                .Unless(vm => vm.DisplayName == null);

            RuleFor(vm => vm.UserName)
                .NotEmpty()
                .WithMessage("Имя пользователя не может быть пустым.")
                .Must((vm, name) => name == vm.User.UserName || !vm.UserNames.Contains(name))
                .WithMessage("Пользователь с именем \"{PropertyValue}\" уже существует.")
                .Unless(vm => vm.UserName == null);
        }
    }
}
