using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeApp.Data;
using StudentGradeApp.Models;
using StudentGradeApp.ViewModels;

namespace StudentGradeApp.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public StudentController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Subjects()
        {
            var subjects = await _context.Subjects
                .Include(s => s.Teacher)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var model = new SelectSubjectsViewModel();

            foreach (var subject in subjects)
            {
                model.Subjects.Add(item: new SubjectSelectionItemViewModel
                {
                    SubjectId = subject.Id,
                    Name = subject.Name,
                    Code = subject.Code,
                    CreditHours = subject.CreditHours,
                    TeacherName = subject.Teacher?.FullName,
                    Selected = false
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectSubjects(
            SelectSubjectsViewModel model)
        {
            var student = await _userManager.GetUserAsync(User);

            if (student == null)
            {
                return Unauthorized();
            }

            var selectedSubjectIds = model.Subjects
                .Where(s => s.Selected)
                .Select(s => s.SubjectId)
                .ToList();

            if (!selectedSubjectIds.Any())
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please select at least one subject.");

                return View("Subjects", model);
            }

            var selectedSubjects = await _context.Subjects
                .Where(s => selectedSubjectIds.Contains(s.Id))
                .ToListAsync();

            var totalCreditHours = selectedSubjects
                .Sum(s => s.CreditHours);

            if (totalCreditHours != 30)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"You must select exactly 30 credit hours. " +
                    $"You currently selected {totalCreditHours}.");

                return View("Subjects", model);
            }

            var existingEnrollments = await _context.StudentSubjects
                .Where(ss =>
                    ss.StudentId == student.Id &&
                    selectedSubjectIds.Contains(ss.SubjectId))
                .ToListAsync();

            if (existingEnrollments.Any())
            {
                ModelState.AddModelError(
                    string.Empty,
                    "You have already requested one or more of these subjects.");

                return View("Subjects", model);
            }

            foreach (var subjectId in selectedSubjectIds)
            {
                var studentSubject = new StudentSubject
                {
                    StudentId = student.Id,
                    SubjectId = subjectId,
                    Status = EnrollmentStatus.Pending,
                    RequestedAt = DateTime.UtcNow
                };

                _context.StudentSubjects.Add(studentSubject);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Subjects");
        }

        [HttpGet]
        public async Task<IActionResult> MySubjects()
        {
            var student = await _userManager.GetUserAsync(User);

            if (student == null)
            {
                return Unauthorized();
            }

            var subjects = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                    .ThenInclude(s => s!.Teacher)
                .Where(ss =>
                    ss.StudentId == student.Id &&
                    ss.Status == EnrollmentStatus.Approved)
                .OrderBy(ss => ss.Subject!.Name)
                .ToListAsync();

            return View(subjects);
        }

        [HttpGet]
        public async Task<IActionResult> Grades(int? subjectId)
        {
            var student = await _userManager.GetUserAsync(User);

            if (student == null)
            {
                return Unauthorized();
            }

            var subjects = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                .Where(ss =>
                    ss.StudentId == student.Id &&
                    ss.Status == EnrollmentStatus.Approved &&
                    ss.Subject != null)
                .Select(ss => ss.Subject!)
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewBag.Subjects = subjects;
            ViewBag.SelectedSubjectId = subjectId;

            if (!subjectId.HasValue)
            {
                return View(new List<StudentGrade>());
            }

            var belongsToStudent = await _context.StudentSubjects
                .AnyAsync(ss =>
                    ss.StudentId == student.Id &&
                    ss.SubjectId == subjectId.Value &&
                    ss.Status == EnrollmentStatus.Approved);

            if (!belongsToStudent)
            {
                return View(new List<StudentGrade>());
            }

            var grades = await _context.StudentGrades
                .Include(g => g.GradeComponent)
                    .ThenInclude(gc => gc!.Subject)
                .Where(g =>
                    g.StudentId == student.Id &&
                    g.GradeComponent != null &&
                    g.GradeComponent.SubjectId == subjectId.Value)
                .OrderBy(g => g.GradeComponent!.Name)
                .ToListAsync();

            return View(grades);
        }

        [HttpGet]
        public async Task<IActionResult> Performance()
        {
            var student = await _userManager.GetUserAsync(User);

            if (student == null)
            {
                return Unauthorized();
            }

            var subjects = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                .Where(ss =>
                    ss.StudentId == student.Id &&
                    ss.Status == EnrollmentStatus.Approved &&
                    ss.Subject != null)
                .Select(ss => ss.Subject!)
                .OrderBy(s => s.Name)
                .ToListAsync();

            var grades = await _context.StudentGrades
                .Include(g => g.GradeComponent)
                .Where(g =>
                    g.StudentId == student.Id &&
                    g.GradeComponent != null)
                .ToListAsync();

            var performance = new List<StudentPerformanceViewModel>();

            foreach (var subject in subjects)
            {
                var subjectGrades = grades
                    .Where(g =>
                        g.GradeComponent != null &&
                        g.GradeComponent.SubjectId == subject.Id)
                    .ToList();

                decimal totalPercentage = 0;

                foreach (var grade in subjectGrades)
                {
                    var component = grade.GradeComponent;

                    if (component == null || component.MaxGrade <= 0)
                    {
                        continue;
                    }

                    var componentPercentage =
                        (grade.Grade / component.MaxGrade) * 100m;

                    var weightedContribution =
                        componentPercentage *
                        (component.WeightPercentage / 100m);

                    totalPercentage += weightedContribution;
                }

                var letterGrade = GetLetterGrade(totalPercentage);
                var gpa = GetGpa(totalPercentage);

                performance.Add(new StudentPerformanceViewModel
                {
                    SubjectId = subject.Id,
                    SubjectName = subject.Name,
                    SubjectCode = subject.Code,
                    CreditHours = subject.CreditHours,
                    Percentage = totalPercentage,
                    LetterGrade = letterGrade,
                    GPA = gpa
                });
            }

            decimal totalCredits = performance
                .Where(p => p.CreditHours > 0)
                .Sum(p => p.CreditHours);

            decimal overallGpa = 0;

            if (totalCredits > 0)
            {
                overallGpa = performance.Sum(p =>
                    p.GPA * p.CreditHours) / totalCredits;
            }

            ViewBag.OverallGPA = overallGpa;
            ViewBag.OverallLetterGrade = GetGpaLetterGrade(overallGpa);

            return View(performance);
        }

        private static string GetLetterGrade(decimal percentage)
        {
            if (percentage >= 90)
                return "A";

            if (percentage >= 85)
                return "A-";

            if (percentage >= 80)
                return "B+";

            if (percentage >= 75)
                return "B";

            if (percentage >= 70)
                return "B-";

            if (percentage >= 65)
                return "C+";

            if (percentage >= 60)
                return "C";

            if (percentage >= 55)
                return "C-";

            if (percentage >= 50)
                return "D";

            return "F";
        }

        private static decimal GetGpa(decimal percentage)
        {
            if (percentage >= 90)
                return 4.0m;

            if (percentage >= 85)
                return 3.7m;

            if (percentage >= 80)
                return 3.3m;

            if (percentage >= 75)
                return 3.0m;

            if (percentage >= 70)
                return 2.7m;

            if (percentage >= 65)
                return 2.3m;

            if (percentage >= 60)
                return 2.0m;

            if (percentage >= 55)
                return 1.7m;

            if (percentage >= 50)
                return 1.0m;

            return 0.0m;
        }

        private static string GetGpaLetterGrade(decimal gpa)
        {
            if (gpa >= 3.85m)
                return "A";

            if (gpa >= 3.50m)
                return "A-";

            if (gpa >= 3.15m)
                return "B+";

            if (gpa >= 2.85m)
                return "B";

            if (gpa >= 2.50m)
                return "B-";

            if (gpa >= 2.15m)
                return "C+";

            if (gpa >= 1.85m)
                return "C";

            if (gpa >= 1.50m)
                return "C-";

            if (gpa >= 1.00m)
                return "D";

            return "F";
        }
    }
}