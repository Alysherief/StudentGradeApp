using System.ComponentModel.DataAnnotations;

namespace StudentGradeApp.ViewModels
{
    public class AssignTeacherViewModel
    {
        [Required]
        public int SubjectId { get; set; }

        [Required]
        [Display(Name = "Teacher")]
        public string TeacherId { get; set; } = string.Empty;
    }
}
