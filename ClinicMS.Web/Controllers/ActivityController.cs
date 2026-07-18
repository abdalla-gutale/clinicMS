using ClinicMS.Web.Filters;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class ActivityController : Controller
    {
        private readonly IAuditApiClient _auditApiClient;
        private readonly IUsersApiClient _usersApiClient;

        public ActivityController(IAuditApiClient auditApiClient, IUsersApiClient usersApiClient)
        {
            _auditApiClient = auditApiClient;
            _usersApiClient = usersApiClient;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var trail = await _auditApiClient.GetTrailAsync(1, 100, cancellationToken);
            var userLogs = await _auditApiClient.GetUserLogsAsync(1, 100, cancellationToken);
            var users = await _usersApiClient.GetAllAsync(cancellationToken);

            var usernameById = users.ToDictionary(u => u.Id, u => u.Username);

            ViewBag.AuditTrailJson = ViewJson.Serialize(trail.Items);
            ViewBag.UserLogsJson = ViewJson.Serialize(userLogs.Items);
            ViewBag.UsernamesJson = ViewJson.Serialize(usernameById);

            return View();
        }
    }
}
