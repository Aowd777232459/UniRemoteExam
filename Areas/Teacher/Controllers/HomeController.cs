using Microsoft.AspNetCore.Mvc;
using UniRemoteExam.Filters;

namespace UniRemoteExam.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    [RequireRole("Teacher")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Exams");
        }
    }
}