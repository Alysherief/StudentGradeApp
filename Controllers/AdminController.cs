using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentGradeApp.Models;
using StudentGradeApp.ViewModels;

namespace StudentGradeApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public AdminController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
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

    }
}

