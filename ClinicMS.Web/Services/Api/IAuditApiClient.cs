using ClinicMS.Web.Models.Api.Audit;

namespace ClinicMS.Web.Services.Api;

public interface IAuditApiClient
{
    Task<PagedResult<AuditTrailDto>> GetTrailAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedResult<UserLogDto>> GetUserLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<ActivityPageDto> GetActivityFeedAsync(int page, int pageSize, string? search, string? actionFilter, CancellationToken cancellationToken = default);

    /// <summary>Records a login/logout event to userLogs. Silently skipped for the code-level master
    /// login (no row in the users table) since userLogs.userId has an FK to users.</summary>
    Task LogUserActionAsync(int userId, string action, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default);
}
