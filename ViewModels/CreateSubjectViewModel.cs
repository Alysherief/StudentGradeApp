using System.ComponentModel.DataAnnotations;

namespace StudentGradeApp.ViewModels
{
    public class CreateSubjectViewModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Subject Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Subject Code")]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Range(1, 10)]
        [Display(Name = "Credit Hours")]
        public int CreditHours { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
    }
}
