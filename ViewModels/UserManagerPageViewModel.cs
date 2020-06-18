using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prism.Commands;
using Prism.Events;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UnitPlanGenerator.Common;
using UnitPlanGenerator.Events;
using UnitPlanGenerator.Extensions;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.ViewModels
{
    public class UserManagerPageViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly IEventAggregator _eventAggregator;

        private HashSet<string> _usersName;

        public NotifyTask InitTask { get; }

        public ObservableCollection<UserDetailsViewModel> Users { get; set; }

        public UserDetailsViewModel SelectedUser { get; set; }

        public bool UserDetailsVisible
        {
            get
            {
                return InitTask != null && InitTask.IsCompleted && SelectedUser != null;
            }
        }

        public DelegateCommand AddUserCommand { get; }

        public DelegateCommand RemoveUserCommand { get; }

        public UserManagerPageViewModel(IDatabaseService databaseService, IEventAggregator eventAggregator)
        {
            _databaseService = databaseService;
            _eventAggregator = eventAggregator;
            InitTask = NotifyTask.Crate(Task.Run(InitAsync));

            AddUserCommand = new DelegateCommand(AddUser);
            RemoveUserCommand = new DelegateCommand(RemoveUser);
        }

        private async Task InitAsync()
        {
            var users = await _databaseService.Context.Users.AsNoTracking().ToListAsync();
            _usersName = new HashSet<string>(users.Select(u => u.UserName));
            Users = new ObservableCollection<UserDetailsViewModel>(users.Where(u => u.Role != Role.Administrator)
                                                                        .Select(u => new UserDetailsViewModel(_databaseService, _eventAggregator, _usersName, u)));
        }

        private void AddUser()
        {
            var userVM = new UserDetailsViewModel(_databaseService, _eventAggregator, _usersName, new User { Role = Role.Lecturer });
            Users.Add(userVM);
            SelectedUser = userVM;
        }

        private void RemoveUser()
        {
            var userVM = SelectedUser;
            SelectedUser = null;
            Users.Remove(userVM);
            _usersName.Remove(userVM.UserName);
            Task.Run(() => RemoveUserAsync(userVM.User));
        }

        private async Task RemoveUserAsync(User user)
        {
            await _databaseService.Context.DeleteAsync(user);
            var args = new UserCollectionChangedEventArgs(user.Id, UserCollectionChangedAction.Remove);
            _eventAggregator.GetEvent<UserCollectionChangedEvent>().Publish(args);
        }
    }

    class UserManagerPageViewModelValidator : AbstractValidator<UserManagerPageViewModel> {}
}
