using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using UniRemoteExam.Data;

namespace UniRemoteExam.Services;

public class SmtpEmailSender
{
    private readonly IConfiguration _cfg;
    private readonly UniRemoteExamDbContext _db;

    public SmtpEmailSender(IConfiguration cfg, UniRemoteExamDbContext db)
    {
        _cfg = cfg;
        _db = db;
    }

    public async Task<bool> SendAndLogAsync(int userId, string toEmail, string subject, string body)
    {
        // 1) نسجل دائمًا في EmailLogs
        var log = new EmailLog
        {
            UserId = userId,
            Subject = subject,
            Body = body,
            SentAt = YemenTime.UtcNow,
            Status = "Queued"
        };
        _db.EmailLogs.Add(log);
        await _db.SaveChangesAsync();

        // 2) إذا ما فيه إعدادات SMTP صحيحة، نكتفي بالتسجيل
        var host = _cfg["Smtp:Host"];
        var username = _cfg["Smtp:Username"];
        var password = _cfg["Smtp:Password"];

        if (string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(toEmail))
        {
            log.Status = "Skipped";
            await _db.SaveChangesAsync();
            return false;
        }

        try
        {
            int port = int.TryParse(_cfg["Smtp:Port"], out var p) ? p : 587;
            bool useSsl = bool.TryParse(_cfg["Smtp:UseSsl"], out var s) ? s : true;
            var fromName = _cfg["Smtp:FromName"] ?? "Uni Remote Exam";

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = useSsl
            };

            using var msg = new MailMessage
            {
                From = new MailAddress(username, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            msg.To.Add(toEmail);

            await client.SendMailAsync(msg);

            log.Status = "Sent";
            await _db.SaveChangesAsync();
            return true;
        }
        catch
        {
            log.Status = "Failed";
            await _db.SaveChangesAsync();
            return false;
        }
    }
}