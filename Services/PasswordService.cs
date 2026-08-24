using System;
using Microsoft.AspNetCore.Identity;
using UniRemoteExam.Data;

namespace UniRemoteExam.Services
{
    public class PasswordService
    {
        private readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();

        public string Hash(User user, string password)
        {
            return HashPassword(user, password);
        }

        public string Hash(string password)
        {
            var user = CreateSystemUser();
            return HashPassword(user, password);
        }

        public string HashPassword(User user, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            return _hasher.HashPassword(user, password);
        }

        public string HashPassword(string password)
        {
            var user = CreateSystemUser();
            return HashPassword(user, password);
        }

        public bool Verify(User? user, string password, out bool needsRehash)
        {
            needsRehash = false;

            if (user == null)
                return false;

            var storedPassword = user.PasswordHash;

            if (string.IsNullOrWhiteSpace(storedPassword) || string.IsNullOrWhiteSpace(password))
                return false;

            // أولاً: دعم كلمات المرور القديمة المحفوظة كنص عادي مثل 123456
            if (storedPassword == password)
            {
                needsRehash = true;
                return true;
            }

            // ثانياً: محاولة التحقق من كلمة المرور إذا كانت محفوظة كـ Identity Hash
            try
            {
                var result = _hasher.VerifyHashedPassword(user, storedPassword, password);

                if (result == PasswordVerificationResult.Success)
                    return true;

                if (result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    needsRehash = true;
                    return true;
                }
            }
            catch (FormatException)
            {
                // هذا يحدث عندما تكون القيمة الموجودة في قاعدة البيانات ليست Hash صالحاً.
                // لا نوقف النظام؛ فقط نعتبر كلمة المرور غير صحيحة إذا لم تطابق النص القديم أعلاه.
                return false;
            }
            catch
            {
                return false;
            }

            return false;
        }

        public bool VerifyPassword(string storedPassword, string providedPassword, out bool needsRehash)
        {
            needsRehash = false;

            if (string.IsNullOrWhiteSpace(storedPassword) || string.IsNullOrWhiteSpace(providedPassword))
                return false;

            // دعم كلمات المرور القديمة النصية
            if (storedPassword == providedPassword)
            {
                needsRehash = true;
                return true;
            }

            var tempUser = CreateSystemUser();

            try
            {
                var result = _hasher.VerifyHashedPassword(tempUser, storedPassword, providedPassword);

                if (result == PasswordVerificationResult.Success)
                    return true;

                if (result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    needsRehash = true;
                    return true;
                }
            }
            catch (FormatException)
            {
                return false;
            }
            catch
            {
                return false;
            }

            return false;
        }

        public bool Verify(string storedPassword, string providedPassword, out bool needsRehash)
        {
            return VerifyPassword(storedPassword, providedPassword, out needsRehash);
        }

        private static User CreateSystemUser()
        {
            return new User
            {
                Email = "system@local",
                FullName = "System"
            };
        }
    }
}
