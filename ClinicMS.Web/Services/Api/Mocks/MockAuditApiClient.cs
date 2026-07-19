using ClinicMS.Web.Models.Api.Audit;

namespace ClinicMS.Web.Services.Api.Mocks;

public class MockAuditApiClient : IAuditApiClient
{
    public Task<PagedResult<AuditTrailDto>> GetTrailAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = Page(MockStore.AuditTrail.OrderByDescending(a => a.CreatedAt), page, pageSize);
        return Task.FromResult(new PagedResult<AuditTrailDto>(items, page, pageSize, MockStore.AuditTrail.Count));
    }

    public Task<PagedResult<UserLogDto>> GetUserLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var items = Page(MockStore.UserLogs.OrderByDescending(l => l.CreatedAt), page, pageSize);
        return Task.FromResult(new PagedResult<UserLogDto>(items, page, pageSize, MockStore.UserLogs.Count));
    }

    private static IReadOnlyList<T> Page<T>(IEnumerable<T> source, int page, int pageSize) =>
        source.Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToList();
}
