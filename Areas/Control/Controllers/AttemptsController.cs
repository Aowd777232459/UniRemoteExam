using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Data;
using UniRemoteExam.Filters;
using UniRemoteExam.Services;

namespace UniRemoteExam.Areas.Control.Controllers;

[Area("Control")]
[RequireRole("Control")]
public class AttemptsController : Controller
{
    private readonly UniRemoteExamDbContext _db;
    private readonly SmtpEmailSender _email;
    private readonly ScoreCalculator _scores;

    public AttemptsController(UniRemoteExamDbContext db, SmtpEmailSender email, ScoreCalculator scores)
    {
        _db = db; _email = email; _scores = scores;
    }

    public async Task<IActionResult> Index()
    {
        var list = await _db.ExamAttempts.Include(a => a.Exam).ThenInclude(e => e.Course).Include(a => a.Student)
            .Where(a => a.Status == "Submitted").OrderByDescending(a => a.SubmittedAt).ToListAsync();
        return View(list);
    }

    public async Task<IActionResult> Bulk()
    {
        var ids = await _db.ExamAttempts.Where(a => a.Status == "Closed").Select(a => a.ExamId).Distinct().ToListAsync();
        return View(await _db.Exams.Include(e => e.Course).Where(e => ids.Contains(e.ExamId)).OrderByDescending(e => e.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Grade(int id)
    {
        var attempt = await _db.ExamAttempts.Include(a => a.Exam).ThenInclude(e => e.Course).Include(a => a.Student)
            .FirstOrDefaultAsync(a => a.AttemptId == id);
        if (attempt == null) return NotFound();
        if (attempt.Status is not ("Submitted" or "Closed"))
        {
            TempData["Error"] = "لا يمكن تصحيح محاولة لم يتم تسليمها.";
            return RedirectToAction(nameof(Index));
        }

        var questions = await _db.Questions.Where(q => q.ExamId == attempt.ExamId).OrderBy(q => q.SortOrder).ToListAsync();
        var qIds = questions.Select(q => q.QuestionId).ToList();
        ViewBag.Questions = questions;
        ViewBag.Answers = await _db.AttemptAnswers.Where(a => a.AttemptId == id).ToListAsync();
        ViewBag.Choices = await _db.QuestionChoices.Where(c => qIds.Contains(c.QuestionId)).OrderBy(c => c.QuestionId).ThenBy(c => c.SortOrder).ToListAsync();
        ViewBag.Keys = await _db.AnswerKeyItems.Where(k => k.ExamId == attempt.ExamId).ToListAsync();
        ViewBag.Manual = await _db.ManualScores.Where(m => m.AttemptId == id).ToListAsync();
        return View(attempt);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEssayScore(int attemptId, int questionId, decimal score)
    {
        var controlId = HttpContext.Session.GetInt32("UserId");
        if (controlId == null) return Redirect("/Account/Login");
        var attempt = await _db.ExamAttempts.FirstOrDefaultAsync(a => a.AttemptId == attemptId);
        if (attempt == null) return NotFound();
        var question = await _db.Questions.FirstOrDefaultAsync(q => q.QuestionId == questionId && q.ExamId == attempt.ExamId);
        if (question == null || question.QuestionType != "Essay") return BadRequest("بيانات التصحيح غير صحيحة.");
        if (attempt.Status != "Submitted")
        {
            TempData["Error"] = "لا يمكن تعديل الدرجات بعد اعتماد النتيجة.";
            return RedirectToAction(nameof(Grade), new { id = attemptId });
        }
        if (score < 0 || score > question.Points)
        {
            TempData["Error"] = $"درجة السؤال يجب أن تكون بين 0 و {question.Points}.";
            return RedirectToAction(nameof(Grade), new { id = attemptId });
        }

        var manual = await _db.ManualScores.FirstOrDefaultAsync(m => m.AttemptId == attemptId && m.QuestionId == questionId);
        if (manual == null)
        {
            manual = new ManualScore { AttemptId = attemptId, QuestionId = questionId, Score = score, GradedByControlId = controlId.Value, GradedAt = YemenTime.UtcNow };
            _db.ManualScores.Add(manual);
        }
        else
        {
            manual.Score = score; manual.GradedByControlId = controlId.Value; manual.GradedAt = YemenTime.UtcNow;
        }
        AddAudit(controlId, "Control.EssayScore.Save", $"AttemptId={attemptId}; QuestionId={questionId}; Score={score}");
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم حفظ الدرجة المقالية.";
        return RedirectToAction(nameof(Grade), new { id = attemptId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Finalize(int attemptId)
    {
        var controlId = HttpContext.Session.GetInt32("UserId");
        if (controlId == null) return Redirect("/Account/Login");
        var attempt = await _db.ExamAttempts.Include(a => a.Exam).Include(a => a.Student)
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId);
        if (attempt == null) return NotFound();
        if (attempt.Status == "Closed")
        {
            TempData["Info"] = "النتيجة معتمدة مسبقًا.";
            return RedirectToAction(nameof(Grade), new { id = attemptId });
        }
        if (attempt.Status != "Submitted") return BadRequest("المحاولة غير جاهزة للاعتماد.");

        var essayIds = await _db.Questions.Where(q => q.ExamId == attempt.ExamId && q.QuestionType == "Essay").Select(q => q.QuestionId).ToListAsync();
        var gradedIds = await _db.ManualScores.Where(m => m.AttemptId == attemptId).Select(m => m.QuestionId).ToListAsync();
        var missing = essayIds.Except(gradedIds).ToList();
        if (missing.Any())
        {
            TempData["Error"] = "لا يمكن اعتماد النتيجة قبل تصحيح جميع الأسئلة المقالية.";
            return RedirectToAction(nameof(Grade), new { id = attemptId });
        }

        var summary = await _scores.CalculateAsync(attemptId, attempt.ExamId);
        attempt.AutoScore = summary.AutoScore;
        attempt.ManualScore = summary.ManualScore;
        attempt.FinalScore = summary.FinalScore;
        attempt.MaximumScore = summary.MaximumScore;
        attempt.Percentage = summary.Percentage;
        attempt.PassPercentage = attempt.Exam.PassPercentage;
        attempt.IsPassed = summary.Percentage >= attempt.Exam.PassPercentage;
        attempt.FinalizedAt = YemenTime.UtcNow;
        attempt.FinalizedByUserId = controlId;
        attempt.Status = "Closed";
        AddAudit(controlId, "Control.Result.Finalize", $"AttemptId={attemptId}; Score={summary.FinalScore}/{summary.MaximumScore}; Percentage={summary.Percentage}");
        await _db.SaveChangesAsync();

        var subject = $"نتيجة الاختبار: {attempt.Exam.Title}";
        var body = BuildResultBody(attempt, summary);
        try
        {
            var ok = await _email.SendAndLogAsync(attempt.StudentId, attempt.Student.Email, subject, body);
            TempData[ok ? "Success" : "Info"] = ok ? "تم اعتماد النتيجة وإرسالها للطالب." : "تم اعتماد النتيجة، وتعذر إرسال البريد.";
        }
        catch
        {
            TempData["Info"] = "تم اعتماد النتيجة، وتعذر إرسال البريد.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SendAllForExam(int examId)
    {
        var controlId = HttpContext.Session.GetInt32("UserId");
        if (controlId == null) return Redirect("/Account/Login");
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId);
        if (exam == null) return NotFound();
        var attempts = await _db.ExamAttempts.Include(a => a.Student)
            .Where(a => a.ExamId == examId && a.Status == "Closed" && a.FinalScore != null).ToListAsync();
        var sent = 0; var failed = 0;
        foreach (var a in attempts)
        {
            var summary = new ScoreSummary(a.AutoScore ?? 0, a.ManualScore ?? 0, a.FinalScore ?? 0, a.MaximumScore ?? 0, a.Percentage ?? 0);
            var ok = await _email.SendAndLogAsync(a.StudentId, a.Student.Email, $"نتيجة الاختبار: {exam.Title}", BuildResultBody(a, summary));
            if (ok) sent++; else failed++;
        }
        AddAudit(controlId, "Control.Results.BulkEmail", $"ExamId={examId}; Sent={sent}; Failed={failed}");
        await _db.SaveChangesAsync();
        TempData["BulkMsg"] = $"تم الإرسال: {sent} | فشل: {failed}";
        return RedirectToAction(nameof(Bulk));
    }

    private static string BuildResultBody(ExamAttempt attempt, ScoreSummary score) =>
        $"مرحباً {attempt.Student.FullName}\n\nتم اعتماد نتيجتك في: {attempt.Exam.Title}\n" +
        $"الدرجة الآلية: {score.AutoScore}\nالدرجة المقالية: {score.ManualScore}\n" +
        $"النتيجة: {score.FinalScore} من {score.MaximumScore}\nالنسبة: {score.Percentage}%\n" +
        $"الحالة: {(attempt.IsPassed == true ? "ناجح" : "راسب")}\n\nجامعة صنعاء - نظام الاختبارات الإلكترونية";

    private void AddAudit(int? actor, string action, string details) => _db.AuditLogs.Add(new AuditLog
    {
        ActorUserId = actor, Action = action, Details = details, CreatedAt = YemenTime.UtcNow
    });
}
