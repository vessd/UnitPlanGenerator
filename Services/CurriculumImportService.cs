using Microsoft.EntityFrameworkCore;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services.Interfaces;

namespace UnitPlanGenerator.Services
{
    public class CurriculumImportService : ICurriculumImportService
    {
        private readonly IDatabaseService _databaseService;

        private Curriculum _curriculum;
        private List<CourseSet> _courseSets;
        private List<Course> _courses;
        private List<Hours> _hours;
        private List<string> _codes;
        private Stack<Course> _parents;

        public bool IoError { get; private set; }

        public CurriculumImportService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        private CellRangeAddress GetMergedRegion(ICell cell)
        {
            var sheet = cell.Row.Sheet;
            for (int i = 0; i < sheet.NumMergedRegions; i++)
            {
                if (sheet.GetMergedRegion(i).IsInRange(cell.RowIndex, cell.ColumnIndex))
                {
                    return sheet.GetMergedRegion(i);
                }
            }

            return new CellRangeAddress(cell.RowIndex, cell.RowIndex, cell.ColumnIndex, cell.ColumnIndex);
        }

        private List<CellRangeAddress> GetSemesterCells(ISheet sheet)
        {
            var list = new List<CellRangeAddress>();
            for (int i = sheet.FirstRowNum; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                foreach (var cell in row.Cells)
                {
                    if (cell.ToString() == "Распределение по семестрам")
                    {
                        row = sheet.GetRow(i + 2);
                        foreach (var semCell in row.Cells)
                        {
                            if (Regex.IsMatch(semCell.ToString(), @"^\d\s?сем"))
                            {
                                list.Add(GetMergedRegion(semCell));
                            }
                        }
                        return list;
                    }
                }
            }

            return null;
        }

        private short GetNumberOfHours(IRow row, CellRangeAddress range, int offset)
        {
            if (range.FirstColumn + offset <= range.LastColumn)
                return (short)(row.GetCell(range.FirstColumn + offset)?.NumericCellValue ?? 0);
            return 0;
        }

        private void GetSemesters(IRow row, List<CellRangeAddress> semesterRanges, Course course)
        {
            foreach (var range in semesterRanges)
            {
                var number = row.Sheet.GetRow(range.FirstRow).GetCell(range.FirstColumn).StringCellValue;

                short independent = GetNumberOfHours(row, range, 1);
                short lecture = GetNumberOfHours(row, range, 3);
                short laboratory = GetNumberOfHours(row, range, 4);
                short courseProject = GetNumberOfHours(row, range, 5);
                short training = 0;

                // часы практики могут быть в первой клетке семестра
                if (lecture + laboratory + courseProject == 0)
                    training = GetNumberOfHours(row, range, 0);

                if (lecture + laboratory + courseProject + training != 0)
                {
                    Func<Hours, bool> filter = (h => h.Lecture == lecture
                                                  && h.Laboratory == laboratory
                                                  && h.Independent == independent
                                                  && h.CourseProject == courseProject
                                                  && h.Training == training);
                    var hours = _hours.FirstOrDefault(filter);
                    if (hours == null)
                    {
                        hours = new Hours
                        {
                            Lecture = lecture,
                            Laboratory = laboratory,
                            Independent = independent,
                            CourseProject = courseProject,
                            Training = training,
                        };
                        _hours.Add(hours);
                    }

                    var semester = new Semester
                    {
                        Number = byte.Parse(number.Substring(0, 1)),
                        Hours = hours,
                        Course = course,
                        Curriculum = _curriculum,
                    };
                    _curriculum.Semesters.Add(semester);
                    course.Semesters.Add(semester);
                }
            }
        }

        private int CourseLevel(string courseCode)
        {
            return courseCode.Split('.').Count(s => s.All(char.IsDigit));
        }

        private Course GetCourse(IRow row, List<CellRangeAddress> semesterRanges)
        {
            var code = row.GetCell(row.FirstCellNum).StringCellValue;
            var name = row.GetCell(row.FirstCellNum + 1).StringCellValue;
            var number = byte.Parse(code.Substring(code.LastIndexOf('.') + 1));

            var parent = _parents.Count > 0 ? _parents.Peek() : null;

            if (parent != null)
            {
                if (CourseLevel(code) == CourseLevel(parent.Code))
                {
                    _parents.Pop();
                    parent = _parents.Count > 0 ? _parents.Peek() : null;
                }
                else if (CourseLevel(code) < CourseLevel(parent.Code))
                {
                    _parents.Pop();
                    if (_parents.Count > 0) _parents.Pop();
                    parent = _parents.Count > 0 ? _parents.Peek() : null;
                }
                else if (CourseLevel(code) - CourseLevel(parent.Code) > 1)
                {
                    parent = null;
                }
            }

            var courses = _courses.Where(c => c.Number == number && c.Name == name);
            if (parent != null)
            {
                courses = courses.Where(c => c.ParentCourse.Id == parent.Id);
            }
            var course = courses.FirstOrDefault(c => c.Code == code);

            if (course == null)
            {
                var codeStartsWith = code.Substring(0, code.IndexOf('.'));
                var set = _courseSets.FirstOrDefault(s => s.Code == codeStartsWith) ?? _courseSets.First(s => s.Code == "ПМ");

                if (parent != null && parent.CourseSet != set)
                {
                    parent = null;
                }

                course = new Course
                {
                    Number = number,
                    Code = code,
                    Name = name,
                    CourseSet = set,
                    ParentCourse = parent,
                    Semesters = new List<Semester>(),
                };

                if (codeStartsWith == "УП")
                    course.Type = CourseType.AcademicTraining;
                else if (codeStartsWith == "ПП")
                    course.Type = CourseType.PracticalTraining;
                else
                    course.Type = CourseType.Сourse;

                _courses.Add(course);
            }

            if (parent?.Semesters != null && parent.Semesters.Count > 0)
            {
                foreach (var semester in parent.Semesters)
                {
                    _curriculum.Semesters.Remove(semester);
                }
                parent.Semesters.Clear();
            }

            GetSemesters(row, semesterRanges, course);
            return course;
        }

        private void GetCourseList(ISheet sheet)
        {
            var semesterRanges = GetSemesterCells(sheet);

            if (semesterRanges != null)
            {
                for (int i = sheet.FirstRowNum; i < sheet.LastRowNum; i++)
                {
                    var row = sheet.GetRow(i);
                    if (row == null || row.Cells.Count == 0) continue;

                    var cell = row.GetCell(row.FirstCellNum).ToString();
                    if (Regex.IsMatch(cell, @"\.\d\d$")
                        && !cell.Contains("00")
                        && (_codes.Any(code => cell.StartsWith(code))
                            || cell.All(c => char.IsDigit(c) || c == '.')))
                    {
                        _parents.Push(GetCourse(row, semesterRanges));
                    }
                }
            }
        }

        private async Task InitAsync(UnitPlanContext context)
        {
            _curriculum = null;
            IoError = false;
            _parents = new Stack<Course>();
            _courseSets = await context.CourseSets.ToListAsync();
            _courses = await context.Courses.ToListAsync();
            _hours = await context.Hours.ToListAsync();

            _codes = _courseSets.Select(s => s.Code).ToList();
            _codes.Add("МДК");
            _codes.Add("УП");
            _codes.Add("ПП");
        }

        private void Clear()
        {
            _parents = null;
            _courseSets = null;
            _courses = null;
            _hours = null;
            _codes = null;
        }

        public async Task<Curriculum> ImportAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)
                || Path.GetExtension(fileName) != ".xls"
                || !File.Exists(fileName))
            {
                throw new ArgumentException("Недопустимое имя файла");
            }

            var context = _databaseService.Context;

            await InitAsync(context);

            try
            {
                IWorkbook workbook;
                using (Stream stream = new FileStream(fileName, FileMode.Open))
                {
                    workbook = new HSSFWorkbook(stream);
                }

                _curriculum = new Curriculum()
                {
                    Semesters = new List<Semester>(),
                };
                context.Curricula.Add(_curriculum);

                for (int i = 0; i < workbook.NumberOfSheets; i++)
                {
                    GetCourseList(workbook.GetSheetAt(i));
                }
            }
            catch (IOException)
            {
                _curriculum = null;
                IoError = true;
            }

            Clear();

            if (_curriculum != null)
            {
                context.Entry(_curriculum).State = EntityState.Detached;
            }

            return _curriculum;
        }
    }
}
