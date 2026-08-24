using Microsoft.AspNetCore.Mvc;
using UniRemoteExam.Filters;

namespace UniRemoteExam.Areas.Control.Controllers
{
    [Area("Control")]
    [RequireRole("Control")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Attempts");
        }
    }
}