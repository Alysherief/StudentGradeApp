using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentGradeApp.Models;

namespace StudentGradeApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            string usernameOrEmail,
            string password,
            bool rememberMe)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) ||
                string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Please enter your username/email and password.";
                return View();
            }

            AppUser? user;

            if (usernameOrEmail.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(usernameOrEmail);
            }
            else
            {
                user = await _userManager.FindByNameAsync(usernameOrEmail);
            }

            if (user == null)
            {
                ViewBag.Error = "Invalid username/email or password.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                password,
                rememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Admin"))
                {
                    return RedirectToAction("Index", "Home");
                }

                if (roles.Contains("Teacher"))
                {
                    return RedirectToAction("Index", "Home");
                }

                if (roles.Contains("Student"))
                {
                    return RedirectToAction("Index", "Home");
                }

                await _signInManager.SignOutAsync();

                ViewBag.Error = "Your account does not have a valid role.";
                return View();
            }

            if (result.IsLockedOut)
            {
                ViewBag.Error = "Your account is temporarily locked.";
                return View();
            }

            ViewBag.Error = "Invalid username/email or password.";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            return View(user);
        }
    }
}
