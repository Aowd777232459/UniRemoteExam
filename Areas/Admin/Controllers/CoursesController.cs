using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Data;
using UniRemoteExam.Filters;
using UniRemoteExam.Services;

namespace UniRemoteExam.Areas.Admin.Controllers;

[Area("Admin")]
[RequireRole("Admin")]
public class CoursesController : Controller
{
    private readonly UniRemoteExamDbContext _db;
    public CoursesController(UniRemoteExamDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? courseId)
    {
        ViewBag.Departments = await _db.Departments.OrderBy(d => d.Name).ToListAsync();
        ViewBag.Terms = await _db.AcademicTerms.OrderByDescending(t => t.StartDate).ToListAsync();
        ViewBag.Teachers = await _db.Users.Include(u => u.Role)
            .Where(u => u.IsActive && u.Role.RoleName == "Teacher")
            .OrderBy(u => u.FullName).ToListAsync();
        ViewBag.Students = await _db.Users.Include(u => u.Role).Include(u => u.StudentProfile)
            .Where(u => u.IsActive && u.Role.RoleName == "Student")
            .OrderBy(u => u.FullName).ToListAsync();

        var courses = await _db.Courses
            .Include(c => c.Department)
            .Include(c => c.AcademicTerm)
            .Include(c => c.Teacher)
            .Include(c => c.Enrollments).ThenInclude(e => e.Student)
            .OrderByDescending(c => c.AcademicTerm.StartDate)
            .ThenBy(c => c.Code)
            .ToListAsync();

        ViewBag.SelectedCourseId = courseId;
        return View(courses);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDepartment(string code, string name)
    {
        code = (code ?? string.Empty).Trim().ToUpperInvariant();
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] = "رمز القسم واسمه مطلوبان.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.Departments.AnyAsync(d => d.Code == code))
        {
            TempData["Error"] = "رمز القسم مستخدم مسبقًا.";
            return RedirectToAction(nameof(Index));
        }
        _db.Departments.Add(new Department { Code = code, Name = name, IsActive = true });
        await LogAndSave("Admin.Department.Create", $"{code} - {name}");
        TempData["Success"] = "تم إنشاء القسم.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTerm(string name, DateTime startDate, DateTime endDate)
    {
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name) || endDate <= startDate)
        {
            TempData["Error"] = "تحقق من اسم الفصل وتواريخ البداية والنهاية.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.AcademicTerms.AnyAsync(t => t.Name == name))
        {
            TempData["Error"] = "الفصل الأكاديمي موجود مسبقًا.";
            return RedirectToAction(nameof(Index));
        }
        _db.AcademicTerms.Add(new AcademicTerm { Name = name, StartDate = startDate.Date, EndDate = endDate.Date, IsActive = true });
        await LogAndSave("Admin.AcademicTerm.Create", name);
        TempData["Success"] = "تم إنشاء الفصل الأكاديمي.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourse(string code, string name, int departmentId, int academicTermId, int teacherId, string? level)
    {
        code = (code ?? string.Empty).Trim().ToUpperInvariant();
        name = (name ?? string.Empty).Trim();
        var valid = !string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name)
            && await _db.Departments.AnyAsync(d => d.DepartmentId == departmentId && d.IsActive)
            && await _db.AcademicTerms.AnyAsync(t => t.AcademicTermId == academicTermId && t.IsActive)
            && await _db.Users.Include(u => u.Role).AnyAsync(u => u.UserId == teacherId && u.IsActive && u.Role.RoleName == "Teacher");
        if (!valid)
        {
            TempData["Error"] = "بيانات المقرر غير مكتملة أو غير صحيحة.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.Courses.AnyAsync(c => c.Code == code && c.AcademicTermId == academicTermId))
        {
            TempData["Error"] = "رمز المقرر مستخدم في هذا الفصل.";
            return RedirectToAction(nameof(Index));
        }
        var course = new Course
        {
            Code = code, Name = name, DepartmentId = departmentId, AcademicTermId = academicTermId,
            TeacherId = teacherId, Level = string.IsNullOrWhiteSpace(level) ? null : level.Trim(), IsActive = true
        };
        _db.Courses.Add(course);
        await LogAndSave("Admin.Course.Create", $"{code} - {name}");
        TempData["Success"] = "تم إنشاء المقرر وربطه بالدكتور.";
        return RedirectToAction(nameof(Index), new { courseId = course.CourseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnrollStudent(int courseId, int studentId)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId && c.IsActive);
        var isStudent = await _db.Users.Include(u => u.Role)
            .AnyAsync(u => u.UserId == studentId && u.IsActive && u.Role.RoleName == "Student");
        if (course == null || !isStudent)
        {
            TempData["Error"] = "المقرر أو الطالب غير صالح.";
            return RedirectToAction(nameof(Index), new { courseId });
        }
        var enrollment = await _db.CourseEnrollments.FirstOrDefaultAsync(e => e.CourseId == courseId && e.StudentId == studentId);
        if (enrollment == null)
            _db.CourseEnrollments.Add(new CourseEnrollment { CourseId = courseId, StudentId = studentId, IsActive = true, EnrolledAt = YemenTime.UtcNow });
        else
            enrollment.IsActive = true;

        await LogAndSave("Admin.Course.EnrollStudent", $"CourseId={courseId}; StudentId={studentId}");
        TempData["Success"] = "تم تسجيل الطالب في المقرر.";
        return RedirectToAction(nameof(Index), new { courseId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveEnrollment(int enrollmentId)
    {
        var enrollment = await _db.CourseEnrollments.FirstOrDefaultAsync(e => e.EnrollmentId == enrollmentId);
        if (enrollment == null) return NotFound();
        enrollment.IsActive = false;
        await LogAndSave("Admin.Course.RemoveEnrollment", $"EnrollmentId={enrollmentId}");
        TempData["Success"] = "تم إلغاء تسجيل الطالب من المقرر.";
        return RedirectToAction(nameof(Index), new { courseId = enrollment.CourseId });
    }

    private async Task LogAndSave(string action, string details)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = HttpContext.Session.GetInt32("UserId"), Action = action, Details = details, CreatedAt = YemenTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
