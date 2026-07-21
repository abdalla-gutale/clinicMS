using ClinicMS.Web.Models.Api.Reports;

namespace ClinicMS.Web.Services.Api;

public interface IReportsApiClient
{
    Task<BalanceSheetDto> GetBalanceSheetAsync(CancellationToken cancellationToken = default);
}
