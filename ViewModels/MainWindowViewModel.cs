using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;
using UnitPlanGenerator.Common;
using UnitPlanGenerator.Events;
using UnitPlanGenerator.Extensions;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services;
using UnitPlanGenerator.Services.Interfaces;
using UnitPlanGenerator.Views;

namespace UnitPlanGenerator.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly IIdentityService _identityService;
        private readonly IDatabaseService _databaseService;
        private readonly IEventAggregator _eventAggregator;

        private readonly NotifyTask _loginSilentTask;

        public bool CanGoBack => NavigationService.CanGoBack;

        public bool IsBusy { get; private set; }

        public bool IsActive => !IsBusy;

        public bool IsPaneVisible { get; private set; }

        public Role? UserRole { get; private set; }

        public NotifyTask UsersTask { get; set; }

        public ObservableCollection<User> Users { get; set; }

        public User SelectedUser { get; set; }

        private void OnSelectedUserChanged()
        {
            _identityService.SetCurrentUser(SelectedUser);
            switch (SelectedUser.Role)
            {
                case Role.Administrator:
                    NavigationService.Navigate(typeof(UserManagerPage), true);
                    break;

                case Role.CurriculumDeveloper:
                    NavigationService.Navigate(typeof(CurriculumPage), true);
                    break;

                case Role.Lecturer:
                    NavigationService.Navigate(typeof(CurriculumPage), true);
                    break;
            }
            OnPropertyChanged(nameof(CanGoBack));
        }

        public MainWindowViewModel(IIdentityService identityService,
                                   IDatabaseService databaseService,
                                   IEventAggregator eventAggregator)
        {
            IsBusy = true;
            IsPaneVisible = false;
            
            _identityService = identityService;
            _databaseService = databaseService;
            _eventAggregator = eventAggregator;

            _loginSilentTask = NotifyTask.Crate(Task.Run(LoginSilentAsync));

            _loginSilentTask.PropertyChanged += LoginSilentTask_PropertyChanged;
            _identityService.LoggedIn += IdentityService_LoggedIn;
            _identityService.LoggedOut += IdentityService_LoggedOut;
            NavigationService.Navigated += NavigationService_Navigated;
        }

        private async Task LoginSilentAsync()
        {
            await _identityService.InitializeAsync();
            await _identityService.LoginSilentAsync();
        }

        private void NavigationService_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            OnPropertyChanged(nameof(CanGoBack));
        }

        private void IdentityService_LoggedIn(object sender, EventArgs e)
        {
            if (_identityService.IsLoggedIn)
            {
                if (string.IsNullOrEmpty(_identityService.User.PasswordHash))
                {
                    NavigationService.Navigate(typeof(SetPasswordPage));
                }
                else
                {
                    UserRole = _identityService.User.Role;

                    if (_identityService.User.Role == Role.Administrator)
                    {
                        UsersTask = NotifyTask.Crate(Task.Run(InitUsers));
                        _eventAggregator.GetEvent<UserCollectionChangedEvent>().Subscribe(OnUserCollectionChanged);
                    }
                    else
                    {
                        SelectedUser = _identityService.User;
                    }

                    IsPaneVisible = true;
                }
            }
        }

        private void IdentityService_LoggedOut(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            Application.Current.Shutdown();
        }

        private void LoginSilentTask_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_loginSilentTask.IsCompleted)
            {
                IsBusy = false;
                if (!_identityService.IsLoggedIn)
                {
                    NavigationService.Navigate(typeof(LogInPage));
                }
            }
        }

        private async Task InitUsers()
        {
            Expression<Func<User, bool>> predicate = u => u.Id == _identityService.User.Id || u.Role != Role.Administrator;
            var users = await _databaseService.Context.Users.Where(predicate).ToListAsync();
            Users = new ObservableCollection<User>(users);
        }

        private async void OnUserCollectionChanged(UserCollectionChangedEventArgs args)
        {
            if (args.Action == UserCollectionChangedAction.Remove)
            {
                Users.RemoveOnUI(Users.FirstOrDefault(u => u.Id == args.UserId));
            }
            else
            {
                var user = await _databaseService.Context.Users.FirstOrDefaultAsync(u => u.Id == args.UserId);
                if (args.Action == UserCollectionChangedAction.Replace)
                {
                    Users.RemoveOnUI(Users.FirstOrDefault(u => u.Id == args.UserId));
                }
                Users.AddOnUI(user);
            }
        }
    }

    class MainWindowViewModelValidator : AbstractValidator<MainWindowViewModel>
    {
        public MainWindowViewModelValidator()
        {
            
        }
    }
}
