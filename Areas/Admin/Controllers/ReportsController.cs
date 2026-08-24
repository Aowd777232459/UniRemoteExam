using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Areas.Admin.ViewModels;
using UniRemoteExam.Data;
using UniRemoteExam.Filters;

namespace UniRemoteExam.Areas.Admin.Controllers;

[Area("Admin")]
[RequireRole("Admin")]
public class ReportsController : Controller
{
    private readonly UniRemoteExamDbContext _db;
    public ReportsController(UniRemoteExamDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var exams = await _db.Exams.Include(e => e.Teacher).Include(e => e.Course)
            .OrderByDescending(e => e.CreatedAt).ToListAsync();
        ViewBag.TotalStudents = await _db.Users.Include(u => u.Role).CountAsync(u => u.Role.RoleName == "Student");
        ViewBag.TotalTeachers = await _db.Users.Include(u => u.Role).CountAsync(u => u.Role.RoleName == "Teacher");
        ViewBag.TotalControl = await _db.Users.Include(u => u.Role).CountAsync(u => u.Role.RoleName == "Control");
        ViewBag.TotalExams = await _db.Exams.CountAsync();
        ViewBag.PublishedExams = await _db.Exams.CountAsync(e => e.Status == "Published");
        ViewBag.TotalAttempts = await _db.ExamAttempts.CountAsync();
        ViewBag.StartedAttempts = await _db.ExamAttempts.CountAsync(a => a.Status == "Started");
        ViewBag.SubmittedAttempts = await _db.ExamAttempts.CountAsync(a => a.Status == "Submitted");
        ViewBag.ClosedAttempts = await _db.ExamAttempts.CountAsync(a => a.Status == "Closed");
        ViewBag.PendingRequests = await _db.ExamPublishRequests.CountAsync(r => r.Status == "Pending");
        ViewBag.ApprovedRequests = await _db.ExamPublishRequests.CountAsync(r => r.Status == "Approved");
        ViewBag.RejectedRequests = await _db.ExamPublishRequests.CountAsync(r => r.Status == "Rejected");
        return View(exams);
    }

    public async Task<IActionResult> Exam(int id)
    {
        var exam = await _db.Exams.Include(e => e.Course).FirstOrDefaultAsync(e => e.ExamId == id);
        if (exam == null) return NotFound();
        var attempts = await _db.ExamAttempts.Include(a => a.Student)
            .Where(a => a.ExamId == id).OrderByDescending(a => a.SubmittedAt).ToListAsync();
        var maxScore = await _db.Questions.Where(q => q.ExamId == id).SumAsync(q => (decimal?)q.Points) ?? 0m;
        var rows = attempts.Select(a => new ResultRow
        {
            AttemptId = a.AttemptId,
            StudentName = a.Student.FullName ?? "-",
            StudentEmail = a.Student.Email,
            Status = a.Status,
            AutoScore = a.AutoScore ?? 0,
            ManualScore = a.ManualScore ?? 0,
            TotalScore = a.FinalScore ?? 0,
            MaximumScore = a.MaximumScore ?? maxScore,
            Percentage = a.Percentage ?? 0,
            IsPassed = a.IsPassed,
            SubmittedAt = a.SubmittedAt,
            FinalizedAt = a.FinalizedAt
        }).ToList();

        var closed = rows.Where(r => r.Status == "Closed").ToList();
        var vm = new ExamReportVm
        {
            ExamId = exam.ExamId,
            ExamTitle = exam.Title,
            CourseName = exam.Course?.Name ?? exam.CourseName,
            MaxScore = maxScore,
            PassPercent = exam.PassPercentage,
            PassMark = Math.Round(maxScore * exam.PassPercentage / 100m, 2),
            StartedCount = rows.Count(r => r.Status == "Started"),
            SubmittedCount = rows.Count(r => r.Status == "Submitted"),
            ClosedCount = closed.Count,
            AvgScore = closed.Count == 0 ? 0 : Math.Round(closed.Average(r => r.TotalScore), 2),
            MaxScoreAchieved = closed.Count == 0 ? 0 : closed.Max(r => r.TotalScore),
            MinScoreAchieved = closed.Count == 0 ? 0 : closed.Min(r => r.TotalScore),
            PassCount = closed.Count(r => r.IsPassed == true),
            FailCount = closed.Count(r => r.IsPassed == false),
            PassRatePercent = closed.Count == 0 ? 0 : (int)Math.Round(closed.Count(r => r.IsPassed == true) * 100.0 / closed.Count),
            Rows = rows.OrderByDescending(r => r.Percentage).ToList(),
            Top5 = closed.OrderByDescending(r => r.Percentage).Take(5).ToList()
        };
        return View(vm);
    }

    public async Task<IActionResult> ExportExamCsv(int id)
    {
        var exam = await _db.Exams.Include(e => e.Course).FirstOrDefaultAsync(e => e.ExamId == id);
        if (exam == null) return NotFound();
        var attempts = await _db.ExamAttempts.Include(a => a.Student).ThenInclude(s => s.StudentProfile)
            .Where(a => a.ExamId == id).OrderBy(a => a.Student.FullName).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("StudentNumber,StudentName,Email,Status,AutoScore,ManualScore,FinalScore,MaximumScore,Percentage,Result,SubmittedAt,FinalizedAt");
        foreach (var a in attempts)
        {
            sb.AppendLine(string.Join(',', new[]
            {
                Csv(a.Student.StudentProfile?.StudentNumber ?? "-"), Csv(a.Student.FullName ?? "-"), Csv(a.Student.Email), Csv(a.Status),
                (a.AutoScore ?? 0).ToString("0.##"), (a.ManualScore ?? 0).ToString("0.##"), (a.FinalScore ?? 0).ToString("0.##"),
                (a.MaximumScore ?? 0).ToString("0.##"), (a.Percentage ?? 0).ToString("0.##"), Csv(a.IsPassed == true ? "ناجح" : a.IsPassed == false ? "راسب" : "قيد المعالجة"),
                Csv(a.SubmittedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-"), Csv(a.FinalizedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-")
            }));
        }
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"Exam_{id}_Results.csv");
    }

    public async Task<IActionResult> ProctorEvents(int attemptId)
    {
        return View(await _db.ProctorEvents.Include(e => e.Student).Include(e => e.Question)
            .Where(e => e.AttemptId == attemptId).OrderByDescending(e => e.CreatedAt).ToListAsync());
    }

    private static string Csv(string? value)
    {
        var safe = (value ?? string.Empty).Replace("\"", "\"\"");
        return "\"" + safe + "\"";
    }
}
