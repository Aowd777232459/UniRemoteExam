using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Areas.Admin.ViewModels;
using UniRemoteExam.Data;
using UniRemoteExam.Filters;
using UniRemoteExam.Services;

namespace UniRemoteExam.Areas.Admin.Controllers;

[Area("Admin")]
[RequireRole("Admin")]
public class UsersController : Controller
{
    private readonly UniRemoteExamDbContext _db;
    private readonly PasswordService _passwords;

    public UsersController(UniRemoteExamDbContext db, PasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    public async Task<IActionResult> Index(string? search, string? roleFilter, string? activeFilter)
    {
        var query = _db.Users
            .Include(u => u.Role)
            .Include(u => u.StudentProfile)
            .Include(u => u.TeacherProfile)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(u =>
                (u.FullName != null && u.FullName.Contains(search)) ||
                u.Email.Contains(search) ||
                (u.StudentProfile != null && u.StudentProfile.StudentNumber != null && u.StudentProfile.StudentNumber.Contains(search)) ||
                (u.StudentProfile != null && u.StudentProfile.Level != null && u.StudentProfile.Level.Contains(search)) ||
                (u.TeacherProfile != null && u.TeacherProfile.Department != null && u.TeacherProfile.Department.Contains(search))
            );
        }

        if (!string.IsNullOrWhiteSpace(roleFilter) && roleFilter != "All")
        {
            query = query.Where(u => u.Role.RoleName == roleFilter);
        }

        if (!string.IsNullOrWhiteSpace(activeFilter) && activeFilter != "All")
        {
            if (activeFilter == "Active")
                query = query.Where(u => u.IsActive == true);

            if (activeFilter == "Inactive")
                query = query.Where(u => u.IsActive == false);
        }

        var allUsers = await _db.Users.Include(u => u.Role).ToListAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new UserListItemVm
            {
                UserId = u.UserId,
                FullName = u.FullName ?? "-",
                Email = u.Email,
                RoleName = u.Role.RoleName,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                StudentNumber = u.StudentProfile != null ? u.StudentProfile.StudentNumber : null,
                Level = u.StudentProfile != null ? u.StudentProfile.Level : null,
                Department = u.TeacherProfile != null ? u.TeacherProfile.Department : null
            })
            .ToListAsync();

        var vm = new UserManagementPageVm
        {
            Search = search,
            RoleFilter = roleFilter ?? "All",
            ActiveFilter = activeFilter ?? "All",

            TotalUsers = allUsers.Count,
            TotalStudents = allUsers.Count(u => u.Role.RoleName == "Student"),
            TotalTeachers = allUsers.Count(u => u.Role.RoleName == "Teacher"),
            TotalControl = allUsers.Count(u => u.Role.RoleName == "Control"),
            TotalAdmins = allUsers.Count(u => u.Role.RoleName == "Admin"),
            ActiveUsers = allUsers.Count(u => u.IsActive),
            InactiveUsers = allUsers.Count(u => !u.IsActive),

            Users = users
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult Create(string? roleName)
    {
        return View(new UserFormVm
        {
            RoleName = string.IsNullOrWhiteSpace(roleName) ? "Student" : roleName,
            IsActive = true
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormVm vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Password) || vm.Password.Length < 8 || !vm.Password.Any(char.IsLetter) || !vm.Password.Any(char.IsDigit))
        {
            ModelState.AddModelError(nameof(vm.Password), "كلمة المرور يجب أن تكون 8 أحرف على الأقل وتحتوي على حروف وأرقام");
        }

        if (!IsValidRole(vm.RoleName))
        {
            ModelState.AddModelError(nameof(vm.RoleName), "الصلاحية المحددة غير صحيحة");
        }

        if (await _db.Users.AnyAsync(u => u.Email == vm.Email.Trim()))
        {
            ModelState.AddModelError(nameof(vm.Email), "هذا البريد مستخدم مسبقًا");
        }

        if (vm.RoleName == "Student" && string.IsNullOrWhiteSpace(vm.StudentNumber))
        {
            ModelState.AddModelError(nameof(vm.StudentNumber), "الرقم الجامعي مطلوب للطالب");
        }

        if (vm.RoleName == "Student" &&
            !string.IsNullOrWhiteSpace(vm.StudentNumber) &&
            await _db.StudentProfiles.AnyAsync(s => s.StudentNumber == vm.StudentNumber.Trim()))
        {
            ModelState.AddModelError(nameof(vm.StudentNumber), "الرقم الجامعي مستخدم مسبقًا");
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == vm.RoleName);
        if (role == null)
        {
            ModelState.AddModelError(nameof(vm.RoleName), "الصلاحية غير موجودة في قاعدة البيانات");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var user = new User
        {
            FullName = vm.FullName.Trim(),
            Email = vm.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwords.Hash(new User { Email = vm.Email.Trim() }, vm.Password!),
            RoleId = role!.RoleId,
            IsActive = vm.IsActive,
            MustChangePassword = true,
            CreatedAt = YemenTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        if (vm.RoleName == "Student")
        {
            _db.StudentProfiles.Add(new StudentProfile
            {
                StudentId = user.UserId,
                StudentNumber = vm.StudentNumber?.Trim(),
                Level = vm.Level?.Trim()
            });
        }

        if (vm.RoleName == "Teacher")
        {
            _db.TeacherProfiles.Add(new TeacherProfile
            {
                TeacherId = user.UserId,
                Department = vm.Department?.Trim()
            });
        }

        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = HttpContext.Session.GetInt32("UserId"),
            Action = "Admin.Users.Create",
            Details = $"تم إنشاء حساب {user.Email} بصلاحية {vm.RoleName}.",
            CreatedAt = YemenTime.UtcNow
        });

        await _db.SaveChangesAsync();

        TempData["Success"] = "تمت إضافة المستخدم بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.StudentProfile)
            .Include(u => u.TeacherProfile)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
            return NotFound();

        var vm = new UserFormVm
        {
            UserId = user.UserId,
            FullName = user.FullName ?? "",
            Email = user.Email,
            RoleName = user.Role.RoleName,
            IsActive = user.IsActive,
            StudentNumber = user.StudentProfile?.StudentNumber,
            Level = user.StudentProfile?.Level,
            Department = user.TeacherProfile?.Department
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserFormVm vm)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .Include(u => u.StudentProfile)
            .Include(u => u.TeacherProfile)
            .FirstOrDefaultAsync(u => u.UserId == vm.UserId);

        if (user == null)
            return NotFound();

        var currentUserId = HttpContext.Session.GetInt32("UserId");

        if (currentUserId == user.UserId && vm.IsActive == false)
        {
            ModelState.AddModelError(nameof(vm.IsActive), "لا يمكنك تعطيل حسابك الحالي");
        }

        if (currentUserId == user.UserId && vm.RoleName != "Admin")
        {
            ModelState.AddModelError(nameof(vm.RoleName), "لا يمكنك تغيير صلاحية حسابك الحالي");
        }

        if (!IsValidRole(vm.RoleName))
        {
            ModelState.AddModelError(nameof(vm.RoleName), "الصلاحية المحددة غير صحيحة");
        }

        if (await _db.Users.AnyAsync(u => u.Email == vm.Email.Trim() && u.UserId != vm.UserId))
        {
            ModelState.AddModelError(nameof(vm.Email), "هذا البريد مستخدم من حساب آخر");
        }

        if (vm.RoleName == "Student" && string.IsNullOrWhiteSpace(vm.StudentNumber))
        {
            ModelState.AddModelError(nameof(vm.StudentNumber), "الرقم الجامعي مطلوب للطالب");
        }

        if (vm.RoleName == "Student" &&
            !string.IsNullOrWhiteSpace(vm.StudentNumber) &&
            await _db.StudentProfiles.AnyAsync(s => s.StudentNumber == vm.StudentNumber.Trim() && s.StudentId != vm.UserId))
        {
            ModelState.AddModelError(nameof(vm.StudentNumber), "الرقم الجامعي مستخدم من طالب آخر");
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.RoleName == vm.RoleName);
        if (role == null)
        {
            ModelState.AddModelError(nameof(vm.RoleName), "الصلاحية غير موجودة");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        user.FullName = vm.FullName.Trim();
        user.Email = vm.Email.Trim().ToLowerInvariant();
        user.RoleId = role!.RoleId;
        user.IsActive = vm.IsActive;

        if (!string.IsNullOrWhiteSpace(vm.Password))
        {
            if (vm.Password.Length < 8 || !vm.Password.Any(char.IsLetter) || !vm.Password.Any(char.IsDigit))
            {
                ModelState.AddModelError(nameof(vm.Password), "كلمة المرور يجب أن تكون 8 أحرف على الأقل وتحتوي على حروف وأرقام");
                return View(vm);
            }
            user.PasswordHash = _passwords.Hash(user, vm.Password);
            user.MustChangePassword = true;
        }

        if (vm.RoleName == "Student")
        {
            if (user.StudentProfile == null)
            {
                _db.StudentProfiles.Add(new StudentProfile
                {
                    StudentId = user.UserId,
                    StudentNumber = vm.StudentNumber?.Trim(),
                    Level = vm.Level?.Trim()
                });
            }
            else
            {
                user.StudentProfile.StudentNumber = vm.StudentNumber?.Trim();
                user.StudentProfile.Level = vm.Level?.Trim();
            }

            if (user.TeacherProfile != null)
                _db.TeacherProfiles.Remove(user.TeacherProfile);
        }
        else if (vm.RoleName == "Teacher")
        {
            if (user.TeacherProfile == null)
            {
                _db.TeacherProfiles.Add(new TeacherProfile
                {
                    TeacherId = user.UserId,
                    Department = vm.Department?.Trim()
                });
            }
            else
            {
                user.TeacherProfile.Department = vm.Department?.Trim();
            }

            if (user.StudentProfile != null)
                _db.StudentProfiles.Remove(user.StudentProfile);
        }
        else
        {
            if (user.StudentProfile != null)
                _db.StudentProfiles.Remove(user.StudentProfile);

            if (user.TeacherProfile != null)
                _db.TeacherProfiles.Remove(user.TeacherProfile);
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "تم تعديل بيانات المستخدم بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
            return NotFound();

        var currentUserId = HttpContext.Session.GetInt32("UserId");

        if (currentUserId == user.UserId)
        {
            TempData["Error"] = "لا يمكنك تعطيل حسابك الحالي.";
            return RedirectToAction(nameof(Index));
        }

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();

        TempData["Success"] = user.IsActive ? "تم تفعيل الحساب بنجاح." : "تم تعطيل الحساب بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users
            .Include(u => u.StudentProfile)
            .Include(u => u.TeacherProfile)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
            return NotFound();

        var currentUserId = HttpContext.Session.GetInt32("UserId");

        if (currentUserId == user.UserId)
        {
            TempData["Error"] = "لا يمكنك حذف حسابك الحالي.";
            return RedirectToAction(nameof(Index));
        }

        var hasData =
            await _db.ExamAttempts.AnyAsync(a => a.StudentId == id) ||
            await _db.Exams.AnyAsync(e => e.TeacherId == id) ||
            await _db.ExamPublishRequests.AnyAsync(r => r.TeacherId == id || r.ReviewedByAdminId == id) ||
            await _db.ManualScores.AnyAsync(m => m.GradedByControlId == id) ||
            await _db.AuditLogs.AnyAsync(a => a.ActorUserId == id) ||
            await _db.CourseEnrollments.AnyAsync(e => e.StudentId == id) ||
            await _db.Courses.AnyAsync(c => c.TeacherId == id) ||
            await _db.ExamAttempts.AnyAsync(a => a.FinalizedByUserId == id);

        if (hasData)
        {
            user.IsActive = false;
            await _db.SaveChangesAsync();

            TempData["Error"] = "لا يمكن حذف المستخدم لوجود بيانات مرتبطة به، لذلك تم تعطيله بدل الحذف.";
            return RedirectToAction(nameof(Index));
        }

        if (user.StudentProfile != null)
            _db.StudentProfiles.Remove(user.StudentProfile);

        if (user.TeacherProfile != null)
            _db.TeacherProfiles.Remove(user.TeacherProfile);

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم حذف المستخدم بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    private static bool IsValidRole(string roleName)
    {
        return roleName is "Admin" or "Teacher" or "Student" or "Control";
    }
}
