namespace ClinicMS.Web.Models.Api.Auth;

public enum OtpPurpose
{
    Login,
    PasswordReset,
    TransactionVerify
}

public record LoginRequest(string Username, string Password);

public record UserSummary(int Id, string Username, string FullName, string Email, int RoleId, string RoleName);

public record LoginResponse(string AccessToken, DateTime ExpiresAt, UserSummary User);

public record LoginChallenge(int UserId, string MaskedEmail, string Message, DateTime OtpExpiresAt);

public record VerifyLoginOtpRequest(int UserId, string OtpCode);

public record RequestOtpRequest(int UserId, OtpPurpose Purpose);

/// <summary>Permission-scoped nav tree for the logged-in user, as returned by GET api/auth/menu --
/// Admin implicitly gets every active page; other roles only see pages their role has at least
/// CanView on. Report pages aren't mirrored here since ClinicMS.Web doesn't have a Reports area yet.
/// Group-header pages (e.g. "Billing & Revenue") carry their real pages in SubPages; SubPages stays
/// nullable defensively since it's still an optional field on the wire.</summary>
public record MenuPageDto(
    int NavPageId,
    string PageName,
    string PageUrl,
    bool CanView,
    bool CanCreate,
    bool CanEdit,
    bool CanDelete,
    IReadOnlyList<MenuPageDto>? SubPages = null);

public record MenuModuleDto(int ModuleId, string ModuleName, IReadOnlyList<MenuPageDto> Pages);

public record MenuDto(IReadOnlyList<MenuModuleDto> Modules);
