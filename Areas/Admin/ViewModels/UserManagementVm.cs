using System.ComponentModel.DataAnnotations;

namespace UniRemoteExam.Areas.Admin.ViewModels;

public class UserManagementPageVm
{
    public string? Search { get; set; }
    public string? RoleFilter { get; set; }
    public string? ActiveFilter { get; set; }

    public int TotalUsers { get; set; }
    public int TotalStudents { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalControl { get; set; }
    public int TotalAdmins { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }

    public List<UserListItemVm> Users { get; set; } = new();
}

public class UserListItemVm
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "-";
    public string Email { get; set; } = "-";
    public string RoleName { get; set; } = "-";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? StudentNumber { get; set; }
    public string? Level { get; set; }
    public string? Department { get; set; }
}

public class UserFormVm
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "الاسم مطلوب")]
    [StringLength(200, ErrorMessage = "الاسم طويل جدًا")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    [StringLength(255, ErrorMessage = "البريد طويل جدًا")]
    public string Email { get; set; } = "";

    [StringLength(255, ErrorMessage = "كلمة المرور طويلة جدًا")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "الصلاحية مطلوبة")]
    public string RoleName { get; set; } = "Student";

    public bool IsActive { get; set; } = true;

    [StringLength(50, ErrorMessage = "الرقم الجامعي طويل جدًا")]
    public string? StudentNumber { get; set; }

    [StringLength(50, ErrorMessage = "المستوى طويل جدًا")]
    public string? Level { get; set; }

    [StringLength(150, ErrorMessage = "القسم طويل جدًا")]
    public string? Department { get; set; }
}
