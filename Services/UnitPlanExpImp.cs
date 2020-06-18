using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using UnitPlanGenerator.Converters;
using UnitPlanGenerator.Models;
using UnitPlanGenerator.Services.Interfaces;
using HorizontalAlignment = NPOI.SS.UserModel.HorizontalAlignment;
using VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment;

namespace UnitPlanGenerator
{
    public class UnitPlanColumn
    {
        public static readonly UnitPlanColumn Semester;
        public static readonly UnitPlanColumn SetTitle;
        public static readonly UnitPlanColumn Number;
        public static readonly UnitPlanColumn Hours;
        public static readonly UnitPlanColumn Classroom;
        public static readonly UnitPlanColumn SubjectType;
        public static readonly UnitPlanColumn SubjectTitle;
        public static readonly UnitPlanColumn TotalHours;
        public static readonly UnitPlanColumn ClassHours;
        public static readonly UnitPlanColumn ElectiveHours;
        public static readonly UnitPlanColumn LectureHours;
        public static readonly UnitPlanColumn PracticeHours;
        public static readonly UnitPlanColumn LaboratoryHours;
        public static readonly UnitPlanColumn IndependentHours;
        public static readonly UnitPlanColumn CourseProjectHours;
        public static readonly UnitPlanColumn AcademicTrainingHours;
        public static readonly UnitPlanColumn PracticalTrainingHours;
        public static readonly UnitPlanColumn InterimAssessmentHours;
        public static readonly UnitPlanColumn ConsultationHours;
        public static readonly UnitPlanColumn UserName;

        public int Index { get; set; }
        public string Header { get; set; }

        private UnitPlanColumn(int index, string header)
        {
            Index = index;
            Header = header;
            mappingIndex.Add(Index, this);
        }

        private static readonly Dictionary<int, UnitPlanColumn> mappingIndex = null;

        static UnitPlanColumn()
        {
            mappingIndex = new Dictionary<int, UnitPlanColumn>();

            Semester = new UnitPlanColumn(0, "Семестр");
            SetTitle = new UnitPlanColumn(1, "Наименование раздела (темы) учебного предмета, дисциплины, профессионального модуля, темы");
            Number = new UnitPlanColumn(2, "№ п/п");
            Hours = new UnitPlanColumn(3, "Количество часов");
            Classroom = new UnitPlanColumn(4, "№ кабинета/лаборатории");
            SubjectType = new UnitPlanColumn(5, "Виды учебной деятельности");
            SubjectTitle = new UnitPlanColumn(6, "Наименование тем");
            TotalHours = new UnitPlanColumn(7, "Общий объем в часах");
            ClassHours = new UnitPlanColumn(8, "в т.ч. обязательная часть");
            ElectiveHours = new UnitPlanColumn(9, "в т.ч. вариативная часть");
            LectureHours = new UnitPlanColumn(10, "Теория");
            PracticeHours = new UnitPlanColumn(11, "Практические занятия");
            LaboratoryHours = new UnitPlanColumn(12, "Лабораторные занятия");
            IndependentHours = new UnitPlanColumn(13, "Самостоятельная работа");
            CourseProjectHours = new UnitPlanColumn(14, "Курсовой проект");
            AcademicTrainingHours = new UnitPlanColumn(15, "Учебная практика");
            PracticalTrainingHours = new UnitPlanColumn(16, "Производственная практика");
            InterimAssessmentHours = new UnitPlanColumn(17, "Промежуточная аттестация");
            ConsultationHours = new UnitPlanColumn(18, "Консультации");
            UserName = new UnitPlanColumn(19, "Ф.И.О. преподавателя");
        }

        public static implicit operator int(UnitPlanColumn column) => column.Index;

        public static implicit operator string(UnitPlanColumn column) => column.Header;

        public static implicit operator char(UnitPlanColumn column) => (char)('A' + column.Index);

        public static implicit operator UnitPlanColumn(int index)
        {
            if (mappingIndex.ContainsKey(index))
                return mappingIndex[index];
            return null;
        }
    }

    public class UnitPlanExporter
    {
        private readonly IDatabaseService _databaseService;
        private readonly IValueConverter _subjectTypeConverter;
        private readonly string _totalHoursFormat;
        private readonly string _classHoursFormat;

        private UnitPlanContext _context;
        private Curriculum _curriculum;
        private Course _course;

        private IWorkbook _workbook;
        private ISheet _sheet;

        private IFont _plainFont;
        private IFont _boldFont;
        private ICellStyle _plainStyle;
        private ICellStyle _plainCenter;
        private ICellStyle _plainCenterYellow;
        private ICellStyle _boldCenter;
        private ICellStyle _boldCenterRotated;

        private const int _columnWidthCoefficient = 256;

        private int _currentRow;
        private int _currentNumber;
        private int _currentHours;

        public UnitPlanExporter(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            _subjectTypeConverter = new SubjectTypeConverter();

            _totalHoursFormat = string.Format("{0}{3}+{1}{3}+{2}{3}",
                                             (char)UnitPlanColumn.ClassHours,
                                             (char)UnitPlanColumn.ElectiveHours,
                                             (char)UnitPlanColumn.IndependentHours,
                                             "{0}");
            _classHoursFormat = string.Format("SUM({0}{3}:{1}{3})-{2}{3}",
                                               (char)UnitPlanColumn.ElectiveHours,
                                               (char)UnitPlanColumn.ConsultationHours,
                                               (char)UnitPlanColumn.IndependentHours,
                                               "{0}");
        }

        private void InitStyles()
        {
            _plainFont = _workbook.CreateFont();
            _plainFont.FontHeightInPoints = 12;
            _plainFont.FontName = "Times New Roman";

            _boldFont = _workbook.CreateFont();
            _boldFont.FontHeightInPoints = 12;
            _boldFont.FontName = "Times New Roman";
            _boldFont.IsBold = true;

            _plainStyle = GetBaseStyle();
            _plainStyle.Alignment = HorizontalAlignment.Left;
            _plainStyle.SetFont(_plainFont);

            _plainCenter = GetBaseStyle();
            _plainCenter.SetFont(_plainFont);

            _plainCenterYellow = GetBaseStyle();
            _plainCenterYellow.SetFont(_plainFont);
            _plainCenterYellow.FillForegroundColor = IndexedColors.Yellow.Index;
            _plainCenterYellow.FillPattern = FillPattern.SolidForeground;

            _boldCenter = GetBaseStyle();
            _boldCenter.SetFont(_boldFont);

            _boldCenterRotated = GetBaseStyle();
            _boldCenterRotated.SetFont(_boldFont);
            _boldCenterRotated.Rotation = 90;
        }

        private ICellStyle GetBaseStyle()
        {
            var style = _workbook.CreateCellStyle();

            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.WrapText = true;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderTop = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;

            return style;
        }

        private IRow GetRow(int rownum)
        {
            var row = _sheet.GetRow(rownum);
            if (row == null) row = _sheet.CreateRow(rownum);
            return row;
        }

        private ICell GetCell(int rownum, int cellnum)
        {
            var cell = GetRow(rownum).GetCell(cellnum);
            if (cell == null) cell = GetRow(rownum).CreateCell(cellnum);
            return cell;
        }

        private void FillHeader()
        {
            List<string> headers = new List<string>
            {
                "Приложение к рабочей программе",
                "Тематический план и содержание профессионального модуля",
                string.Format("{0} {1}", _course.Code, _course.Name)
            };

            for (int i = 1; i < headers.Count + 1; i++)
            {
                GetCell(i, 0).SetCellValue(headers[i - 1]);
                GetCell(i, 0).CellStyle = _workbook.CreateCellStyle();
                GetCell(i, 0).CellStyle.SetFont(_plainFont);
                _sheet.AddMergedRegion(new CellRangeAddress(i, i, 0, 18));
            }
            GetCell(1, 0).CellStyle.Alignment = HorizontalAlignment.Right;

            for (int i = UnitPlanColumn.Semester; i <= UnitPlanColumn.SubjectTitle; i++)
            {
                GetCell(5, i).SetCellValue(((UnitPlanColumn)i).Header);
                _sheet.AddMergedRegion(new CellRangeAddress(5, 6, i, i));
            }

            for (int i = UnitPlanColumn.TotalHours; i <= UnitPlanColumn.ConsultationHours; i++)
            {
                GetCell(6, i).SetCellValue(((UnitPlanColumn)i).Header);
            }

            GetCell(5, UnitPlanColumn.TotalHours).SetCellValue(UnitPlanColumn.Hours.Header);
            _sheet.AddMergedRegion(new CellRangeAddress(5, 5, UnitPlanColumn.TotalHours, UnitPlanColumn.ConsultationHours));

            GetCell(5, UnitPlanColumn.UserName).SetCellValue(UnitPlanColumn.UserName.Header);
            _sheet.AddMergedRegion(new CellRangeAddress(5, 6, UnitPlanColumn.UserName, UnitPlanColumn.UserName));

            for (int i = 5; i < 7; i++)
            {
                for (int j = 0; j <= UnitPlanColumn.UserName; j++)
                {
                    var cell = GetCell(i, j);
                    cell.CellStyle = _boldCenter;
                    if (cell.RowIndex == 5
                        && cell.ColumnIndex <= UnitPlanColumn.Classroom
                        && cell.ColumnIndex != UnitPlanColumn.SetTitle
                        || cell.RowIndex == 6)
                    {
                        cell.CellStyle = _boldCenterRotated;
                    }
                }
            }

            _sheet.SetColumnWidth(UnitPlanColumn.SetTitle, 22 * _columnWidthCoefficient);
            _sheet.SetColumnWidth(UnitPlanColumn.SubjectType, 19 * _columnWidthCoefficient);
            _sheet.SetColumnWidth(UnitPlanColumn.SubjectTitle, 47 * _columnWidthCoefficient);
            _sheet.SetColumnWidth(UnitPlanColumn.UserName, 18 * _columnWidthCoefficient);

            _sheet.CreateFreezePane(0, 7);
            _currentRow = 7;
        }

        private int ReserveRow()
        {
            int row = _currentRow;
            _currentRow++;
            return row;
        }

        private void ExportSubject(Subject subject)
        {
            int subjectRow = ReserveRow();
            _currentNumber++;
            _currentHours += subject.Hours;

            for (int i = 0; i <= UnitPlanColumn.UserName; i++)
            {
                GetCell(subjectRow, i).CellStyle = _plainCenter;

                if (i == UnitPlanColumn.SubjectTitle)
                {
                    GetCell(subjectRow, i).CellStyle = _plainStyle;
                }

                if (i > UnitPlanColumn.ClassHours && i < UnitPlanColumn.UserName)
                {
                    GetCell(subjectRow, i).CellStyle = _plainCenterYellow;
                }
            }

            GetCell(subjectRow, UnitPlanColumn.TotalHours).SetCellType(CellType.Formula);
            GetCell(subjectRow, UnitPlanColumn.TotalHours).CellStyle = _boldCenter;
            GetCell(subjectRow, UnitPlanColumn.TotalHours).SetCellFormula(string.Format(_totalHoursFormat, subjectRow + 1));
            GetCell(subjectRow, UnitPlanColumn.ClassHours).SetCellType(CellType.Formula);
            GetCell(subjectRow, UnitPlanColumn.ClassHours).CellStyle = _boldCenter;
            GetCell(subjectRow, UnitPlanColumn.ClassHours).SetCellFormula(string.Format(_classHoursFormat, subjectRow + 1));

            GetCell(subjectRow, UnitPlanColumn.Semester).SetCellValue(subject.SubjectSet.Semester.Number);
            GetCell(subjectRow, UnitPlanColumn.Number).SetCellValue(_currentNumber);
            GetCell(subjectRow, UnitPlanColumn.Hours).SetCellValue(string.Format("{0}/{1}", subject.Hours, _currentHours));
            GetCell(subjectRow, UnitPlanColumn.SubjectType).SetCellValue((string)_subjectTypeConverter.Convert(subject.Type, typeof(string), null, null));
            GetCell(subjectRow, UnitPlanColumn.SubjectTitle).SetCellValue(subject.Title.Value);
            GetCell(subjectRow, UnitPlanColumn.ElectiveHours + (int)subject.Type).SetCellValue(subject.Hours);
            GetCell(subjectRow, UnitPlanColumn.UserName).SetCellValue(subject.SubjectSet.Semester.User.DisplayName);
        }

        private int ExportSubjectSet(SubjectSet subjectSet)
        {
            int subjectSetRow = ReserveRow();

            GetCell(subjectSetRow, UnitPlanColumn.Semester).SetCellValue(subjectSet.Semester.Number);
            GetCell(subjectSetRow, UnitPlanColumn.Semester).CellStyle = _plainCenter;

            GetCell(subjectSetRow, UnitPlanColumn.SetTitle).SetCellValue(subjectSet.Title.Value);
            GetCell(subjectSetRow, UnitPlanColumn.SetTitle).CellStyle = _boldCenter;

            GetCell(subjectSetRow, UnitPlanColumn.Number).SetCellValue("Содержание");
            GetCell(subjectSetRow, UnitPlanColumn.Number).CellStyle = _boldCenter;
            _sheet.AddMergedRegion(new CellRangeAddress(subjectSetRow, subjectSetRow, UnitPlanColumn.Number, UnitPlanColumn.SubjectTitle));

            for (int i = UnitPlanColumn.TotalHours; i < UnitPlanColumn.UserName; i++)
            {
                GetCell(subjectSetRow, i).SetCellType(CellType.Formula);
                GetCell(subjectSetRow, i).CellStyle = _boldCenter;
            }
            GetCell(subjectSetRow, UnitPlanColumn.UserName).CellStyle = _plainCenter;

            GetCell(subjectSetRow, UnitPlanColumn.TotalHours).SetCellFormula(string.Format(_totalHoursFormat, subjectSetRow + 1));
            GetCell(subjectSetRow, UnitPlanColumn.ClassHours).SetCellFormula(string.Format(_classHoursFormat, subjectSetRow + 1));
            for (int i = UnitPlanColumn.ElectiveHours; i < UnitPlanColumn.UserName; i++)
            {
                GetCell(subjectSetRow, i)
                    .SetCellFormula(string.Format("SUM({0}{1}:{0}{2})",
                                                  (char)('A' + i),
                                                  subjectSetRow + 2,
                                                  subjectSetRow + subjectSet.Subjects.Count + 1));
            }

            foreach (var subject in subjectSet.Subjects.OrderBy(s => s.Number))
            {
                ExportSubject(subject);
            }

            return subjectSetRow;
        }

        private void ExportPracticalTraining(int courseRow, Semester semester)
        {
            GetCell(courseRow, UnitPlanColumn.Semester).SetCellValue(semester.Number);
            GetCell(courseRow, UnitPlanColumn.SetTitle).SetCellValue(semester.Course.Code);
            GetCell(courseRow, UnitPlanColumn.Number).SetCellValue(1);
            GetCell(courseRow, UnitPlanColumn.Hours).SetCellValue(string.Format("{0}/{0}", semester.Hours.Training));
            GetCell(courseRow, UnitPlanColumn.SubjectType).SetCellValue("Производственная практика");
            GetCell(courseRow, UnitPlanColumn.SubjectTitle).SetCellValue("Производственная практика");
            GetCell(courseRow, UnitPlanColumn.TotalHours).SetCellFormula(string.Format(_totalHoursFormat, courseRow + 1));
            GetCell(courseRow, UnitPlanColumn.ClassHours).SetCellFormula(string.Format(_classHoursFormat, courseRow + 1));
            GetCell(courseRow, UnitPlanColumn.PracticalTrainingHours).SetCellValue(semester.Hours.Training);
            GetCell(courseRow, UnitPlanColumn.UserName).SetCellValue(semester.User?.DisplayName);

            for (int i = UnitPlanColumn.Semester; i <= UnitPlanColumn.UserName; i++)
            {
                if (i == UnitPlanColumn.SetTitle || i == UnitPlanColumn.TotalHours || i == UnitPlanColumn.ClassHours)
                {
                    GetCell(courseRow, i).CellStyle = _boldCenter;
                }
                else if (i > UnitPlanColumn.ClassHours && i < UnitPlanColumn.UserName)
                {
                    GetCell(courseRow, i).CellStyle = _plainCenterYellow;
                }
                else
                {
                    GetCell(courseRow, i).CellStyle = _plainCenter;
                }
            }
        }

        private int AddTotalRow(string subjectType, List<int> rows)
        {
            int totalRow = ReserveRow();
            GetCell(totalRow, UnitPlanColumn.SubjectType).SetCellValue(subjectType);
            for (int i = UnitPlanColumn.TotalHours; i <= UnitPlanColumn.ConsultationHours; i++)
            {
                GetCell(totalRow, i).SetCellType(CellType.Formula);
            }
            GetCell(totalRow, UnitPlanColumn.TotalHours).SetCellFormula(string.Format(_totalHoursFormat, totalRow + 1));
            GetCell(totalRow, UnitPlanColumn.ClassHours).SetCellFormula(string.Format(_classHoursFormat, totalRow + 1));

            if (rows.Count > 0)
            {
                for (int i = UnitPlanColumn.ElectiveHours; i <= UnitPlanColumn.ConsultationHours; i++)
                {
                    var formula = string.Join(",", rows.Select(r => string.Format("{0}{1}", (char)('A' + i), r + 1)));
                    formula = string.Format("SUM({0})", formula);

                    GetCell(totalRow, i).SetCellFormula(formula);
                }
            }

            for (int i = UnitPlanColumn.SetTitle; i <= UnitPlanColumn.UserName; i++)
            {
                GetCell(totalRow, i).CellStyle = _boldCenter;
            }

            return totalRow;
        }

        private int ExportSemester(Semester semester)
        {
            var subjectSetRows = new List<int>();

            foreach (var subjectSet in semester.SubjectSets)
            {
                subjectSetRows.Add(ExportSubjectSet(subjectSet));
            }

            return AddTotalRow(string.Format("ИТОГО за {0} семестр", semester.Number), subjectSetRows);
        }

        private int ExportCourse(Course course)
        {
            var totalRows = new List<int>();
            var courseRow = ReserveRow();

            if (course.Type == CourseType.PracticalTraining)
            {
                ExportPracticalTraining(courseRow, course.Semesters.First());
                return courseRow;
            }

            GetCell(courseRow, UnitPlanColumn.SetTitle).SetCellValue(course.Code);
            GetCell(courseRow, UnitPlanColumn.Number).SetCellValue(course.Name);
            _sheet.AddMergedRegion(new CellRangeAddress(courseRow, courseRow, UnitPlanColumn.Number, UnitPlanColumn.SubjectTitle));

            for (int i = 0; i <= UnitPlanColumn.UserName; i++)
            {
                GetCell(courseRow, i).CellStyle = _boldCenter;
            }

            if (course.ChildCourses != null && course.ChildCourses.Count > 0)
            {
                foreach (var childCourse in course.ChildCourses.OrderBy(s => s.Number).OrderBy(s => s.Type))
                {
                    totalRows.Add(ExportCourse(childCourse));
                }
            }
            else
            {
                _currentNumber = 0;
                _currentHours = 0;
                foreach (var semester in course.Semesters.Where(s => s.Curriculum == _curriculum).OrderBy(s => s.Number))
                {
                    if (semester.Number % 2 == 1)
                    {
                        _currentNumber = 0;
                        _currentHours = 0;
                    }
                    totalRows.Add(ExportSemester(semester));
                }
            }

            return AddTotalRow(string.Format("ИТОГО за {0}", course.Code), totalRows);
        }

        private bool CurriculumContains(Course course)
        {
            return course.Semesters.Any(s => s.Curriculum.Id == _curriculum.Id)
                || course.ChildCourses.Any(c => CurriculumContains(c));
        }

        private void FillBody()
        {
            var totalRows = new List<int>();
            if (_course.ChildCourses.Count > 0)
            {
                var childCourses = _course
                    .ChildCourses
                    .Where(c => CurriculumContains(c))
                    .OrderBy(c => c.Number)
                    .OrderBy(c => c.Type);
                foreach (var childCourse in childCourses)
                {
                    totalRows.Add(ExportCourse(childCourse));
                }
            }
            else
            {
                foreach (var semester in _course.Semesters.Where(s => s.Curriculum == _curriculum).OrderBy(s => s.Number))
                {
                    totalRows.Add(ExportSemester(semester));
                }
            }

            AddTotalRow("ВСЕГО", totalRows);
        }

        private void Clear()
        {
            _course = null;
            _curriculum = null;
            _context = null;
        }

        public async Task ExportAsync(int curriculumId, int courseId, string fileName)
        {
            _context = _databaseService.Context;

            _curriculum = await _context.Curricula.FirstOrDefaultAsync(c => c.Id == curriculumId);
            _course = await _context.Courses.FirstOrDefaultAsync(c => c.Id == courseId);

            if (_curriculum == null || _course == null || _course.ParentCourse != null)
            {
                throw new ArgumentException();
            }

            _workbook = new XSSFWorkbook();
            _sheet = _workbook.CreateSheet("Тематический план");

            InitStyles();
            FillHeader();
            FillBody();

            using (var file = new FileStream(fileName, FileMode.Create))
            {
                _workbook.Write(file);
            }

            Clear();
        }

        public async Task ExportAsync(ICollection<int> semesterIds, string fileName)
        {
            _context = _databaseService.Context;

            var semesters = await _context.Semesters.Where(s => semesterIds.Any(id => id == s.Id)).ToListAsync();
            _curriculum = semesters.First().Curriculum;
            _course = semesters.First().Course;

            _workbook = new XSSFWorkbook();
            _sheet = _workbook.CreateSheet("Тематический план");

            InitStyles();
            FillHeader();

            foreach (var semester in semesters)
            {
                ExportSemester(semester);
            }

            using (var file = new FileStream(fileName, FileMode.Create))
            {
                _workbook.Write(file);
            }

            Clear();
        }
    }

    public class UnitPlanImporter
    {
        private readonly IDatabaseService _databaseService;
        private UnitPlanContext _context;
        private Semester _semester;

        private IWorkbook _workbook;
        private ISheet _sheet;

        private int _currentRow;

        public bool IoError { get; private set; }

        public UnitPlanImporter(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        private void ImportSubject(SubjectSet set)
        {
            var subjectTitle = _sheet.GetRow(_currentRow).GetCell(UnitPlanColumn.SubjectTitle).StringCellValue;
            var title = _context.Titles.FirstOrDefault(t => t.Value == subjectTitle);

            if (title == null)
            {
                title = new Title { Value = subjectTitle };
                _context.Titles.Add(title);
            }

            short hours = 0;
            var subjectType = SubjectType.Lecture;

            for (int i = UnitPlanColumn.ElectiveHours; i <= UnitPlanColumn.ConsultationHours; i++)
            {
                var cell = _sheet.GetRow(_currentRow).GetCell(i);
                if (cell != null && cell.CellType == CellType.Numeric && cell.NumericCellValue > 0)
                {
                    hours = (short)cell.NumericCellValue;
                    subjectType = (SubjectType)(i - UnitPlanColumn.ElectiveHours);
                    break;
                }
            }

            var subject = new Subject
            {
                Number = (short)set.Subjects.Count,
                Hours = hours,
                Type = subjectType,
                Title = title,
                SubjectSet = set,
            };

            _context.Subjects.Add(subject);

            _currentRow++;
        }

        private void ImportSubjectSet()
        {
            var subjectSetTitle = _sheet.GetRow(_currentRow).GetCell(UnitPlanColumn.SetTitle).StringCellValue;
            var title = _context.Titles.FirstOrDefault(t => t.Value == subjectSetTitle);

            if (title == null)
            {
                title = new Title { Value = subjectSetTitle };
                _context.Titles.Add(title);
            }

            var subjectSet = new SubjectSet
            {
                Number = (short)_semester.SubjectSets.Count,
                Title = title,
                Semester = _semester,
                Subjects = new List<Subject>(),
            };
            _context.SubjectSets.Add(subjectSet);

            _currentRow++;

            var cell = _sheet.GetRow(_currentRow).GetCell(UnitPlanColumn.Number);
            while (cell != null && cell.CellType == CellType.Numeric)
            {
                ImportSubject(subjectSet);
                cell = _sheet.GetRow(_currentRow).GetCell(UnitPlanColumn.Number);
            }
        }

        private void ImportSemester()
        {
            _currentRow = -1;

            for (int i = _sheet.FirstRowNum; i < _sheet.LastRowNum; i++)
            {
                var courseRow = _sheet.GetRow(i);
                if (courseRow != null)
                {
                    var cell = courseRow.GetCell(UnitPlanColumn.SetTitle);
                    if (cell != null && cell.ToString() == _semester.Course.Code)
                    {
                        for (int j = courseRow.RowNum + 1; j < _sheet.LastRowNum; j++)
                        {
                            var semesterRow = _sheet.GetRow(j);
                            if (semesterRow != null)
                            {
                                cell = semesterRow.GetCell(UnitPlanColumn.Semester);
                                if (cell != null && cell.CellType == CellType.Numeric && cell.NumericCellValue == _semester.Number)
                                {
                                    _currentRow = semesterRow.RowNum;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }

            if (_currentRow != -1)
            {
                var cell = _sheet.GetRow(_currentRow).GetCell(UnitPlanColumn.Semester);
                while (cell != null && cell.CellType == CellType.Numeric && cell.NumericCellValue == _semester.Number)
                {
                    ImportSubjectSet();
                    cell = _sheet.GetRow(_currentRow).GetCell(UnitPlanColumn.Semester);
                }
            }
        }

        public async Task ImportAsync(int semesterId, string fileName)
        {
            _context = _databaseService.Context;

            _semester = await _context.Semesters.FirstOrDefaultAsync(s => s.Id == semesterId);

            if (_semester == null || _semester.SubjectSets.Count > 0)
            {
                _context = null;
                return;
            }

            try
            {
                using (Stream stream = new FileStream(fileName, FileMode.Open))
                {
                    _workbook = new XSSFWorkbook(stream);
                }

                _sheet = _workbook.GetSheetAt(_workbook.ActiveSheetIndex);
                ImportSemester();
                _context.SaveChanges();
                _context = null;
            }
            catch (IOException)
            {
                _context = null;
                IoError = true;
            }
        }
    }
}
