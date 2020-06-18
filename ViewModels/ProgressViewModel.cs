using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.ViewModels
{
    public class ProgressViewModel : BaseViewModel
    {
        private readonly bool asPercent = false;

        public string Title { get; set; }

        private int _progress;

        public int Progress
        {
            get
            {
                if (asPercent && _total != 0)
                    return (int)((decimal)_progress / _total * 100);

                return _progress;
            }
            set
            {
                _progress = value;
            }
        }

        private int _total;

        public int Total
        {
            get
            {
                if (asPercent)
                    return 100;

                return _total;
            }
            set
            {
                _total = value;
            }
        }

        public bool Overflow
        {
            get
            {
                if (_total == 0) return true;
                if (decimal.Compare((decimal)_progress / _total, 1) > 0) return true;
                if (ChildProgressViewModels != null) return ChildProgressViewModels.Any(c => c.Overflow);
                return false;
            }
        }

        public string ProgressString
        {
            get
            {
                if (asPercent)
                    return string.Format("{0}%", Progress);

                return string.Format("{0}/{1}", Progress, Total);
            }
        }

        public Curriculum Curriculum { get; }

        public Course Course { get; }

        public Semester Semester { get; }

        public List<int> SemesterIds { get; }

        public ObservableCollection<ProgressViewModel> ChildProgressViewModels { get; set; }

        //public ObservableCollection<ProgressViewModel> SemesterProgressViewModels { get; set; }

        public ProgressViewModel()
        {
            
        }

        public async Task InitAsync(IDatabaseService databaseService, User user)
        {
            Progress = 0;
            Total = 0;

            var context = databaseService.Context;

            if (user.Role == Role.CurriculumDeveloper)
            {
                Title = "Учебные планы";
                var courseSets = await context.CourseSets.Where(set => set.ParentCourseSet == null).ToListAsync();
                var curricula = await context.Curricula.ToListAsync();
                ChildProgressViewModels = new ObservableCollection<ProgressViewModel>(curricula
                    .GroupBy(c => c.Year)
                    .Select(g => new ProgressViewModel(courseSets, g.Key, g.AsEnumerable())));
            }
            else if (user.Role == Role.Lecturer)
            {
                Title = "Предметы";
                var courses = await context.Courses.Where(c => c.Semesters.Any(s => s.User.Id == user.Id))
                                                   .OrderBy(c => c.Number)
                                                   .OrderBy(c => c.Type)
                                                   .ToListAsync();
                ChildProgressViewModels = new ObservableCollection<ProgressViewModel>(courses
                    .Select(c => new ProgressViewModel(c, user)));
            }
        }

        private ProgressViewModel(IEnumerable<CourseSet> courseSets, byte year, IEnumerable<Curriculum> curriculums)
        {
            asPercent = true;

            ChildProgressViewModels = new ObservableCollection<ProgressViewModel>
            (
                curriculums
                .OrderBy(c => c.Specialty)
                .Select(c => new ProgressViewModel(courseSets, c))
            );

            Title = string.Format("20{0}", year);
            Progress = ChildProgressViewModels.Sum(pv => pv._progress);
            Total = ChildProgressViewModels.Sum(pv => pv._total);
        }

        private ProgressViewModel(IEnumerable<CourseSet> courseSets, Curriculum curriculum)
        {
            asPercent = true;

            ChildProgressViewModels = new ObservableCollection<ProgressViewModel>
            (
                courseSets
                .Select(s => new ProgressViewModel(s, curriculum))
                .Where(pv => pv._total > 0)
            );

            Title = curriculum.Specialty;
            Progress = ChildProgressViewModels.Sum(pv => pv._progress);
            Total = ChildProgressViewModels.Sum(pv => pv._total);
        }

        private ProgressViewModel(CourseSet set, Curriculum curriculum)
        {
            if (set.ChildCourseSets != null && set.ChildCourseSets.Count != 0)
            {
                ChildProgressViewModels = new ObservableCollection<ProgressViewModel>
                (
                    set
                    .ChildCourseSets
                    .Select(s => new ProgressViewModel(s, curriculum))
                );
            }
            else if (set.Courses != null && set.Courses.Count != 0)
            {
                ChildProgressViewModels = new ObservableCollection<ProgressViewModel>
                (
                    set
                    .Courses
                    .Where(c => c.ParentCourse == null)
                    .OrderBy(c => c.Number)
                    .OrderBy(c => c.Type)
                    .Select(c => new ProgressViewModel(c, curriculum))
                    .Where(pv => pv._total != 0)
                );
            }

            Title = set.Name;
            if (ChildProgressViewModels != null)
            {
                Progress = ChildProgressViewModels.Sum(pv => pv._progress);
                Total = ChildProgressViewModels.Sum(pv => pv._total);
            }
            else
            {
                Progress = 0;
                Total = 0;
            }
        }

        private ProgressViewModel(Course course, Curriculum curriculum)
        {
            Title = course.Name;

            if (course.ParentCourse == null)
            {
                Curriculum = curriculum;
                Course = course;
            }

            if (course.ChildCourses != null && course.ChildCourses.Count != 0)
            {
                ChildProgressViewModels = new ObservableCollection<ProgressViewModel>
                (
                    course
                    .ChildCourses
                    .OrderBy(c => c.Number)
                    .OrderBy(c => c.Type)
                    .Select(c => new ProgressViewModel(c, curriculum))
                    .Where(pv => pv._total != 0)
                );
            }
            else
            {
                //SemesterProgressViewModels
                ChildProgressViewModels = new ObservableCollection<ProgressViewModel>
                (
                    course
                    .Semesters
                    .Where(s => s.Curriculum == curriculum)
                    .OrderBy(s => s.Number)
                    .Select(s => new ProgressViewModel(s))
                );
            }

            if (ChildProgressViewModels != null)
            {
                Progress = ChildProgressViewModels.Sum(pv => pv._progress);
                Total = ChildProgressViewModels.Sum(pv => pv._total);
            }
            /*else
            {
                Progress = SemesterProgressViewModels.Sum(pv => pv._progress);
                Total = SemesterProgressViewModels.Sum(pv => pv._total);
            }*/
        }

        private ProgressViewModel(Course course, User user)
        {
            Title = course.Name;
            asPercent = true;

            ChildProgressViewModels = new ObservableCollection<ProgressViewModel>
            (
                course
                .Semesters
                .Where(s => s.User != null && s.User.Id == user.Id)
                .GroupBy(s => s.Curriculum)
                .OrderBy(g => g.Key.Specialty)
                .OrderBy(g => g.Key.Year)
                .Select(g => new ProgressViewModel(g.Key, g.AsEnumerable()))
            );

            Progress = ChildProgressViewModels.Sum(pv => pv._progress);
            Total = ChildProgressViewModels.Sum(pv => pv._total);
        }

        private ProgressViewModel(Curriculum curriculum, IEnumerable<Semester> semesters)
        {
            Title = curriculum.Specialty + "-" + curriculum.Year;

            SemesterIds = new List<int>(semesters.OrderBy(s => s.Number).Select(s => s.Id));

            // SemesterProgressViewModels
            ChildProgressViewModels = new ObservableCollection<ProgressViewModel>
            (
                semesters
                .OrderBy(s => s.Number)
                .Select(s => new ProgressViewModel(s))
            );

            // SemesterProgressViewModels
            Progress = ChildProgressViewModels.Sum(pv => pv._progress);
            Total = ChildProgressViewModels.Sum(pv => pv._total);
        }

        private ProgressViewModel(Semester semester)
        {
            Title = semester.Number + " семестер";

            Progress = semester.SubjectSets.Sum(set => set.Subjects.Sum(sub => (int?)sub.Hours)) ?? 0;
            Total = semester.Hours.Total;
            Semester = semester;
        }
    }

    class ProgressViewModelValidator : AbstractValidator<ProgressViewModel> {}
}
