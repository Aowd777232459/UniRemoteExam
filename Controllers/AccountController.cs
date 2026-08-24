using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using UniRemoteExam.Data;
using UniRemoteExam.Services;

namespace UniRemoteExam.Controllers;

public class AccountController : Controller
{
    private readonly UniRemoteExamDbContext _db;
    private readonly PasswordService _passwords;
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

    public AccountController(UniRemoteExamDbContext db, PasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(string email, string password)
    {
        email = (email ?? string.Empty).Trim().ToLowerInvariant();
        password = password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "أدخل البريد وكلمة المرور.";
            return View();
        }

        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

        var now = YemenTime.UtcNow;
        if (user?.LockedUntil is not null && user.LockedUntil > now)
        {
            ViewBag.Error = $"تم إيقاف تسجيل الدخول مؤقتًا حتى {YemenTime.ToLocal(user.LockedUntil.Value):yyyy-MM-dd HH:mm}.";
            await AddAuditAsync(user.UserId, "Login.Locked", email);
            return View();
        }

        var loginOk = user != null && _passwords.Verify(user, password, out var needsRehash);
        if (!loginOk || user == null)
        {
            if (user != null)
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= MaxFailedAttempts)
                {
                    user.LockedUntil = now.Add(LockDuration);
                    user.FailedLoginCount = 0;
                }
            }

            await AddAuditAsync(user?.UserId, "Login.Failed", email, save: false);
            await _db.SaveChangesAsync();
            ViewBag.Error = "بيانات الدخول غير صحيحة.";
            return View();
        }

        if (needsRehash)
            user.PasswordHash = _passwords.Hash(user, password);

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        await AddAuditAsync(user.UserId, "Login.Success", email, save: false);
        await _db.SaveChangesAsync();

        var roleName = user.Role?.RoleName ?? string.Empty;
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("RoleName", roleName);
        HttpContext.Session.SetString("FullName", user.FullName ?? string.Empty);
        HttpContext.Session.SetString("MustChangePassword", user.MustChangePassword ? "1" : "0");

        if (user.MustChangePassword)
        {
            TempData["Info"] = "يجب تغيير كلمة المرور المؤقتة قبل استخدام النظام.";
            return RedirectToAction(nameof(ChangePassword));
        }

        return RedirectToRoleHome(roleName);
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        if (HttpContext.Session.GetInt32("UserId") == null)
            return RedirectToAction(nameof(Login));
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null) return RedirectToAction(nameof(Login));

        var user = await _db.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId.Value && u.IsActive);
        if (user == null) return RedirectToAction(nameof(Login));

        currentPassword ??= string.Empty;
        newPassword ??= string.Empty;
        confirmPassword ??= string.Empty;

        if (!_passwords.Verify(user, currentPassword, out _))
        {
            ViewBag.Error = "كلمة المرور الحالية غير صحيحة.";
            return View();
        }

        if (newPassword.Length < 8 || !newPassword.Any(char.IsLetter) || !newPassword.Any(char.IsDigit))
        {
            ViewBag.Error = "كلمة المرور الجديدة يجب أن تكون 8 أحرف على الأقل وتحتوي على حروف وأرقام.";
            return View();
        }

        if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
        {
            ViewBag.Error = "تأكيد كلمة المرور غير مطابق.";
            return View();
        }

        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
        {
            ViewBag.Error = "كلمة المرور الجديدة يجب أن تختلف عن الحالية.";
            return View();
        }

        user.PasswordHash = _passwords.Hash(user, newPassword);
        user.MustChangePassword = false;
        HttpContext.Session.SetString("MustChangePassword", "0");
        await AddAuditAsync(user.UserId, "Account.ChangePassword", "Password changed", save: false);
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم تغيير كلمة المرور بنجاح.";
        return RedirectToRoleHome(user.Role?.RoleName ?? string.Empty);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LogoutPost()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToRoleHome(string roleName)
    {
        if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)) return Redirect("/Admin/Dashboard");
        if (roleName.Equals("Teacher", StringComparison.OrdinalIgnoreCase)) return Redirect("/Teacher/Home");
        if (roleName.Equals("Control", StringComparison.OrdinalIgnoreCase)) return Redirect("/Control/Home");
        return Redirect("/Student/Home");
    }

    private async Task AddAuditAsync(int? userId, string action, string email, bool save = true)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            Action = action,
            Details = $"Email={email}; IP={HttpContext.Connection.RemoteIpAddress}; UA={Request.Headers.UserAgent}",
            CreatedAt = YemenTime.UtcNow
        });
        if (save) await _db.SaveChangesAsync();
    }
}
