using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnitPlanGenerator.Common;
using UnitPlanGenerator.Extensions;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.ViewModels
{
    public class SubjectSetViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        
        private Task _saveTask;
        private CancellationTokenSource _cancellationTokenSource;

        public SubjectSet SubjectSet { get; }

        public NotifyTask InitTask { get; private set; }

        public string Title { get; set; }

        public ObservableCollection<SubjectViewModel> SubjectViewModels { get; set; }

        public SubjectSetViewModel(IDatabaseService databaseService, SubjectSet subjectSet)
        {
            _databaseService = databaseService;
            SubjectSet = subjectSet;

            Title = SubjectSet.Title.Value;

            InitTask = NotifyTask.Crate(Task.Run(InitAsync));

            _validationTemplate.PropertyChanged += ValidationTemplate_PropertyChanged;
        }

        private async Task InitAsync()
        {
            var subjects = await _databaseService.Context
                                                 .Subjects
                                                 .Where(subject => subject.SubjectSet.Id == SubjectSet.Id)
                                                 .Include(subject => subject.Title)
                                                 .AsNoTracking()
                                                 .ToListAsync();

            SubjectViewModels = new ObservableCollection<SubjectViewModel>(subjects.Select(subject => new SubjectViewModel(_databaseService, subject)));
        }

        private void ValidationTemplate_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ValidationTemplate.HasErrors))
            {
                if (!_validationTemplate.HasErrors)
                {
                    if (_saveTask != null && !_saveTask.IsCompleted)
                    {
                        _cancellationTokenSource.Cancel();
                        _cancellationTokenSource.Dispose();
                        _cancellationTokenSource = null;
                    }

                    if (_cancellationTokenSource == null)
                    {
                        _cancellationTokenSource = new CancellationTokenSource();
                    }

                    _saveTask = Task.Run(() => SaveAsync(Title));
                }
            }
        }

        private async Task SaveAsync(string title)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), _cancellationTokenSource.Token);
            if (SubjectSet.Title.Value != title)
            {
                SubjectSet.Title.Value = title;
                SubjectSet.Title = await _databaseService.Context.UpsertAsync(SubjectSet.Title);
                await _databaseService.Context.UpsertAsync(SubjectSet);
            }
        }

        public void AddSubject(SubjectType type = SubjectType.Lecture, short hours = 2)
        {
            var subject = new Subject
            {
                Number = (short)(SubjectViewModels.Count + 1),
                Hours = hours,
                Type = type,
                SubjectSet = SubjectSet,
                Title = new Title(),
            };

            SubjectViewModels.Add(new SubjectViewModel(_databaseService, subject));
        }
    }

    class SubjectSetViewModelValidator : AbstractValidator<SubjectSetViewModel>
    {
        public SubjectSetViewModelValidator()
        {
            RuleFor(vm => vm)
                .Must(vm => vm.Title != null)
                .OverridePropertyName(nameof(SubjectSetViewModel));

            RuleFor(vm => vm.Title)
                .NotEmpty()
                .WithMessage("Название раздела не может быть пустым.")
                .Unless(vm => vm.Title == null);
        }
    }
}