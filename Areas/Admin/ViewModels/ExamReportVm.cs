namespace UniRemoteExam.Areas.Admin.ViewModels;

public class ExamReportVm
{
    public int ExamId { get; set; }
    public string? ExamTitle { get; set; }
    public string? CourseName { get; set; }
    public decimal MaxScore { get; set; }
    public decimal PassPercent { get; set; }
    public decimal PassMark { get; set; }
    public int StartedCount { get; set; }
    public int SubmittedCount { get; set; }
    public int ClosedCount { get; set; }
    public decimal AvgScore { get; set; }
    public decimal MaxScoreAchieved { get; set; }
    public decimal MinScoreAchieved { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public int PassRatePercent { get; set; }
    public List<ResultRow> Rows { get; set; } = new();
    public List<ResultRow> Top5 { get; set; } = new();
}

public class ResultRow
{
    public int AttemptId { get; set; }
    public string StudentName { get; set; } = "-";
    public string StudentEmail { get; set; } = "-";
    public string Status { get; set; } = "-";
    public decimal AutoScore { get; set; }
    public decimal ManualScore { get; set; }
    public decimal TotalScore { get; set; }
    public decimal MaximumScore { get; set; }
    public decimal Percentage { get; set; }
    public bool? IsPassed { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? FinalizedAt { get; set; }
}
