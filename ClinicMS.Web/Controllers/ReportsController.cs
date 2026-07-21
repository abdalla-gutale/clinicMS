using ClinicMS.Web.Filters;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class ReportsController : Controller
    {
        private readonly IReportsApiClient _reportsApiClient;

        public ReportsController(IReportsApiClient reportsApiClient)
        {
            _reportsApiClient = reportsApiClient;
        }

        [RequirePermission("/reports/balance-sheet", PermissionAction.View)]
        public async Task<IActionResult> BalanceSheet(CancellationToken cancellationToken)
        {
            var balanceSheet = await _reportsApiClient.GetBalanceSheetAsync(cancellationToken);
            return View(balanceSheet);
        }
    }
}
