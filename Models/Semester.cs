using System.Collections.Generic;
using UnitPlanGenerator.Models.Interfaces;

namespace UnitPlanGenerator.Models
{
    public class Semester : IBaseModel
    {
        public int Id { get; set; }

        public byte Number { get; set; }

        public virtual Hours Hours { get; set; }

        public virtual Course Course { get; set; }

        public virtual Curriculum Curriculum { get; set; }

        public virtual User User { get; set; }

        public virtual ICollection<SubjectSet> SubjectSets { get; set; }
    }
}
