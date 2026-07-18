using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Notifications;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class NotificationsController : Controller
    {
        private readonly INotificationsApiClient _notificationsApiClient;

        public NotificationsController(INotificationsApiClient notificationsApiClient)
        {
            _notificationsApiClient = notificationsApiClient;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var patients = await _notificationsApiClient.GetPatientsAsync(cancellationToken);
            ViewBag.PatientsJson = ViewJson.Serialize(patients);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendEmail([FromBody] SendPatientEmailRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _notificationsApiClient.SendEmailAsync(request, cancellationToken);
                return Json(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendWhatsApp([FromBody] SendPatientWhatsAppRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _notificationsApiClient.SendWhatsAppAsync(request, cancellationToken);
                return Json(result);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
