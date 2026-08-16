using System.ComponentModel.DataAnnotations;

namespace StudentGradeApp.Models
{
    public class StudentSubject
    {
        public int Id { get; set; }

        public string StudentId { get; set; } = string.Empty;

        public AppUser? Student { get; set; }

        public int SubjectId { get; set; }

        public Subject? Subject { get; set; }

        [Required]
        public EnrollmentStatus Status { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }
    }

    public enum EnrollmentStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
