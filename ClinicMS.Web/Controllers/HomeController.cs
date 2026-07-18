using ClinicMS.Web.Filters;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDashboardApiClient _dashboardApiClient;

        public HomeController(IDashboardApiClient dashboardApiClient)
        {
            _dashboardApiClient = dashboardApiClient;
        }

        [RequireAuth]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var summary = await _dashboardApiClient.GetSummaryAsync(cancellationToken);
            ViewBag.DashboardJson = ViewJson.Serialize(summary);
            return View();
        }

        public IActionResult StatusCode(int code)
        {
            if (code == 404) return View("NotFound");
            return View("ServerError");
        }
    }
}
