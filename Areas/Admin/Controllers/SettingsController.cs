using Microsoft.AspNetCore.Mvc;
using UniRemoteExam.Areas.Admin.ViewModels;
using UniRemoteExam.Filters;

namespace UniRemoteExam.Areas.Admin.Controllers;

[Area("Admin")]
[RequireRole("Admin")]
public class SettingsController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var vm = new AdminSettingsVm
        {
            DeanName = HttpContext.Session.GetString("AdminDeanName") ?? "العميد / عبدالملك محسن عواد",
            HeaderSubtitle = HttpContext.Session.GetString("AdminHeaderSubtitle") ?? "منصة جامعية لإدارة الاختبارات الإلكترونية عن بُعد",
            LogoUrl = HttpContext.Session.GetString("AdminLogoUrl") ?? "/images/sanaa-university-logo.jpg",
            FooterText = HttpContext.Session.GetString("AdminFooterText") ?? "نظام الاختبارات عن بُعد - مشروع تخرج 2026 - جامعة صنعاء",
            ShowSystemStatus = HttpContext.Session.GetString("AdminShowSystemStatus") != "false"
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(AdminSettingsVm vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        HttpContext.Session.SetString("AdminDeanName", vm.DeanName?.Trim() ?? "العميد / عبدالملك محسن عواد");
        HttpContext.Session.SetString("AdminHeaderSubtitle", vm.HeaderSubtitle?.Trim() ?? "منصة جامعية لإدارة الاختبارات الإلكترونية عن بُعد");
        HttpContext.Session.SetString("AdminLogoUrl", vm.LogoUrl?.Trim() ?? "/images/sanaa-university-logo.jpg");
        HttpContext.Session.SetString("AdminFooterText", vm.FooterText?.Trim() ?? "نظام الاختبارات عن بُعد - مشروع تخرج 2026 - جامعة صنعاء");
        HttpContext.Session.SetString("AdminShowSystemStatus", vm.ShowSystemStatus ? "true" : "false");

        TempData["Success"] = "تم حفظ إعدادات الواجهة لهذه الجلسة بنجاح.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reset()
    {
        HttpContext.Session.Remove("AdminDeanName");
        HttpContext.Session.Remove("AdminHeaderSubtitle");
        HttpContext.Session.Remove("AdminLogoUrl");
        HttpContext.Session.Remove("AdminFooterText");
        HttpContext.Session.Remove("AdminShowSystemStatus");

        TempData["Success"] = "تمت استعادة إعدادات الواجهة الافتراضية.";
        return RedirectToAction(nameof(Index));
    }
}
