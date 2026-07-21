using ClinicMS.Web.Filters;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class ActivityController : Controller
    {
        private const int DefaultPageSize = 12;

        private readonly IAuditApiClient _auditApiClient;

        public ActivityController(IAuditApiClient auditApiClient)
        {
            _auditApiClient = auditApiClient;
        }

        [RequirePermission("/admin/audit", PermissionAction.View)]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var initialPage = await _auditApiClient.GetActivityFeedAsync(1, DefaultPageSize, null, null, cancellationToken);
            ViewBag.ActivityPageJson = ViewJson.Serialize(initialPage);
            return View();
        }

        [HttpGet]
        [RequirePermission("/admin/audit", PermissionAction.View)]
        public async Task<IActionResult> GetPage(int page, int pageSize, string? search, string? action, CancellationToken cancellationToken)
        {
            var result = await _auditApiClient.GetActivityFeedAsync(page, pageSize, search, action, cancellationToken);
            return Json(result);
        }
    }
}
