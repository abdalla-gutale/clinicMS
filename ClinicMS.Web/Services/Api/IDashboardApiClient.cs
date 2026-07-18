using ClinicMS.Web.Models.Api.Dashboard;

namespace ClinicMS.Web.Services.Api;

public interface IDashboardApiClient
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
