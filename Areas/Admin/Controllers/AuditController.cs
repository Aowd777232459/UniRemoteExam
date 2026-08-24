using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Areas.Admin.ViewModels;
using UniRemoteExam.Data;
using UniRemoteExam.Filters;

namespace UniRemoteExam.Areas.Admin.Controllers;

[Area("Admin")]
[RequireRole("Admin")]
public class AuditController : Controller
{
    private readonly UniRemoteExamDbContext _db;

    public AuditController(UniRemoteExamDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? actionFilter)
    {
        var query = _db.AuditLogs
            .Include(a => a.ActorUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(a =>
                a.Action.Contains(value) ||
                (a.Details != null && a.Details.Contains(value)) ||
                (a.ActorUser != null && a.ActorUser.FullName != null && a.ActorUser.FullName.Contains(value)) ||
                (a.ActorUser != null && a.ActorUser.Email.Contains(value)));
        }

        if (!string.IsNullOrWhiteSpace(actionFilter) && actionFilter != "All")
        {
            query = query.Where(a => a.Action.Contains(actionFilter));
        }

        var today = DateTime.Today;

        var allLogs = await _db.AuditLogs.ToListAsync();

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(250)
            .Select(a => new AuditItemVm
            {
                AuditId = a.AuditId,
                ActorName = a.ActorUser != null ? (a.ActorUser.FullName ?? "-") : "النظام",
                ActorEmail = a.ActorUser != null ? a.ActorUser.Email : "-",
                Action = a.Action,
                Details = a.Details ?? "-",
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        var vm = new AuditPageVm
        {
            Search = search,
            ActionFilter = actionFilter ?? "All",
            TotalLogs = allLogs.Count,
            TodayLogs = allLogs.Count(a => a.CreatedAt.Date == today),
            UserActions = allLogs.Count(a => a.Action.Contains("User") || a.Action.Contains("مستخدم") || a.Action.Contains("حساب")),
            ApprovalActions = allLogs.Count(a => a.Action.Contains("Approve") || a.Action.Contains("Reject") || a.Action.Contains("نشر") || a.Action.Contains("رفض")),
            Logs = logs
        };

        return View(vm);
    }
}
