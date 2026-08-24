using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UniRemoteExam.Filters;

public class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly string _role;
    public RequireRoleAttribute(string role) => _role = role;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var roleName = (session.GetString("RoleName") ?? string.Empty).Trim();
        if (!string.Equals(roleName, _role.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
            return;
        }
        if (session.GetString("MustChangePassword") == "1")
        {
            context.Result = new RedirectToActionResult("ChangePassword", "Account", new { area = "" });
            return;
        }
        base.OnActionExecuting(context);
    }
}
