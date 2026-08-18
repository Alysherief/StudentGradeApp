using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeApp.Data;
using StudentGradeApp.Interfaces;
using StudentGradeApp.Models;
using StudentGradeApp.ViewModels;

namespace StudentGradeApp.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;

        public TeacherController(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Index", "DashBoard");
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
        public async Task<IActionResult> Grades(
            int? subjectId,
            int? componentId)
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

            var approvedStudents = await _context.StudentSubjects
                .Where(ss =>
                    ss.SubjectId == subjectId &&
                    ss.Status == EnrollmentStatus.Approved)
                .ToListAsync();

            var approvedStudentIds = approvedStudents
                .Select(ss => ss.StudentId)
                .ToList();

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

                if (grade.Value < 0 ||
                    grade.Value > component.MaxGrade)
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

            /*
             * Check whether every grade component for the subject
             * has now been entered for each student.
             */

            var allComponents = await _context.GradeComponents
                .Where(g => g.SubjectId == subjectId)
                .OrderBy(g => g.Id)
                .ToListAsync();

            if (allComponents.Any())
            {
                foreach (var enrollment in approvedStudents)
                {
                    /*
                     * Do not send the notification again if it
                     * has already been sent for this enrollment.
                     */

                    if (enrollment.GradeNotificationSent)
                    {
                        continue;
                    }

                    var studentGrades = await _context.StudentGrades
                        .Where(g =>
                            g.StudentId == enrollment.StudentId &&
                            g.GradeComponent!.SubjectId == subjectId)
                        .ToListAsync();

                    /*
                     * The student's grade is considered complete
                     * only when they have a StudentGrade record
                     * for EVERY GradeComponent belonging to the subject.
                     */

                    var completed =
                        allComponents.All(componentDefinition =>
                            studentGrades.Any(g =>
                                g.GradeComponentId ==
                                componentDefinition.Id));

                    if (!completed)
                    {
                        continue;
                    }

                    var student = await _userManager
                        .FindByIdAsync(enrollment.StudentId);

                    if (student == null ||
                        string.IsNullOrWhiteSpace(student.Email))
                    {
                        continue;
                    }

                    /*
                     * Generate the student's My Performance URL.
                     *
                     * Url.Action generates the URL using the
                     * Student controller and Performance action.
                     *
                     * Request.Scheme makes it an absolute URL,
                     * such as:
                     *
                     * https://localhost:xxxx/Student/Performance
                     */

                    var performanceUrl = Url.Action(
                        "Performance",
                        "Student",
                        null,
                        Request.Scheme);

                    if (string.IsNullOrWhiteSpace(performanceUrl))
                    {
                        continue;
                    }

                    var htmlMessage = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Grade Available</title>
</head>

<body style='font-family: Arial, sans-serif;
             background-color: #f5f7fb;
             padding: 30px;'>

    <div style='max-width: 600px;
                margin: auto;
                background: white;
                padding: 35px;
                border-radius: 15px;
                box-shadow: 0 5px 20px rgba(0,0,0,0.08);'>

        <h2 style='color: #0d6efd;'>
            Your Grade Is Available
        </h2>

        <p>
            Hello
            {System.Net.WebUtility.HtmlEncode(student.FullName)},
        </p>

        <p>
            All grade components for your subject
            <strong>
                {System.Net.WebUtility.HtmlEncode(subject.Name)}
            </strong>
            have now been released.
        </p>

        <p>
            Your complete grade is now available on
            StudentGradeApp.
        </p>

        <p>
            Click the button below to view your grade and
            detailed performance.
        </p>

        <p style='text-align: center; margin: 30px 0;'>

            <a href='{performanceUrl}'
               style='background-color: #0d6efd;
                      color: white;
                      padding: 13px 28px;
                      text-decoration: none;
                      border-radius: 8px;
                      font-weight: bold;
                      display: inline-block;'>

                View My Performance

            </a>

        </p>

        <p>
            From the My Performance page, you can view your
            individual grade components and your complete
            academic results.
        </p>

        <p>
            Thank you.
        </p>

        <hr>

        <p style='color: #777;
                  font-size: 13px;'>
            StudentGradeApp
        </p>

    </div>

</body>
</html>";

                    await _emailService.SendEmailAsync(
                        student.Email,
                        $"StudentGradeApp - {subject.Name} Grade Available",
                        htmlMessage);

                    /*
                     * Mark the notification as sent so the same
                     * student does not receive another email for
                     * this subject every time grades are saved.
                     */

                    enrollment.GradeNotificationSent = true;
                }

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] =
                "Grades saved successfully.";

            return RedirectToAction(
                "Grades",
                new
                {
                    subjectId,
                    componentId
                });
        }

        [HttpGet]
        public async Task<IActionResult> Students(
            int? subjectId,
            string sortOrder = "name")
        {
            var teacher = await _userManager.GetUserAsync(User);

            if (teacher == null)
            {
                return Unauthorized();
            }

            var teacherSubjects = await _context.Subjects
                .Where(s => s.TeacherId == teacher.Id)
                .OrderBy(s => s.Name)
                .Select(s => new TeacherSubjectOptionViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    Code = s.Code
                })
                .ToListAsync();

            var model = new TeacherStudentsViewModel
            {
                SelectedSubjectId = subjectId,
                Subjects = teacherSubjects
            };

            if (!subjectId.HasValue)
            {
                return View(model);
            }

            var selectedSubject = await _context.Subjects
                .Include(s => s.GradeComponents)
                .FirstOrDefaultAsync(s =>
                    s.Id == subjectId.Value &&
                    s.TeacherId == teacher.Id);

            if (selectedSubject == null)
            {
                return NotFound();
            }

            var students = await _context.StudentSubjects
                .Include(ss => ss.Student)
                .Where(ss =>
                    ss.SubjectId == subjectId.Value &&
                    ss.Status == EnrollmentStatus.Approved)
                .OrderBy(ss => ss.Student!.FullName)
                .ToListAsync();

            var studentIds = students
                .Select(s => s.StudentId)
                .ToList();

            var grades = await _context.StudentGrades
                .Include(g => g.GradeComponent)
                .Where(g =>
                    studentIds.Contains(g.StudentId) &&
                    g.GradeComponent!.SubjectId == subjectId.Value)
                .ToListAsync();

            var studentResults =
                new List<TeacherStudentViewModel>();

            foreach (var enrollment in students)
            {
                var student = enrollment.Student;

                if (student == null)
                {
                    continue;
                }

                var studentGrades = grades
                    .Where(g => g.StudentId == student.Id)
                    .ToList();

                decimal subjectGrade = 0;

                foreach (var grade in studentGrades)
                {
                    var component = grade.GradeComponent;

                    if (component == null ||
                        component.MaxGrade <= 0)
                    {
                        continue;
                    }

                    decimal percentage =
                        (grade.Grade /
                         component.MaxGrade) * 100;

                    decimal weightedGrade =
                        percentage *
                        (component.WeightPercentage / 100);

                    subjectGrade += weightedGrade;
                }

                studentResults.Add(
                    new TeacherStudentViewModel
                    {
                        StudentId = student.Id,
                        FullName = student.FullName,
                        Email = student.Email ?? "No email",
                        AverageGrade =
                            (double)Math.Round(
                                subjectGrade,
                                2),
                        GradeCount =
                            studentGrades.Count
                    });
            }

            switch (sortOrder)
            {
                case "gradeDesc":

                    studentResults = studentResults
                        .OrderByDescending(
                            s => s.AverageGrade)
                        .ThenBy(s => s.FullName)
                        .ToList();

                    break;

                case "gradeAsc":

                    studentResults = studentResults
                        .OrderBy(s => s.AverageGrade)
                        .ThenBy(s => s.FullName)
                        .ToList();

                    break;

                case "nameDesc":

                    studentResults = studentResults
                        .OrderByDescending(
                            s => s.FullName)
                        .ToList();

                    break;

                default:

                    studentResults = studentResults
                        .OrderBy(s => s.FullName)
                        .ToList();

                    break;
            }

            model.Students = studentResults;

            ViewBag.SelectedSubject = selectedSubject;
            ViewBag.SortOrder = sortOrder;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> StudentProfile(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var teacher = await _userManager.GetUserAsync(User);

            if (teacher == null)
            {
                return Unauthorized();
            }

            var isStudentOfTeacher =
                await _context.StudentSubjects
                    .AnyAsync(ss =>
                        ss.StudentId == id &&
                        ss.Status == EnrollmentStatus.Approved &&
                        ss.Subject!.TeacherId == teacher.Id);

            if (!isStudentOfTeacher)
            {
                return Forbid();
            }

            var student =
                await _userManager.FindByIdAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }
    }
}
