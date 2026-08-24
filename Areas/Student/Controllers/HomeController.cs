using Microsoft.AspNetCore.Mvc;
using UniRemoteExam.Filters;

namespace UniRemoteExam.Areas.Student.Controllers
{
    [Area("Student")]
    [RequireRole("Student")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}