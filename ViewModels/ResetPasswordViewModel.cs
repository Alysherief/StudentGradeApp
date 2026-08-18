using System.ComponentModel.DataAnnotations;

namespace StudentGradeApp.ViewModels
{
    public class ResetPasswordViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string Token { get; set; } = string.Empty;

        public string ResetKey { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The passwords do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}