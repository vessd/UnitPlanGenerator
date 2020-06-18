using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prism.Commands;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UnitPlanGenerator.Common;
using UnitPlanGenerator.Extensions;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.ViewModels
{
    public class SemesterDetailsViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly Semester _semester;

        public NotifyTask InitTask { get; private set; }

        public ObservableCollection<SubjectSetViewModel> SubjectSetViewModels { get; set; }

        public object SelectedNode { get; set; }

        public DelegateCommand AddSubjectSetCommand { get; }

        public DelegateCommand AddSubjectCommand { get; set; }

        public DelegateCommand DeleteCommand { get; set; }

        public SemesterDetailsViewModel(IDatabaseService databaseService, Semester semester)
        {
            _databaseService = databaseService;
            _semester = semester;

            InitTask = NotifyTask.Crate(Task.Run(InitAsync));

            AddSubjectSetCommand = new DelegateCommand(AddSubjectSet);
            AddSubjectCommand = new DelegateCommand(AddSubject, CanAddSubject).ObservesProperty(() => SelectedNode);
            DeleteCommand = new DelegateCommand(Delete, CanDelete).ObservesProperty(() => SelectedNode);
        }

        private async Task InitAsync()
        {
            var subjectSets = await _databaseService.Context
                                                    .SubjectSets
                                                    .Where(set => set.Semester.Id == _semester.Id)
                                                    .Include(set => set.Title)
                                                    .AsNoTracking()
                                                    .ToListAsync();
            SubjectSetViewModels = new ObservableCollection<SubjectSetViewModel>(subjectSets.Select(set => new SubjectSetViewModel(_databaseService, set)));
        }

        private void AddSubjectSet()
        {
            var subjectSet = new SubjectSet
            {
                Number = (short)(SubjectSetViewModels.Count + 1),
                Title = new Title(),
                Semester = _semester,
            };

            SubjectSetViewModels.Add(new SubjectSetViewModel(_databaseService, subjectSet));
        }

        private void AddSubject()
        {
            var subjectSetViewModel = (SubjectSetViewModel)SelectedNode;
            subjectSetViewModel.AddSubject();
        }

        private bool CanAddSubject()
        {
            return SelectedNode is SubjectSetViewModel;
        }

        private void Delete()
        {
            Task.Run(DeleteAsync);
        }

        private async Task DeleteAsync()
        {
            if (SelectedNode is SubjectSetViewModel subjectSetViewModel)
            {
                SubjectSetViewModels.RemoveOnUI(subjectSetViewModel);
                await _databaseService.Context.DeleteAsync(subjectSetViewModel.SubjectSet);
            }
            else if (SelectedNode is SubjectViewModel subjectViewModel)
            {
                foreach (var subjectSetVM in SubjectSetViewModels)
                {
                    subjectSetVM.SubjectViewModels.RemoveOnUI(subjectViewModel);
                }
                await _databaseService.Context.DeleteAsync(subjectViewModel.Subject);
            }
        }

        private bool CanDelete()
        {
            return SelectedNode != null;
        }
    }

    class SemesterDetailsViewModelValidator : AbstractValidator<SemesterDetailsViewModel> { }
}
