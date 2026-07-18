using ClinicMS.Web.Models.Api.Audit;

namespace ClinicMS.Web.Services.Api;

public class AuditApiClient : ApiClientBase, IAuditApiClient
{
    public AuditApiClient(HttpClient http) : base(http)
    {
    }

    public Task<PagedResult<AuditTrailDto>> GetTrailAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        GetAsync<PagedResult<AuditTrailDto>>($"api/audit/trail?page={page}&pageSize={pageSize}", cancellationToken);

    public Task<PagedResult<UserLogDto>> GetUserLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        GetAsync<PagedResult<UserLogDto>>($"api/audit/user-logs?page={page}&pageSize={pageSize}", cancellationToken);
}
