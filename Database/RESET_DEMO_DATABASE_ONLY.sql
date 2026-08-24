/* خطر: يحذف قاعدة العرض بالكامل. لا تستخدمه على بيانات حقيقية. */
USE master;
GO
IF DB_ID(N'UniRemoteExamDb_Final') IS NOT NULL
BEGIN
 ALTER DATABASE [UniRemoteExamDb_Final] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
 DROP DATABASE [UniRemoteExamDb_Final];
END
GO
