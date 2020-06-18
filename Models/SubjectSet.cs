using System.Collections.Generic;
using UnitPlanGenerator.Models.Interfaces;

namespace UnitPlanGenerator.Models
{
    public class SubjectSet : IBaseModel
    {
        public int Id { get; set; }

        public short Number { get; set; }

        public virtual Title Title { get; set; }

        public virtual Semester Semester { get; set; }

        public virtual ICollection<Subject> Subjects { get; set; }
    }
}
