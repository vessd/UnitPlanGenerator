using System.Collections.Generic;
using UnitPlanGenerator.Models.Interfaces;

namespace UnitPlanGenerator.Models
{
    public class Title : IBaseModel
    {
        public int Id { get; set; }

        public string Value { get; set; }

        public virtual ICollection<Subject> Subjects { get; set; }

        public virtual ICollection<SubjectSet> SubjectSets { get; set; }
    }
}
