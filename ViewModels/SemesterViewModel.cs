using FluentValidation;
using Prism.Events;
using UnitPlanGenerator.Events;
using UnitPlanGenerator.Models;

namespace UnitPlanGenerator.ViewModels
{
    public class SemesterViewModel : BaseViewModel
    {
        private readonly IEventAggregator _eventAggregator;

        public string Year { get; set; }

        public string Specialty { get; set; }

        public int Semester { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public bool IsSelected { get; set; }

        private void OnIsSelectedChanged()
        {
            _eventAggregator.GetEvent<SemesterSelectionChangedEvent>().Publish(SemesterId);
        }

        public int SemesterId { get; }

        public SemesterViewModel(IEventAggregator eventAggregator, Semester semester)
        {
            _eventAggregator = eventAggregator;

            Year = string.Format("20{0}", semester.Curriculum.Year);
            Specialty = semester.Curriculum.Specialty;
            Semester = semester.Number;
            Code = semester.Course.Code;
            Name = semester.Course.Name;
            SemesterId = semester.Id;
        }
    }

    class SemesterViewModelValidator : AbstractValidator<SemesterViewModel> { }
}
