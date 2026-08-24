using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

public partial class UniRemoteExamDbContext : DbContext
{
    public UniRemoteExamDbContext(DbContextOptions<UniRemoteExamDbContext> options) : base(options) { }

    public DbSet<AcademicTerm> AcademicTerms => Set<AcademicTerm>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();
    public DbSet<AnswerKeyItem> AnswerKeyItems => Set<AnswerKeyItem>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<ExamPublishRequest> ExamPublishRequests => Set<ExamPublishRequest>();
    public DbSet<ManualScore> ManualScores => Set<ManualScore>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionChoice> QuestionChoices => Set<QuestionChoice>();
    public DbSet<ProctorEvent> ProctorEvents => Set<ProctorEvent>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<StudentNotice> StudentNotices => Set<StudentNotice>();
    public DbSet<StudentNoticeRead> StudentNoticeReads => Set<StudentNoticeRead>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(entity =>
        {
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<AcademicTerm>(entity =>
        {
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.ToTable(t => t.HasCheckConstraint("CK_AcademicTerms_Dates", "[EndDate] > [StartDate]"));
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasOne(e => e.Department).WithMany(e => e.Courses).HasForeignKey(e => e.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AcademicTerm).WithMany(e => e.Courses).HasForeignKey(e => e.AcademicTermId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Teacher).WithMany(e => e.CoursesTaught).HasForeignKey(e => e.TeacherId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CourseEnrollment>(entity =>
        {
            entity.Property(e => e.EnrolledAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasOne(e => e.Course).WithMany(e => e.Enrollments).HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Student).WithMany(e => e.CourseEnrollments).HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AnswerKeyItem>(entity =>
        {
            entity.HasKey(e => new { e.ExamId, e.QuestionId });
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(e => e.CorrectChoice).WithMany(e => e.AnswerKeyItems).HasForeignKey(e => e.CorrectChoiceId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Exam).WithMany(e => e.AnswerKeyItems).HasForeignKey(e => e.ExamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Question).WithMany(e => e.AnswerKeyItems).HasForeignKey(e => e.QuestionId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.UploadedByTeacher).WithMany(e => e.AnswerKeyItems).HasForeignKey(e => e.UploadedByTeacherId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AttemptAnswer>(entity =>
        {
            entity.HasIndex(e => new { e.AttemptId, e.QuestionId }).IsUnique();
            entity.HasOne(e => e.Attempt).WithMany(e => e.AttemptAnswers).HasForeignKey(e => e.AttemptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Question).WithMany(e => e.AttemptAnswers).HasForeignKey(e => e.QuestionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SelectedChoice).WithMany(e => e.AttemptAnswers).HasForeignKey(e => e.SelectedChoiceId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(e => e.ActorUser).WithMany(e => e.AuditLogs).HasForeignKey(e => e.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<EmailLog>(entity =>
        {
            entity.Property(e => e.SentAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.Status).HasDefaultValue("Queued");
            entity.HasOne(e => e.User).WithMany(e => e.EmailLogs).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.Status).HasDefaultValue("Draft");
            entity.Property(e => e.MaxAttempts).HasDefaultValue(1);
            entity.Property(e => e.DurationMinutes).HasDefaultValue(60);
            entity.Property(e => e.PassPercentage).HasDefaultValue(50m);
            entity.Property(e => e.AutoSubmitOnExpiry).HasDefaultValue(true);
            entity.HasOne(e => e.Course).WithMany(e => e.Exams).HasForeignKey(e => e.CourseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Teacher).WithMany(e => e.Exams).HasForeignKey(e => e.TeacherId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Exams_Status", "[Status] IN ('Draft','PendingReview','Rejected','Published','Closed','Archived')");
                t.HasCheckConstraint("CK_Exams_MaxAttempts", "[MaxAttempts] > 0");
                t.HasCheckConstraint("CK_Exams_Duration", "[DurationMinutes] > 0");
                t.HasCheckConstraint("CK_Exams_PassPercentage", "[PassPercentage] >= 0 AND [PassPercentage] <= 100");
                t.HasCheckConstraint("CK_Exams_Window", "[AvailableTo] IS NULL OR [AvailableFrom] IS NULL OR [AvailableTo] > [AvailableFrom]");
            });
        });

        modelBuilder.Entity<ExamAttempt>(entity =>
        {
            entity.Property(e => e.StartedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.Status).HasDefaultValue("Started");
            entity.HasIndex(e => new { e.ExamId, e.StudentId, e.Status });
            entity.HasIndex(e => new { e.ExamId, e.StudentId }).IsUnique().HasFilter("[Status] = 'Started'");
            entity.HasOne(e => e.Exam).WithMany(e => e.ExamAttempts).HasForeignKey(e => e.ExamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Student).WithMany(e => e.ExamAttempts).HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.FinalizedByUser).WithMany(e => e.FinalizedAttempts).HasForeignKey(e => e.FinalizedByUserId).OnDelete(DeleteBehavior.NoAction);
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Attempts_Status", "[Status] IN ('Started','Submitted','Closed')");
                t.HasCheckConstraint("CK_Attempts_Percentage", "[Percentage] IS NULL OR ([Percentage] >= 0 AND [Percentage] <= 100)");
            });
        });

        modelBuilder.Entity<ExamPublishRequest>(entity =>
        {
            entity.Property(e => e.RequestedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.Status).HasDefaultValue("Pending");
            entity.HasOne(e => e.Exam).WithMany(e => e.ExamPublishRequests).HasForeignKey(e => e.ExamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ReviewedByAdmin).WithMany(e => e.ExamPublishRequestReviewedByAdmins).HasForeignKey(e => e.ReviewedByAdminId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Teacher).WithMany(e => e.ExamPublishRequestTeachers).HasForeignKey(e => e.TeacherId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_EPR_Status", "[Status] IN ('Pending','Approved','Rejected')"));
        });

        modelBuilder.Entity<ManualScore>(entity =>
        {
            entity.Property(e => e.GradedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(e => new { e.AttemptId, e.QuestionId }).IsUnique();
            entity.HasOne(e => e.Attempt).WithMany(e => e.ManualScores).HasForeignKey(e => e.AttemptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.GradedByControl).WithMany(e => e.ManualScores).HasForeignKey(e => e.GradedByControlId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Question).WithMany(e => e.ManualScores).HasForeignKey(e => e.QuestionId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(t => t.HasCheckConstraint("CK_ManualScores_NonNegative", "[Score] >= 0"));
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.Property(e => e.Points).HasDefaultValue(1m);
            entity.Property(e => e.SortOrder).HasDefaultValue(1);
            entity.HasOne(e => e.Exam).WithMany(e => e.Questions).HasForeignKey(e => e.ExamId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.ExamId, e.SortOrder });
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Questions_Type", "[QuestionType] IN ('MCQ','TF','Essay')");
                t.HasCheckConstraint("CK_Questions_Points", "[Points] > 0");
                t.HasCheckConstraint("CK_Questions_Time", "[TimeLimitSeconds] IS NULL OR [TimeLimitSeconds] > 0");
            });
        });

        modelBuilder.Entity<QuestionChoice>(entity =>
        {
            entity.Property(e => e.SortOrder).HasDefaultValue(1);
            entity.HasOne(e => e.Question).WithMany(e => e.QuestionChoices).HasForeignKey(e => e.QuestionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.QuestionId, e.SortOrder });
        });

        modelBuilder.Entity<ProctorEvent>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(e => e.Attempt).WithMany(e => e.ProctorEvents).HasForeignKey(e => e.AttemptId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Question).WithMany(e => e.ProctorEvents).HasForeignKey(e => e.QuestionId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(e => e.Student).WithMany(e => e.ProctorEvents).HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StudentNotice>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<StudentNoticeRead>(entity =>
        {
            entity.HasKey(e => new { e.NoticeId, e.StudentId });
            entity.Property(e => e.ReadAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(e => e.Notice).WithMany(e => e.StudentNoticeReads).HasForeignKey(e => e.NoticeId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Student).WithMany(e => e.StudentNoticeReads).HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StudentProfile>(entity =>
        {
            entity.Property(e => e.StudentId).ValueGeneratedNever();
            entity.HasOne(e => e.Student).WithOne(e => e.StudentProfile).HasForeignKey<StudentProfile>(e => e.StudentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TeacherProfile>(entity =>
        {
            entity.Property(e => e.TeacherId).ValueGeneratedNever();
            entity.HasOne(e => e.Teacher).WithOne(e => e.TeacherProfile).HasForeignKey<TeacherProfile>(e => e.TeacherId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.FailedLoginCount).HasDefaultValue(0);
            entity.HasOne(e => e.Role).WithMany(e => e.Users).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
