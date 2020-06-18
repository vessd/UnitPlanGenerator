using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;
using System.Windows.Documents;

namespace UnitPlanGenerator.Models
{
    public class UnitPlanContext : DbContext
    {
        public UnitPlanContext(DbContextOptions<UnitPlanContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }

        public DbSet<CourseSet> CourseSets { get; set; }

        public DbSet<Curriculum> Curricula { get; set; }

        public DbSet<Hours> Hours { get; set; }

        public DbSet<Semester> Semesters { get; set; }

        public DbSet<Subject> Subjects { get; set; }

        public DbSet<SubjectSet> SubjectSets { get; set; }

        public DbSet<Title> Titles { get; set; }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new CourseConfiguration());
            modelBuilder.ApplyConfiguration(new CourseSetConfiguration());
            modelBuilder.ApplyConfiguration(new CurriculumConfiguration());
            modelBuilder.ApplyConfiguration(new HoursConfiguration());
            modelBuilder.ApplyConfiguration(new SemesterConfiguration());
            modelBuilder.ApplyConfiguration(new SubjectConfiguration());
            modelBuilder.ApplyConfiguration(new SubjectSetConfiguration());
            modelBuilder.ApplyConfiguration(new TitleConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
        }
    }

    class CourseConfiguration : IEntityTypeConfiguration<Course>
    {
        public void Configure(EntityTypeBuilder<Course> builder)
        {
            builder.Property(course => course.Number).IsRequired();
            builder.Property(course => course.Code).IsRequired();
            builder.Property(course => course.Name).IsRequired();
            builder.Property(course => course.Type).IsRequired();
            builder.HasOne(course => course.CourseSet)
                   .WithMany(courseSet => courseSet.Courses)
                   .IsRequired();
        }
    }

    class CourseSetConfiguration : IEntityTypeConfiguration<CourseSet>
    {
        public void Configure(EntityTypeBuilder<CourseSet> builder)
        {
            builder.Property(courseSet => courseSet.Code).HasMaxLength(8).IsRequired();
            builder.HasIndex(courseSet => courseSet.Code).IsUnique();
            builder.Property(courseSet => courseSet.Name).IsRequired();
        }
    }

    class CurriculumConfiguration : IEntityTypeConfiguration<Curriculum>
    {
        public void Configure(EntityTypeBuilder<Curriculum> builder)
        {
            builder.Property(curriculum => curriculum.Year).IsRequired();
            builder.Property(curriculum => curriculum.Specialty).IsRequired();
            builder.HasIndex(curriculum => new
            {
                curriculum.Year,
                curriculum.Specialty,
            })
            .HasName("IX_Curriculum")
            .IsUnique();
        }
    }

    class HoursConfiguration : IEntityTypeConfiguration<Hours>
    {
        public void Configure(EntityTypeBuilder<Hours> builder)
        {
            builder.Property(hours => hours.Lecture).IsRequired();
            builder.Property(hours => hours.Laboratory).IsRequired();
            builder.Property(hours => hours.Independent).IsRequired();
            builder.Property(hours => hours.CourseProject).IsRequired();
            builder.Property(hours => hours.Training).IsRequired();
            builder.HasIndex(hours => new
            {
                hours.Lecture,
                hours.Laboratory,
                hours.Independent,
                hours.CourseProject,
                hours.Training,
            })
            .HasName("IX_Hours")
            .IsUnique();
        }
    }

    class SemesterConfiguration : IEntityTypeConfiguration<Semester>
    {
        public void Configure(EntityTypeBuilder<Semester> builder)
        {
            builder.Property(semester => semester.Number).IsRequired();
            builder.HasOne(semester => semester.Hours)
                   .WithMany(hours => hours.Semesters)
                   .IsRequired();
            builder.HasOne(semester => semester.Course)
                   .WithMany(course => course.Semesters)
                   .IsRequired();
            builder.HasOne(semester => semester.Curriculum)
                   .WithMany(curriculum => curriculum.Semesters)
                   .IsRequired();
        }
    }

    class SubjectConfiguration : IEntityTypeConfiguration<Subject>
    {
        public void Configure(EntityTypeBuilder<Subject> builder)
        {
            builder.Property(subject => subject.Number).IsRequired();
            builder.Property(subject => subject.Hours).IsRequired();
            builder.Property(subject => subject.Type).IsRequired();
            builder.HasOne(subject => subject.Title)
                   .WithMany(title => title.Subjects)
                   .IsRequired();
            builder.HasOne(subject => subject.SubjectSet)
                   .WithMany(subjectSet => subjectSet.Subjects)
                   .IsRequired();
        }
    }

    class SubjectSetConfiguration : IEntityTypeConfiguration<SubjectSet>
    {
        public void Configure(EntityTypeBuilder<SubjectSet> builder)
        {
            builder.Property(subjectSet => subjectSet.Number).IsRequired();
            builder.HasOne(subjectSet => subjectSet.Title)
                   .WithMany(title => title.SubjectSets)
                   .IsRequired();
            builder.HasOne(subjectSet => subjectSet.Semester)
                   .WithMany(semester => semester.SubjectSets)
                   .IsRequired();
        }
    }

    class TitleConfiguration : IEntityTypeConfiguration<Title>
    {
        public void Configure(EntityTypeBuilder<Title> builder)
        {
            builder.Property(title => title.Value).IsRequired();
        }
    }

    class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(user => user.UserName).HasMaxLength(64).IsRequired();
            builder.HasIndex(user => user.UserName).IsUnique();
            builder.Property(user => user.PasswordHash).HasMaxLength(48).IsFixedLength();

            var users = new List<User>
            {
                new User
                {
                    Id = 1,
                    UserName = "admin",
                    PasswordHash = null,
                    Role = Role.Administrator,
                    DisplayName = "Admin",
                },
                new User
                {
                    Id = 2,
                    UserName = "user1",
                    PasswordHash = null,
                    Role = Role.CurriculumDeveloper,
                    DisplayName = "Методист 1",
                },
                new User
                {
                    Id = 3,
                    UserName = "user2",
                    PasswordHash = null,
                    Role = Role.Lecturer,
                    DisplayName = "Преподаватель 1",
                },
            };

            builder.HasData(users);
        }
    }
}
