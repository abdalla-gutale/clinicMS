using ClinicMS.Web.Filters;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class SmsController : Controller
    {
        private readonly ISmsApiClient _smsApiClient;

        public SmsController(ISmsApiClient smsApiClient)
        {
            _smsApiClient = smsApiClient;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var templates = await _smsApiClient.GetTemplatesAsync(cancellationToken);
            ViewBag.TemplatesJson = ViewJson.Serialize(templates.Where(t => t.IsActive));
            return View();
        }
    }
}
