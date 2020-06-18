using FluentValidation;
using Prism.Commands;
using Prism.Events;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnitPlanGenerator.Common;
using UnitPlanGenerator.Events;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.ViewModels
{
    public class CurriculumPageViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly IIdentityService _identityService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IDialogService _dialogService;

        private readonly UnitPlanExporter _unitPlanExporter;

        public NotifyTask InitTask { get; private set; }

        public NotifyTask ExportUnitTask { get; private set; }

        public List<ProgressViewModel> ProgressViewModels { get; set; }

        public ProgressViewModel SelectedProgressViewModel { get; set; }

        private void OnSelectedProgressViewModelChanged()
        {
            if (SelectedProgressViewModel != null && SelectedProgressViewModel.ChildProgressViewModels == null)
            {
                SemesterDetailsViewModel = new SemesterDetailsViewModel(_databaseService, SelectedProgressViewModel.Semester);
            }
            else
            {
                SemesterDetailsViewModel = null;
            }
        }

        public SemesterDetailsViewModel SemesterDetailsViewModel { get; set; }

        public bool IsExportUnitVisible
        {
            get
            {
                return SelectedProgressViewModel != null &&
                      (SelectedProgressViewModel.Curriculum != null &&
                       SelectedProgressViewModel.Course != null ||
                       SelectedProgressViewModel.SemesterIds != null);
            }
        }

        public DelegateCommand ExportUnitCommand { get; }

        public CurriculumPageViewModel(IDatabaseService databaseService,
                                       IIdentityService identityService,
                                       IEventAggregator eventAggregator,
                                       IDialogService dialogService)
        {
            _databaseService = databaseService;
            _identityService = identityService;
            _eventAggregator = eventAggregator;
            _dialogService = dialogService;

            _unitPlanExporter = new UnitPlanExporter(_databaseService);

            _identityService.CurrentUserChanged += IdentityService_CurrentUserChanged;
            _eventAggregator.GetEvent<CurriculumCollectionChangedEvent>().Subscribe(OnCurriculumCollectionChanged);
            _eventAggregator.GetEvent<SemesterCollectionChangedEvent>().Subscribe(OnCourseCollectionChanged);

            InitTask = NotifyTask.Crate(Task.Run(InitAsync));

            ExportUnitCommand = new DelegateCommand(ExportUnit, CanExportUnit)
                .ObservesProperty(() => SelectedProgressViewModel);
        }

        private async Task InitAsync()
        {
            SelectedProgressViewModel = null;
            var progressViewModel = new ProgressViewModel();
            await progressViewModel.InitAsync(_databaseService, _identityService.CurrentUser);
            ProgressViewModels =  new List<ProgressViewModel> { progressViewModel };
            SelectedProgressViewModel = progressViewModel;
        }

        private void IdentityService_CurrentUserChanged(object sender, CurrentUserChangedEventArgs e)
        {
            if (e.NewUser.Role != Role.Administrator)
            {
                if (e.NewUser.Role != Role.CurriculumDeveloper || e.PreviousUser.Role != Role.CurriculumDeveloper)
                {
                    InitTask = NotifyTask.Crate(Task.Run(InitAsync));
                }
            }
        }

        private void OnCurriculumCollectionChanged(int curriculumId)
        {
            InitTask = NotifyTask.Crate(Task.Run(InitAsync));
        }

        private void OnCourseCollectionChanged()
        {
            InitTask = NotifyTask.Crate(Task.Run(InitAsync));
        }

        private void ExportUnit()
        {
            if (_dialogService.SaveFileDialog("Книга Excel (*.xlsx)|*.xlsx") is string fileName)
            {
                Task task;
                if (SelectedProgressViewModel.SemesterIds == null)
                {
                    var curriculumId = SelectedProgressViewModel.Curriculum.Id;
                    var courseId = SelectedProgressViewModel.Course.Id;
                    task = Task.Run(() => _unitPlanExporter.ExportAsync(curriculumId, courseId, fileName));
                }
                else
                {
                    task = Task.Run(() => _unitPlanExporter.ExportAsync(SelectedProgressViewModel.SemesterIds, fileName));
                }
                task = task.ContinueWith(t => ExportUnitCommand.RaiseCanExecuteChanged());
                ExportUnitTask = NotifyTask.Crate(task);
                ExportUnitCommand.RaiseCanExecuteChanged();
            }
        }

        private bool CanExportUnit()
        {
            return (ExportUnitTask == null || ExportUnitTask.IsCompleted) && IsExportUnitVisible;
        }
    }

    class CurriculumPageViewModelValidator : AbstractValidator<CurriculumPageViewModel> {}
}
