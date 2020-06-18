using System.Linq;
using UnitPlanGenerator.Models;

namespace UnitPlanGenerator.ViewModels
{
    public class CourseSetViewModel
    {
        public string Name { get; set; }

        public int Total => Class + Independent;

        public int Class { get; set; }

        public int Independent { get; set; }

        public int Training { get; set; }

        public CourseSetViewModel(Curriculum curriculum, CourseSet set)
        {
            var semesters = curriculum.Semesters.Where(s => s.Course.CourseSet.Id == set.Id).ToList();
            if (set != null)
            {
                Name = set.Name;

                var hours = semesters.Select(s => s.Hours).ToList();
                Class = hours.Sum(h => h.Class);
                Independent = hours.Sum(h => h.Independent);
                Training = hours.Sum(h => h.Training);
            }
        }
    }
}
