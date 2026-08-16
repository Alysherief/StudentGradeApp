using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeApp.Data;
using StudentGradeApp.Models;

namespace StudentGradeApp.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public TeacherController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Subjects()
        {
            var teacher = await _userManager.GetUserAsync(User);

            if (teacher == null)
            {
                return Unauthorized();
            }

            var subjects = await _context.Subjects
                .Where(s => s.TeacherId == teacher.Id)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(subjects);
        }
        [HttpGet]
        public async Task<IActionResult> Grades(int? subjectId, int? componentId)
        {
            var teacher = await _userManager.GetUserAsync(User);

            if (teacher == null)
            {
                return Unauthorized();
            }

            
            var subjects = await _context.Subjects
                .Where(s => s.TeacherId == teacher.Id)
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewBag.Subjects = subjects;

            if (subjectId == null)
            {
                return View();
            }

            
            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s =>
                    s.Id == subjectId &&
                    s.TeacherId == teacher.Id);

            if (subject == null)
            {
                return NotFound();
            }

            
            var components = await _context.GradeComponents
                .Where(g => g.SubjectId == subjectId)
                .OrderBy(g => g.Id)
                .ToListAsync();

            ViewBag.SelectedSubject = subject;
            ViewBag.Components = components;

            if (componentId == null)
            {
                return View();
            }

            
            var component = await _context.GradeComponents
                .FirstOrDefaultAsync(g =>
                    g.Id == componentId &&
                    g.SubjectId == subjectId);

            if (component == null)
            {
                return NotFound();
            }

            ViewBag.SelectedComponent = component;

            
            var students = await _context.StudentSubjects
                .Include(ss => ss.Student)
                .Where(ss =>
                    ss.SubjectId == subjectId &&
                    ss.Status == EnrollmentStatus.Approved)
                .OrderBy(ss => ss.Student!.FullName)
                .ToListAsync();

            
            var studentIds = students
                .Select(s => s.StudentId)
                .ToList();

            var existingGrades = await _context.StudentGrades
                .Where(g =>
                    g.GradeComponentId == componentId &&
                    studentIds.Contains(g.StudentId))
                .ToListAsync();

            ViewBag.Students = students;
            ViewBag.ExistingGrades = existingGrades;

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGrades(
    int subjectId,
    int componentId,
    Dictionary<string, decimal?> grades)
        {
            var teacher = await _userManager.GetUserAsync(User);

            if (teacher == null)
            {
                return Unauthorized();
            }

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s =>
                    s.Id == subjectId &&
                    s.TeacherId == teacher.Id);

            if (subject == null)
            {
                return NotFound();
            }

            var component = await _context.GradeComponents
                .FirstOrDefaultAsync(g =>
                    g.Id == componentId &&
                    g.SubjectId == subjectId);

            if (component == null)
            {
                return NotFound();
            }

            var approvedStudentIds = await _context.StudentSubjects
                .Where(ss =>
                    ss.SubjectId == subjectId &&
                    ss.Status == EnrollmentStatus.Approved)
                .Select(ss => ss.StudentId)
                .ToListAsync();

            foreach (var entry in grades)
            {
                var studentId = entry.Key;
                var grade = entry.Value;

                if (!grade.HasValue)
                {
                    continue;
                }

                if (!approvedStudentIds.Contains(studentId))
                {
                    continue;
                }

                if (grade.Value < 0 || grade.Value > component.MaxGrade)
                {
                    TempData["ErrorMessage"] =
                        $"A grade must be between 0 and {component.MaxGrade}.";

                    return RedirectToAction(
                        "Grades",
                        new
                        {
                            subjectId,
                            componentId
                        });
                }

                var existingGrade = await _context.StudentGrades
                    .FirstOrDefaultAsync(g =>
                        g.StudentId == studentId &&
                        g.GradeComponentId == componentId);

                if (existingGrade == null)
                {
                    var studentGrade = new StudentGrade
                    {
                        StudentId = studentId,
                        GradeComponentId = componentId,
                        Grade = grade.Value,
                        GradedAt = DateTime.UtcNow
                    };

                    _context.StudentGrades.Add(studentGrade);
                }
                else
                {
                    existingGrade.Grade = grade.Value;
                    existingGrade.GradedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Grades saved successfully.";

            return RedirectToAction(
                "Grades",
                new
                {
                    subjectId,
                    componentId
                });
        }
    }
}