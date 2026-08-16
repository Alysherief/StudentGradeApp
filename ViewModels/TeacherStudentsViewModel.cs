using StudentGradeApp.Models;

namespace StudentGradeApp.ViewModels
{
    public class TeacherStudentsViewModel
    {
        public int? SelectedSubjectId { get; set; }

        public List<TeacherSubjectOptionViewModel> Subjects { get; set; }
            = new List<TeacherSubjectOptionViewModel>();

        public List<TeacherStudentViewModel> Students { get; set; }
            = new List<TeacherStudentViewModel>();
    }

    public class TeacherSubjectOptionViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
    }

    public class TeacherStudentViewModel
    {
        public string StudentId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public double AverageGrade { get; set; }

        public int GradeCount { get; set; }
    }
}