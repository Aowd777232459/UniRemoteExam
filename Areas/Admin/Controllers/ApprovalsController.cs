using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Areas.Admin.ViewModels;
using UniRemoteExam.Data;
using UniRemoteExam.Services;
using UniRemoteExam.Filters;

namespace UniRemoteExam.Areas.Admin.Controllers
{
    [Area("Admin")]
    [RequireRole("Admin")]
    public class ApprovalsController : Controller
    {
        private readonly UniRemoteExamDbContext _context;
        private readonly SmtpEmailSender _emailSender;

        public ApprovalsController(UniRemoteExamDbContext context, SmtpEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("RoleName");
            return !string.IsNullOrWhiteSpace(role) && (role == "Admin" || role == "إدارة");
        }

        [HttpGet]
        public async Task<IActionResult> Pending()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var requests = await _context.ExamPublishRequests
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Teacher)
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Course)
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Questions)
                .Where(r => r.Status == "Pending")
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            var vm = new ApprovalPendingPageVm
            {
                TotalPending = requests.Count,
                TotalQuestions = requests.Sum(r => r.Exam?.Questions?.Count ?? 0),
                TotalPoints = requests.Sum(r => r.Exam?.Questions?.Sum(q => q.Points) ?? 0),
                ReadyToPublish = requests.Count(r =>
                {
                    var qCount = r.Exam?.Questions?.Count ?? 0;
                    var pts = r.Exam?.Questions?.Sum(q => q.Points) ?? 0;
                    return qCount > 0 && pts > 0;
                }),
                NeedReview = requests.Count(r =>
                {
                    var qCount = r.Exam?.Questions?.Count ?? 0;
                    var pts = r.Exam?.Questions?.Sum(q => q.Points) ?? 0;
                    return qCount == 0 || pts == 0;
                }),
                Requests = requests.Select(r => new ApprovalRequestItemVm
                {
                    RequestId = r.RequestId,
                    ExamId = r.ExamId,
                    ExamTitle = r.Exam?.Title ?? "-",
                    CourseName = r.Exam?.Course?.Name ?? r.Exam?.CourseName ?? "-",
                    TeacherName = r.Exam?.Teacher?.FullName ?? r.Teacher?.FullName ?? "-",
                    TeacherEmail = r.Exam?.Teacher?.Email ?? r.Teacher?.Email ?? "-",
                    Status = r.Status ?? "-",
                    RequestedAt = r.RequestedAt,
                    IsPublished = r.Exam?.IsPublished ?? false,
                    QuestionCount = r.Exam?.Questions?.Count ?? 0,
                    TotalPoints = r.Exam?.Questions?.Sum(q => q.Points) ?? 0,
                    TotalTimeSeconds = r.Exam?.Questions?.Sum(q => q.TimeLimitSeconds ?? 0) ?? 0,
                    AdminNote = r.AdminNote
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var req = await _context.ExamPublishRequests
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Teacher)
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Course)
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(q => q.QuestionChoices)
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(q => q.AnswerKeyItems)
                .FirstOrDefaultAsync(r => r.RequestId == id);

            if (req == null || req.Exam == null)
                return NotFound();

            var exam = req.Exam;

            var vm = new ApprovalDetailsVm
            {
                RequestId = req.RequestId,
                ExamId = exam.ExamId,
                ExamTitle = exam.Title ?? "-",
                CourseName = exam.Course?.Name ?? exam.CourseName ?? "-",
                TeacherName = exam.Teacher?.FullName ?? "-",
                TeacherEmail = exam.Teacher?.Email ?? "-",
                Status = req.Status ?? "-",
                RequestedAt = req.RequestedAt,
                IsPublished = exam.IsPublished,
                QuestionCount = exam.Questions?.Count ?? 0,
                TotalPoints = exam.Questions?.Sum(q => q.Points) ?? 0,
                TotalTimeSeconds = exam.Questions?.Sum(q => q.TimeLimitSeconds ?? 0) ?? 0,
                AdminNote = req.AdminNote,
                Questions = exam.Questions
                    .OrderBy(q => q.SortOrder)
                    .Select(q =>
                    {
                        var key = q.AnswerKeyItems.FirstOrDefault(k => k.ExamId == exam.ExamId && k.QuestionId == q.QuestionId);
                        return new ApprovalQuestionVm
                        {
                            QuestionId = q.QuestionId,
                            SortOrder = q.SortOrder,
                            QuestionType = q.QuestionType ?? "-",
                            Body = q.Body ?? "-",
                            Points = q.Points,
                            TimeLimitSeconds = q.TimeLimitSeconds,
                            CorrectChoiceId = key?.CorrectChoiceId,
                            CorrectBool = key?.CorrectBool,
                            ModelAnswer = key?.ModelAnswer,
                            Choices = q.QuestionChoices
                                .OrderBy(c => c.SortOrder)
                                .Select(c => new ApprovalChoiceVm
                                {
                                    ChoiceId = c.ChoiceId,
                                    SortOrder = c.SortOrder,
                                    ChoiceText = c.ChoiceText ?? "",
                                    IsCorrect = key?.CorrectChoiceId == c.ChoiceId
                                }).ToList()
                        };
                    }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAll(ApprovalDetailsVm vm)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            if (vm?.Questions == null || vm.Questions.Count == 0) return RedirectToAction(nameof(Pending));

            var request = await _context.ExamPublishRequests.Include(r => r.Exam)
                .FirstOrDefaultAsync(r => r.RequestId == vm.RequestId && r.ExamId == vm.ExamId);
            if (request?.Exam == null) return NotFound();
            if (request.Status != "Pending" || request.Exam.Status != "PendingReview" || request.Exam.IsPublished ||
                await _context.ExamAttempts.AnyAsync(a => a.ExamId == vm.ExamId))
            {
                TempData["Error"] = "لا يمكن تعديل اختبار خرج من حالة المراجعة أو بدأت عليه محاولات.";
                return RedirectToAction(nameof(Pending));
            }

            foreach (var qvm in vm.Questions)
            {
                var question = await _context.Questions.Include(q => q.QuestionChoices)
                    .FirstOrDefaultAsync(x => x.QuestionId == qvm.QuestionId && x.ExamId == vm.ExamId);
                if (question == null) continue;

                var body = (qvm.Body ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(body) || qvm.Points <= 0 || qvm.TimeLimitSeconds is <= 0)
                {
                    TempData["Error"] = $"تحقق من نص ودرجة ووقت السؤال رقم {qvm.SortOrder}.";
                    return RedirectToAction(nameof(Details), new { id = vm.RequestId });
                }

                question.Body = body;
                question.Points = Math.Min(qvm.Points, 9999m);
                question.TimeLimitSeconds = qvm.TimeLimitSeconds;

                if (qvm.Choices != null && question.QuestionType == "MCQ")
                {
                    foreach (var choiceVm in qvm.Choices)
                    {
                        var choice = question.QuestionChoices.FirstOrDefault(x => x.ChoiceId == choiceVm.ChoiceId);
                        if (choice == null) continue;
                        var text = (choiceVm.ChoiceText ?? string.Empty).Trim();
                        choice.ChoiceText = string.IsNullOrWhiteSpace(text) ? $"خيار {choice.SortOrder}" : text;
                        choice.SortOrder = Math.Max(1, choiceVm.SortOrder);
                    }
                }

                var key = await _context.AnswerKeyItems
                    .FirstOrDefaultAsync(x => x.ExamId == vm.ExamId && x.QuestionId == qvm.QuestionId);
                if (key == null)
                {
                    key = new AnswerKeyItem
                    {
                        ExamId = vm.ExamId, QuestionId = qvm.QuestionId,
                        UploadedByTeacherId = request.Exam.TeacherId, UploadedAt = YemenTime.UtcNow
                    };
                    _context.AnswerKeyItems.Add(key);
                }

                switch (question.QuestionType)
                {
                    case "MCQ":
                        if (qvm.CorrectChoiceId.HasValue && !question.QuestionChoices.Any(c => c.ChoiceId == qvm.CorrectChoiceId.Value))
                        {
                            TempData["Error"] = $"الإجابة الصحيحة للسؤال رقم {qvm.SortOrder} لا تتبع السؤال.";
                            return RedirectToAction(nameof(Details), new { id = vm.RequestId });
                        }
                        key.CorrectChoiceId = qvm.CorrectChoiceId; key.CorrectBool = null; key.ModelAnswer = null;
                        break;
                    case "TF":
                        key.CorrectChoiceId = null; key.CorrectBool = qvm.CorrectBool; key.ModelAnswer = null;
                        break;
                    case "Essay":
                        key.CorrectChoiceId = null; key.CorrectBool = null; key.ModelAnswer = string.IsNullOrWhiteSpace(qvm.ModelAnswer) ? null : qvm.ModelAnswer.Trim();
                        break;
                }
                key.UploadedByTeacherId = request.Exam.TeacherId;
                key.UploadedAt = YemenTime.UtcNow;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                ActorUserId = HttpContext.Session.GetInt32("UserId"), Action = "Admin.Exam.ReviewEdit",
                Details = $"ExamId={vm.ExamId}; RequestId={vm.RequestId}", CreatedAt = YemenTime.UtcNow
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم حفظ تعديلات المراجعة بنجاح.";
            return RedirectToAction(nameof(Details), new { id = vm.RequestId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int requestId, string? adminNote)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var req = await _context.ExamPublishRequests
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Teacher)
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(x => x.RequestId == requestId);

            if (req == null || req.Exam == null)
                return NotFound();

            var exam = req.Exam;
            if (req.Status != "Pending" || exam.Status != "PendingReview" || exam.IsPublished ||
                await _context.ExamAttempts.AnyAsync(a => a.ExamId == exam.ExamId))
            {
                TempData["Error"] = "الطلب لم يعد في حالة تسمح بالاعتماد.";
                return RedirectToAction(nameof(Pending));
            }
            if (!exam.Course.IsActive || exam.DurationMinutes <= 0 || exam.PassPercentage is < 0 or > 100 ||
                (exam.AvailableFrom.HasValue && exam.AvailableTo.HasValue && exam.AvailableTo <= exam.AvailableFrom))
            {
                TempData["Error"] = "إعدادات المقرر أو مدة الاختبار أو نسبة النجاح غير صحيحة.";
                return RedirectToAction(nameof(Details), new { id = requestId });
            }
            var questionCount = await _context.Questions.CountAsync(q => q.ExamId == exam.ExamId);
            var totalPoints = await _context.Questions
                .Where(q => q.ExamId == exam.ExamId)
                .SumAsync(q => q.Points);

            if (questionCount == 0 || totalPoints == 0)
            {
                TempData["Error"] = "لا يمكن اعتماد الاختبار لأنه لا يحتوي على أسئلة أو درجات.";
                return RedirectToAction(nameof(Details), new { id = requestId });
            }

            var questions = await _context.Questions.Where(q => q.ExamId == exam.ExamId).ToListAsync();
            var questionIds = questions.Select(q => q.QuestionId).ToList();
            var keys = await _context.AnswerKeyItems.Where(k => k.ExamId == exam.ExamId).ToListAsync();
            var validChoiceIds = (await _context.QuestionChoices
                .Where(c => questionIds.Contains(c.QuestionId))
                .Select(c => new { c.QuestionId, c.ChoiceId }).ToListAsync())
                .Select(x => (x.QuestionId, x.ChoiceId)).ToHashSet();
            var incomplete = questions.Any(q =>
            {
                var key = keys.FirstOrDefault(k => k.QuestionId == q.QuestionId);
                if (q.Points <= 0 || q.TimeLimitSeconds is <= 0 || string.IsNullOrWhiteSpace(q.Body)) return true;
                return key == null || q.QuestionType switch
                {
                    "MCQ" => !key.CorrectChoiceId.HasValue || !validChoiceIds.Contains((q.QuestionId, key.CorrectChoiceId.Value)),
                    "TF" => !key.CorrectBool.HasValue,
                    "Essay" => string.IsNullOrWhiteSpace(key.ModelAnswer),
                    _ => true
                };
            });
            if (incomplete)
            {
                TempData["Error"] = "لا يمكن اعتماد الاختبار قبل اكتمال نموذج الإجابة لجميع الأسئلة.";
                return RedirectToAction(nameof(Details), new { id = requestId });
            }

            adminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
            if (adminNote?.Length > 500) adminNote = adminNote[..500];
            req.Status = "Approved";
            req.AdminNote = adminNote;
            req.ReviewedAt = YemenTime.UtcNow;
            req.ReviewedByAdminId = HttpContext.Session.GetInt32("UserId");

            exam.IsPublished = true;
            exam.Status = "Published";
            _context.AuditLogs.Add(new AuditLog { ActorUserId = req.ReviewedByAdminId, Action = "Admin.Exam.Approve", Details = $"ExamId={exam.ExamId}; RequestId={requestId}", CreatedAt = YemenTime.UtcNow });

            await _context.SaveChangesAsync();

            try
            {
                var teacherEmail = exam.Teacher?.Email;
                if (!string.IsNullOrWhiteSpace(teacherEmail))
                {
                    await _emailSender.SendAndLogAsync(
                        exam.TeacherId,
                        teacherEmail,
                        "تم اعتماد الاختبار",
                        $"تم اعتماد اختبارك: {exam.Title}\n\nملاحظة المدير:\n{adminNote ?? "-"}"
                    );
                }
            }
            catch
            {
                // تجاهل أخطاء الإرسال حتى لا ينهار النظام
            }

            TempData["Success"] = "تم اعتماد الاختبار وإرسال الإشعار للدكتور.";
            return RedirectToAction(nameof(Pending));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int requestId, string? adminNote)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            var req = await _context.ExamPublishRequests
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Teacher)
                .Include(r => r.Exam)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(x => x.RequestId == requestId);

            if (req == null || req.Exam == null)
                return NotFound();

            var exam = req.Exam;
            if (req.Status != "Pending" || exam.Status != "PendingReview" || exam.IsPublished ||
                await _context.ExamAttempts.AnyAsync(a => a.ExamId == exam.ExamId))
            {
                TempData["Error"] = "الطلب لم يعد في حالة تسمح بالرفض.";
                return RedirectToAction(nameof(Pending));
            }
            adminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
            if (adminNote?.Length > 500) adminNote = adminNote[..500];

            req.Status = "Rejected";
            req.AdminNote = adminNote;
            req.ReviewedAt = YemenTime.UtcNow;
            req.ReviewedByAdminId = HttpContext.Session.GetInt32("UserId");
            exam.IsPublished = false;
            exam.Status = "Rejected";
            _context.AuditLogs.Add(new AuditLog { ActorUserId = req.ReviewedByAdminId, Action = "Admin.Exam.Reject", Details = $"ExamId={exam.ExamId}; RequestId={requestId}; Note={adminNote}", CreatedAt = YemenTime.UtcNow });

            await _context.SaveChangesAsync();

            try
            {
                var teacherEmail = exam.Teacher?.Email;
                if (!string.IsNullOrWhiteSpace(teacherEmail))
                {
                    await _emailSender.SendAndLogAsync(
                        exam.TeacherId,
                        teacherEmail,
                        "تم رفض الاختبار",
                        $"تم رفض اختبارك: {exam.Title}\n\nسبب الرفض:\n{adminNote ?? "-"}"
                    );
                }
            }
            catch
            {
                // تجاهل أخطاء الإرسال حتى لا ينهار النظام
            }

            TempData["Success"] = "تم رفض الاختبار وإرسال الإشعار للدكتور.";
            return RedirectToAction(nameof(Pending));
        }
    }
}
