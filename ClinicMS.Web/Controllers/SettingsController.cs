using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Settings;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class SettingsController : Controller
    {
        private readonly ISettingsApiClient _settingsApiClient;
        private readonly IConfiguration _configuration;

        public SettingsController(ISettingsApiClient settingsApiClient, IConfiguration configuration)
        {
            _settingsApiClient = settingsApiClient;
            _configuration = configuration;
        }

        public async Task<IActionResult> General(CancellationToken cancellationToken)
        {
            var settings = await _settingsApiClient.GetClinicSettingsAsync(cancellationToken);
            // Kept separate from SettingsJson (below) so Save round-trips the original relative
            // LogoUrl rather than persisting the API-origin-qualified one used only for the preview.
            ViewBag.LogoPreviewUrl = LogoUrlResolver.Resolve(settings?.LogoUrl, _configuration);
            ViewBag.SettingsJson = ViewJson.Serialize(settings);
            return View();
        }

        public IActionResult Branches() => View();

        [HttpPost]
        public async Task<IActionResult> SaveGeneral([FromBody] UpsertClinicSettingRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var settings = await _settingsApiClient.UpsertClinicSettingsAsync(request, cancellationToken);
                return Json(settings);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
