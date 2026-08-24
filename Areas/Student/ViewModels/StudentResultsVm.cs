namespace UniRemoteExam.Areas.Student.ViewModels;

public class StudentResultsVm
{
    public string StudentName { get; set; } = "";
    public string StudentEmail { get; set; } = "";
    public List<StudentResultRow> Rows { get; set; } = new();
    public int CompletedCount { get; set; }
    public decimal AveragePercent { get; set; }
}

public class StudentResultRow
{
    public int AttemptId { get; set; }
    public string ExamTitle { get; set; } = "";
    public string CourseName { get; set; } = "";
    public decimal FinalScore { get; set; }
    public decimal MaximumScore { get; set; }
    public decimal Percentage { get; set; }
    public decimal PassPercentage { get; set; }
    public bool? Passed { get; set; }
    public string Status { get; set; } = "";
    public bool CanReview { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
}

public class StudentReviewVm
{
    public int AttemptId { get; set; }
    public string ExamTitle { get; set; } = "";
    public string CourseName { get; set; } = "";
    public decimal FinalScore { get; set; }
    public decimal MaximumScore { get; set; }
    public decimal Percentage { get; set; }
    public List<StudentReviewQuestionVm> Questions { get; set; } = new();
}

public class StudentReviewQuestionVm
{
    public int SortOrder { get; set; }
    public string Type { get; set; } = "";
    public string Body { get; set; } = "";
    public decimal Points { get; set; }
    public string StudentAnswer { get; set; } = "لم تتم الإجابة";
    public string CorrectAnswer { get; set; } = "";
    public decimal AwardedScore { get; set; }
    public bool IsCorrect { get; set; }
}
