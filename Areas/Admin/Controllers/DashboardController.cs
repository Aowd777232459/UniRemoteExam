using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Data;
using UniRemoteExam.Filters;

namespace UniRemoteExam.Areas.Admin.Controllers
{
    [Area("Admin")]
    [RequireRole("Admin")]
    public class DashboardController : Controller
    {
        private readonly UniRemoteExamDbContext _db;

        public DashboardController(UniRemoteExamDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Stats()
        {
            var liveTaking = await _db.ExamAttempts.CountAsync(a => a.Status == "Started");
            var submitted = await _db.ExamAttempts.CountAsync(a => a.Status == "Submitted");
            var closed = await _db.ExamAttempts.CountAsync(a => a.Status == "Closed");

            var students = await _db.Users
                .Include(u => u.Role)
                .CountAsync(u => u.IsActive == true && u.Role.RoleName == "Student");

            var teachers = await _db.Users
                .Include(u => u.Role)
                .CountAsync(u => u.IsActive == true && u.Role.RoleName == "Teacher");

            var totalExams = await _db.Exams.CountAsync();
            var publishedExams = await _db.Exams.CountAsync(e => e.IsPublished == true);
            var pendingPublish = await _db.ExamPublishRequests.CountAsync(r => r.Status == "Pending");

            var livePercent = students == 0 ? 0 : (int)Math.Round((liveTaking * 100.0) / students);

            return Json(new
            {
                liveTaking,
                submitted,
                closed,
                students,
                teachers,
                totalExams,
                publishedExams,
                pendingPublish,
                livePercent
            });
        }
    }
}
