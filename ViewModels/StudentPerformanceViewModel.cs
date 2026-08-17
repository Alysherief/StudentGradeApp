namespace StudentGradeApp.ViewModels
{
    public class StudentPerformanceViewModel
    {
        public int SubjectId { get; set; }

        public string SubjectName { get; set; } = string.Empty;

        public string SubjectCode { get; set; } = string.Empty;

        public int CreditHours { get; set; }

        public decimal Percentage { get; set; }

        public string LetterGrade { get; set; } = string.Empty;

        public decimal GPA { get; set; }

        public bool IsComplete { get; set; }

        public int GradedComponents { get; set; }

        public int TotalComponents { get; set; }
    }
}