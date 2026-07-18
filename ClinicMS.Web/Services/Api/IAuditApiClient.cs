using ClinicMS.Web.Models.Api.Audit;

namespace ClinicMS.Web.Services.Api;

public interface IAuditApiClient
{
    Task<PagedResult<AuditTrailDto>> GetTrailAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedResult<UserLogDto>> GetUserLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
