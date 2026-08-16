using System.ComponentModel.DataAnnotations;

namespace StudentGradeApp.ViewModels
{
    public class SelectSubjectsViewModel
    {
        public List<SubjectSelectionItemViewModel> Subjects { get; set; }
            = new List<SubjectSelectionItemViewModel>();

        public int TotalCreditHours { get; set; }
    }

    public class SubjectSelectionItemViewModel
    {
        public int SubjectId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public int CreditHours { get; set; }

        public string? TeacherName { get; set; }

        public bool Selected { get; set; }
    }
}
