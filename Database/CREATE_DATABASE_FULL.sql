/* UniRemoteExam v6 Final - إنشاء قاعدة غير تدميري */
IF DB_ID(N'UniRemoteExamDb_Final') IS NULL
    CREATE DATABASE [UniRemoteExamDb_Final];
GO
USE [UniRemoteExamDb_Final];
GO
/* UniRemoteExam v6 Final - المخطط النهائي. لا يحذف أي بيانات. */
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL
    THROW 51000, N'الجداول موجودة. استخدم 04_Upgrade_From_v5.sql بدل إنشاء المخطط من جديد.', 1;
GO

CREATE TABLE dbo.Roles(RoleId INT IDENTITY PRIMARY KEY, RoleName NVARCHAR(50) NOT NULL UNIQUE);
CREATE TABLE dbo.Users(
 UserId INT IDENTITY PRIMARY KEY, Email NVARCHAR(255) NOT NULL UNIQUE, PasswordHash NVARCHAR(500) NOT NULL,
 FullName NVARCHAR(200) NULL, RoleId INT NOT NULL, IsActive BIT NOT NULL DEFAULT(1), MustChangePassword BIT NOT NULL DEFAULT(0),
 FailedLoginCount INT NOT NULL DEFAULT(0), LockedUntil DATETIME2 NULL, CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
 CONSTRAINT FK_Users_Roles FOREIGN KEY(RoleId) REFERENCES dbo.Roles(RoleId));
CREATE TABLE dbo.TeacherProfiles(TeacherId INT PRIMARY KEY, Department NVARCHAR(150) NULL,
 CONSTRAINT FK_TeacherProfiles_Users FOREIGN KEY(TeacherId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE);
CREATE TABLE dbo.StudentProfiles(StudentId INT PRIMARY KEY, StudentNumber NVARCHAR(50) NULL UNIQUE, [Level] NVARCHAR(50) NULL,
 CONSTRAINT FK_StudentProfiles_Users FOREIGN KEY(StudentId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE);

CREATE TABLE dbo.Departments(DepartmentId INT IDENTITY PRIMARY KEY, Code NVARCHAR(30) NOT NULL UNIQUE, Name NVARCHAR(150) NOT NULL, IsActive BIT NOT NULL DEFAULT(1));
CREATE TABLE dbo.AcademicTerms(AcademicTermId INT IDENTITY PRIMARY KEY, Name NVARCHAR(100) NOT NULL UNIQUE, StartDate DATE NOT NULL, EndDate DATE NOT NULL, IsActive BIT NOT NULL DEFAULT(1), CONSTRAINT CK_AcademicTerms_Dates CHECK(EndDate > StartDate));
CREATE TABLE dbo.Courses(
 CourseId INT IDENTITY PRIMARY KEY, Code NVARCHAR(30) NOT NULL, Name NVARCHAR(200) NOT NULL, DepartmentId INT NOT NULL,
 AcademicTermId INT NOT NULL, TeacherId INT NOT NULL, [Level] NVARCHAR(50) NULL, IsActive BIT NOT NULL DEFAULT(1),
 CONSTRAINT UQ_Courses_Code_Term UNIQUE(Code, AcademicTermId),
 CONSTRAINT FK_Courses_Department FOREIGN KEY(DepartmentId) REFERENCES dbo.Departments(DepartmentId),
 CONSTRAINT FK_Courses_Term FOREIGN KEY(AcademicTermId) REFERENCES dbo.AcademicTerms(AcademicTermId),
 CONSTRAINT FK_Courses_Teacher FOREIGN KEY(TeacherId) REFERENCES dbo.Users(UserId));
CREATE TABLE dbo.CourseEnrollments(
 EnrollmentId INT IDENTITY PRIMARY KEY, CourseId INT NOT NULL, StudentId INT NOT NULL, EnrolledAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), IsActive BIT NOT NULL DEFAULT(1),
 CONSTRAINT UQ_CourseEnrollments UNIQUE(CourseId,StudentId),
 CONSTRAINT FK_Enrollments_Course FOREIGN KEY(CourseId) REFERENCES dbo.Courses(CourseId) ON DELETE CASCADE,
 CONSTRAINT FK_Enrollments_Student FOREIGN KEY(StudentId) REFERENCES dbo.Users(UserId));

CREATE TABLE dbo.Exams(
 ExamId INT IDENTITY PRIMARY KEY, Title NVARCHAR(200) NOT NULL, CourseId INT NOT NULL, CourseName NVARCHAR(200) NULL, TeacherId INT NOT NULL,
 [Status] NVARCHAR(20) NOT NULL DEFAULT(N'Draft'), IsPublished BIT NOT NULL DEFAULT(0), CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
 AvailableFrom DATETIME2 NULL, AvailableTo DATETIME2 NULL, MaxAttempts INT NOT NULL DEFAULT(1), DurationMinutes INT NOT NULL DEFAULT(60),
 PassPercentage DECIMAL(5,2) NOT NULL DEFAULT(50), AutoSubmitOnExpiry BIT NOT NULL DEFAULT(1), ShuffleQuestions BIT NOT NULL DEFAULT(0),
 ShuffleChoices BIT NOT NULL DEFAULT(0), ShowCorrectAnswers BIT NOT NULL DEFAULT(0),
 CONSTRAINT FK_Exams_Course FOREIGN KEY(CourseId) REFERENCES dbo.Courses(CourseId),
 CONSTRAINT FK_Exams_Teacher FOREIGN KEY(TeacherId) REFERENCES dbo.Users(UserId),
 CONSTRAINT CK_Exams_Status CHECK([Status] IN(N'Draft',N'PendingReview',N'Rejected',N'Published',N'Closed',N'Archived')),
 CONSTRAINT CK_Exams_MaxAttempts CHECK(MaxAttempts > 0), CONSTRAINT CK_Exams_Duration CHECK(DurationMinutes > 0),
 CONSTRAINT CK_Exams_Pass CHECK(PassPercentage BETWEEN 0 AND 100),
 CONSTRAINT CK_Exams_Window CHECK(AvailableTo IS NULL OR AvailableFrom IS NULL OR AvailableTo > AvailableFrom));
CREATE TABLE dbo.Questions(
 QuestionId INT IDENTITY PRIMARY KEY, ExamId INT NOT NULL, QuestionType NVARCHAR(20) NOT NULL, Body NVARCHAR(MAX) NOT NULL,
 Points DECIMAL(6,2) NOT NULL DEFAULT(1), TimeLimitSeconds INT NULL, SortOrder INT NOT NULL DEFAULT(1),
 CONSTRAINT FK_Questions_Exams FOREIGN KEY(ExamId) REFERENCES dbo.Exams(ExamId) ON DELETE CASCADE,
 CONSTRAINT CK_Questions_Type CHECK(QuestionType IN(N'MCQ',N'TF',N'Essay')), CONSTRAINT CK_Questions_Points CHECK(Points > 0),
 CONSTRAINT CK_Questions_Time CHECK(TimeLimitSeconds IS NULL OR TimeLimitSeconds > 0));
CREATE TABLE dbo.QuestionChoices(ChoiceId INT IDENTITY PRIMARY KEY, QuestionId INT NOT NULL, ChoiceText NVARCHAR(500) NOT NULL, SortOrder INT NOT NULL DEFAULT(1), CONSTRAINT FK_Choices_Questions FOREIGN KEY(QuestionId) REFERENCES dbo.Questions(QuestionId) ON DELETE CASCADE);
CREATE TABLE dbo.AnswerKeyItems(
 ExamId INT NOT NULL, QuestionId INT NOT NULL, CorrectChoiceId INT NULL, CorrectBool BIT NULL, ModelAnswer NVARCHAR(MAX) NULL,
 UploadedByTeacherId INT NOT NULL, UploadedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), CONSTRAINT PK_AnswerKeyItems PRIMARY KEY(ExamId,QuestionId),
 CONSTRAINT FK_AK_Exam FOREIGN KEY(ExamId) REFERENCES dbo.Exams(ExamId) ON DELETE CASCADE,
 CONSTRAINT FK_AK_Q FOREIGN KEY(QuestionId) REFERENCES dbo.Questions(QuestionId), CONSTRAINT FK_AK_Choice FOREIGN KEY(CorrectChoiceId) REFERENCES dbo.QuestionChoices(ChoiceId),
 CONSTRAINT FK_AK_Teacher FOREIGN KEY(UploadedByTeacherId) REFERENCES dbo.Users(UserId));
CREATE TABLE dbo.ExamPublishRequests(
 RequestId INT IDENTITY PRIMARY KEY, ExamId INT NOT NULL, TeacherId INT NOT NULL, [Status] NVARCHAR(20) NOT NULL DEFAULT(N'Pending'),
 RequestedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), ReviewedByAdminId INT NULL, ReviewedAt DATETIME2 NULL, AdminNote NVARCHAR(500) NULL,
 CONSTRAINT FK_EPR_Exam FOREIGN KEY(ExamId) REFERENCES dbo.Exams(ExamId) ON DELETE CASCADE,
 CONSTRAINT FK_EPR_Teacher FOREIGN KEY(TeacherId) REFERENCES dbo.Users(UserId), CONSTRAINT FK_EPR_Admin FOREIGN KEY(ReviewedByAdminId) REFERENCES dbo.Users(UserId),
 CONSTRAINT CK_EPR_Status CHECK([Status] IN(N'Pending',N'Approved',N'Rejected')));
CREATE TABLE dbo.ExamAttempts(
 AttemptId INT IDENTITY PRIMARY KEY, ExamId INT NOT NULL, StudentId INT NOT NULL, StartedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),
 ExpiresAt DATETIME2 NULL, SubmittedAt DATETIME2 NULL, [Status] NVARCHAR(20) NOT NULL DEFAULT(N'Started'), AutoSubmitted BIT NOT NULL DEFAULT(0),
 AutoScore DECIMAL(8,2) NULL, ManualScore DECIMAL(8,2) NULL, FinalScore DECIMAL(8,2) NULL, MaximumScore DECIMAL(8,2) NULL,
 Percentage DECIMAL(5,2) NULL, PassPercentage DECIMAL(5,2) NULL, IsPassed BIT NULL, FinalizedAt DATETIME2 NULL, FinalizedByUserId INT NULL,
 CONSTRAINT FK_Attempts_Exam FOREIGN KEY(ExamId) REFERENCES dbo.Exams(ExamId) ON DELETE CASCADE,
 CONSTRAINT FK_Attempts_Student FOREIGN KEY(StudentId) REFERENCES dbo.Users(UserId), CONSTRAINT FK_Attempts_Finalizer FOREIGN KEY(FinalizedByUserId) REFERENCES dbo.Users(UserId),
 CONSTRAINT CK_Attempts_Status CHECK([Status] IN(N'Started',N'Submitted',N'Closed')), CONSTRAINT CK_Attempts_Percentage CHECK(Percentage IS NULL OR Percentage BETWEEN 0 AND 100));
CREATE TABLE dbo.AttemptAnswers(
 AttemptAnswerId INT IDENTITY PRIMARY KEY, AttemptId INT NOT NULL, QuestionId INT NOT NULL, SelectedChoiceId INT NULL, BoolAnswer BIT NULL,
 EssayAnswer NVARCHAR(MAX) NULL, Confirmed BIT NOT NULL DEFAULT(0), ConfirmedAt DATETIME2 NULL, OpenedAt DATETIME2 NULL, LockedAt DATETIME2 NULL, TimeExpired BIT NOT NULL DEFAULT(0),
 CONSTRAINT UQ_AttemptAnswers UNIQUE(AttemptId,QuestionId), CONSTRAINT FK_AttemptAnswers_Attempt FOREIGN KEY(AttemptId) REFERENCES dbo.ExamAttempts(AttemptId) ON DELETE CASCADE,
 CONSTRAINT FK_AttemptAnswers_Question FOREIGN KEY(QuestionId) REFERENCES dbo.Questions(QuestionId), CONSTRAINT FK_AttemptAnswers_Choice FOREIGN KEY(SelectedChoiceId) REFERENCES dbo.QuestionChoices(ChoiceId));
CREATE TABLE dbo.ManualScores(
 ManualScoreId INT IDENTITY PRIMARY KEY, AttemptId INT NOT NULL, QuestionId INT NOT NULL, Score DECIMAL(6,2) NOT NULL,
 GradedByControlId INT NOT NULL, GradedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), CONSTRAINT UQ_ManualScores UNIQUE(AttemptId,QuestionId),
 CONSTRAINT FK_ManualScores_Attempt FOREIGN KEY(AttemptId) REFERENCES dbo.ExamAttempts(AttemptId) ON DELETE CASCADE,
 CONSTRAINT FK_ManualScores_Question FOREIGN KEY(QuestionId) REFERENCES dbo.Questions(QuestionId), CONSTRAINT FK_ManualScores_Control FOREIGN KEY(GradedByControlId) REFERENCES dbo.Users(UserId),
 CONSTRAINT CK_ManualScores_NonNegative CHECK(Score >= 0));
CREATE TABLE dbo.ProctorEvents(
 ProctorEventId INT IDENTITY PRIMARY KEY, AttemptId INT NOT NULL, QuestionId INT NULL, StudentId INT NOT NULL, EventType NVARCHAR(80) NOT NULL,
 Details NVARCHAR(MAX) NULL, CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), CONSTRAINT FK_ProctorEvents_Attempt FOREIGN KEY(AttemptId) REFERENCES dbo.ExamAttempts(AttemptId) ON DELETE CASCADE,
 CONSTRAINT FK_ProctorEvents_Question FOREIGN KEY(QuestionId) REFERENCES dbo.Questions(QuestionId), CONSTRAINT FK_ProctorEvents_Student FOREIGN KEY(StudentId) REFERENCES dbo.Users(UserId));
CREATE TABLE dbo.AuditLogs(AuditId INT IDENTITY PRIMARY KEY, ActorUserId INT NULL, [Action] NVARCHAR(100) NOT NULL, Details NVARCHAR(MAX) NULL, CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), CONSTRAINT FK_Audit_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(UserId));
CREATE TABLE dbo.EmailLogs(EmailLogId INT IDENTITY PRIMARY KEY, UserId INT NOT NULL, [Subject] NVARCHAR(200) NOT NULL, Body NVARCHAR(MAX) NOT NULL, SentAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), [Status] NVARCHAR(30) NOT NULL DEFAULT(N'Queued'), CONSTRAINT FK_EmailLogs_User FOREIGN KEY(UserId) REFERENCES dbo.Users(UserId));
CREATE TABLE dbo.StudentNotices(NoticeId INT IDENTITY PRIMARY KEY, Title NVARCHAR(200) NOT NULL, Body NVARCHAR(MAX) NOT NULL, IsActive BIT NOT NULL DEFAULT(1), CreatedAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()));
CREATE TABLE dbo.StudentNoticeReads(NoticeId INT NOT NULL, StudentId INT NOT NULL, ReadAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()), CONSTRAINT PK_NoticeReads PRIMARY KEY(NoticeId,StudentId), CONSTRAINT FK_NoticeReads_Notice FOREIGN KEY(NoticeId) REFERENCES dbo.StudentNotices(NoticeId) ON DELETE CASCADE, CONSTRAINT FK_NoticeReads_Student FOREIGN KEY(StudentId) REFERENCES dbo.Users(UserId));

CREATE INDEX IX_Questions_Exam_Sort ON dbo.Questions(ExamId,SortOrder);
CREATE INDEX IX_Choices_Question_Sort ON dbo.QuestionChoices(QuestionId,SortOrder);
CREATE INDEX IX_Attempts_Exam_Student_Status ON dbo.ExamAttempts(ExamId,StudentId,[Status]);
CREATE UNIQUE INDEX UX_Attempts_OneStarted ON dbo.ExamAttempts(ExamId,StudentId) WHERE [Status]=N'Started';
CREATE INDEX IX_ProctorEvents_Attempt_Created ON dbo.ProctorEvents(AttemptId,CreatedAt);
CREATE INDEX IX_EPR_Status ON dbo.ExamPublishRequests([Status]);
GO
/* بيانات عرض آمنة - كلمة المرور لجميع الحسابات: Demo@12345 ويجب تغييرها عند أول دخول */
USE [UniRemoteExamDb_Final];
GO
INSERT dbo.Roles(RoleName) VALUES(N'Admin'),(N'Teacher'),(N'Student'),(N'Control');
DECLARE @AdminRole INT=(SELECT RoleId FROM dbo.Roles WHERE RoleName=N'Admin');
DECLARE @TeacherRole INT=(SELECT RoleId FROM dbo.Roles WHERE RoleName=N'Teacher');
DECLARE @StudentRole INT=(SELECT RoleId FROM dbo.Roles WHERE RoleName=N'Student');
DECLARE @ControlRole INT=(SELECT RoleId FROM dbo.Roles WHERE RoleName=N'Control');
INSERT dbo.Users(Email,PasswordHash,FullName,RoleId,IsActive,MustChangePassword) VALUES
(N'admin@sanaau.edu.ye',N'AQAAAAIAAYagAAAAEAEBAQEBAQEBAQEBAQEBAQHfscnZvOZQmkusTtdTilKcQ9jOC9L11Pml8Kw+U0ZRfA==',N'مدير النظام',@AdminRole,1,1),
(N'teacher@sanaau.edu.ye',N'AQAAAAIAAYagAAAAEAICAgICAgICAgICAgICAgJGfYYbMg+AMphqvErdhyZfQQqzjzG+JvhrSkjMCNQ3qA==',N'د. أحمد محمد',@TeacherRole,1,1),
(N'control@sanaau.edu.ye',N'AQAAAAIAAYagAAAAEAMDAwMDAwMDAwMDAwMDAwO6ufuTEh2BE+WNSYUDkLeo/kcz0RLn7HzfKywEXtR04w==',N'موظف الكنترول',@ControlRole,1,1),
(N'student1@sanaau.edu.ye',N'AQAAAAIAAYagAAAAEAQEBAQEBAQEBAQEBAQEBASXy31B5hOeL4NZ2znveO/3KLGqGaLJqq+5UNwpXwyNeg==',N'سارة علي',@StudentRole,1,1),
(N'student2@sanaau.edu.ye',N'AQAAAAIAAYagAAAAEAUFBQUFBQUFBQUFBQUFBQXymd3NKNXMTBbelf5va8obkULwks/g6fPayA+4YK5e1A==',N'محمد عبدالله',@StudentRole,1,1),
(N'student3@sanaau.edu.ye',N'AQAAAAIAAYagAAAAEAYGBgYGBgYGBgYGBgYGBgZb2s87fhIct4jRwa6byNhZOqZ+p7kmfZ0Ga88JQCo71w==',N'أروى حسن',@StudentRole,1,1);
DECLARE @Teacher INT=(SELECT UserId FROM dbo.Users WHERE Email=N'teacher@sanaau.edu.ye');
INSERT dbo.TeacherProfiles(TeacherId,Department) VALUES(@Teacher,N'علوم الحاسوب');
INSERT dbo.StudentProfiles(StudentId,StudentNumber,[Level]) SELECT UserId,CASE Email WHEN N'student1@sanaau.edu.ye' THEN N'2026001' WHEN N'student2@sanaau.edu.ye' THEN N'2026002' ELSE N'2026003' END,N'الرابع' FROM dbo.Users WHERE Email LIKE N'student%@sanaau.edu.ye';
INSERT dbo.Departments(Code,Name) VALUES(N'CS',N'علوم الحاسوب');
INSERT dbo.AcademicTerms(Name,StartDate,EndDate,IsActive) VALUES(N'2026/2027 - الفصل الأول','2026-09-01','2027-01-31',1);
DECLARE @Department INT=SCOPE_IDENTITY(); -- يعاد ضبطه أدناه لضمان القيمة الصحيحة
SET @Department=(SELECT DepartmentId FROM dbo.Departments WHERE Code=N'CS');
DECLARE @Term INT=(SELECT AcademicTermId FROM dbo.AcademicTerms WHERE Name=N'2026/2027 - الفصل الأول');
INSERT dbo.Courses(Code,Name,DepartmentId,AcademicTermId,TeacherId,[Level]) VALUES(N'CS401',N'قواعد البيانات',@Department,@Term,@Teacher,N'الرابع');
DECLARE @Course INT=SCOPE_IDENTITY();
INSERT dbo.CourseEnrollments(CourseId,StudentId) SELECT @Course,UserId FROM dbo.Users WHERE Email LIKE N'student%@sanaau.edu.ye';
INSERT dbo.Exams(Title,CourseId,CourseName,TeacherId,[Status],IsPublished,AvailableFrom,AvailableTo,MaxAttempts,DurationMinutes,PassPercentage,AutoSubmitOnExpiry,ShuffleQuestions,ShuffleChoices,ShowCorrectAnswers)
VALUES(N'اختبار تجريبي في قواعد البيانات',@Course,N'قواعد البيانات',@Teacher,N'Published',1,DATEADD(DAY,-30,SYSUTCDATETIME()),DATEADD(DAY,365,SYSUTCDATETIME()),1,45,50,1,1,1,1);
DECLARE @Exam INT=SCOPE_IDENTITY();
INSERT dbo.Questions(ExamId,QuestionType,Body,Points,TimeLimitSeconds,SortOrder) VALUES
(@Exam,N'TF',N'قاعدة البيانات العلائقية تعتمد على الجداول.',1,60,1),
(@Exam,N'MCQ',N'أي أمر يستخدم لاسترجاع البيانات؟',2,120,2),
(@Exam,N'Essay',N'اشرح الفرق بين المفتاح الأساسي والمفتاح الأجنبي.',7,300,3);
DECLARE @Q1 INT=(SELECT QuestionId FROM dbo.Questions WHERE ExamId=@Exam AND SortOrder=1);
DECLARE @Q2 INT=(SELECT QuestionId FROM dbo.Questions WHERE ExamId=@Exam AND SortOrder=2);
DECLARE @Q3 INT=(SELECT QuestionId FROM dbo.Questions WHERE ExamId=@Exam AND SortOrder=3);
INSERT dbo.QuestionChoices(QuestionId,ChoiceText,SortOrder) VALUES(@Q2,N'INSERT',1),(@Q2,N'SELECT',2),(@Q2,N'DELETE',3),(@Q2,N'UPDATE',4);
DECLARE @CorrectChoice INT=(SELECT ChoiceId FROM dbo.QuestionChoices WHERE QuestionId=@Q2 AND SortOrder=2);
INSERT dbo.AnswerKeyItems(ExamId,QuestionId,CorrectChoiceId,CorrectBool,ModelAnswer,UploadedByTeacherId) VALUES
(@Exam,@Q1,NULL,1,NULL,@Teacher),(@Exam,@Q2,@CorrectChoice,NULL,NULL,@Teacher),(@Exam,@Q3,NULL,NULL,N'المفتاح الأساسي يميز السجل، والمفتاح الأجنبي يربط الجداول.',@Teacher);
DECLARE @Admin INT=(SELECT UserId FROM dbo.Users WHERE Email=N'admin@sanaau.edu.ye');
INSERT dbo.ExamPublishRequests(ExamId,TeacherId,[Status],ReviewedByAdminId,ReviewedAt,AdminNote) VALUES(@Exam,@Teacher,N'Approved',@Admin,SYSUTCDATETIME(),N'اختبار عرض معتمد.');
INSERT dbo.StudentNotices(Title,Body) VALUES(N'تعليمات الاختبار',N'المدة الكلية ووقت السؤال يتحقق منهما السيرفر، وتُسلم المحاولة تلقائيًا عند انتهاء الوقت.');
GO
