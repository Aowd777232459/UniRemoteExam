namespace UniRemoteExam.Areas.Control.ViewModels;

public class StudentProvisionVm
{
    public List<StudentAccountRow> Students { get; set; } = new();
}

public class StudentAccountRow
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string StudentNumber { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
