namespace StudentGradeApp.ViewModels
{
    public class TeacherProfileViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public List<TeacherSubjectViewModel> Subjects { get; set; } = new();
    }

    public class TeacherSubjectViewModel
    {
        public int SubjectId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public int CreditHours { get; set; }
    }
}