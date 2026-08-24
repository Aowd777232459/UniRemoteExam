using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Areas.Control.ViewModels;
using UniRemoteExam.Data;
using UniRemoteExam.Filters;
using UniRemoteExam.Services;

namespace UniRemoteExam.Areas.Control.Controllers;

[Area("Control")]
[RequireRole("Control")]
public class StudentsController : Controller
{
    private readonly UniRemoteExamDbContext _db;
    private readonly PasswordService _passwords;
    public StudentsController(UniRemoteExamDbContext db, PasswordService passwords) { _db = db; _passwords = passwords; }

    public async Task<IActionResult> Index()
    {
        var students = await _db.Users.Include(u => u.Role).Include(u => u.StudentProfile)
            .Where(u => u.Role.RoleName == "Student").OrderByDescending(u => u.CreatedAt).Take(100).ToListAsync();
        return View(new StudentProvisionVm
        {
            Students = students.Select(u => new StudentAccountRow
            {
                FullName = u.FullName ?? "-", Email = u.Email, StudentNumber = u.StudentProfile?.StudentNumber ?? "-", IsActive = u.IsActive, CreatedAt = u.CreatedAt
            }).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string fullName, string password, string? studentNumber, string? level)
    {
        fullName = (fullName ?? string.Empty).Trim();
        password ??= string.Empty;
        if (string.IsNullOrWhiteSpace(fullName) || password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            TempData["Error"] = "الاسم مطلوب، وكلمة المرور المؤقتة يجب أن تكون 8 أحرف وتحتوي على حروف وأرقام.";
            return RedirectToAction(nameof(Index));
        }
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Student");
        if (role == null) return BadRequest("دور الطالب غير موجود.");
        if (!string.IsNullOrWhiteSpace(studentNumber) && await _db.StudentProfiles.AnyAsync(s => s.StudentNumber == studentNumber.Trim()))
        {
            TempData["Error"] = "الرقم الجامعي مستخدم مسبقًا.";
            return RedirectToAction(nameof(Index));
        }

        var tempEmail = $"tmp_{Guid.NewGuid():N}@temp.local";
        var user = new User
        {
            FullName = fullName, Email = tempEmail, RoleId = role.RoleId, IsActive = true, MustChangePassword = true,
            CreatedAt = YemenTime.UtcNow, PasswordHash = _passwords.Hash(new User { Email = tempEmail }, password)
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        user.Email = $"st{user.UserId:D5}@exam.su.edu.ye";
        user.PasswordHash = _passwords.Hash(user, password);
        _db.StudentProfiles.Add(new StudentProfile
        {
            StudentId = user.UserId,
            StudentNumber = string.IsNullOrWhiteSpace(studentNumber) ? $"SU{DateTime.UtcNow.Year}{user.UserId:D5}" : studentNumber.Trim(),
            Level = string.IsNullOrWhiteSpace(level) ? null : level.Trim()
        });
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = HttpContext.Session.GetInt32("UserId"), Action = "Control.Student.Create",
            Details = $"StudentId={user.UserId}; Email={user.Email}", CreatedAt = YemenTime.UtcNow
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم إنشاء الحساب. كلمة المرور تظهر مرة واحدة فقط ويجب على الطالب تغييرها عند أول دخول.";
        TempData["GeneratedName"] = user.FullName;
        TempData["GeneratedEmail"] = user.Email;
        TempData["GeneratedPassword"] = password;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ExportCsv()
    {
        var students = await _db.Users.Include(u => u.Role).Include(u => u.StudentProfile)
            .Where(u => u.Role.RoleName == "Student").OrderBy(u => u.FullName).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("StudentNumber,FullName,Email,IsActive,CreatedAt");
        foreach (var s in students)
            sb.AppendLine($"{Csv(s.StudentProfile?.StudentNumber ?? "-")},{Csv(s.FullName ?? "-")},{Csv(s.Email)},{s.IsActive},{s.CreatedAt:yyyy-MM-dd HH:mm}");
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"student_accounts_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
    }

    private static string Csv(string? value)
    {
        var safe = (value ?? string.Empty).Replace("\"", "\"\"");
        return "\"" + safe + "\"";
    }
}
