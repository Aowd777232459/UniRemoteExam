/*
    تعديل قاعدة البيانات لدعم نظام السؤال الواحد مع قفل السؤال بالوقت.
    شغّل هذا السكربت مرة واحدة على قاعدة بيانات UniRemoteExam قبل تجربة التعديل.
*/

IF COL_LENGTH('dbo.AttemptAnswers', 'OpenedAt') IS NULL
BEGIN
    ALTER TABLE dbo.AttemptAnswers ADD OpenedAt DATETIME2 NULL;
END
GO

IF COL_LENGTH('dbo.AttemptAnswers', 'LockedAt') IS NULL
BEGIN
    ALTER TABLE dbo.AttemptAnswers ADD LockedAt DATETIME2 NULL;
END
GO

IF COL_LENGTH('dbo.AttemptAnswers', 'TimeExpired') IS NULL
BEGIN
    ALTER TABLE dbo.AttemptAnswers
    ADD TimeExpired BIT NOT NULL
        CONSTRAINT DF_AttemptAnswers_TimeExpired DEFAULT (0);
END
GO

-- فهرس مساعد لتسريع البحث عن إجابات محاولة الطالب.
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_AttemptAnswers_AttemptId_QuestionId'
      AND object_id = OBJECT_ID('dbo.AttemptAnswers')
)
BEGIN
    CREATE INDEX IX_AttemptAnswers_AttemptId_QuestionId
    ON dbo.AttemptAnswers (AttemptId, QuestionId);
END
GO
