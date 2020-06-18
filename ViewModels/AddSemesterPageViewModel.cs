using FluentValidation;
using ImTools;
using Microsoft.EntityFrameworkCore;
using Prism.Commands;
using Prism.Events;
using PropertyChanged;
using System;
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
    public class AddSemesterPageViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly IIdentityService _identityService;
        private readonly IEventAggregator _eventAggregator;

        private const string _allString = "<Все>";

        public NotifyTask InitTask { get; set; }

        public NotifyTask AddTask { get; set; }

        public List<string> Years { get; set; }

        public string SelectedYear { get; set; }

        private void OnSelectedYearChanged()
        {
            FilterSemesters();
        }

        public List<string> Specialties { get; set; }

        public string SelectedSpecialty { get; set; }

        private void OnSelectedSpecialtyChanged()
        {
            FilterSemesters();
        }

        public List<string> Semesters { get; set; }

        public string SelectedSemester { get; set; }

        private void OnSelectedSemesterChanged()
        {
            FilterSemesters();
        }

        public string Filter { get; set; }

        private void OnFilterChanged()
        {
            FilterSemesters();
        }

        public List<SemesterViewModel> SemesterViewModels { get; set; }

        private void OnSemesterViewModelsChanged()
        {
            FilterSemesters();
        }

        [DoNotNotify]
        public List<SemesterViewModel> FilteredSemesterViewModels { get; set; }

        public DelegateCommand AddSemesterCommand { get; }

        public AddSemesterPageViewModel(IDatabaseService databaseService,
                                      IIdentityService identityService,
                                      IEventAggregator eventAggregator)
        {
            _databaseService = databaseService;
            _identityService = identityService;
            _eventAggregator = eventAggregator;

            _identityService.CurrentUserChanged += IdentityService_OnCurrentUserChanged;
            _eventAggregator.GetEvent<SemesterSelectionChangedEvent>().Subscribe(OnSemesterSelectionChanged);

            AddSemesterCommand = new DelegateCommand(AddSemester, CanAddSemester);

            InitTask = NotifyTask.Crate(Task.Run(InitAsync));
        }

        private void IdentityService_OnCurrentUserChanged(object sender, CurrentUserChangedEventArgs e)
        {
            if (e.NewUser.Role == Role.Lecturer)
            {
                InitTask = NotifyTask.Crate(Task.Run(InitAsync));
            }
        }

        private void OnSemesterSelectionChanged(int semesterId)
        {
            AddSemesterCommand.RaiseCanExecuteChanged();
        }

        private void AddSemester()
        {
            AddTask = NotifyTask.Crate(Task.Run(AddSemesterAsync).ContinueWith(t =>
            {
                FilterSemesters();
                AddSemesterCommand.RaiseCanExecuteChanged();
                _eventAggregator.GetEvent<SemesterCollectionChangedEvent>().Publish();
            }));
        }

        private async Task AddSemesterAsync()
        {
            var context = _databaseService.Context;

            var selectedVMs = SemesterViewModels.Where(vm => vm.IsSelected).ToList();
            var selectedIds = selectedVMs.Select(vm => vm.SemesterId).ToHashSet();

            var semesters = await context.Semesters.Where(s => selectedIds.Any(id => id == s.Id)).ToListAsync();
                    
            foreach(var vm in selectedVMs)
            {
                SemesterViewModels.RemoveOnUI(vm);
            }

            foreach (var semester in semesters)
            {
                semester.User = _identityService.CurrentUser;

                if (semester.Course.Type == CourseType.PracticalTraining)
                {
                    var title = context.Titles.FirstOrDefault(t => t.Value == "Производственная практика");
                    if (title == null)
                    {
                        title = new Title { Value = "Производственная практика" };
                        context.Titles.Add(title);
                    }

                    var subjectSet = new SubjectSet
                    {
                        Number = 1,
                        Title = title,
                        Semester = semester
                    };
                    context.SubjectSets.Add(subjectSet);

                    var subject = new Subject
                    {
                        Number = 1,
                        Type = SubjectType.PracticalTraining,
                        Title = title,
                        Hours = semester.Hours.Training,
                        SubjectSet = subjectSet
                    };
                    context.Subjects.Add(subject);
                }
            }
            await context.SaveChangesAsync();
        }

        private bool CanAddSemester()
        {
            return (AddTask == null || AddTask.IsCompleted) &&
                   SemesterViewModels != null &&
                   SemesterViewModels.Any(vm => vm.IsSelected);
        }

        private async Task InitAsync()
        {
            var context = _databaseService.Context;

            var vacantSemesters = await context.Semesters.Where(s => s.User == null).ToListAsync();

            var courseViewModels = vacantSemesters
                .OrderBy(s => s.Number)
                .OrderBy(s => s.Course.Name)
                .Select(s => new SemesterViewModel(_eventAggregator, s))
                .ToList();

            var years = courseViewModels.Select(vm => vm.Year).Distinct().OrderBy(v => v).ToList();
            years.Insert(0, _allString);
            Years = years;
            SelectedYear = Years.First();

            var specialties = courseViewModels.Select(vm => vm.Specialty).Distinct().OrderBy(v => v).ToList();
            specialties.Insert(0, _allString);
            Specialties = specialties;
            SelectedSpecialty = Specialties.First();

            var semesters = courseViewModels.Select(vm => vm.Semester.ToString()).Distinct().OrderBy(v => v).ToList();
            semesters.Insert(0, _allString);
            Semesters = semesters;
            SelectedSemester = Semesters.First();

            SemesterViewModels = courseViewModels;
        }

        private void FilterSemesters()
        {
            if (SemesterViewModels != null)
            {
                foreach (var courseViewModel in SemesterViewModels.Where(vm => vm.IsSelected))
                {
                    courseViewModel.IsSelected = false;
                }

                IEnumerable<SemesterViewModel> filteredSemesterViewModels = SemesterViewModels;

                if (SelectedYear != _allString)
                {
                    filteredSemesterViewModels = filteredSemesterViewModels.Where(vm => vm.Year == SelectedYear);
                }

                if (SelectedSpecialty != _allString)
                {
                    filteredSemesterViewModels = filteredSemesterViewModels.Where(vm => vm.Specialty == SelectedSpecialty);
                }

                if (SelectedSemester != _allString)
                {
                    filteredSemesterViewModels = filteredSemesterViewModels.Where(vm => vm.Semester.ToString() == SelectedSemester);
                }

                if (!string.IsNullOrEmpty(Filter))
                {
                    filteredSemesterViewModels = filteredSemesterViewModels.Where(vm => vm.Name.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                FilteredSemesterViewModels = filteredSemesterViewModels.ToList();
            }
            else
            {
                FilteredSemesterViewModels = null;
            }

            OnPropertyChanged(nameof(FilteredSemesterViewModels));
        }
    }

    class AddSemesterPageViewModelValidator : AbstractValidator<AddSemesterPageViewModel> { }
}
