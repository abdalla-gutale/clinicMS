using System.Text.Json;
using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Auth;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Filters;

public enum PermissionAction
{
    View,
    Create,
    Edit,
    Delete
}

/// <summary>Gates a controller action on the current user's role having the given CRUD permission
/// for the given navPage (matched by navPages.pageUrl). SuperAdmin always passes, mirroring the
/// sidebar's own bypass in DbAuthApiClient.GetMenuAsync. A missing permission row denies by
/// default, same as the sidebar hides pages with no row -- there is no implicit "view but can't
/// edit" fallback. Resolved via IFilterFactory so the filter can pull a scoped DbContext from DI
/// (attributes themselves are static metadata, not DI-constructed).</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute, IFilterFactory
{
    private readonly string _pageUrl;
    private readonly PermissionAction _action;

    public RequirePermissionAttribute(string pageUrl, PermissionAction action)
    {
        _pageUrl = pageUrl;
        _action = action;
    }

    public bool IsReusable => false;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var db = serviceProvider.GetRequiredService<ClinicMsDbContext>();
        return new RequirePermissionFilter(db, _pageUrl, _action);
    }
}

public class RequirePermissionFilter : IAsyncActionFilter
{
    private readonly ClinicMsDbContext _db;
    private readonly string _pageUrl;
    private readonly PermissionAction _action;

    public RequirePermissionFilter(ClinicMsDbContext db, string pageUrl, PermissionAction action)
    {
        _db = db;
        _pageUrl = pageUrl;
        _action = action;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var json = context.HttpContext.Session.GetString(SessionKeys.AuthUser);
        if (string.IsNullOrEmpty(json))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        UserSummary? user;
        try
        {
            user = JsonSerializer.Deserialize<UserSummary>(json);
        }
        catch (JsonException)
        {
            user = null;
        }

        if (user is null)
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var allowed = user.RoleName == "SuperAdmin" || await HasPermissionAsync(user.RoleId, context.HttpContext.RequestAborted);
        if (!allowed)
        {
            context.Result = context.HttpContext.Request.Method == HttpMethods.Get
                ? new RedirectToActionResult("AccessDenied", "Account", null)
                : new ObjectResult(new { message = "You don't have permission to perform this action." }) { StatusCode = 403 };
            return;
        }

        await next();
    }

    private async Task<bool> HasPermissionAsync(int roleId, CancellationToken cancellationToken)
    {
        var navPage = await _db.NavPages.FirstOrDefaultAsync(p => p.PageUrl == _pageUrl, cancellationToken);
        if (navPage is null)
        {
            return false;
        }

        var permission = await _db.Permissions.FirstOrDefaultAsync(
            p => p.RoleId == roleId && p.NavPageId == navPage.Id, cancellationToken);
        if (permission is null)
        {
            return false;
        }

        return _action switch
        {
            PermissionAction.View => permission.CanView,
            PermissionAction.Create => permission.CanCreate,
            PermissionAction.Edit => permission.CanEdit,
            PermissionAction.Delete => permission.CanDelete,
            _ => false,
        };
    }
}
