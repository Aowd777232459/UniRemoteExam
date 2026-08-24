/* ترقية قاعدة v5 إلى v6. خذ نسخة احتياطية قبل التنفيذ. */
USE [UniRemoteExamDb_Final];
GO
BEGIN TRY
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.Users','MustChangePassword') IS NULL ALTER TABLE dbo.Users ADD MustChangePassword BIT NOT NULL CONSTRAINT DF_Users_MustChangePassword DEFAULT(0);
IF COL_LENGTH('dbo.Users','FailedLoginCount') IS NULL ALTER TABLE dbo.Users ADD FailedLoginCount INT NOT NULL CONSTRAINT DF_Users_FailedLoginCount DEFAULT(0);
IF COL_LENGTH('dbo.Users','LockedUntil') IS NULL ALTER TABLE dbo.Users ADD LockedUntil DATETIME2 NULL;
IF OBJECT_ID('dbo.Departments','U') IS NULL CREATE TABLE dbo.Departments(DepartmentId INT IDENTITY PRIMARY KEY,Code NVARCHAR(30) NOT NULL UNIQUE,Name NVARCHAR(150) NOT NULL,IsActive BIT NOT NULL DEFAULT(1));
IF OBJECT_ID('dbo.AcademicTerms','U') IS NULL CREATE TABLE dbo.AcademicTerms(AcademicTermId INT IDENTITY PRIMARY KEY,Name NVARCHAR(100) NOT NULL UNIQUE,StartDate DATE NOT NULL,EndDate DATE NOT NULL,IsActive BIT NOT NULL DEFAULT(1));
IF OBJECT_ID('dbo.Courses','U') IS NULL CREATE TABLE dbo.Courses(CourseId INT IDENTITY PRIMARY KEY,Code NVARCHAR(30) NOT NULL,Name NVARCHAR(200) NOT NULL,DepartmentId INT NOT NULL,AcademicTermId INT NOT NULL,TeacherId INT NOT NULL,[Level] NVARCHAR(50) NULL,IsActive BIT NOT NULL DEFAULT(1),CONSTRAINT UQ_Courses_Code_Term UNIQUE(Code,AcademicTermId),CONSTRAINT FK_Courses_Department FOREIGN KEY(DepartmentId) REFERENCES dbo.Departments(DepartmentId),CONSTRAINT FK_Courses_Term FOREIGN KEY(AcademicTermId) REFERENCES dbo.AcademicTerms(AcademicTermId),CONSTRAINT FK_Courses_Teacher FOREIGN KEY(TeacherId) REFERENCES dbo.Users(UserId));
IF OBJECT_ID('dbo.CourseEnrollments','U') IS NULL CREATE TABLE dbo.CourseEnrollments(EnrollmentId INT IDENTITY PRIMARY KEY,CourseId INT NOT NULL,StudentId INT NOT NULL,EnrolledAt DATETIME2 NOT NULL DEFAULT(SYSUTCDATETIME()),IsActive BIT NOT NULL DEFAULT(1),CONSTRAINT UQ_CourseEnrollments UNIQUE(CourseId,StudentId),CONSTRAINT FK_Enrollments_Course FOREIGN KEY(CourseId) REFERENCES dbo.Courses(CourseId) ON DELETE CASCADE,CONSTRAINT FK_Enrollments_Student FOREIGN KEY(StudentId) REFERENCES dbo.Users(UserId));
IF NOT EXISTS(SELECT 1 FROM dbo.Departments WHERE Code=N'LEGACY') INSERT dbo.Departments(Code,Name) VALUES(N'LEGACY',N'قسم مرحّل من النسخة السابقة');
IF NOT EXISTS(SELECT 1 FROM dbo.AcademicTerms WHERE Name=N'فصل مرحّل من v5') INSERT dbo.AcademicTerms(Name,StartDate,EndDate) VALUES(N'فصل مرحّل من v5','2026-01-01','2027-12-31');
DECLARE @D INT=(SELECT DepartmentId FROM dbo.Departments WHERE Code=N'LEGACY'), @T INT=(SELECT AcademicTermId FROM dbo.AcademicTerms WHERE Name=N'فصل مرحّل من v5');
IF COL_LENGTH('dbo.Exams','CourseId') IS NULL ALTER TABLE dbo.Exams ADD CourseId INT NULL;
;WITH X AS(SELECT TeacherId,ISNULL(NULLIF(CourseName,N''),N'مقرر مرحّل') CourseName,ROW_NUMBER() OVER(ORDER BY TeacherId,ISNULL(CourseName,N'')) rn FROM dbo.Exams GROUP BY TeacherId,CourseName)
INSERT dbo.Courses(Code,Name,DepartmentId,AcademicTermId,TeacherId,IsActive)
SELECT CONCAT(N'LEG-',TeacherId,N'-',rn),CourseName,@D,@T,TeacherId,1 FROM X WHERE NOT EXISTS(SELECT 1 FROM dbo.Courses c WHERE c.TeacherId=X.TeacherId AND c.Name=X.CourseName AND c.AcademicTermId=@T);
UPDATE e SET CourseId=(SELECT TOP(1)c.CourseId FROM dbo.Courses c WHERE c.TeacherId=e.TeacherId AND c.Name=ISNULL(NULLIF(e.CourseName,N''),N'مقرر مرحّل') ORDER BY c.CourseId) FROM dbo.Exams e WHERE e.CourseId IS NULL;
ALTER TABLE dbo.Exams ALTER COLUMN CourseId INT NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Exams_Course') ALTER TABLE dbo.Exams ADD CONSTRAINT FK_Exams_Course FOREIGN KEY(CourseId) REFERENCES dbo.Courses(CourseId);
IF COL_LENGTH('dbo.Exams','Status') IS NULL ALTER TABLE dbo.Exams ADD [Status] NVARCHAR(20) NOT NULL CONSTRAINT DF_Exams_Status DEFAULT(N'Draft');
IF COL_LENGTH('dbo.Exams','DurationMinutes') IS NULL ALTER TABLE dbo.Exams ADD DurationMinutes INT NOT NULL CONSTRAINT DF_Exams_Duration DEFAULT(60);
IF COL_LENGTH('dbo.Exams','PassPercentage') IS NULL ALTER TABLE dbo.Exams ADD PassPercentage DECIMAL(5,2) NOT NULL CONSTRAINT DF_Exams_Pass DEFAULT(50);
IF COL_LENGTH('dbo.Exams','AutoSubmitOnExpiry') IS NULL ALTER TABLE dbo.Exams ADD AutoSubmitOnExpiry BIT NOT NULL CONSTRAINT DF_Exams_AutoSubmit DEFAULT(1);
UPDATE e SET [Status]=CASE WHEN IsPublished=1 THEN N'Published' WHEN EXISTS(SELECT 1 FROM dbo.ExamPublishRequests r WHERE r.ExamId=e.ExamId AND r.Status=N'Pending') THEN N'PendingReview' WHEN EXISTS(SELECT 1 FROM dbo.ExamPublishRequests r WHERE r.ExamId=e.ExamId AND r.Status=N'Rejected') THEN N'Rejected' ELSE N'Draft' END FROM dbo.Exams e;
IF COL_LENGTH('dbo.ExamAttempts','ExpiresAt') IS NULL ALTER TABLE dbo.ExamAttempts ADD ExpiresAt DATETIME2 NULL;
IF COL_LENGTH('dbo.ExamAttempts','AutoSubmitted') IS NULL ALTER TABLE dbo.ExamAttempts ADD AutoSubmitted BIT NOT NULL CONSTRAINT DF_Attempts_AutoSubmitted DEFAULT(0);
IF COL_LENGTH('dbo.ExamAttempts','AutoScore') IS NULL ALTER TABLE dbo.ExamAttempts ADD AutoScore DECIMAL(8,2) NULL;
IF COL_LENGTH('dbo.ExamAttempts','ManualScore') IS NULL ALTER TABLE dbo.ExamAttempts ADD ManualScore DECIMAL(8,2) NULL;
IF COL_LENGTH('dbo.ExamAttempts','FinalScore') IS NULL ALTER TABLE dbo.ExamAttempts ADD FinalScore DECIMAL(8,2) NULL;
IF COL_LENGTH('dbo.ExamAttempts','MaximumScore') IS NULL ALTER TABLE dbo.ExamAttempts ADD MaximumScore DECIMAL(8,2) NULL;
IF COL_LENGTH('dbo.ExamAttempts','Percentage') IS NULL ALTER TABLE dbo.ExamAttempts ADD Percentage DECIMAL(5,2) NULL;
IF COL_LENGTH('dbo.ExamAttempts','PassPercentage') IS NULL ALTER TABLE dbo.ExamAttempts ADD PassPercentage DECIMAL(5,2) NULL;
IF COL_LENGTH('dbo.ExamAttempts','IsPassed') IS NULL ALTER TABLE dbo.ExamAttempts ADD IsPassed BIT NULL;
IF COL_LENGTH('dbo.ExamAttempts','FinalizedAt') IS NULL ALTER TABLE dbo.ExamAttempts ADD FinalizedAt DATETIME2 NULL;
IF COL_LENGTH('dbo.ExamAttempts','FinalizedByUserId') IS NULL ALTER TABLE dbo.ExamAttempts ADD FinalizedByUserId INT NULL;
UPDATE a SET ExpiresAt=DATEADD(MINUTE,e.DurationMinutes,a.StartedAt) FROM dbo.ExamAttempts a JOIN dbo.Exams e ON e.ExamId=a.ExamId WHERE a.ExpiresAt IS NULL;
;WITH D AS(SELECT AttemptAnswerId,ROW_NUMBER() OVER(PARTITION BY AttemptId,QuestionId ORDER BY AttemptAnswerId) rn FROM dbo.AttemptAnswers) DELETE FROM D WHERE rn>1;
;WITH D AS(SELECT ManualScoreId,ROW_NUMBER() OVER(PARTITION BY AttemptId,QuestionId ORDER BY GradedAt DESC,ManualScoreId DESC) rn FROM dbo.ManualScores) DELETE FROM D WHERE rn>1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_AttemptAnswers_Attempt_Question') CREATE UNIQUE INDEX UX_AttemptAnswers_Attempt_Question ON dbo.AttemptAnswers(AttemptId,QuestionId);
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_ManualScores_Attempt_Question') CREATE UNIQUE INDEX UX_ManualScores_Attempt_Question ON dbo.ManualScores(AttemptId,QuestionId);
;WITH D AS(SELECT AttemptId,ROW_NUMBER() OVER(PARTITION BY ExamId,StudentId ORDER BY StartedAt DESC,AttemptId DESC) rn FROM dbo.ExamAttempts WHERE Status=N'Started') UPDATE a SET Status=N'Submitted',SubmittedAt=ISNULL(SubmittedAt,SYSUTCDATETIME()),AutoSubmitted=1 FROM dbo.ExamAttempts a JOIN D ON D.AttemptId=a.AttemptId WHERE D.rn>1;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_Attempts_OneStarted') CREATE UNIQUE INDEX UX_Attempts_OneStarted ON dbo.ExamAttempts(ExamId,StudentId) WHERE Status=N'Started';
INSERT dbo.CourseEnrollments(CourseId,StudentId,IsActive) SELECT DISTINCT e.CourseId,a.StudentId,1 FROM dbo.ExamAttempts a JOIN dbo.Exams e ON e.ExamId=a.ExamId WHERE NOT EXISTS(SELECT 1 FROM dbo.CourseEnrollments ce WHERE ce.CourseId=e.CourseId AND ce.StudentId=a.StudentId);
;WITH M AS(SELECT ExamId,SUM(Points) MaximumScore FROM dbo.Questions GROUP BY ExamId),
A AS(SELECT aa.AttemptId,SUM(CASE WHEN q.QuestionType=N'MCQ' AND aa.SelectedChoiceId=k.CorrectChoiceId THEN q.Points WHEN q.QuestionType=N'TF' AND aa.BoolAnswer=k.CorrectBool THEN q.Points ELSE 0 END) AutoScore FROM dbo.AttemptAnswers aa JOIN dbo.Questions q ON q.QuestionId=aa.QuestionId JOIN dbo.AnswerKeyItems k ON k.ExamId=q.ExamId AND k.QuestionId=q.QuestionId GROUP BY aa.AttemptId),
MS AS(SELECT AttemptId,SUM(Score) ManualScore FROM dbo.ManualScores GROUP BY AttemptId)
UPDATE at SET AutoScore=ISNULL(A.AutoScore,0),ManualScore=ISNULL(MS.ManualScore,0),MaximumScore=M.MaximumScore,FinalScore=CASE WHEN ISNULL(A.AutoScore,0)+ISNULL(MS.ManualScore,0)>M.MaximumScore THEN M.MaximumScore ELSE ISNULL(A.AutoScore,0)+ISNULL(MS.ManualScore,0) END,PassPercentage=e.PassPercentage,FinalizedAt=ISNULL(at.FinalizedAt,at.SubmittedAt)
FROM dbo.ExamAttempts at JOIN dbo.Exams e ON e.ExamId=at.ExamId JOIN M ON M.ExamId=at.ExamId LEFT JOIN A ON A.AttemptId=at.AttemptId LEFT JOIN MS ON MS.AttemptId=at.AttemptId WHERE at.Status=N'Closed';
UPDATE dbo.ExamAttempts SET Percentage=CASE WHEN MaximumScore>0 THEN ROUND(FinalScore*100/MaximumScore,2) ELSE 0 END WHERE Status=N'Closed';
UPDATE dbo.ExamAttempts SET IsPassed=CASE WHEN Percentage>=PassPercentage THEN 1 ELSE 0 END WHERE Status=N'Closed';
COMMIT;
PRINT N'اكتملت ترقية v5 إلى v6.';
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;
GO
