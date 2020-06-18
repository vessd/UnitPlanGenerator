using System.Collections.Generic;
using UnitPlanGenerator.Models.Interfaces;

namespace UnitPlanGenerator.Models
{
    public class CourseSet : IBaseModel
    {
        public int Id { get; set; }

        public string Code { get; set; }
        
        public string Name { get; set; }

        public virtual CourseSet ParentCourseSet { get; set; }

        public virtual ICollection<CourseSet> ChildCourseSets { get; set; }

        public virtual ICollection<Course> Courses { get; set; }
    }
}
