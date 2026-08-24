# دليل التثبيت والتشغيل

## 1. تجهيز SQL Server

للتثبيت الجديد شغّل `Database/CREATE_DATABASE_FULL.sql`. الملف:

- ينشئ قاعدة `UniRemoteExamDb_Final` إذا لم تكن موجودة.
- يرفض إعادة إنشاء المخطط فوق جداول قائمة.
- يدخل بيانات عرض مشفرة.

للترقية من v5:

- خذ نسخة احتياطية.
- شغّل `Database/04_Upgrade_From_v5.sql`.
- راجع المقررات المرحّلة وسجّل الطلاب الذين لم تكن لهم محاولات قديمة.

## 2. إعداد الاتصال

الإعداد الافتراضي يستخدم LocalDB:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=UniRemoteExamDb_Final;Trusted_Connection=True;TrustServerCertificate=True;"
```

لـ SQL Express مثال:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=UniRemoteExamDb_Final;Trusted_Connection=True;TrustServerCertificate=True;"
```

لا تضع كلمة مرور قاعدة الإنتاج داخل ملف التسليم؛ استخدم متغيرات البيئة أو User Secrets.

## 3. إعداد البريد

اضبط القيم داخل User Secrets أو متغيرات البيئة:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Smtp:Port" "587"
dotnet user-secrets set "Smtp:Username" "example@gmail.com"
dotnet user-secrets set "Smtp:Password" "APP_PASSWORD"
```

عند غياب هذه القيم يعمل النظام طبيعيًا، ويُسجل البريد `Skipped`.

## 4. بناء المشروع

```powershell
dotnet restore
dotnet build --configuration Release
dotnet run
```

أو شغّل `VERIFY_V6.ps1` من PowerShell.

## 5. الزمن

- إدخال الدكتور يُعامل كتوقيت صنعاء.
- يحول النظام الوقت إلى UTC قبل التخزين.
- يعاد تحويله إلى توقيت صنعاء عند العرض.

## 6. النسخ الاحتياطي

قبل أي ترقية:

```sql
BACKUP DATABASE [UniRemoteExamDb_Final]
TO DISK = N'C:\Backup\UniRemoteExamDb_Final.bak'
WITH INIT, COMPRESSION;
```
