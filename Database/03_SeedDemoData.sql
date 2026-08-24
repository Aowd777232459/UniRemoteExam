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
