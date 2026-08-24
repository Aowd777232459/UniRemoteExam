# إنتاج APK وبرنامج ويندوز من GitHub

## أندرويد

سير العمل `Build Android APK` ينتج ملفًا باسم `UniRemoteExam-Android.apk`. يلزم إعداد أربعة أسرار للمستودع:

- `ANDROID_KEYSTORE_BASE64`
- `ANDROID_KEY_ALIAS`
- `ANDROID_KEYSTORE_PASSWORD`
- `ANDROID_KEY_PASSWORD`

يجب الاحتفاظ بملف التوقيع وكلماته في مكان آمن. فقدان مفتاح التوقيع يمنع تثبيت تحديثات فوق النسخة السابقة.

بعد نجاح سير العمل، افتح صفحة Actions ثم التشغيل الأخير، ونزّل `UniRemoteExam-Android-APK` من قسم Artifacts.

## ويندوز

سير العمل `Build Windows Installer` ينتج `UniRemoteExam-Windows-Setup.exe`. لا يحتاج المستخدم إلى تثبيت .NET مسبقًا لأن الحزمة ذاتية الاحتواء.

الملف غير موقع بشهادة ناشر تجارية، لذلك قد يعرض Windows SmartScreen تنبيه ناشر غير معروف. لتوزيع جامعي رسمي، وقّع المثبت بشهادة Code Signing موثوقة.

## الخادم

سير العمل `Server CI` يفحص تجميع مشروع ASP.NET Core وينتج حزمة `UniRemoteExam-Server`. يمكن نشرها على خادم يدعم .NET 10، أو استخدام `Dockerfile`.
