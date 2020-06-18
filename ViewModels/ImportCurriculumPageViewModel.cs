using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Prism.Commands;
using Prism.Events;
using PropertyChanged;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnitPlanGenerator.Common;
using UnitPlanGenerator.Events;
using UnitPlanGenerator.Extensions;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.ViewModels
{
    public class ImportCurriculumPageViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly ICurriculumImportService _curriculumImportService;
        private readonly IEventAggregator _eventAggregator;

        public NotifyTask InitTask { get; set; }

        public NotifyTask ImportTask { get; set; }

        public NotifyTask SaveTask { get; set; }

        public List<Curriculum> Curricula { get; set; }

        private List<CourseSet> CourseSets { get; set; }

        public Curriculum Curriculum { get; set; }

        public void OnCurriculumChanged()
        {
            if (Curriculum != null)
            {
                CourseSetViewModels = CourseSets.Select(set => new CourseSetViewModel(Curriculum, set))
                                                .Where(vm => vm.Total != 0 || vm.Training != 0)
                                                .ToList();
            }
            else
            {
                CourseSetViewModels = null;
            }
        }

        public string FilePath { get; set; }

        public void OnFilePathChanged()
        {
            if (FilePath != null && File.Exists(FilePath))
            {
                Message = null;
                ImportTask = NotifyTask.Crate(Task.Run(ImportAsync));
                SaveCommand.RaiseCanExecuteChanged();
            }
            else
            {
                Curriculum = null;
            }
        }

        public string Message { get; set; }

        public ICollection<string> Specialties { get; private set; }

        [DependsOn(nameof(SelectedYear), nameof(EditedYear))]
        public string SelectedSpecialty { get; set; }

        public void OnSelectedSpecialtyChanged()
        {
            if (!string.IsNullOrEmpty(SelectedSpecialty) && Curriculum != null)
            {
                Curriculum.Specialty = SelectedSpecialty;
            }
        }

        public ICollection<byte> Years { get; private set; }

        [DependsOn(nameof(SelectedSpecialty), nameof(EditedYear))]
        public byte? SelectedYear { get; set; }

        public void OnSelectedYearChanged()
        {
            if (Curriculum != null)
            {
                Curriculum.Year = SelectedYear ?? 0;
            }
        }

        [DependsOn(nameof(SelectedSpecialty), nameof(SelectedYear))]
        public string EditedYear { get; set; }

        public void OnEditedYearChanged()
        {
            byte year;
            if (byte.TryParse(EditedYear, out year))
                SelectedYear = year;
            else
                SelectedYear = null;
        }

        public bool CurriculumIsUnique
        {
            get
            {
                if (SelectedSpecialty != null && SelectedYear != null)
                {
                    return !Curricula.Any(c => c.Year == SelectedYear &&
                                               c.Specialty == SelectedSpecialty);
                }
                return false;
            }
        }

        public List<CourseSetViewModel> CourseSetViewModels { get; set; }

        public DelegateCommand SaveCommand { get; set; }

        public ImportCurriculumPageViewModel(IDatabaseService databaseService,
                                             ICurriculumImportService curriculumImportService,
                                             IEventAggregator eventAggregator)
        {
            _databaseService = databaseService;
            _curriculumImportService = curriculumImportService;
            _eventAggregator = eventAggregator;

            InitTask = NotifyTask.Crate(Task.Run(InitAsync));
            ImportTask = NotifyTask.Crate(Task.CompletedTask);
            SaveTask = NotifyTask.Crate(Task.CompletedTask);

            SaveCommand = new DelegateCommand(Save, CanSave)
                .ObservesProperty(() => Curriculum)
                .ObservesProperty(() => SelectedSpecialty)
                .ObservesProperty(() => SelectedYear);
        }

        private async Task InitAsync()
        {
            var context = _databaseService.Context;

            Curricula = await context.Curricula.ToListAsync();
            CourseSets = await context.CourseSets.ToListAsync();

            Specialties = Curricula.Select(c => c.Specialty).Distinct().OrderBy(s => s).ToList();
            Years = Curricula.Select(c => c.Year).Distinct().OrderBy(y => y).ToList();
        }

        private async Task ImportAsync()
        {
            Curriculum = await _curriculumImportService.ImportAsync(FilePath);
            if (!_curriculumImportService.IoError && Curriculum != null)
            {
                OnSelectedYearChanged();
                OnSelectedSpecialtyChanged();
            }
            else
            {
                FilePath = null;
                Message = "Файл уже используется.";
            }
        }

        private void Save()
        {
            SaveTask = NotifyTask.Crate(Task.Run(SaveAsync));
            SaveCommand.RaiseCanExecuteChanged();
        }

        private async Task SaveAsync()
        {
            await _databaseService.Context.UpsertAsync(Curriculum);
            _eventAggregator.GetEvent<CurriculumCollectionChangedEvent>().Publish(Curriculum.Id);
            FilePath = null;
            EditedYear = null;
            SelectedSpecialty = null;
        }

        private bool CanSave()
        {
            return InitTask.IsCompleted &&
                   ImportTask.IsCompleted &&
                   SaveTask.IsCompleted &&
                   Curriculum != null &&
                   CurriculumIsUnique;
        }
    }

    class ImportCurriculumPageViewModelValidator : AbstractValidator<ImportCurriculumPageViewModel> {}
}
