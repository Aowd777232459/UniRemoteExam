using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Data;
using UniRemoteExam.Filters;
using UniRemoteExam.Services;

namespace UniRemoteExam.Areas.Admin.Controllers;

[Area("Admin")]
[RequireRole("Admin")]
public class SystemHealthController : Controller
{
    private readonly UniRemoteExamDbContext _db;
    private readonly IWebHostEnvironment _env;

    public SystemHealthController(UniRemoteExamDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var model = new Dictionary<string, string>();
        model["قاعدة البيانات"] = await _db.Database.CanConnectAsync() ? "متصلة" : "غير متصلة";
        model["عدد المستخدمين"] = (await _db.Users.CountAsync()).ToString();
        model["عدد الاختبارات"] = (await _db.Exams.CountAsync()).ToString();
        model["عدد المحاولات"] = (await _db.ExamAttempts.CountAsync()).ToString();
        model["طلبات النشر المعلقة"] = (await _db.ExamPublishRequests.CountAsync(r => r.Status == "Pending")).ToString();
        model["أحداث المراقبة"] = (await _db.ProctorEvents.CountAsync()).ToString();
        model["سجلات العمليات"] = (await _db.AuditLogs.CountAsync()).ToString();
        model["مسار المشروع"] = _env.ContentRootPath;
        model["البيئة"] = _env.EnvironmentName;
        model["وقت الفحص"] = YemenTime.ToLocal(YemenTime.UtcNow).ToString("yyyy/MM/dd hh:mm tt");
        return View(model);
    }
}
