using ClinicMS.Web.Models.Api.Auth;
using ClinicMS.Web.Models.Api.Rbac;

namespace ClinicMS.Web.Services.Api.Mocks;

/// <summary>Backs IAuthApiClient with MockStore data. AccountController's login/OTP actions bypass
/// this entirely with their own static-credential check; the only method actually exercised is
/// GetMenuAsync, which SidebarViewComponent calls on every authenticated page.</summary>
public class MockAuthApiClient : IAuthApiClient
{
    public Task<LoginChallenge> LoginAsync(string username, string password, CancellationToken cancellationToken = default) =>
        Task.FromResult(new LoginChallenge(1, "ad***@clinicms.com", "OTP sent.", DateTime.UtcNow.AddMinutes(5)));

    public Task<LoginResponse> VerifyLoginOtpAsync(int userId, string otpCode, CancellationToken cancellationToken = default)
    {
        var user = MockStore.Users.First(u => u.Id == userId);
        var summary = new UserSummary(user.Id, user.Username, user.FullName, user.Email, user.RoleId, user.RoleName);
        return Task.FromResult(new LoginResponse("mock-dev-token", DateTime.UtcNow.AddHours(8), summary));
    }

    public Task RequestOtpAsync(int userId, OtpPurpose purpose, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<MenuDto> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        var modules = MockStore.Modules
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .Select(m => new MenuModuleDto(
                m.Id,
                m.ModuleName,
                MockStore.NavPages
                    .Where(p => p.ModuleId == m.Id && p.IsActive && p.ParentPageId is null)
                    .OrderBy(p => p.DisplayOrder)
                    .Select(BuildMenuPage)
                    .ToList()))
            .Where(m => m.Pages.Count > 0)
            .ToList();

        return Task.FromResult(new MenuDto(modules));
    }

    private static MenuPageDto BuildMenuPage(NavPageDto page)
    {
        var subPages = MockStore.NavPages
            .Where(p => p.ParentPageId == page.Id && p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .Select(BuildMenuPage)
            .ToList();

        return new MenuPageDto(page.Id, page.PageName, page.PageUrl, true, true, true, true, subPages.Count > 0 ? subPages : null);
    }
}
