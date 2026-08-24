using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Data;
using UniRemoteExam.Filters;
using UniRemoteExam.Services;

namespace UniRemoteExam.Areas.Teacher.Controllers;

[Area("Teacher")]
[RequireRole("Teacher")]
public class ExamsController : Controller
{
    private readonly UniRemoteExamDbContext _db;
    public ExamsController(UniRemoteExamDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");

        var exams = await _db.Exams.Include(e => e.Course)
            .Where(e => e.TeacherId == teacherId)
            .OrderByDescending(e => e.CreatedAt).ToListAsync();
        var ids = exams.Select(e => e.ExamId).ToList();
        ViewBag.PendingExamIds = (await _db.ExamPublishRequests
            .Where(r => ids.Contains(r.ExamId) && r.Status == "Pending")
            .Select(r => r.ExamId).ToListAsync()).ToHashSet();
        return View(exams);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");
        await LoadTeacherCourses(teacherId.Value);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string title, int courseId, DateTime? availableFrom, DateTime? availableTo,
        int? maxAttempts, int? durationMinutes, decimal? passPercentage, bool shuffleQuestions, bool shuffleChoices,
        bool showCorrectAnswers, bool autoSubmitOnExpiry)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");
        await LoadTeacherCourses(teacherId.Value);

        title = (title ?? string.Empty).Trim();
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId && c.TeacherId == teacherId && c.IsActive);
        var fromUtc = YemenTime.LocalInputToUtc(availableFrom);
        var toUtc = YemenTime.LocalInputToUtc(availableTo);

        if (string.IsNullOrWhiteSpace(title) || course == null)
        {
            ViewBag.Error = "عنوان الاختبار والمقرر الصحيح مطلوبان.";
            return View();
        }
        if (fromUtc.HasValue && toUtc.HasValue && toUtc <= fromUtc)
        {
            ViewBag.Error = "وقت نهاية الاختبار يجب أن يكون بعد وقت البداية.";
            return View();
        }

        var exam = new Exam
        {
            Title = title,
            CourseId = course.CourseId,
            CourseName = course.Name,
            TeacherId = teacherId.Value,
            Status = "Draft",
            IsPublished = false,
            CreatedAt = YemenTime.UtcNow,
            AvailableFrom = fromUtc,
            AvailableTo = toUtc,
            MaxAttempts = Math.Clamp(maxAttempts ?? 1, 1, 10),
            DurationMinutes = Math.Clamp(durationMinutes ?? 60, 5, 600),
            PassPercentage = Math.Clamp(passPercentage ?? 50m, 0m, 100m),
            AutoSubmitOnExpiry = true,
            ShuffleQuestions = shuffleQuestions,
            ShuffleChoices = shuffleChoices,
            ShowCorrectAnswers = showCorrectAnswers
        };

        _db.Exams.Add(exam);
        AddAudit(teacherId, "Teacher.Exam.Create", $"CourseId={courseId}; Title={title}");
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = exam.ExamId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");
        var exam = await _db.Exams.Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.ExamId == id && e.TeacherId == teacherId);
        if (exam == null) return NotFound();

        var hasPending = await _db.ExamPublishRequests.AnyAsync(r => r.ExamId == id && r.Status == "Pending");
        if ((hasPending || exam.Status is "Published" or "Closed" or "Archived" || exam.IsPublished) && Request.Query["viewOnly"] != "1")
        {
            TempData["Info"] = hasPending ? "الاختبار قيد مراجعة المدير ولا يمكن تعديله." : "الاختبار منشور أو مغلق ولا يمكن تعديل بنائه.";
            return RedirectToAction(nameof(Index));
        }

        var questions = await _db.Questions.Where(q => q.ExamId == id).OrderBy(q => q.SortOrder).ToListAsync();
        var qIds = questions.Select(q => q.QuestionId).ToList();
        ViewBag.Questions = questions;
        ViewBag.Choices = await _db.QuestionChoices.Where(c => qIds.Contains(c.QuestionId))
            .OrderBy(c => c.QuestionId).ThenBy(c => c.SortOrder).ToListAsync();
        ViewBag.HasPending = hasPending;
        ViewBag.AvailableFromLocal = YemenTime.ToLocal(exam.AvailableFrom);
        ViewBag.AvailableToLocal = YemenTime.ToLocal(exam.AvailableTo);
        await LoadTeacherCourses(teacherId.Value);
        return View(exam);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTrueFalse(int examId, string body, decimal? points, int? timeLimitMinutes)
        => await AddQuestion(examId, "TF", body, points, timeLimitMinutes, null);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEssay(int examId, string body, decimal? points, int? timeLimitMinutes)
        => await AddQuestion(examId, "Essay", body, points, timeLimitMinutes, null);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMcq(int examId, string body, decimal? points, int? timeLimitMinutes,
        string c1, string c2, string c3, string c4)
        => await AddQuestion(examId, "MCQ", body, points, timeLimitMinutes, new[] { c1, c2, c3, c4 });

    private async Task<IActionResult> AddQuestion(int examId, string type, string body, decimal? points, int? timeLimitMinutes, string[]? choices)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");
        if (!await CanModifyExamAsync(examId, teacherId.Value))
        {
            TempData["Error"] = "لا يمكن إضافة سؤال بعد إرسال الاختبار للمراجعة أو نشره.";
            return RedirectToAction(nameof(Index));
        }
        body = (body ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            TempData["Error"] = "نص السؤال مطلوب.";
            return RedirectToAction(nameof(Edit), new { id = examId });
        }

        var q = new Question
        {
            ExamId = examId, QuestionType = type, Body = body,
            Points = points is > 0 ? Math.Min(points.Value, 9999m) : 1m,
            TimeLimitSeconds = MinutesToSeconds(timeLimitMinutes),
            SortOrder = await GetNextSortOrder(examId)
        };
        _db.Questions.Add(q);
        await _db.SaveChangesAsync();

        if (type == "MCQ" && choices != null)
        {
            for (var i = 0; i < 4; i++)
            {
                var text = (choices.ElementAtOrDefault(i) ?? string.Empty).Trim();
                _db.QuestionChoices.Add(new QuestionChoice
                {
                    QuestionId = q.QuestionId, ChoiceText = string.IsNullOrWhiteSpace(text) ? $"خيار {i + 1}" : text, SortOrder = i + 1
                });
            }
        }
        AddAudit(teacherId, "Teacher.Question.Create", $"ExamId={examId}; QuestionId={q.QuestionId}; Type={type}");
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Edit), new { id = examId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuestion(int examId, int questionId, string body, decimal? points, int? timeLimitMinutes,
        string? c1, string? c2, string? c3, string? c4)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");
        if (!await CanModifyExamAsync(examId, teacherId.Value))
        {
            TempData["Error"] = "لا يمكن تعديل الأسئلة في حالة الاختبار الحالية.";
            return RedirectToAction(nameof(Index));
        }
        var q = await _db.Questions.FirstOrDefaultAsync(x => x.QuestionId == questionId && x.ExamId == examId);
        if (q == null) return NotFound();
        body = (body ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(body))
        {
            TempData["Error"] = "نص السؤال مطلوب.";
            return RedirectToAction(nameof(Edit), new { id = examId });
        }
        q.Body = body;
        q.Points = points is > 0 ? Math.Min(points.Value, 9999m) : q.Points;
        q.TimeLimitSeconds = MinutesToSeconds(timeLimitMinutes);

        if (q.QuestionType == "MCQ")
        {
            var texts = new[] { c1, c2, c3, c4 };
            var old = await _db.QuestionChoices.Where(c => c.QuestionId == questionId)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.ChoiceId).ToListAsync();
            for (var i = 0; i < 4; i++)
            {
                var text = (texts[i] ?? string.Empty).Trim();
                text = string.IsNullOrWhiteSpace(text) ? $"خيار {i + 1}" : text;
                if (i < old.Count) { old[i].ChoiceText = text; old[i].SortOrder = i + 1; }
                else _db.QuestionChoices.Add(new QuestionChoice { QuestionId = questionId, ChoiceText = text, SortOrder = i + 1 });
            }
        }
        AddAudit(teacherId, "Teacher.Question.Update", $"ExamId={examId}; QuestionId={questionId}");
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تعديل السؤال.";
        return RedirectToAction(nameof(Edit), new { id = examId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int examId, int questionId)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");
        if (!await CanModifyExamAsync(examId, teacherId.Value))
        {
            TempData["Error"] = "لا يمكن حذف السؤال في حالة الاختبار الحالية.";
            return RedirectToAction(nameof(Index));
        }
        var q = await _db.Questions.FirstOrDefaultAsync(x => x.QuestionId == questionId && x.ExamId == examId);
        if (q == null) return NotFound();
        _db.Questions.Remove(q);
        AddAudit(teacherId, "Teacher.Question.Delete", $"ExamId={examId}; QuestionId={questionId}");
        await _db.SaveChangesAsync();
        await NormalizeQuestionOrderAsync(examId);
        TempData["Success"] = "تم حذف السؤال.";
        return RedirectToAction(nameof(Edit), new { id = examId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveQuestion(int examId, int questionId, string direction)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");
        if (!await CanModifyExamAsync(examId, teacherId.Value)) return RedirectToAction(nameof(Index));
        var questions = await _db.Questions.Where(q => q.ExamId == examId)
            .OrderBy(q => q.SortOrder).ThenBy(q => q.QuestionId).ToListAsync();
        var index = questions.FindIndex(q => q.QuestionId == questionId);
        var target = direction == "up" ? index - 1 : index + 1;
        if (index >= 0 && target >= 0 && target < questions.Count)
        {
            (questions[index].SortOrder, questions[target].SortOrder) = (questions[target].SortOrder, questions[index].SortOrder);
            await _db.SaveChangesAsync();
            await NormalizeQuestionOrderAsync(examId);
        }
        return RedirectToAction(nameof(Edit), new { id = examId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateExamSettings(int examId, string title, int courseId, DateTime? availableFrom,
        DateTime? availableTo, int? maxAttempts, int? durationMinutes, decimal? passPercentage, bool shuffleQuestions,
        bool shuffleChoices, bool showCorrectAnswers, bool autoSubmitOnExpiry)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");
        if (!await CanModifyExamAsync(examId, teacherId.Value))
        {
            TempData["Error"] = "لا يمكن تعديل إعدادات الاختبار في حالته الحالية.";
            return RedirectToAction(nameof(Index));
        }
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId && e.TeacherId == teacherId);
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.CourseId == courseId && c.TeacherId == teacherId && c.IsActive);
        if (exam == null || course == null) return NotFound();

        title = (title ?? string.Empty).Trim();
        var fromUtc = YemenTime.LocalInputToUtc(availableFrom);
        var toUtc = YemenTime.LocalInputToUtc(availableTo);
        if (string.IsNullOrWhiteSpace(title) || (fromUtc.HasValue && toUtc.HasValue && toUtc <= fromUtc))
        {
            TempData["Error"] = "تحقق من العنوان ونافذة الإتاحة.";
            return RedirectToAction(nameof(Edit), new { id = examId });
        }
        exam.Title = title;
        exam.CourseId = courseId;
        exam.CourseName = course.Name;
        exam.AvailableFrom = fromUtc;
        exam.AvailableTo = toUtc;
        exam.MaxAttempts = Math.Clamp(maxAttempts ?? 1, 1, 10);
        exam.DurationMinutes = Math.Clamp(durationMinutes ?? 60, 5, 600);
        exam.PassPercentage = Math.Clamp(passPercentage ?? 50m, 0m, 100m);
        exam.AutoSubmitOnExpiry = true;
        exam.ShuffleQuestions = shuffleQuestions;
        exam.ShuffleChoices = shuffleChoices;
        exam.ShowCorrectAnswers = showCorrectAnswers;
        AddAudit(teacherId, "Teacher.Exam.Update", $"ExamId={examId}");
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم حفظ إعدادات الاختبار.";
        return RedirectToAction(nameof(Edit), new { id = examId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPublish(int examId)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");
        if (!await CanModifyExamAsync(examId, teacherId.Value))
        {
            TempData["Error"] = "لا يمكن إرسال طلب جديد في حالة الاختبار الحالية.";
            return RedirectToAction(nameof(Index));
        }
        var exam = await _db.Exams.Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.ExamId == examId && e.TeacherId == teacherId);
        if (exam == null) return NotFound();
        if (!exam.Course.IsActive || exam.DurationMinutes <= 0 || exam.PassPercentage is < 0 or > 100)
        {
            TempData["Error"] = "إعدادات المقرر أو الاختبار غير صالحة للنشر.";
            return RedirectToAction(nameof(Edit), new { id = examId });
        }

        var questions = await _db.Questions.Where(q => q.ExamId == examId).OrderBy(q => q.SortOrder).ToListAsync();
        if (!questions.Any())
        {
            TempData["Error"] = "أضف سؤالًا واحدًا على الأقل.";
            return RedirectToAction(nameof(Edit), new { id = examId });
        }
        var ids = questions.Select(q => q.QuestionId).ToList();
        var keys = await _db.AnswerKeyItems.Where(k => k.ExamId == examId && ids.Contains(k.QuestionId)).ToListAsync();
        var missing = questions.Where(q =>
        {
            var key = keys.FirstOrDefault(k => k.QuestionId == q.QuestionId);
            return key == null || q.QuestionType switch
            {
                "MCQ" => !key.CorrectChoiceId.HasValue,
                "TF" => !key.CorrectBool.HasValue,
                "Essay" => string.IsNullOrWhiteSpace(key.ModelAnswer),
                _ => true
            };
        }).ToList();
        if (missing.Any())
        {
            TempData["Error"] = "أكمل نموذج الإجابة للأسئلة: " + string.Join("، ", missing.Select(q => q.SortOrder));
            return RedirectToAction("Manage", "AnswerKey", new { examId });
        }

        _db.ExamPublishRequests.Add(new ExamPublishRequest
        {
            ExamId = examId, TeacherId = teacherId.Value, Status = "Pending", RequestedAt = YemenTime.UtcNow
        });
        exam.Status = "PendingReview";
        exam.IsPublished = false;
        AddAudit(teacherId, "Teacher.Exam.RequestPublish", $"ExamId={examId}");
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم إرسال الاختبار إلى المدير للمراجعة.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadTeacherCourses(int teacherId)
    {
        ViewBag.Courses = await _db.Courses.Include(c => c.AcademicTerm)
            .Where(c => c.TeacherId == teacherId && c.IsActive && c.AcademicTerm.IsActive)
            .OrderBy(c => c.Code).ToListAsync();
    }

    private async Task<bool> CanModifyExamAsync(int examId, int teacherId)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId && e.TeacherId == teacherId);
        if (exam == null || exam.IsPublished || exam.Status is "PendingReview" or "Published" or "Closed" or "Archived") return false;
        if (await _db.ExamPublishRequests.AnyAsync(r => r.ExamId == examId && r.Status == "Pending")) return false;
        return !await _db.ExamAttempts.AnyAsync(a => a.ExamId == examId);
    }

    private async Task<int> GetNextSortOrder(int examId) =>
        (await _db.Questions.Where(q => q.ExamId == examId).MaxAsync(q => (int?)q.SortOrder) ?? 0) + 1;

    private static int? MinutesToSeconds(int? minutes) => minutes is > 0 ? Math.Min(minutes.Value, 600) * 60 : null;

    private async Task NormalizeQuestionOrderAsync(int examId)
    {
        var ordered = await _db.Questions.Where(q => q.ExamId == examId)
            .OrderBy(q => q.SortOrder).ThenBy(q => q.QuestionId).ToListAsync();
        for (var i = 0; i < ordered.Count; i++) ordered[i].SortOrder = i + 1;
        await _db.SaveChangesAsync();
    }

    private void AddAudit(int? actorId, string action, string details) => _db.AuditLogs.Add(new AuditLog
    {
        ActorUserId = actorId, Action = action, Details = details, CreatedAt = YemenTime.UtcNow
    });
}
