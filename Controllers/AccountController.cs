using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentGradeApp.Interfaces;
using StudentGradeApp.Models;
using StudentGradeApp.ViewModels;

namespace StudentGradeApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
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
                ViewBag.Error =
                    "Please enter your username/email and password.";

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
                ViewBag.Error =
                    "Invalid username/email or password.";

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

                ViewBag.Error =
                    "Your account does not have a valid role.";

                return View();
            }

            if (result.IsLockedOut)
            {
                ViewBag.Error =
                    "Your account is temporarily locked.";

                return View();
            }

            ViewBag.Error =
                "Invalid username/email or password.";

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

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            string currentPassword,
            string newPassword,
            string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(currentPassword) ||
                string.IsNullOrWhiteSpace(newPassword) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                ViewBag.Error =
                    "Please fill in all fields.";

                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error =
                    "The new passwords do not match.";

                return View();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                currentPassword,
                newPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                TempData["SuccessMessage"] =
                    "Your password has been changed successfully.";

                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                TempData["PasswordResetMessage"] =
                    "If an account with that email exists, a password reset link has been sent.";

                return RedirectToAction(nameof(ForgotPassword));
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("Student") &&
                !roles.Contains("Teacher"))
            {
                TempData["PasswordResetMessage"] =
                    "If an account with that email exists, a password reset link has been sent.";

                return RedirectToAction(nameof(ForgotPassword));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var encodedToken =
                System.Net.WebUtility.UrlEncode(token);

            var resetUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new
                {
                    userId = user.Id,
                    token = encodedToken
                },
                Request.Scheme);

            if (resetUrl == null)
            {
                return View(model);
            }

            var htmlMessage = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif; background-color: #f5f7fb; padding: 30px;'>
    <div style='max-width: 600px; margin: auto; background: white; padding: 35px; border-radius: 15px;'>
        <h2 style='color: #0d6efd;'>Password Reset</h2>

        <p>Hello {System.Net.WebUtility.HtmlEncode(user.FullName)},</p>

        <p>
            We received a request to reset the password for your
            StudentGradeApp account.
        </p>

        <p>
            Click the button below to create a new password:
        </p>

        <p style='text-align: center; margin: 30px 0;'>
            <a href='{resetUrl}'
               style='background-color: #0d6efd;
                      color: white;
                      padding: 12px 25px;
                      text-decoration: none;
                      border-radius: 8px;
                      font-weight: bold;'>
                Reset Password
            </a>
        </p>

        <p>
            If you did not request a password reset, you can safely
            ignore this email.
        </p>

        <p>
            This link will only work for your account.
        </p>

        <hr>

        <p style='color: #777; font-size: 13px;'>
            StudentGradeApp
        </p>
    </div>
</body>
</html>";

            await _emailService.SendEmailAsync(
                user.Email!,
                "StudentGradeApp - Password Reset",
                htmlMessage);

            TempData["PasswordResetMessage"] =
                "If an account with that email exists, a password reset link has been sent.";

            return RedirectToAction(nameof(ForgotPassword));
        }

        [HttpGet]
        public IActionResult ResetPassword(
            string userId,
            string token)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(token))
            {
                return RedirectToAction(nameof(Login));
            }

            var model = new ResetPasswordViewModel
            {
                UserId = userId,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);

            if (user == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The password reset link is invalid.");

                return View(model);
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (!roles.Contains("Student") &&
                !roles.Contains("Teacher"))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The password reset link is invalid.");

                return View(model);
            }

            var decodedToken =
                System.Net.WebUtility.UrlDecode(model.Token);

            var result = await _userManager.ResetPasswordAsync(
                user,
                decodedToken,
                model.NewPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] =
                    "Your password has been reset successfully. You can now log in.";

                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(model);
        }
    }
}