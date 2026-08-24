namespace UniRemoteExam.Areas.Admin.ViewModels;

public class AuditPageVm
{
    public string? Search { get; set; }
    public string? ActionFilter { get; set; }
    public int TotalLogs { get; set; }
    public int TodayLogs { get; set; }
    public int UserActions { get; set; }
    public int ApprovalActions { get; set; }
    public List<AuditItemVm> Logs { get; set; } = new();
}

public class AuditItemVm
{
    public int AuditId { get; set; }
    public string ActorName { get; set; } = "-";
    public string ActorEmail { get; set; } = "-";
    public string Action { get; set; } = "-";
    public string Details { get; set; } = "-";
    public DateTime CreatedAt { get; set; }
}

public class AdminSettingsVm
{
    public string DeanName { get; set; } = "العميد / عبدالملك محسن عواد";
    public string HeaderSubtitle { get; set; } = "منصة جامعية لإدارة الاختبارات الإلكترونية عن بُعد";
    public string LogoUrl { get; set; } = "/images/sanaa-university-logo.jpg";
    public string FooterText { get; set; } = "نظام الاختبارات عن بُعد - مشروع تخرج 2026 - جامعة صنعاء";
    public bool ShowSystemStatus { get; set; } = true;
}
