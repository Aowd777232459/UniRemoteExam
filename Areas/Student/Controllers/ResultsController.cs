using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Areas.Student.ViewModels;
using UniRemoteExam.Data;
using UniRemoteExam.Filters;

namespace UniRemoteExam.Areas.Student.Controllers;

[Area("Student")]
[RequireRole("Student")]
public class ResultsController : Controller
{
    private readonly UniRemoteExamDbContext _db;
    public ResultsController(UniRemoteExamDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var studentId = HttpContext.Session.GetInt32("UserId");
        if (studentId == null) return Redirect("/Account/Login");
        var student = await _db.Users.FirstOrDefaultAsync(u => u.UserId == studentId);
        if (student == null) return NotFound();

        var attempts = await _db.ExamAttempts.Include(a => a.Exam).ThenInclude(e => e.Course)
            .Where(a => a.StudentId == studentId && (a.Status == "Closed" || a.Status == "Submitted"))
            .OrderByDescending(a => a.SubmittedAt).ToListAsync();

        var rows = attempts.Select(a => new StudentResultRow
        {
            AttemptId = a.AttemptId,
            ExamTitle = a.Exam.Title,
            CourseName = a.Exam.Course?.Name ?? a.Exam.CourseName ?? "-",
            FinalScore = a.FinalScore ?? 0,
            MaximumScore = a.MaximumScore ?? 0,
            Percentage = a.Percentage ?? 0,
            PassPercentage = a.PassPercentage ?? a.Exam.PassPercentage,
            Passed = a.IsPassed,
            Status = a.Status == "Closed" ? "تم اعتماد النتيجة" : "قيد التصحيح",
            CanReview = a.Status == "Closed" && a.Exam.ShowCorrectAnswers,
            SubmittedAt = a.SubmittedAt,
            FinalizedAt = a.FinalizedAt
        }).ToList();

        var closed = rows.Where(r => r.Passed.HasValue).ToList();
        return View(new StudentResultsVm
        {
            StudentName = student.FullName ?? "",
            StudentEmail = student.Email,
            Rows = rows,
            CompletedCount = closed.Count,
            AveragePercent = closed.Count == 0 ? 0 : Math.Round(closed.Average(r => r.Percentage), 2)
        });
    }

    public async Task<IActionResult> Submitted(int attemptId)
    {
        var studentId = HttpContext.Session.GetInt32("UserId");
        if (studentId == null) return Redirect("/Account/Login");
        var attempt = await _db.ExamAttempts.Include(a => a.Exam).ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.StudentId == studentId);
        if (attempt == null) return NotFound();
        ViewBag.AttemptId = attempt.AttemptId;
        ViewBag.ExamTitle = attempt.Exam.Title;
        ViewBag.CourseName = attempt.Exam.Course?.Name ?? attempt.Exam.CourseName ?? "";
        ViewBag.Status = attempt.Status == "Closed" ? "تم اعتماد النتيجة" : "قيد التصحيح";
        ViewBag.CanView = attempt.Status == "Closed";
        ViewBag.CanReview = attempt.Status == "Closed" && attempt.Exam.ShowCorrectAnswers;
        ViewBag.Score = attempt.FinalScore;
        ViewBag.Maximum = attempt.MaximumScore;
        ViewBag.Percentage = attempt.Percentage;
        ViewBag.Passed = attempt.IsPassed;
        return View();
    }

    public async Task<IActionResult> Review(int attemptId)
    {
        var studentId = HttpContext.Session.GetInt32("UserId");
        if (studentId == null) return Redirect("/Account/Login");
        var attempt = await _db.ExamAttempts.Include(a => a.Exam).ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.StudentId == studentId && a.Status == "Closed");
        if (attempt == null) return NotFound();
        if (!attempt.Exam.ShowCorrectAnswers)
        {
            TempData["Error"] = "الدكتور لم يسمح بعرض الإجابات الصحيحة لهذا الاختبار.";
            return RedirectToAction(nameof(Index));
        }

        var questions = await _db.Questions.Where(q => q.ExamId == attempt.ExamId).OrderBy(q => q.SortOrder).ToListAsync();
        var ids = questions.Select(q => q.QuestionId).ToList();
        var answers = await _db.AttemptAnswers.Where(a => a.AttemptId == attemptId).ToListAsync();
        var keys = await _db.AnswerKeyItems.Where(k => k.ExamId == attempt.ExamId).ToListAsync();
        var choices = await _db.QuestionChoices.Where(c => ids.Contains(c.QuestionId)).ToListAsync();
        var manual = await _db.ManualScores.Where(m => m.AttemptId == attemptId).ToListAsync();

        var vm = new StudentReviewVm
        {
            AttemptId = attemptId,
            ExamTitle = attempt.Exam.Title,
            CourseName = attempt.Exam.Course?.Name ?? attempt.Exam.CourseName ?? "-",
            FinalScore = attempt.FinalScore ?? 0,
            MaximumScore = attempt.MaximumScore ?? 0,
            Percentage = attempt.Percentage ?? 0
        };

        foreach (var q in questions)
        {
            var a = answers.FirstOrDefault(x => x.QuestionId == q.QuestionId);
            var k = keys.FirstOrDefault(x => x.QuestionId == q.QuestionId);
            var row = new StudentReviewQuestionVm { SortOrder = q.SortOrder, Type = q.QuestionType, Body = q.Body, Points = q.Points };
            if (q.QuestionType == "MCQ")
            {
                row.StudentAnswer = choices.FirstOrDefault(c => c.ChoiceId == a?.SelectedChoiceId)?.ChoiceText ?? "لم تتم الإجابة";
                row.CorrectAnswer = choices.FirstOrDefault(c => c.ChoiceId == k?.CorrectChoiceId)?.ChoiceText ?? "-";
                row.IsCorrect = a?.SelectedChoiceId != null && a.SelectedChoiceId == k?.CorrectChoiceId;
                row.AwardedScore = row.IsCorrect ? q.Points : 0;
            }
            else if (q.QuestionType == "TF")
            {
                row.StudentAnswer = a?.BoolAnswer == true ? "صح" : a?.BoolAnswer == false ? "خطأ" : "لم تتم الإجابة";
                row.CorrectAnswer = k?.CorrectBool == true ? "صح" : k?.CorrectBool == false ? "خطأ" : "-";
                row.IsCorrect = a?.BoolAnswer.HasValue == true && a.BoolAnswer == k?.CorrectBool;
                row.AwardedScore = row.IsCorrect ? q.Points : 0;
            }
            else
            {
                row.StudentAnswer = a?.EssayAnswer ?? "لم تتم الإجابة";
                row.CorrectAnswer = k?.ModelAnswer ?? "-";
                row.AwardedScore = manual.FirstOrDefault(m => m.QuestionId == q.QuestionId)?.Score ?? 0;
                row.IsCorrect = row.AwardedScore >= q.Points / 2m;
            }
            vm.Questions.Add(row);
        }
        return View(vm);
    }
}
