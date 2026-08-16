using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentGradeApp.Data;
using StudentGradeApp.Models;
using StudentGradeApp.ViewModels;

namespace StudentGradeApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<AppUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CreateTeacher()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(
            CreateTeacherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUsername =
                await _userManager.FindByNameAsync(model.Username);

            if (existingUsername != null)
            {
                ModelState.AddModelError(
                    "Username",
                    "This username is already taken.");

                return View(model);
            }

            var existingEmail =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingEmail != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                return View(model);
            }

            var teacher = new AppUser
            {
                FullName = model.FullName,
                UserName = model.Username,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(
                teacher,
                model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(
                    teacher,
                    "Teacher");

                return RedirectToAction("ManageTeachers");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }
        
        [HttpGet]
        public async Task<IActionResult> ManageTeachers()
        {
            var teachers = await _userManager.GetUsersInRoleAsync("Teacher");

            return View(teachers);
        }
        [HttpGet]
        public async Task<IActionResult> EditTeacher(string id)
        {
            var teacher = await _userManager.FindByIdAsync(id);

            if (teacher == null)
            {
                return NotFound();
            }

            var isTeacher = await _userManager.IsInRoleAsync(teacher, "Teacher");

            if (!isTeacher)
            {
                return NotFound();
            }

            var model = new EditTeacherViewModel
            {
                Id = teacher.Id,
                FullName = teacher.FullName,
                Username = teacher.UserName ?? string.Empty,
                Email = teacher.Email ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher(
            EditTeacherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var teacher = await _userManager.FindByIdAsync(model.Id);

            if (teacher == null)
            {
                return NotFound();
            }

            var isTeacher = await _userManager.IsInRoleAsync(teacher, "Teacher");

            if (!isTeacher)
            {
                return NotFound();
            }

            var existingUsername =
                await _userManager.FindByNameAsync(model.Username);

            if (existingUsername != null &&
                existingUsername.Id != teacher.Id)
            {
                ModelState.AddModelError(
                    "Username",
                    "This username is already taken.");

                return View(model);
            }

            var existingEmail =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingEmail != null &&
                existingEmail.Id != teacher.Id)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                return View(model);
            }

            teacher.FullName = model.FullName;
            teacher.UserName = model.Username;
            teacher.Email = model.Email;

            var result = await _userManager.UpdateAsync(teacher);

            if (result.Succeeded)
            {
                return RedirectToAction("ManageTeachers");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> DeleteTeacher(string id)
        {
            var teacher = await _userManager.FindByIdAsync(id);

            if (teacher == null)
            {
                return NotFound();
            }

            var isTeacher = await _userManager.IsInRoleAsync(teacher, "Teacher");

            if (!isTeacher)
            {
                return NotFound();
            }

            return View(teacher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacherConfirmed(string id)
        {
            var teacher = await _userManager.FindByIdAsync(id);

            if (teacher == null)
            {
                return NotFound();
            }

            var isTeacher = await _userManager.IsInRoleAsync(teacher, "Teacher");

            if (!isTeacher)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(teacher);

            if (result.Succeeded)
            {
                return RedirectToAction("ManageTeachers");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View("DeleteTeacher", teacher);
        }
        [HttpGet]
        public IActionResult CreateStudent()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(
            CreateStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUsername =
                await _userManager.FindByNameAsync(model.Username);

            if (existingUsername != null)
            {
                ModelState.AddModelError(
                    "Username",
                    "This username is already taken.");

                return View(model);
            }

            var existingEmail =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingEmail != null)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                return View(model);
            }

            var student = new AppUser
            {
                FullName = model.FullName,
                UserName = model.Username,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(
                student,
                model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(
                    student,
                    "Student");

                return RedirectToAction("ManageStudents");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> ManageStudents()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");

            return View(students);
        }
        [HttpGet]
        public async Task<IActionResult> EditStudent(string id)
        {
            var student = await _userManager.FindByIdAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            var isStudent = await _userManager.IsInRoleAsync(student, "Student");

            if (!isStudent)
            {
                return NotFound();
            }

            var model = new EditStudentViewModel
            {
                Id = student.Id,
                FullName = student.FullName,
                Username = student.UserName ?? string.Empty,
                Email = student.Email ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(
            EditStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var student = await _userManager.FindByIdAsync(model.Id);

            if (student == null)
            {
                return NotFound();
            }

            var isStudent = await _userManager.IsInRoleAsync(student, "Student");

            if (!isStudent)
            {
                return NotFound();
            }

            var existingUsername =
                await _userManager.FindByNameAsync(model.Username);

            if (existingUsername != null &&
                existingUsername.Id != student.Id)
            {
                ModelState.AddModelError(
                    "Username",
                    "This username is already taken.");

                return View(model);
            }

            var existingEmail =
                await _userManager.FindByEmailAsync(model.Email);

            if (existingEmail != null &&
                existingEmail.Id != student.Id)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                return View(model);
            }

            student.FullName = model.FullName;
            student.UserName = model.Username;
            student.Email = model.Email;

            var result = await _userManager.UpdateAsync(student);

            if (result.Succeeded)
            {
                return RedirectToAction("ManageStudents");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }
        public async Task<IActionResult> DeleteStudent(string id)
        {
            var student = await _userManager.FindByIdAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            var isStudent = await _userManager.IsInRoleAsync(student, "Student");

            if (!isStudent)
            {
                return NotFound();
            }

            return View(student);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudentConfirmed(string id)
        {
            var student = await _userManager.FindByIdAsync(id);

            if (student == null)
            {
                return NotFound();
            }

            var isStudent = await _userManager.IsInRoleAsync(student, "Student");

            if (!isStudent)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(student);

            if (result.Succeeded)
            {
                return RedirectToAction("ManageStudents");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View("DeleteStudent", student);
        }
        [HttpGet]
        public IActionResult CreateSubject()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSubject(
            CreateSubjectViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingCode = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Code == model.Code);

            if (existingCode != null)
            {
                ModelState.AddModelError(
                    "Code",
                    "This subject code is already in use.");

                return View(model);
            }

            var subject = new Subject
            {
                Name = model.Name,
                Code = model.Code,
                CreditHours = model.CreditHours,
                Description = model.Description
            };

            _context.Subjects.Add(subject);

            await _context.SaveChangesAsync();

            return RedirectToAction("ManageSubjects");
        }
        [HttpGet]
        public async Task<IActionResult> ManageSubjects()
        {
            var subjects = await _context.Subjects
                .Include(s => s.Teacher)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(subjects);
        }
        [HttpGet]
        public async Task<IActionResult> EditSubject(int id)
        {
            var subject = await _context.Subjects.FindAsync(id);

            if (subject == null)
            {
                return NotFound();
            }

            var model = new EditSubjectViewModel
            {
                Id = subject.Id,
                Name = subject.Name,
                Code = subject.Code,
                CreditHours = subject.CreditHours,
                Description = subject.Description
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSubject(
            EditSubjectViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var subject = await _context.Subjects.FindAsync(model.Id);

            if (subject == null)
            {
                return NotFound();
            }

            var existingCode = await _context.Subjects
                .FirstOrDefaultAsync(s =>
                    s.Code == model.Code &&
                    s.Id != model.Id);

            if (existingCode != null)
            {
                ModelState.AddModelError(
                    "Code",
                    "This subject code is already in use.");

                return View(model);
            }

            subject.Name = model.Name;
            subject.Code = model.Code;
            subject.CreditHours = model.CreditHours;
            subject.Description = model.Description;

            await _context.SaveChangesAsync();

            return RedirectToAction("ManageSubjects");
        }
        [HttpGet]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            var subject = await _context.Subjects
                .Include(s => s.Teacher)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
            {
                return NotFound();
            }

            return View(subject);
        }

        [HttpPost, ActionName("DeleteSubject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSubjectConfirmed(int id)
        {
            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subject == null)
            {
                return NotFound();
            }

            _context.Subjects.Remove(subject);

            await _context.SaveChangesAsync();

            return RedirectToAction("ManageSubjects");
        }
        [HttpGet]
        public async Task<IActionResult> AssignTeacher()
        {
            var subjects = await _context.Subjects
                .OrderBy(s => s.Name)
                .ToListAsync();

            var teachers = await _userManager.GetUsersInRoleAsync("Teacher");

            ViewBag.Subjects = subjects;
            ViewBag.Teachers = teachers;

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTeacher(
    AssignTeacherViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var subjects = await _context.Subjects
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var teachers = await _userManager
                    .GetUsersInRoleAsync("Teacher");

                ViewBag.Subjects = subjects;
                ViewBag.Teachers = teachers;

                return View(model);
            }

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == model.SubjectId);

            if (subject == null)
            {
                return NotFound();
            }

            var teacher = await _userManager
                .FindByIdAsync(model.TeacherId);

            if (teacher == null)
            {
                ModelState.AddModelError(
                    "TeacherId",
                    "The selected teacher could not be found.");

                var subjects = await _context.Subjects
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var teachers = await _userManager
                    .GetUsersInRoleAsync("Teacher");

                ViewBag.Subjects = subjects;
                ViewBag.Teachers = teachers;

                return View(model);
            }

            var isTeacher = await _userManager
                .IsInRoleAsync(teacher, "Teacher");

            if (!isTeacher)
            {
                ModelState.AddModelError(
                    "TeacherId",
                    "The selected user is not a teacher.");

                var subjects = await _context.Subjects
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var teachers = await _userManager
                    .GetUsersInRoleAsync("Teacher");

                ViewBag.Subjects = subjects;
                ViewBag.Teachers = teachers;

                return View(model);
            }

            subject.TeacherId = teacher.Id;

            await _context.SaveChangesAsync();

            return RedirectToAction("ManageSubjects");
        }
        [HttpGet]
        public async Task<IActionResult> SubjectRequests()
        {
            var requests = await _context.StudentSubjects
                .Include(ss => ss.Student)
                .Include(ss => ss.Subject)
                    .ThenInclude(s => s!.Teacher)
                .Where(ss => ss.Status == EnrollmentStatus.Pending)
                .OrderBy(ss => ss.RequestedAt)
                .ToListAsync();

            return View(requests);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSubject(int id)
        {
            var request = await _context.StudentSubjects
                .FirstOrDefaultAsync(ss => ss.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            request.Status = EnrollmentStatus.Approved;
            request.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(SubjectRequests));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectSubject(int id)
        {
            var request = await _context.StudentSubjects
                .FirstOrDefaultAsync(ss => ss.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            request.Status = EnrollmentStatus.Rejected;
            request.ApprovedAt = null;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(SubjectRequests));
        }

        [HttpGet]
        public async Task<IActionResult> GradeComponents(int? subjectId)
        {
            var subjects = await _context.Subjects
                .OrderBy(s => s.Name)
                .ToListAsync();

            ViewBag.Subjects = subjects;

            if (subjectId == null)
            {
                return View(new List<GradeComponent>());
            }

            var components = await _context.GradeComponents
                .Include(g => g.Subject)
                .Where(g => g.SubjectId == subjectId)
                .OrderBy(g => g.Id)
                .ToListAsync();

            ViewBag.SelectedSubjectId = subjectId;

            return View(components);
        }


        [HttpGet]
        public async Task<IActionResult> CreateGradeComponent(int subjectId)
        {
            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == subjectId);

            if (subject == null)
            {
                return NotFound();
            }

            ViewBag.Subject = subject;

            return View(new GradeComponent
            {
                SubjectId = subjectId
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGradeComponent(
            GradeComponent model)
        {
            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.Id == model.SubjectId);

            if (subject == null)
            {
                return NotFound();
            }

            var currentWeight = await _context.GradeComponents
                .Where(g => g.SubjectId == model.SubjectId)
                .SumAsync(g => g.WeightPercentage);

            if (currentWeight + model.WeightPercentage > 100)
            {
                ModelState.AddModelError(
                    "WeightPercentage",
                    $"The total weight cannot exceed 100%. " +
                    $"The subject currently has {currentWeight}% assigned."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Subject = subject;
                return View(model);
            }

            _context.GradeComponents.Add(model);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(GradeComponents),
                new { subjectId = model.SubjectId }
            );
        }


        [HttpGet]
        public async Task<IActionResult> EditGradeComponent(int id)
        {
            var component = await _context.GradeComponents
                .Include(g => g.Subject)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (component == null)
            {
                return NotFound();
            }

            ViewBag.Subject = component.Subject;

            return View(component);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGradeComponent(
            int id,
            GradeComponent model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var existing = await _context.GradeComponents
                .FirstOrDefaultAsync(g => g.Id == id);

            if (existing == null)
            {
                return NotFound();
            }

            var currentWeight = await _context.GradeComponents
                .Where(g =>
                    g.SubjectId == model.SubjectId &&
                    g.Id != model.Id)
                .SumAsync(g => g.WeightPercentage);

            if (currentWeight + model.WeightPercentage > 100)
            {
                ModelState.AddModelError(
                    "WeightPercentage",
                    $"The total weight cannot exceed 100%. " +
                    $"The other components currently use {currentWeight}%."
                );
            }

            if (!ModelState.IsValid)
            {
                var subject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.Id == model.SubjectId);

                ViewBag.Subject = subject;

                return View(model);
            }

            existing.Name = model.Name;
            existing.MaxGrade = model.MaxGrade;
            existing.WeightPercentage = model.WeightPercentage;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(GradeComponents),
                new { subjectId = model.SubjectId }
            );
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGradeComponent(int id)
        {
            var component = await _context.GradeComponents
                .FirstOrDefaultAsync(g => g.Id == id);

            if (component == null)
            {
                return NotFound();
            }

            var subjectId = component.SubjectId;

            _context.GradeComponents.Remove(component);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(GradeComponents),
                new { subjectId }
            );
        }

    }
}

