using System.Collections.Generic;
using UnitPlanGenerator.Models.Interfaces;

namespace UnitPlanGenerator.Models
{
    public class Hours : IBaseModel
    {
        public int Id { get; set; }

        public short Lecture { get; set; }

        public short Laboratory { get; set; }

        public short Independent { get; set; }

        public short CourseProject { get; set; }

        public short Training { get; set; }

        public virtual ICollection<Semester> Semesters { get; set; }

        public short Class => (short)(Lecture + Laboratory + CourseProject);

        public short Total => (short)(Class + Training + Independent);
    }
}
