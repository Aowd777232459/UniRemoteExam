using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Data;
using UniRemoteExam.Filters;
using UniRemoteExam.Services;

namespace UniRemoteExam.Areas.Student.Controllers;

[Area("Student")]
[RequireRole("Student")]
public class ExamsController : Controller
{
    private readonly UniRemoteExamDbContext _db;
    private const int ExpiryGraceSeconds = 10;
    public ExamsController(UniRemoteExamDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var studentId = HttpContext.Session.GetInt32("UserId");
        if (studentId == null) return Redirect("/Account/Login");
        var now = YemenTime.UtcNow;
        var courseIds = await _db.CourseEnrollments
            .Where(e => e.StudentId == studentId && e.IsActive && e.Course.IsActive)
            .Select(e => e.CourseId).ToListAsync();

        var exams = await _db.Exams.Include(e => e.Course)
            .Where(e => courseIds.Contains(e.CourseId) && e.IsPublished && e.Status == "Published")
            .Where(e => e.AvailableFrom == null || e.AvailableFrom <= now)
            .Where(e => e.AvailableTo == null || e.AvailableTo >= now)
            .OrderByDescending(e => e.CreatedAt).ToListAsync();
        return View(exams);
    }

    public async Task<IActionResult> Take(int id)
    {
        var studentId = HttpContext.Session.GetInt32("UserId");
        if (studentId == null) return Redirect("/Account/Login");
        if (!await IsEnrolledAsync(studentId.Value, id))
        {
            TempData["Error"] = "هذا الاختبار لا يخص مقررًا مسجلًا لديك.";
            return RedirectToAction(nameof(Index));
        }

        var exam = await _db.Exams.Include(e => e.Teacher).Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.ExamId == id && e.IsPublished && e.Status == "Published");
        if (exam == null) return NotFound();

        var now = YemenTime.UtcNow;
        if (exam.AvailableFrom.HasValue && now < exam.AvailableFrom)
        {
            TempData["Error"] = "لم يبدأ وقت الاختبار بعد.";
            return RedirectToAction(nameof(Index));
        }

        var attempt = await _db.ExamAttempts.FirstOrDefaultAsync(a => a.ExamId == id && a.StudentId == studentId && a.Status == "Started");
        if (attempt != null && ((attempt.ExpiresAt.HasValue && now >= attempt.ExpiresAt) || (exam.AvailableTo.HasValue && now > exam.AvailableTo)))
            return await SubmitInternalAsync(attempt, true);

        if (exam.AvailableTo.HasValue && now > exam.AvailableTo)
        {
            TempData["Error"] = "انتهت نافذة إتاحة الاختبار.";
            return RedirectToAction(nameof(Index));
        }

        var maxAttempts = Math.Max(1, exam.MaxAttempts);
        var completed = await _db.ExamAttempts.CountAsync(a => a.ExamId == id && a.StudentId == studentId && a.Status != "Started");
        if (completed >= maxAttempts)
        {
            var last = await _db.ExamAttempts.Where(a => a.ExamId == id && a.StudentId == studentId && a.Status != "Started")
                .OrderByDescending(a => a.SubmittedAt).FirstOrDefaultAsync();
            return last != null
                ? RedirectToAction("Submitted", "Results", new { area = "Student", attemptId = last.AttemptId })
                : RedirectToAction(nameof(Index));
        }

        if (attempt == null)
        {
            attempt = new ExamAttempt
            {
                ExamId = id, StudentId = studentId.Value, StartedAt = now,
                ExpiresAt = now.AddMinutes(Math.Max(5, exam.DurationMinutes)), Status = "Started"
            };
            _db.ExamAttempts.Add(attempt);
            _db.AuditLogs.Add(new AuditLog { ActorUserId = studentId, Action = "Student.Attempt.Start", Details = $"ExamId={id}", CreatedAt = now });
            try { await _db.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                attempt = await _db.ExamAttempts.FirstAsync(a => a.ExamId == id && a.StudentId == studentId && a.Status == "Started");
            }
        }

        var questions = await _db.Questions.Where(q => q.ExamId == id).OrderBy(q => q.SortOrder).ThenBy(q => q.QuestionId).ToListAsync();
        if (!questions.Any())
        {
            TempData["Error"] = "لا توجد أسئلة في الاختبار.";
            return RedirectToAction(nameof(Index));
        }
        if (exam.ShuffleQuestions)
            questions = questions.OrderBy(q => DeterministicShuffleKey(attempt.AttemptId, q.QuestionId)).ToList();

        await LockExpiredOpenedAnswersAsync(attempt.AttemptId, questions);
        var answers = await _db.AttemptAnswers.Where(a => a.AttemptId == attempt.AttemptId).ToListAsync();
        var currentQuestion = questions.FirstOrDefault(q => answers.All(a => a.QuestionId != q.QuestionId || !a.Confirmed));
        if (currentQuestion == null) return await SubmitInternalAsync(attempt, false);

        var currentAnswer = answers.FirstOrDefault(a => a.QuestionId == currentQuestion.QuestionId);
        if (currentAnswer == null)
        {
            currentAnswer = new AttemptAnswer { AttemptId = attempt.AttemptId, QuestionId = currentQuestion.QuestionId, OpenedAt = now };
            _db.AttemptAnswers.Add(currentAnswer);
            await _db.SaveChangesAsync();
        }
        else if (!currentAnswer.OpenedAt.HasValue)
        {
            currentAnswer.OpenedAt = now;
            await _db.SaveChangesAsync();
        }

        var questionRemaining = 0;
        if (currentQuestion.TimeLimitSeconds is > 0 && currentAnswer.OpenedAt.HasValue)
        {
            questionRemaining = Math.Max(0, (int)Math.Ceiling((currentAnswer.OpenedAt.Value.AddSeconds(currentQuestion.TimeLimitSeconds.Value) - now).TotalSeconds));
            if (questionRemaining <= 0)
            {
                LockAnswer(currentAnswer, now, true);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Take), new { id });
            }
        }

        var choices = await _db.QuestionChoices.Where(c => c.QuestionId == currentQuestion.QuestionId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.ChoiceId).ToListAsync();
        if (exam.ShuffleChoices)
            choices = choices.OrderBy(c => DeterministicShuffleKey(attempt.AttemptId, c.ChoiceId)).ToList();

        ViewBag.AttemptId = attempt.AttemptId;
        ViewBag.CurrentQuestion = currentQuestion;
        ViewBag.CurrentAnswer = currentAnswer;
        ViewBag.Choices = choices;
        ViewBag.TotalQuestions = questions.Count;
        var currentIndex = questions.FindIndex(q => q.QuestionId == currentQuestion.QuestionId) + 1;
        ViewBag.CurrentIndex = currentIndex;
        ViewBag.ConfirmedCount = answers.Count(a => a.Confirmed);
        ViewBag.RemainingSeconds = questionRemaining;
        ViewBag.ExamRemainingSeconds = attempt.ExpiresAt.HasValue ? Math.Max(0, (int)Math.Ceiling((attempt.ExpiresAt.Value - now).TotalSeconds)) : 0;
        ViewBag.IsLastQuestion = currentIndex == questions.Count;
        return View(exam);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAnswer(int attemptId, int questionId, int? selectedChoiceId, bool? boolAnswer, string? essayAnswer, bool confirmed)
    {
        var studentId = HttpContext.Session.GetInt32("UserId");
        if (studentId == null) return Unauthorized();
        var attempt = await _db.ExamAttempts.Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.StudentId == studentId);
        if (attempt == null) return NotFound();
        if (attempt.Status != "Started") return BadRequest("المحاولة غير نشطة.");
        if (!await IsEnrolledAsync(studentId.Value, attempt.ExamId)) return Forbid();

        var now = YemenTime.UtcNow;
        if ((attempt.ExpiresAt.HasValue && now >= attempt.ExpiresAt) || (attempt.Exam.AvailableTo.HasValue && now > attempt.Exam.AvailableTo))
        {
            await SubmitInternalAsync(attempt, true);
            return Ok(new { ok = true, locked = true, expired = true, nextUrl = Url.Action("Submitted", "Results", new { area = "Student", attemptId }) });
        }

        var question = await _db.Questions.FirstOrDefaultAsync(q => q.QuestionId == questionId && q.ExamId == attempt.ExamId);
        if (question == null) return BadRequest("السؤال لا يتبع الاختبار.");
        var answer = await _db.AttemptAnswers.FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == questionId);
        if (answer == null)
        {
            answer = new AttemptAnswer { AttemptId = attemptId, QuestionId = questionId, OpenedAt = now };
            _db.AttemptAnswers.Add(answer);
        }
        if (answer.Confirmed) return BadRequest("تم قفل السؤال.");
        answer.OpenedAt ??= now;

        var expired = false;
        var afterGrace = false;
        if (question.TimeLimitSeconds is > 0)
        {
            var end = answer.OpenedAt.Value.AddSeconds(question.TimeLimitSeconds.Value);
            expired = now > end;
            afterGrace = now > end.AddSeconds(ExpiryGraceSeconds);
        }

        if (!afterGrace)
        {
            switch (question.QuestionType)
            {
                case "MCQ":
                    if (selectedChoiceId.HasValue && !await _db.QuestionChoices.AnyAsync(c => c.ChoiceId == selectedChoiceId && c.QuestionId == questionId))
                        return BadRequest("الخيار لا يتبع السؤال.");
                    answer.SelectedChoiceId = selectedChoiceId; answer.BoolAnswer = null; answer.EssayAnswer = null;
                    break;
                case "TF":
                    answer.SelectedChoiceId = null; answer.BoolAnswer = boolAnswer; answer.EssayAnswer = null;
                    break;
                case "Essay":
                    answer.SelectedChoiceId = null; answer.BoolAnswer = null;
                    answer.EssayAnswer = string.IsNullOrWhiteSpace(essayAnswer) ? null : essayAnswer.Trim();
                    break;
                default: return BadRequest("نوع السؤال غير مدعوم.");
            }
        }

        if (confirmed || expired) LockAnswer(answer, now, expired);
        await _db.SaveChangesAsync();
        return Ok(new
        {
            ok = true, locked = answer.Confirmed, expired = answer.TimeExpired,
            message = afterGrace ? "انتهى وقت السؤال وتم قفله من السيرفر." : null,
            nextUrl = answer.Confirmed ? Url.Action(nameof(Take), new { id = attempt.ExamId }) : null
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int attemptId)
    {
        var studentId = HttpContext.Session.GetInt32("UserId");
        if (studentId == null) return Redirect("/Account/Login");
        var attempt = await _db.ExamAttempts.Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.StudentId == studentId);
        if (attempt == null) return NotFound();
        if (attempt.Status != "Started") return RedirectToAction("Submitted", "Results", new { area = "Student", attemptId });

        var now = YemenTime.UtcNow;
        var expired = attempt.ExpiresAt.HasValue && now >= attempt.ExpiresAt;
        if (!expired)
        {
            var questionIds = await _db.Questions.Where(q => q.ExamId == attempt.ExamId).Select(q => q.QuestionId).ToListAsync();
            var confirmedIds = await _db.AttemptAnswers.Where(a => a.AttemptId == attemptId && a.Confirmed).Select(a => a.QuestionId).ToListAsync();
            if (questionIds.Except(confirmedIds).Any())
            {
                TempData["Error"] = "لا يمكن التسليم قبل المرور على جميع الأسئلة.";
                return RedirectToAction(nameof(Take), new { id = attempt.ExamId });
            }
        }
        return await SubmitInternalAsync(attempt, expired);
    }

    private async Task<IActionResult> SubmitInternalAsync(ExamAttempt attempt, bool autoSubmitted)
    {
        if (attempt.Status == "Started")
        {
            var now = YemenTime.UtcNow;
            var questions = await _db.Questions.Where(q => q.ExamId == attempt.ExamId).ToListAsync();
            var answers = await _db.AttemptAnswers.Where(a => a.AttemptId == attempt.AttemptId).ToListAsync();
            foreach (var q in questions)
            {
                var answer = answers.FirstOrDefault(a => a.QuestionId == q.QuestionId);
                if (answer == null)
                {
                    answer = new AttemptAnswer { AttemptId = attempt.AttemptId, QuestionId = q.QuestionId, OpenedAt = now };
                    _db.AttemptAnswers.Add(answer);
                }
                if (!answer.Confirmed) LockAnswer(answer, now, autoSubmitted);
            }
            attempt.Status = "Submitted";
            attempt.SubmittedAt = now;
            attempt.AutoSubmitted = autoSubmitted;
            _db.AuditLogs.Add(new AuditLog { ActorUserId = attempt.StudentId, Action = autoSubmitted ? "Student.Attempt.AutoSubmit" : "Student.Attempt.Submit", Details = $"AttemptId={attempt.AttemptId}", CreatedAt = now });
            await _db.SaveChangesAsync();
        }
        return RedirectToAction("Submitted", "Results", new { area = "Student", attemptId = attempt.AttemptId });
    }

    private async Task LockExpiredOpenedAnswersAsync(int attemptId, List<Question> questions)
    {
        var now = YemenTime.UtcNow;
        var map = questions.ToDictionary(q => q.QuestionId);
        var answers = await _db.AttemptAnswers.Where(a => a.AttemptId == attemptId && !a.Confirmed && a.OpenedAt != null).ToListAsync();
        var changed = false;
        foreach (var answer in answers)
        {
            if (!map.TryGetValue(answer.QuestionId, out var q) || q.TimeLimitSeconds is not > 0) continue;
            if (now <= answer.OpenedAt!.Value.AddSeconds(q.TimeLimitSeconds.Value)) continue;
            LockAnswer(answer, now, true); changed = true;
        }
        if (changed) await _db.SaveChangesAsync();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TrackEvent(int attemptId, int? questionId, string eventType, string? details)
    {
        var studentId = HttpContext.Session.GetInt32("UserId");
        if (studentId == null) return Unauthorized();
        var attempt = await _db.ExamAttempts.FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.StudentId == studentId && a.Status == "Started");
        if (attempt == null) return NotFound();
        if (questionId.HasValue && !await _db.Questions.AnyAsync(q => q.QuestionId == questionId && q.ExamId == attempt.ExamId)) return BadRequest();
        eventType = string.IsNullOrWhiteSpace(eventType) ? "Unknown" : eventType.Trim();
        if (eventType.Length > 80) eventType = eventType[..80];
        var safeDetails = string.IsNullOrWhiteSpace(details) ? null : details.Trim();
        if (safeDetails?.Length > 1000) safeDetails = safeDetails[..1000];
        _db.ProctorEvents.Add(new ProctorEvent
        {
            AttemptId = attemptId, QuestionId = questionId, StudentId = studentId.Value, EventType = eventType,
            Details = safeDetails, CreatedAt = YemenTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    private async Task<bool> IsEnrolledAsync(int studentId, int examId)
    {
        var courseId = await _db.Exams.Where(e => e.ExamId == examId).Select(e => (int?)e.CourseId).FirstOrDefaultAsync();
        return courseId.HasValue && await _db.CourseEnrollments
            .AnyAsync(en => en.CourseId == courseId.Value && en.StudentId == studentId && en.IsActive && en.Course.IsActive);
    }

    private static void LockAnswer(AttemptAnswer answer, DateTime now, bool expired)
    {
        answer.Confirmed = true; answer.ConfirmedAt ??= now; answer.LockedAt ??= now; answer.TimeExpired = expired;
    }

    private static int DeterministicShuffleKey(int attemptId, int itemId)
    {
        unchecked { var x = attemptId * 73856093 ^ itemId * 19349663; x ^= x << 13; x ^= x >> 17; x ^= x << 5; return x; }
    }
}
