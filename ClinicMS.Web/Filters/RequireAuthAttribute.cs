using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClinicMS.Web.Filters;

/// <summary>Redirects to /Account/Login when the session has no JWT, i.e. the user never
/// completed the login+OTP flow or their session expired.</summary>
public class RequireAuthAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var token = context.HttpContext.Session.GetString(SessionKeys.AuthToken);
        if (string.IsNullOrEmpty(token))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
        }

        base.OnActionExecuting(context);
    }
}
