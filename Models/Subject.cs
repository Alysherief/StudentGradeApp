using System.ComponentModel.DataAnnotations;

namespace StudentGradeApp.Models
{
    public class Subject
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Range(1, 10)]
        [Display(Name = "Credit Hours")]
        public int CreditHours { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public string? TeacherId { get; set; }

        public AppUser? Teacher { get; set; }

        public ICollection<GradeComponent> GradeComponents { get; set; }
            = new List<GradeComponent>();
    }
}