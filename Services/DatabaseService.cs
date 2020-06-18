using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.Services
{
    public class DatabaseService : IDatabaseService
    {
        private DbContextOptions<UnitPlanContext> _dbOptions;

        public UnitPlanContext Context
        {
            get
            {
                if (_dbOptions != null)
                {
                    return new UnitPlanContext(_dbOptions);
                }

                return null;
            }
        }

        public void SetOptions(DbContextOptions<UnitPlanContext> dbOptions)
        {
            if (dbOptions != null)
            {
                _dbOptions = dbOptions;
                using (var db = new UnitPlanContext(_dbOptions))
                {
                    if (db.Database.EnsureCreated())
                    {
                        var defaultCourseSets = new List<CourseSet>
                        {
                            new CourseSet
                            {
                                Id = 1,
                                Code = "ОУД",
                                Name = "Общеобразовательные учебные дисциплины",
                            },
                            new CourseSet
                            {
                                Id = 2,
                                Code = "ОГСЭ",
                                Name = "Общий гуманитарный и социально-экономический цикл",
                            },
                            new CourseSet
                            {
                                Id = 3,
                                Code = "ЕН",
                                Name = "Математический и общий естественнонаучный цикл",
                            },
                            new CourseSet
                            {
                                Id = 4,
                                Code = "П",
                                Name = "Профессиональный цикл",
                            },
                            new CourseSet
                            {
                                Id = 5,
                                Code = "ОП",
                                Name = "Общепрофессиональные дисциплины",
                            },
                            new CourseSet
                            {
                                Id = 6,
                                Code = "ПМ",
                                Name = "Профессиональные модули",
                            },
                            new CourseSet
                            {
                                Id = 7,
                                Code = "ФК",
                                Name = "Физическая культура",
                            },
                            new CourseSet
                            {
                                Id = 8,
                                Code = "ОУП",
                                Name = "Общеобразовательные учебные предметы",
                            },
                        };
                        defaultCourseSets[4].ParentCourseSet = defaultCourseSets[3];
                        defaultCourseSets[5].ParentCourseSet = defaultCourseSets[3];

                        db.CourseSets.AddRange(defaultCourseSets);

                        db.SaveChanges();
                    }
                }
            }
        }
    }
}
