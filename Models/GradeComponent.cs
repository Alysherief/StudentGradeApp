using System.ComponentModel.DataAnnotations;

namespace StudentGradeApp.Models
{
    public class GradeComponent
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(1, 100)]
        [Display(Name = "Maximum Grade")]
        public decimal MaxGrade { get; set; }

        [Required]
        [Range(0, 100)]
        [Display(Name = "Weight Percentage")]
        public decimal WeightPercentage { get; set; }

        public int SubjectId { get; set; }

        public Subject? Subject { get; set; }
    }
}
