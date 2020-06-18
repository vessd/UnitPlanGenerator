using System.Collections.Generic;
using UnitPlanGenerator.Models.Interfaces;

namespace UnitPlanGenerator.Models
{
    public enum CourseType : byte
    {
        Сourse,
        AcademicTraining,
        PracticalTraining
    }

    public class Course : IBaseModel
    {
        public int Id { get; set; }

        public byte Number { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public CourseType Type { get; set; }

        public virtual CourseSet CourseSet { get; set; }

        public virtual Course ParentCourse { get; set; }

        public virtual ICollection<Course> ChildCourses { get; set; }

        public virtual ICollection<Semester> Semesters { get; set; }
    }
}
