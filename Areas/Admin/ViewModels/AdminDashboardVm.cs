namespace UniRemoteExam.Areas.Admin.ViewModels;

public class AdminDashboardVm
{
    public int TotalStudents { get; set; }
    public int ActiveStudents { get; set; }

    public int TotalTeachers { get; set; }
    public int ActiveTeachers { get; set; }

    public int TotalExams { get; set; }
    public int PublishedExams { get; set; }
    public int DraftExams { get; set; }

    public int PendingPublishRequests { get; set; }
    public int ApprovedPublishRequests { get; set; }
    public int RejectedPublishRequests { get; set; }

    public int StartedAttempts { get; set; }
    public int SubmittedAttempts { get; set; }
    public int ClosedAttempts { get; set; }
    public int TotalAttempts { get; set; }

    public int LivePercent { get; set; }

    public List<PendingPublishItemVm> PendingRequests { get; set; } = new();
    public List<RecentExamItemVm> RecentExams { get; set; } = new();
    public List<RecentAttemptItemVm> RecentAttempts { get; set; } = new();
}

public class PendingPublishItemVm
{
    public int RequestId { get; set; }
    public string ExamTitle { get; set; } = "-";
    public string CourseName { get; set; } = "-";
    public string TeacherName { get; set; } = "-";
    public DateTime? RequestedAt { get; set; }
    public string Status { get; set; } = "-";
}

public class RecentExamItemVm
{
    public int ExamId { get; set; }
    public string Title { get; set; } = "-";
    public string CourseName { get; set; } = "-";
    public bool IsPublished { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class RecentAttemptItemVm
{
    public int AttemptId { get; set; }
    public string StudentName { get; set; } = "-";
    public string ExamTitle { get; set; } = "-";
    public string Status { get; set; } = "-";
    public DateTime? StartedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}