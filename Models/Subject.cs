using UnitPlanGenerator.Models.Interfaces;

namespace UnitPlanGenerator.Models
{
    public enum SubjectType : byte
    {
        Elective,
        Lecture,
        Practice,
        Laboratory,
        Independent,
        CourseProject,
        AcademicTraining,
        PracticalTraining,
        InterimAssessment,
        Consultation
    }

    public class Subject : IBaseModel
    {
        public int Id { get; set; }

        public short Number { get; set; }

        public short Hours { get; set; }

        public SubjectType Type { get; set; }

        public virtual Title Title { get; set; }

        public virtual SubjectSet SubjectSet { get; set; }
    }
}
