using System.Text.Json;
using ClinicMS.Web.Data;
using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Auth;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace ClinicMS.Web.Tests;

public class RequirePermissionFilterTests
{
    private static (ActionExecutingContext Context, ActionExecutionDelegate Next, Func<bool> WasNextCalled) BuildContext(string method, UserSummary? user)
    {
        var httpContext = new DefaultHttpContext { Session = new TestSession() };
        httpContext.Request.Method = method;
        if (user is not null)
        {
            httpContext.Session.SetString(SessionKeys.AuthUser, JsonSerializer.Serialize(user));
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
        var executingContext = new ActionExecutingContext(
            actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: new object());

        var nextCalled = false;
        Task<ActionExecutedContext> Next()
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()));
        }

        return (executingContext, Next, () => nextCalled);
    }

    [Fact]
    public async Task NoLoggedInUser_RedirectsToLogin_AndDoesNotProceed()
    {
        var db = TestDb.Create();
        var filter = new RequirePermissionFilter(db, "/patients", PermissionAction.View);
        var (context, next, wasNextCalled) = BuildContext("GET", user: null);

        await filter.OnActionExecutionAsync(context, next);

        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Equal("Login", redirect.ActionName);
        Assert.False(wasNextCalled());
        db.Dispose();
    }

    [Fact]
    public async Task SuperAdmin_AlwaysProceeds_RegardlessOfPermissionRows()
    {
        var db = TestDb.Create();
        var filter = new RequirePermissionFilter(db, "/patients", PermissionAction.Delete);
        var user = new UserSummary(-1, "Raadso", "Raadso", "mailabdallas@gmail.com", -1, "SuperAdmin");
        var (context, next, wasNextCalled) = BuildContext("POST", user);

        await filter.OnActionExecutionAsync(context, next);

        Assert.Null(context.Result);
        Assert.True(wasNextCalled());
        db.Dispose();
    }

    [Fact]
    public async Task NonAdminWithNoPermissionRow_GetRequest_RedirectsToAccessDenied()
    {
        var db = TestDb.Create();
        SeedNavPage(db, "/patients");
        var filter = new RequirePermissionFilter(db, "/patients", PermissionAction.View);
        var user = new UserSummary(2, "receptionist", "Receptionist", "r@example.com", 2, "Receptionist");
        var (context, next, wasNextCalled) = BuildContext("GET", user);

        await filter.OnActionExecutionAsync(context, next);

        var redirect = Assert.IsType<RedirectToActionResult>(context.Result);
        Assert.Equal("AccessDenied", redirect.ActionName);
        Assert.False(wasNextCalled());
        db.Dispose();
    }

    [Fact]
    public async Task NonAdminWithNoPermissionRow_PostRequest_Returns403Json()
    {
        var db = TestDb.Create();
        SeedNavPage(db, "/patients");
        var filter = new RequirePermissionFilter(db, "/patients", PermissionAction.Delete);
        var user = new UserSummary(2, "receptionist", "Receptionist", "r@example.com", 2, "Receptionist");
        var (context, next, wasNextCalled) = BuildContext("POST", user);

        await filter.OnActionExecutionAsync(context, next);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
        Assert.False(wasNextCalled());
        db.Dispose();
    }

    [Fact]
    public async Task NonAdminWithGrantedPermission_Proceeds()
    {
        var db = TestDb.Create();
        var navPageId = SeedNavPage(db, "/patients");
        db.Permissions.Add(new PermissionEntity { RoleId = 2, NavPageId = navPageId, CanView = true, CanCreate = false, CanEdit = false, CanDelete = false });
        db.SaveChanges();

        var filter = new RequirePermissionFilter(db, "/patients", PermissionAction.View);
        var user = new UserSummary(2, "receptionist", "Receptionist", "r@example.com", 2, "Receptionist");
        var (context, next, wasNextCalled) = BuildContext("GET", user);

        await filter.OnActionExecutionAsync(context, next);

        Assert.Null(context.Result);
        Assert.True(wasNextCalled());
        db.Dispose();
    }

    [Fact]
    public async Task NonAdminWithViewButNotDelete_DeniedOnlyForDelete()
    {
        var db = TestDb.Create();
        var navPageId = SeedNavPage(db, "/patients");
        db.Permissions.Add(new PermissionEntity { RoleId = 2, NavPageId = navPageId, CanView = true, CanCreate = false, CanEdit = false, CanDelete = false });
        db.SaveChanges();
        var user = new UserSummary(2, "receptionist", "Receptionist", "r@example.com", 2, "Receptionist");

        var viewFilter = new RequirePermissionFilter(db, "/patients", PermissionAction.View);
        var (viewContext, viewNext, _) = BuildContext("GET", user);
        await viewFilter.OnActionExecutionAsync(viewContext, viewNext);
        Assert.Null(viewContext.Result);

        var deleteFilter = new RequirePermissionFilter(db, "/patients", PermissionAction.Delete);
        var (deleteContext, deleteNext, _) = BuildContext("POST", user);
        await deleteFilter.OnActionExecutionAsync(deleteContext, deleteNext);
        Assert.IsType<ObjectResult>(deleteContext.Result);
        db.Dispose();
    }

    private static int SeedNavPage(ClinicMsDbContext db, string pageUrl)
    {
        var navPage = new NavPageEntity { ModuleId = 1, PageName = "Patients", PageUrl = pageUrl, DisplayOrder = 1, IsActive = true };
        db.NavPages.Add(navPage);
        db.SaveChanges();
        return navPage.Id;
    }
}
