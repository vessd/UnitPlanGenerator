using System.Collections.Generic;
using UnitPlanGenerator.Models.Interfaces;

namespace UnitPlanGenerator.Models
{
    public class Curriculum : IBaseModel
    {
        public int Id { get; set; }

        public byte Year { get; set; }

        public string Specialty { get; set; }

        public virtual ICollection<Semester> Semesters { get; set; }
    }
}
