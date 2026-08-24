using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

[Index(nameof(Email), IsUnique = true)]
public partial class User
{
    [Key]
    public int UserId { get; set; }

    [Required, StringLength(255)]
    public string Email { get; set; } = null!;

    [Required, StringLength(500)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(200)]
    public string? FullName { get; set; }

    public int RoleId { get; set; }
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(RoleId))]
    [InverseProperty(nameof(Role.Users))]
    public Role Role { get; set; } = null!;

    [InverseProperty(nameof(AnswerKeyItem.UploadedByTeacher))]
    public ICollection<AnswerKeyItem> AnswerKeyItems { get; set; } = new List<AnswerKeyItem>();
    [InverseProperty(nameof(AuditLog.ActorUser))]
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    [InverseProperty(nameof(EmailLog.User))]
    public ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();
    [InverseProperty(nameof(ExamAttempt.Student))]
    public ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();
    [InverseProperty(nameof(ExamAttempt.FinalizedByUser))]
    public ICollection<ExamAttempt> FinalizedAttempts { get; set; } = new List<ExamAttempt>();
    [InverseProperty(nameof(ExamPublishRequest.ReviewedByAdmin))]
    public ICollection<ExamPublishRequest> ExamPublishRequestReviewedByAdmins { get; set; } = new List<ExamPublishRequest>();
    [InverseProperty(nameof(ExamPublishRequest.Teacher))]
    public ICollection<ExamPublishRequest> ExamPublishRequestTeachers { get; set; } = new List<ExamPublishRequest>();
    [InverseProperty(nameof(Exam.Teacher))]
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    [InverseProperty(nameof(Course.Teacher))]
    public ICollection<Course> CoursesTaught { get; set; } = new List<Course>();
    [InverseProperty(nameof(CourseEnrollment.Student))]
    public ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();
    [InverseProperty(nameof(ManualScore.GradedByControl))]
    public ICollection<ManualScore> ManualScores { get; set; } = new List<ManualScore>();
    [InverseProperty(nameof(StudentNoticeRead.Student))]
    public ICollection<StudentNoticeRead> StudentNoticeReads { get; set; } = new List<StudentNoticeRead>();
    [InverseProperty(nameof(StudentProfile.Student))]
    public StudentProfile? StudentProfile { get; set; }
    [InverseProperty(nameof(TeacherProfile.Teacher))]
    public TeacherProfile? TeacherProfile { get; set; }
    [InverseProperty(nameof(ProctorEvent.Student))]
    public ICollection<ProctorEvent> ProctorEvents { get; set; } = new List<ProctorEvent>();
}
