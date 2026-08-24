using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Data;
using UniRemoteExam.Filters;
using UniRemoteExam.Services;

namespace UniRemoteExam.Areas.Teacher.Controllers;

[Area("Teacher")]
[RequireRole("Teacher")]
public class AnswerKeyController : Controller
{
    private readonly UniRemoteExamDbContext _db;
    public AnswerKeyController(UniRemoteExamDbContext db) => _db = db;

    // صفحة مستقلة لإدخال نموذج الإجابة بعد الانتهاء من إنشاء أسئلة الاختبار
    [HttpGet]
    public async Task<IActionResult> Manage(int examId, bool saved = false)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");

        var exam = await _db.Exams.Include(e => e.Course).FirstOrDefaultAsync(e => e.ExamId == examId && e.TeacherId == teacherId);
        if (exam == null) return NotFound();
        if (!await CanModifyExamAsync(examId, teacherId.Value))
        {
            TempData["Error"] = "لا يمكن تعديل نموذج الإجابة بعد إرسال الاختبار للمراجعة أو نشره.";
            return RedirectToAction("Index", "Exams");
        }

        var questions = await _db.Questions
            .Where(q => q.ExamId == examId)
            .OrderBy(q => q.SortOrder)
            .ToListAsync();

        var questionIds = questions.Select(q => q.QuestionId).ToList();

        var choices = await _db.QuestionChoices
            .Where(c => questionIds.Contains(c.QuestionId))
            .OrderBy(c => c.QuestionId)
            .ThenBy(c => c.SortOrder)
            .ToListAsync();

        var keys = await _db.AnswerKeyItems
            .Where(k => k.ExamId == examId)
            .ToListAsync();

        ViewBag.Questions = questions;
        ViewBag.Choices = choices;
        ViewBag.AnswerKeys = keys;
        ViewBag.Saved = saved;

        return View(exam);
    }

    // حفظ جميع الإجابات مرة واحدة ثم تصبح جاهزة للمراجعة/الكنترول
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAll(int examId, IFormCollection form)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");

        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId && e.TeacherId == teacherId);
        if (exam == null) return NotFound();
        if (!await CanModifyExamAsync(examId, teacherId.Value))
        {
            TempData["Error"] = "لا يمكن تعديل نموذج الإجابة في حالة الاختبار الحالية.";
            return RedirectToAction("Index", "Exams");
        }

        var questions = await _db.Questions
            .Where(q => q.ExamId == examId)
            .OrderBy(q => q.SortOrder)
            .ToListAsync();

        var questionIds = questions.Select(q => q.QuestionId).ToList();
        var choices = await _db.QuestionChoices
            .Where(c => questionIds.Contains(c.QuestionId))
            .ToListAsync();

        foreach (var q in questions)
        {
            int? correctChoiceId = null;
            bool? correctBool = null;
            string? modelAnswer = null;
            bool hasAnswer = false;

            if (q.QuestionType == "MCQ")
            {
                var raw = form[$"correctChoiceId_{q.QuestionId}"].ToString();
                if (int.TryParse(raw, out var choiceId) && choices.Any(c => c.QuestionId == q.QuestionId && c.ChoiceId == choiceId))
                {
                    correctChoiceId = choiceId;
                    hasAnswer = true;
                }
            }
            else if (q.QuestionType == "TF")
            {
                var raw = form[$"correctBool_{q.QuestionId}"].ToString();
                if (bool.TryParse(raw, out var boolValue))
                {
                    correctBool = boolValue;
                    hasAnswer = true;
                }
            }
            else if (q.QuestionType == "Essay")
            {
                modelAnswer = (form[$"modelAnswer_{q.QuestionId}"].ToString() ?? string.Empty).Trim();
                hasAnswer = !string.IsNullOrWhiteSpace(modelAnswer);
            }

            // لا ننشئ مفتاحًا فارغًا؛ لكن إذا كان موجودًا وتم ترك السؤال فارغًا نحذف بياناته حتى يظهر كغير مكتمل
            var item = await _db.AnswerKeyItems.FirstOrDefaultAsync(x => x.ExamId == examId && x.QuestionId == q.QuestionId);

            if (!hasAnswer)
            {
                if (item != null)
                {
                    item.CorrectChoiceId = null;
                    item.CorrectBool = null;
                    item.ModelAnswer = null;
                    item.UploadedByTeacherId = teacherId.Value;
                    item.UploadedAt = YemenTime.UtcNow;
                }
                continue;
            }

            if (item == null)
            {
                item = new AnswerKeyItem
                {
                    ExamId = examId,
                    QuestionId = q.QuestionId,
                    UploadedByTeacherId = teacherId.Value,
                    UploadedAt = YemenTime.UtcNow
                };
                _db.AnswerKeyItems.Add(item);
            }

            item.CorrectChoiceId = correctChoiceId;
            item.CorrectBool = correctBool;
            item.ModelAnswer = modelAnswer;
            item.UploadedByTeacherId = teacherId.Value;
            item.UploadedAt = YemenTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Manage), new { examId, saved = true });
    }

    // أبقينا الدوال القديمة حتى لا ينكسر أي رابط قديم، لكنها لم تعد الطريقة الأساسية
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMcq(int examId, int questionId, int correctChoiceId)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");

        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId && e.TeacherId == teacherId);
        if (exam == null) return NotFound();
        if (!await CanModifyExamAsync(examId, teacherId.Value))
        {
            TempData["Error"] = "لا يمكن تعديل نموذج الإجابة في حالة الاختبار الحالية.";
            return RedirectToAction("Index", "Exams");
        }

        var validChoice = await _db.QuestionChoices.AnyAsync(c => c.ChoiceId == correctChoiceId && c.QuestionId == questionId)
            && await _db.Questions.AnyAsync(q => q.QuestionId == questionId && q.ExamId == examId && q.QuestionType == "MCQ");
        if (!validChoice) return BadRequest("الخيار لا يتبع السؤال المحدد.");

        var item = await _db.AnswerKeyItems.FirstOrDefaultAsync(x => x.ExamId == examId && x.QuestionId == questionId);
        if (item == null)
        {
            item = new AnswerKeyItem
            {
                ExamId = examId,
                QuestionId = questionId,
                UploadedByTeacherId = teacherId.Value,
                UploadedAt = YemenTime.UtcNow
            };
            _db.AnswerKeyItems.Add(item);
        }

        item.CorrectChoiceId = correctChoiceId;
        item.CorrectBool = null;
        item.ModelAnswer = null;
        item.UploadedByTeacherId = teacherId.Value;
        item.UploadedAt = YemenTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Manage), new { examId, saved = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetTf(int examId, int questionId, bool correctBool)
    {
        var teacherId = HttpContext.Session.GetInt32("UserId");
        if (teacherId == null) return Redirect("/Account/Login");

        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId && e.TeacherId == teacherId);
        if (exam == null) return NotFound();
        if (!await CanModifyExamAsync(examId, teacherId.Value))
        {
            TempData["Error"] = "لا يمكن تعديل نموذج الإجابة في حالة الاختبار الحالية.";
            return RedirectToAction("Index", "Exams");
        }

        var item = await _db.AnswerKeyItems.FirstOrDefaultAsync(x => x.ExamId == examId && x.QuestionId == questionId);
        if (item == null)
        {
            item = new AnswerKeyItem
            {
                ExamId = examId,
                QuestionId = questionId,
                UploadedByTeacherId = teacherId.Value,
                UploadedAt = YemenTime.UtcNow
            };
            _db.AnswerKeyItems.Add(item);
        }

        item.CorrectChoiceId = null;
        item.CorrectBool = correctBool;
        item.ModelAnswer = null;
        item.UploadedByTeacherId = teacherId.Value;
        item.UploadedAt = YemenTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Manage), new { examId, saved = true });
    }

    private async Task<bool> CanModifyExamAsync(int examId, int teacherId)
    {
        var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId && e.TeacherId == teacherId);
        if (exam == null || exam.IsPublished || exam.Status is "PendingReview" or "Published" or "Closed" or "Archived") return false;
        if (await _db.ExamPublishRequests.AnyAsync(r => r.ExamId == examId && r.Status == "Pending")) return false;
        return !await _db.ExamAttempts.AnyAsync(a => a.ExamId == examId);
    }
}
