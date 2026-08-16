using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentGradeApp.Models;

namespace StudentGradeApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        
    }
        public DbSet<Subject> Subjects { get; set; }

        public DbSet<GradeComponent> GradeComponents { get; set; }

        public DbSet<StudentSubject> StudentSubjects { get; set; }
        public DbSet<StudentGrade> StudentGrades { get; set; }
    }
}
