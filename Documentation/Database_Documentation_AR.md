# توثيق قاعدة البيانات v6

## المجموعات الرئيسة

### الهوية والصلاحيات

- `Roles`: أدوار النظام.
- `Users`: الحسابات وحالة القفل وتغيير كلمة المرور.
- `TeacherProfiles`, `StudentProfiles`: البيانات الأكاديمية الإضافية.

### الهيكل الأكاديمي

- `Departments`: الأقسام.
- `AcademicTerms`: الفصول الأكاديمية.
- `Courses`: المقررات والدكتور والفصل والقسم.
- `CourseEnrollments`: تسجيل الطلاب في المقررات.

### الاختبارات

- `Exams`: إعدادات الاختبار وحالته ومدته ونسبة النجاح.
- `Questions`: الأسئلة والدرجة والوقت والترتيب.
- `QuestionChoices`: خيارات MCQ.
- `AnswerKeyItems`: الإجابة الصحيحة أو النموذجية.
- `ExamPublishRequests`: دورة مراجعة المدير.

### المحاولات والنتائج

- `ExamAttempts`: المحاولة والمدة والحالة والنتيجة المثبتة.
- `AttemptAnswers`: إجابة الطالب وقفل السؤال.
- `ManualScores`: درجة السؤال المقالي.
- `ProctorEvents`: أحداث المراقبة السلوكية.

### التدقيق والتواصل

- `AuditLogs`: العمليات الحساسة.
- `EmailLogs`: حالة رسائل النتائج.
- `StudentNotices`, `StudentNoticeReads`: التنبيهات والقراءة.

## قواعد السلامة

- `CourseEnrollments(CourseId, StudentId)` فريد.
- `AttemptAnswers(AttemptId, QuestionId)` فريد.
- `ManualScores(AttemptId, QuestionId)` فريد.
- محاولة واحدة فقط بحالة `Started` لكل طالب واختبار.
- الدرجات والأسئلة والمدد لا تقبل قيمًا غير منطقية.
- النتيجة المثبتة لا تعتمد على إعادة حساب صفحة الطالب.

## حالات الاختبار

`Draft → PendingReview → Published → Closed/Archived`

يمكن العودة من `Rejected` إلى التعديل ثم الإرسال مجددًا.

## حالات المحاولة

`Started → Submitted → Closed`

`Closed` تعني أن الكنترول ثبت النتيجة النهائية.

## سياسة الترقية

- `CREATE_DATABASE_FULL.sql`: تثبيت جديد غير تدميري.
- `04_Upgrade_From_v5.sql`: ترقية محافظة على البيانات.
- `RESET_DEMO_DATABASE_ONLY.sql`: للعرض فقط ويحذف قاعدة العرض.
