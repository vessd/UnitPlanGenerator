using System.Collections.Generic;
using UnitPlanGenerator.Models.Interfaces;

namespace UnitPlanGenerator.Models
{
    public enum Role : byte
    {
        Administrator,
        CurriculumDeveloper,
        Lecturer
    }

    public class User : IBaseModel
    {
        public int Id { get; set; }

        public string UserName { get; set; }

        public string PasswordHash { get; set; }

        public Role Role { get; set; }

        public string DisplayName { get; set; }

        public virtual ICollection<Semester> Semesters { get; set; }
    }
}
