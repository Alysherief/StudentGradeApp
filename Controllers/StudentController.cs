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

        public IActionResult Index()
        {
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Subjects()
        {
            var student = await _userManager.GetUserAsync(User);

            if (student == null)
            {
                return Unauthorized();
            }

            var existingEnrollments = await _context.StudentSubjects
                .Where(ss =>
                    ss.StudentId == student.Id &&
                    (ss.Status == EnrollmentStatus.Pending ||
                     ss.Status == EnrollmentStatus.Approved))
                .Include(ss => ss.Subject)
                .ToListAsync();

            var existingSubjectIds = existingEnrollments
                .Select(ss => ss.SubjectId)
                .ToHashSet();

            var currentCreditHours = existingEnrollments
                .Where(ss => ss.Subject != null)
                .Sum(ss => ss.Subject!.CreditHours);

            var availableSubjects = await _context.Subjects
                .Include(s => s.Teacher)
                .Where(s => !existingSubjectIds.Contains(s.Id))
                .OrderBy(s => s.Name)
                .ToListAsync();

            var model = new SelectSubjectsViewModel();

            foreach (var subject in availableSubjects)
            {
                model.Subjects.Add(new SubjectSelectionItemViewModel
                {
                    SubjectId = subject.Id,
                    Name = subject.Name,
                    Code = subject.Code,
                    CreditHours = subject.CreditHours,
                    TeacherName = subject.Teacher?.FullName,
                    Selected = false
                });
            }

            ViewBag.CurrentCreditHours = currentCreditHours;
            ViewBag.RemainingCreditHours = Math.Max(0, 30 - currentCreditHours);

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

            var existingEnrollments = await _context.StudentSubjects
                .Where(ss =>
                    ss.StudentId == student.Id &&
                    (ss.Status == EnrollmentStatus.Pending ||
                     ss.Status == EnrollmentStatus.Approved))
                .Include(ss => ss.Subject)
                .ToListAsync();

            var existingSubjectIds = existingEnrollments
                .Select(ss => ss.SubjectId)
                .ToHashSet();

            var currentCreditHours = existingEnrollments
                .Where(ss => ss.Subject != null)
                .Sum(ss => ss.Subject!.CreditHours);

            var selectedSubjectIds = model.Subjects
                .Where(s => s.Selected)
                .Select(s => s.SubjectId)
                .Distinct()
                .ToList();

            if (!selectedSubjectIds.Any())
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please select at least one subject.");

                await RebuildSubjectsModel(
                    model,
                    existingSubjectIds,
                    currentCreditHours);

                return View("Subjects", model);
            }

            var alreadySelectedIds = selectedSubjectIds
                .Where(id => existingSubjectIds.Contains(id))
                .ToList();

            if (alreadySelectedIds.Any())
            {
                ModelState.AddModelError(
                    string.Empty,
                    "One or more of the selected subjects have already been submitted.");

                await RebuildSubjectsModel(
                    model,
                    existingSubjectIds,
                    currentCreditHours);

                return View("Subjects", model);
            }

            var selectedSubjects = await _context.Subjects
                .Where(s => selectedSubjectIds.Contains(s.Id))
                .ToListAsync();

            if (selectedSubjects.Count != selectedSubjectIds.Count)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "One or more selected subjects could not be found.");

                await RebuildSubjectsModel(
                    model,
                    existingSubjectIds,
                    currentCreditHours);

                return View("Subjects", model);
            }

            var newlySelectedCreditHours = selectedSubjects
                .Sum(s => s.CreditHours);

            var totalCreditHours =
                currentCreditHours + newlySelectedCreditHours;

            if (totalCreditHours > 30)
            {
                var remainingCreditHours =
                    Math.Max(0, 30 - currentCreditHours);

                ModelState.AddModelError(
                    string.Empty,
                    $"You can select up to {remainingCreditHours} more credit hours. " +
                    $"Your selection would bring your total to {totalCreditHours} credit hours, " +
                    $"which exceeds the 30 credit hour limit.");

                await RebuildSubjectsModel(
                    model,
                    existingSubjectIds,
                    currentCreditHours);

                return View("Subjects", model);
            }

            foreach (var subjectId in selectedSubjectIds)
            {
                _context.StudentSubjects.Add(new StudentSubject
                {
                    StudentId = student.Id,
                    SubjectId = subjectId,
                    Status = EnrollmentStatus.Pending,
                    RequestedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Your subject selection has been submitted successfully. " +
                $"You are now enrolled in {totalCreditHours} credit hours " +
                $"including your pending requests.";

            return RedirectToAction(nameof(Subjects));
        }

        private async Task RebuildSubjectsModel(
            SelectSubjectsViewModel model,
            HashSet<int> existingSubjectIds,
            int currentCreditHours)
        {
            var availableSubjects = await _context.Subjects
                .Include(s => s.Teacher)
                .Where(s => !existingSubjectIds.Contains(s.Id))
                .OrderBy(s => s.Name)
                .ToListAsync();

            var selectedIds = model.Subjects
                .Where(s => s.Selected)
                .Select(s => s.SubjectId)
                .ToHashSet();

            model.Subjects.Clear();

            foreach (var subject in availableSubjects)
            {
                model.Subjects.Add(new SubjectSelectionItemViewModel
                {
                    SubjectId = subject.Id,
                    Name = subject.Name,
                    Code = subject.Code,
                    CreditHours = subject.CreditHours,
                    TeacherName = subject.Teacher?.FullName,
                    Selected = selectedIds.Contains(subject.Id)
                });
            }

            ViewBag.CurrentCreditHours = currentCreditHours;
            ViewBag.RemainingCreditHours =
                Math.Max(0, 30 - currentCreditHours);
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

            ViewBag.RemovalRequests = await _context.SubjectRemovalRequests
                .Where(r =>
                    r.StudentId == student.Id &&
                    r.Status == RemovalRequestStatus.Pending)
                .Select(r => r.SubjectId)
                .ToListAsync();

            return View(subjects);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestSubjectRemoval(int subjectId)
        {
            var student = await _userManager.GetUserAsync(User);

            if (student == null)
            {
                return Unauthorized();
            }

            var enrollment = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                .FirstOrDefaultAsync(ss =>
                    ss.StudentId == student.Id &&
                    ss.SubjectId == subjectId &&
                    ss.Status == EnrollmentStatus.Approved);

            if (enrollment == null)
            {
                TempData["ErrorMessage"] =
                    "You are not currently enrolled in this subject.";

                return RedirectToAction(nameof(MySubjects));
            }

            var existingRequest = await _context.SubjectRemovalRequests
                .FirstOrDefaultAsync(r =>
                    r.StudentId == student.Id &&
                    r.SubjectId == subjectId &&
                    r.Status == RemovalRequestStatus.Pending);

            if (existingRequest != null)
            {
                TempData["ErrorMessage"] =
                    "You already have a pending removal request for this subject.";

                return RedirectToAction(nameof(MySubjects));
            }

            _context.SubjectRemovalRequests.Add(
                new SubjectRemovalRequest
                {
                    StudentId = student.Id,
                    SubjectId = subjectId,
                    Status = RemovalRequestStatus.Pending,
                    RequestedAt = DateTime.UtcNow
                });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Your request to remove {enrollment.Subject?.Name} has been submitted for administrator approval.";

            return RedirectToAction(nameof(MySubjects));
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
                ViewBag.GradedComponentIds = new HashSet<int>();
                ViewBag.GradedComponents = 0;
                ViewBag.TotalComponents = 0;
                ViewBag.IsComplete = false;
                ViewBag.FinalPercentage = 0m;
                ViewBag.FinalLetterGrade = "Pending";
                ViewBag.FinalGPA = null;

                return View(new List<StudentGrade>());
            }

            var belongsToStudent = await _context.StudentSubjects
                .AnyAsync(ss =>
                    ss.StudentId == student.Id &&
                    ss.SubjectId == subjectId.Value &&
                    ss.Status == EnrollmentStatus.Approved);

            if (!belongsToStudent)
            {
                ViewBag.GradedComponentIds = new HashSet<int>();
                ViewBag.GradedComponents = 0;
                ViewBag.TotalComponents = 0;
                ViewBag.IsComplete = false;
                ViewBag.FinalPercentage = 0m;
                ViewBag.FinalLetterGrade = "Pending";
                ViewBag.FinalGPA = null;

                return View(new List<StudentGrade>());
            }

            var components = await _context.GradeComponents
                .Where(gc => gc.SubjectId == subjectId.Value)
                .OrderBy(gc => gc.Id)
                .ToListAsync();

            var studentGrades = await _context.StudentGrades
                .Include(g => g.GradeComponent)
                .Where(g =>
                    g.StudentId == student.Id &&
                    g.GradeComponent != null &&
                    g.GradeComponent.SubjectId == subjectId.Value)
                .ToListAsync();

            var gradedComponentIds = studentGrades
                .Select(g => g.GradeComponentId)
                .ToHashSet();

            var grades = new List<StudentGrade>();

            foreach (var component in components)
            {
                var existingGrade = studentGrades
                    .FirstOrDefault(g =>
                        g.GradeComponentId == component.Id);

                if (existingGrade != null)
                {
                    grades.Add(existingGrade);
                }
                else
                {
                    grades.Add(new StudentGrade
                    {
                        StudentId = student.Id,
                        GradeComponentId = component.Id,
                        GradeComponent = component
                    });
                }
            }

            var gradedComponents = gradedComponentIds.Count;
            var totalComponents = components.Count;

            var isComplete =
                totalComponents > 0 &&
                gradedComponents == totalComponents;

            decimal finalPercentage = 0m;

            if (isComplete)
            {
                foreach (var grade in studentGrades)
                {
                    var component = grade.GradeComponent;

                    if (component == null ||
                        component.MaxGrade <= 0)
                    {
                        continue;
                    }

                    var componentPercentage =
                        (grade.Grade / component.MaxGrade) * 100m;

                    var weightedContribution =
                        componentPercentage *
                        (component.WeightPercentage / 100m);

                    finalPercentage += weightedContribution;
                }
            }

            ViewBag.GradedComponentIds = gradedComponentIds;
            ViewBag.GradedComponents = gradedComponents;
            ViewBag.TotalComponents = totalComponents;
            ViewBag.IsComplete = isComplete;
            ViewBag.FinalPercentage = finalPercentage;

            if (isComplete)
            {
                ViewBag.FinalLetterGrade =
                    GetLetterGrade(finalPercentage);

                ViewBag.FinalGPA =
                    GetGpa(finalPercentage);
            }
            else
            {
                ViewBag.FinalLetterGrade = "Pending";
                ViewBag.FinalGPA = null;
            }

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

            var performance =
                new List<StudentPerformanceViewModel>();

            foreach (var subject in subjects)
            {
                var components = await _context.GradeComponents
                    .Where(gc => gc.SubjectId == subject.Id)
                    .OrderBy(gc => gc.Id)
                    .ToListAsync();

                var subjectGrades = grades
                    .Where(g =>
                        g.GradeComponent != null &&
                        g.GradeComponent.SubjectId == subject.Id)
                    .ToList();

                var gradedComponentIds = subjectGrades
                    .Select(g => g.GradeComponentId)
                    .ToHashSet();

                var gradedComponents =
                    components.Count(component =>
                        gradedComponentIds.Contains(component.Id));

                var totalComponents = components.Count;

                var isComplete =
                    totalComponents > 0 &&
                    gradedComponents == totalComponents;

                decimal totalPercentage = 0m;

                if (isComplete)
                {
                    foreach (var grade in subjectGrades)
                    {
                        var component = grade.GradeComponent;

                        if (component == null ||
                            component.MaxGrade <= 0)
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
                }

                var letterGrade = isComplete
                    ? GetLetterGrade(totalPercentage)
                    : "Pending";

                var gpa = isComplete
                    ? GetGpa(totalPercentage)
                    : 0m;

                performance.Add(
                    new StudentPerformanceViewModel
                    {
                        SubjectId = subject.Id,
                        SubjectName = subject.Name,
                        SubjectCode = subject.Code,
                        CreditHours = subject.CreditHours,
                        Percentage = totalPercentage,
                        LetterGrade = letterGrade,
                        GPA = gpa,
                        IsComplete = isComplete,
                        GradedComponents = gradedComponents,
                        TotalComponents = totalComponents
                    });
            }

            var completedPerformance = performance
                .Where(p =>
                    p.IsComplete &&
                    p.CreditHours > 0)
                .ToList();

            decimal totalCredits =
                completedPerformance.Sum(p => p.CreditHours);

            decimal overallGpa = 0m;

            if (totalCredits > 0)
            {
                overallGpa =
                    completedPerformance.Sum(p =>
                        p.GPA * p.CreditHours) /
                    totalCredits;
            }

            ViewBag.OverallGPA = overallGpa;

            ViewBag.OverallLetterGrade =
                completedPerformance.Any()
                    ? GetGpaLetterGrade(overallGpa)
                    : "N/A";

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
        [HttpGet]
        public async Task<IActionResult> Teachers()
        {
            var student = await _userManager.GetUserAsync(User);

            if (student == null)
            {
                return Unauthorized();
            }

            var teachers = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                    .ThenInclude(s => s!.Teacher)
                .Where(ss =>
                    ss.StudentId == student.Id &&
                    ss.Status == EnrollmentStatus.Approved &&
                    ss.Subject != null &&
                    ss.Subject.Teacher != null)
                .Select(ss => ss.Subject!.Teacher!)
                .Distinct()
                .OrderBy(t => t.FullName)
                .ToListAsync();

            return View(teachers);
        }

        [HttpGet]
        public async Task<IActionResult> TeacherProfile(string id)
        {
            var student = await _userManager.GetUserAsync(User);

            if (student == null)
            {
                return Unauthorized();
            }

            var teacher = await _userManager.FindByIdAsync(id);

            if (teacher == null)
            {
                return NotFound();
            }

            var isTeacher = await _userManager.IsInRoleAsync(
                teacher,
                "Teacher");

            if (!isTeacher)
            {
                return NotFound();
            }

            var subjects = await _context.StudentSubjects
                .Include(ss => ss.Subject)
                .Where(ss =>
                    ss.StudentId == student.Id &&
                    ss.Status == EnrollmentStatus.Approved &&
                    ss.Subject != null &&
                    ss.Subject.TeacherId == teacher.Id)
                .Select(ss => new TeacherSubjectViewModel
                {
                    SubjectId = ss.Subject!.Id,
                    Name = ss.Subject.Name,
                    Code = ss.Subject.Code,
                    CreditHours = ss.Subject.CreditHours
                })
                .OrderBy(s => s.Name)
                .ToListAsync();

            if (!subjects.Any())
            {
                return NotFound();
            }

            var model = new TeacherProfileViewModel
            {
                Id = teacher.Id,
                FullName = teacher.FullName,
                Username = teacher.UserName ?? string.Empty,
                Email = teacher.Email ?? string.Empty,
                Subjects = subjects
            };

            return View(model);
        }
    }
}