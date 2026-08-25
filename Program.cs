using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using UniRemoteExam.Data;
using UniRemoteExam.Services;

var builder = WebApplication.CreateBuilder(args);
var lanMode = builder.Configuration.GetValue<bool>("LanMode")
    || string.Equals(
        Environment.GetEnvironmentVariable("UNIREMOTE_LAN_MODE"),
        "true",
        StringComparison.OrdinalIgnoreCase);
var cloudDemoMode = builder.Configuration.GetValue<bool>("CloudDemoMode")
    || string.Equals(
        Environment.GetEnvironmentVariable("UNIREMOTE_CLOUD_DEMO_MODE"),
        "true",
        StringComparison.OrdinalIgnoreCase);

if (lanMode)
{
    builder.WebHost.UseUrls("http://0.0.0.0:5113");
}
else if (cloudDemoMode)
{
    var port = Environment.GetEnvironmentVariable("PORT");
    if (string.IsNullOrWhiteSpace(port)) port = "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

if (lanMode || cloudDemoMode)
{
    string databasePath;
    if (lanMode)
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UniRemoteExam");
        Directory.CreateDirectory(dataDirectory);
        databasePath = Path.Combine(dataDirectory, "UniRemoteExam-LAN.db");
    }
    else
    {
        databasePath = Environment.GetEnvironmentVariable("UNIREMOTE_SQLITE_PATH")
            ?? Path.Combine(Path.GetTempPath(), "UniRemoteExam-CloudDemo.db");
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }

    builder.Services.AddDbContext<UniRemoteExamDbContext>(options =>
        options.UseSqlite($"Data Source={databasePath};Cache=Shared"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection غير موجود في الإعدادات.");

    builder.Services.AddDbContext<UniRemoteExamDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
        {
            sql.EnableRetryOnFailure();
            sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        }));
}
builder.Services.AddScoped<SmtpEmailSender>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<ScoreCalculator>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.Name = ".UniRemoteExam.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || lanMode
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    context.Response.Headers.TryAdd("Content-Security-Policy", "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; font-src 'self' data:;");
    await next();
});

if (!app.Environment.IsDevelopment() && !lanMode)
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!lanMode && !cloudDemoMode)
    app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseRateLimiter();

if (lanMode || cloudDemoMode)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UniRemoteExamDbContext>();
    var passwords = scope.ServiceProvider.GetRequiredService<PasswordService>();
    await LanDatabaseInitializer.InitializeAsync(db, passwords);
}

app.MapGet("/health", async (UniRemoteExamDbContext db, CancellationToken cancellationToken) =>
{
    var databaseAvailable = await db.Database.CanConnectAsync(cancellationToken);
    return databaseAvailable
        ? Results.Ok(new { status = "healthy", database = "connected", utc = DateTimeOffset.UtcNow })
        : Results.Json(
            new { status = "unhealthy", database = "disconnected", utc = DateTimeOffset.UtcNow },
            statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
