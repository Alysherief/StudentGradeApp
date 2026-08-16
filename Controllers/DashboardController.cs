using Microsoft.AspNetCore.Mvc;

namespace StudentGradeApp.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
