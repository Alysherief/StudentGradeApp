
using Microsoft.AspNetCore.Identity;

namespace StudentGradeApp.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}