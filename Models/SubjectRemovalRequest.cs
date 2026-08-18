using System.ComponentModel.DataAnnotations;

namespace StudentGradeApp.Models
{
    public class SubjectRemovalRequest
    {
        public int Id { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        public AppUser? Student { get; set; }

        [Required]
        public int SubjectId { get; set; }

        public Subject? Subject { get; set; }

        [Required]
        public RemovalRequestStatus Status { get; set; }
            = RemovalRequestStatus.Pending;

        public DateTime RequestedAt { get; set; }
            = DateTime.UtcNow;

        public DateTime? ProcessedAt { get; set; }
    }

    public enum RemovalRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
