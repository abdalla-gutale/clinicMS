using ClinicMS.Web.Models.Api.Dashboard;

namespace ClinicMS.Web.Services.Api;

public class DashboardApiClient : ApiClientBase, IDashboardApiClient
{
    public DashboardApiClient(HttpClient http) : base(http)
    {
    }

    public Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default) =>
        GetAsync<DashboardSummaryDto>("api/dashboard/summary", cancellationToken);
}
