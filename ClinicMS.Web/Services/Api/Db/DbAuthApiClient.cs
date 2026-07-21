using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Auth;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Services.Api.Db;

/// <summary>AccountController owns the actual credential check (real users table + a code-level
/// master bypass) and writes the resulting UserSummary straight into session, so
/// LoginAsync/VerifyLoginOtpAsync/RequestOtpAsync here are never actually called -- only
/// GetMenuAsync is exercised, by SidebarViewComponent on every authenticated page.</summary>
public class DbAuthApiClient : IAuthApiClient
{
    private readonly ClinicMsDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DbAuthApiClient(ClinicMsDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginChallenge> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken)
            ?? throw new ApiException(401, "Invalid username or password.");
        return new LoginChallenge(user.Id, MaskEmail(user.Email), "OTP sent.", DateTime.UtcNow.AddMinutes(5));
    }

    public async Task<LoginResponse> VerifyLoginOtpAsync(int userId, string otpCode, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new ApiException(404, "User not found.");
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == user.RoleId, cancellationToken);
        var summary = new UserSummary(user.Id, user.Username, user.FullName, user.Email, user.RoleId, role?.RoleName ?? "");
        return new LoginResponse("db-dev-token", DateTime.UtcNow.AddHours(8), summary);
    }

    public Task RequestOtpAsync(int userId, OtpPurpose purpose, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task<MenuDto> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        var (roleId, roleName) = GetCurrentRole();

        // SuperAdmin is documented ("Full, unrestricted access... reserved for the primary account")
        // to always see every active page, independent of whatever rows happen to exist in
        // permissions -- everyone else is gated strictly by their role's CanView permission. Read
        // straight off the session's RoleName (set once at login) rather than re-querying the roles
        // table by id, so the code-level master login (whose RoleId isn't a real row) still works,
        // and so this bypass survives the roles table being cleared/reseeded during testing.
        var isFullAccess = roleName == "SuperAdmin";

        var modules = await _db.Modules.Where(m => m.IsActive).OrderBy(m => m.DisplayOrder).ToListAsync(cancellationToken);
        var navPages = await _db.NavPages.Where(p => p.IsActive).ToListAsync(cancellationToken);
        var permissions = !isFullAccess && roleId is int permRoleId
            ? await _db.Permissions.Where(p => p.RoleId == permRoleId).ToDictionaryAsync(p => p.NavPageId, cancellationToken)
            : new Dictionary<int, PermissionEntity>();

        var menuModules = modules
            .Select(m => new MenuModuleDto(
                m.Id,
                m.ModuleName,
                m.ModuleIcon,
                navPages.Where(p => p.ModuleId == m.Id && p.ParentPageId is null)
                    .OrderBy(p => p.DisplayOrder)
                    .Select(p => BuildMenuPage(p, navPages, permissions, isFullAccess))
                    .Where(mp => mp is not null)
                    .Select(mp => mp!)
                    .ToList()))
            .Where(m => m.Pages.Count > 0)
            .ToList();

        return new MenuDto(menuModules);
    }

    private (int? RoleId, string? RoleName) GetCurrentRole()
    {
        var json = _httpContextAccessor.HttpContext?.Session.GetString(SessionKeys.AuthUser);
        if (string.IsNullOrEmpty(json))
        {
            return (null, null);
        }

        var user = System.Text.Json.JsonSerializer.Deserialize<UserSummary>(json);
        return (user?.RoleId, user?.RoleName);
    }

    private static MenuPageDto? BuildMenuPage(
        NavPageEntity page, List<NavPageEntity> allPages, Dictionary<int, PermissionEntity> permissions, bool isFullAccess)
    {
        var subPages = allPages.Where(p => p.ParentPageId == page.Id)
            .OrderBy(p => p.DisplayOrder)
            .Select(p => BuildMenuPage(p, allPages, permissions, isFullAccess))
            .Where(mp => mp is not null)
            .Select(mp => mp!)
            .ToList();

        // A group header (e.g. "Medical Services") is just structural -- it shows whenever it has at
        // least one visible child, regardless of its own permission state.
        if (subPages.Count > 0)
        {
            return new MenuPageDto(page.Id, page.PageName, page.PageUrl, true, true, true, true, subPages);
        }

        if (isFullAccess)
        {
            return new MenuPageDto(page.Id, page.PageName, page.PageUrl, true, true, true, true, null);
        }

        if (!permissions.TryGetValue(page.Id, out var perm) || !perm.CanView)
        {
            return null;
        }

        return new MenuPageDto(page.Id, page.PageName, page.PageUrl, perm.CanView, perm.CanCreate, perm.CanEdit, perm.CanDelete, null);
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return email;
        }
        return $"{email[..2]}***{email[atIndex..]}";
    }
}
