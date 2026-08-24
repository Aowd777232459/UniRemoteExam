using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Services;

namespace UniRemoteExam.Data;

public static class LanDatabaseInitializer
{
    public static async Task InitializeAsync(
        UniRemoteExamDbContext db,
        PasswordService passwords,
        CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
        if (await db.Roles.AnyAsync(cancellationToken)) return;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var adminRole = new Role { RoleName = "Admin" };
        var teacherRole = new Role { RoleName = "Teacher" };
        var studentRole = new Role { RoleName = "Student" };
        var controlRole = new Role { RoleName = "Control" };
        db.Roles.AddRange(adminRole, teacherRole, studentRole, controlRole);
        await db.SaveChangesAsync(cancellationToken);

        var admin = CreateUser("admin@sanaau.edu.ye", "مدير النظام", adminRole.RoleId, now);
        var teacher = CreateUser("teacher@sanaau.edu.ye", "د. أحمد محمد", teacherRole.RoleId, now);
        var control = CreateUser("control@sanaau.edu.ye", "موظف الكنترول", controlRole.RoleId, now);
        var student1 = CreateUser("student1@sanaau.edu.ye", "سارة علي", studentRole.RoleId, now);
        var student2 = CreateUser("student2@sanaau.edu.ye", "محمد عبدالله", studentRole.RoleId, now);
        var student3 = CreateUser("student3@sanaau.edu.ye", "أروى حسن", studentRole.RoleId, now);

        foreach (var user in new[] { admin, teacher, control, student1, student2, student3 })
            user.PasswordHash = passwords.Hash(user, "Demo@12345");

        db.Users.AddRange(admin, teacher, control, student1, student2, student3);
        await db.SaveChangesAsync(cancellationToken);

        db.TeacherProfiles.Add(new TeacherProfile
        {
            TeacherId = teacher.UserId,
            Department = "علوم الحاسوب"
        });
        db.StudentProfiles.AddRange(
            new StudentProfile { StudentId = student1.UserId, StudentNumber = "2026001", Level = "الرابع" },
            new StudentProfile { StudentId = student2.UserId, StudentNumber = "2026002", Level = "الرابع" },
            new StudentProfile { StudentId = student3.UserId, StudentNumber = "2026003", Level = "الرابع" });

        var department = new Department { Code = "CS", Name = "علوم الحاسوب", IsActive = true };
        var term = new AcademicTerm
        {
            Name = "2026/2027 - الفصل الأول",
            StartDate = new DateTime(2026, 9, 1),
            EndDate = new DateTime(2027, 1, 31),
            IsActive = true
        };
        db.Departments.Add(department);
        db.AcademicTerms.Add(term);
        await db.SaveChangesAsync(cancellationToken);

        var course = new Course
        {
            Code = "CS401",
            Name = "قواعد البيانات",
            DepartmentId = department.DepartmentId,
            AcademicTermId = term.AcademicTermId,
            TeacherId = teacher.UserId,
            Level = "الرابع",
            IsActive = true
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);

        db.CourseEnrollments.AddRange(
            Enrollment(course.CourseId, student1.UserId, now),
            Enrollment(course.CourseId, student2.UserId, now),
            Enrollment(course.CourseId, student3.UserId, now));

        var exam = new Exam
        {
            Title = "اختبار تجريبي في قواعد البيانات",
            CourseId = course.CourseId,
            CourseName = course.Name,
            TeacherId = teacher.UserId,
            Status = "Published",
            IsPublished = true,
            CreatedAt = now,
            AvailableFrom = now.AddDays(-30),
            AvailableTo = now.AddDays(365),
            MaxAttempts = 1,
            DurationMinutes = 45,
            PassPercentage = 50m,
            AutoSubmitOnExpiry = true,
            ShuffleQuestions = true,
            ShuffleChoices = true,
            ShowCorrectAnswers = true
        };
        db.Exams.Add(exam);
        await db.SaveChangesAsync(cancellationToken);

        var trueFalse = new Question
        {
            ExamId = exam.ExamId,
            QuestionType = "TF",
            Body = "قاعدة البيانات العلائقية تعتمد على الجداول.",
            Points = 1m,
            TimeLimitSeconds = 60,
            SortOrder = 1
        };
        var multipleChoice = new Question
        {
            ExamId = exam.ExamId,
            QuestionType = "MCQ",
            Body = "أي أمر يستخدم لاسترجاع البيانات؟",
            Points = 2m,
            TimeLimitSeconds = 120,
            SortOrder = 2
        };
        var essay = new Question
        {
            ExamId = exam.ExamId,
            QuestionType = "Essay",
            Body = "اشرح الفرق بين المفتاح الأساسي والمفتاح الأجنبي.",
            Points = 7m,
            TimeLimitSeconds = 300,
            SortOrder = 3
        };
        db.Questions.AddRange(trueFalse, multipleChoice, essay);
        await db.SaveChangesAsync(cancellationToken);

        var insertChoice = Choice(multipleChoice.QuestionId, "INSERT", 1);
        var selectChoice = Choice(multipleChoice.QuestionId, "SELECT", 2);
        var deleteChoice = Choice(multipleChoice.QuestionId, "DELETE", 3);
        var updateChoice = Choice(multipleChoice.QuestionId, "UPDATE", 4);
        db.QuestionChoices.AddRange(insertChoice, selectChoice, deleteChoice, updateChoice);
        await db.SaveChangesAsync(cancellationToken);

        db.AnswerKeyItems.AddRange(
            new AnswerKeyItem
            {
                ExamId = exam.ExamId,
                QuestionId = trueFalse.QuestionId,
                CorrectBool = true,
                UploadedByTeacherId = teacher.UserId,
                UploadedAt = now
            },
            new AnswerKeyItem
            {
                ExamId = exam.ExamId,
                QuestionId = multipleChoice.QuestionId,
                CorrectChoiceId = selectChoice.ChoiceId,
                UploadedByTeacherId = teacher.UserId,
                UploadedAt = now
            },
            new AnswerKeyItem
            {
                ExamId = exam.ExamId,
                QuestionId = essay.QuestionId,
                ModelAnswer = "المفتاح الأساسي يميز السجل، والمفتاح الأجنبي يربط الجداول.",
                UploadedByTeacherId = teacher.UserId,
                UploadedAt = now
            });
        db.ExamPublishRequests.Add(new ExamPublishRequest
        {
            ExamId = exam.ExamId,
            TeacherId = teacher.UserId,
            Status = "Approved",
            RequestedAt = now,
            ReviewedByAdminId = admin.UserId,
            ReviewedAt = now,
            AdminNote = "اختبار عرض معتمد."
        });
        db.StudentNotices.Add(new StudentNotice
        {
            Title = "تعليمات الاختبار",
            Body = "المدة الكلية ووقت السؤال يتحقق منهما السيرفر، وتُسلم المحاولة تلقائيًا عند انتهاء الوقت.",
            IsActive = true,
            CreatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static User CreateUser(string email, string fullName, int roleId, DateTime now) => new()
    {
        Email = email,
        FullName = fullName,
        PasswordHash = string.Empty,
        RoleId = roleId,
        IsActive = true,
        MustChangePassword = true,
        FailedLoginCount = 0,
        CreatedAt = now
    };

    private static CourseEnrollment Enrollment(int courseId, int studentId, DateTime now) => new()
    {
        CourseId = courseId,
        StudentId = studentId,
        EnrolledAt = now,
        IsActive = true
    };

    private static QuestionChoice Choice(int questionId, string text, int sortOrder) => new()
    {
        QuestionId = questionId,
        ChoiceText = text,
        SortOrder = sortOrder
    };
}
