using FluentValidation;
using Org.BouncyCastle.Bcpg;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using UnitPlanGenerator.Extensions;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.ViewModels
{
    public class SubjectViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;

        private Task _saveTask;
        private CancellationTokenSource _cancellationTokenSource;

        public Subject Subject { get; }

        public SubjectType SubjectType { get; set; }

        private void OnSubjectTypeChanged()
        {
            Debug.WriteLine("SubjectType changed");
        }

        public string Title { get; set; }

        public short? Hours { get; set; }

        public SubjectViewModel(IDatabaseService databaseService, Subject subject)
        {
            _databaseService = databaseService;
            Subject = subject;

            SubjectType = Subject.Type;
            Title = Subject.Title.Value;
            Hours = Subject.Hours;

            _validationTemplate.PropertyChanged += ValidationTemplate_PropertyChanged;
        }

        private void ValidationTemplate_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ValidationTemplate.HasErrors))
            {
                Debug.WriteLine("HasErrors changed");
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

                    _saveTask = Task.Run(() => SaveAsync(SubjectType, Title, (short)Hours));
                }
            }
        }

        private async Task SaveAsync(SubjectType type, string title, short hours)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), _cancellationTokenSource.Token);
            
            if (Subject.Type != type || Subject.Title.Value != title || Subject.Hours != hours)
            {
                Subject.Type = type;
                Subject.Title.Value = title;
                Subject.Hours = hours;

                await _databaseService.Context.UpsertAsync(Subject.Title);
                await _databaseService.Context.UpsertAsync(Subject);
            }
        }
    }

    class SubjectViewModelValidator : AbstractValidator<SubjectViewModel>
    {
        public SubjectViewModelValidator()
        {
            RuleFor(vm => vm)
                .Must(vm => vm.Title != null)
                .Must(vm => vm.Hours != null)
                .OverridePropertyName(nameof(SubjectViewModel));

            RuleFor(vm => vm.Title)
                .NotEmpty()
                .WithMessage("Тема занятия не может быть пустой.")
                .Unless(vm => vm.Title == null);

            RuleFor(vm => vm.Hours)
                .Must(hours => hours > 0)
                .WithMessage("Количество часов должно быть больше 0.");
        }
    }
}