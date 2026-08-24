namespace UniRemoteExam.Areas.Admin.ViewModels;

public class ApprovalPendingPageVm
{
    public int TotalPending { get; set; }
    public int TotalQuestions { get; set; }
    public decimal TotalPoints { get; set; }
    public int ReadyToPublish { get; set; }
    public int NeedReview { get; set; }

    public List<ApprovalRequestItemVm> Requests { get; set; } = new();
}

public class ApprovalRequestItemVm
{
    public int RequestId { get; set; }
    public int ExamId { get; set; }

    public string ExamTitle { get; set; } = "-";
    public string CourseName { get; set; } = "-";
    public string TeacherName { get; set; } = "-";
    public string TeacherEmail { get; set; } = "-";
    public string Status { get; set; } = "-";

    public DateTime RequestedAt { get; set; }
    public bool IsPublished { get; set; }

    public int QuestionCount { get; set; }
    public decimal TotalPoints { get; set; }
    public int TotalTimeSeconds { get; set; }
    public string? AdminNote { get; set; }

    public bool CanApprove => QuestionCount > 0 && TotalPoints > 0;
}

public class ApprovalDetailsVm
{
    public int RequestId { get; set; }
    public int ExamId { get; set; }

    public string ExamTitle { get; set; } = "-";
    public string CourseName { get; set; } = "-";
    public string TeacherName { get; set; } = "-";
    public string TeacherEmail { get; set; } = "-";
    public string Status { get; set; } = "-";

    public DateTime RequestedAt { get; set; }
    public bool IsPublished { get; set; }

    public int QuestionCount { get; set; }
    public decimal TotalPoints { get; set; }
    public int TotalTimeSeconds { get; set; }
    public string? AdminNote { get; set; }

    public bool CanApprove => QuestionCount > 0 && TotalPoints > 0;

    public List<ApprovalQuestionVm> Questions { get; set; } = new();
}

public class ApprovalQuestionVm
{
    public int QuestionId { get; set; }
    public int SortOrder { get; set; }

    public string QuestionType { get; set; } = "-";
    public string Body { get; set; } = "-";

    public decimal Points { get; set; }
    public int? TimeLimitSeconds { get; set; }

    public int? CorrectChoiceId { get; set; }
    public bool? CorrectBool { get; set; }
    public string? ModelAnswer { get; set; }

    public List<ApprovalChoiceVm> Choices { get; set; } = new();
}

public class ApprovalChoiceVm
{
    public int ChoiceId { get; set; }
    public int SortOrder { get; set; }
    public string ChoiceText { get; set; } = "";
    public bool IsCorrect { get; set; }
}