using System.ComponentModel.DataAnnotations;

namespace StudentGradeApp.Models
{
    public class StudentGrade
    {
        public int Id { get; set; }

        
        [Required]
        public string StudentId { get; set; } = string.Empty;

        public AppUser? Student { get; set; }


        
        public int GradeComponentId { get; set; }

        public GradeComponent? GradeComponent { get; set; }


       
        [Required]
        [Range(0, 100)]
        [Display(Name = "Grade")]
        public decimal Grade { get; set; }


        public DateTime GradedAt { get; set; } = DateTime.UtcNow;
    }
}