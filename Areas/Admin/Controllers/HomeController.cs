using Microsoft.AspNetCore.Mvc;
using UniRemoteExam.Filters;

namespace UniRemoteExam.Areas.Admin.Controllers
{
    [Area("Admin")]
    [RequireRole("Admin")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
    }
}
