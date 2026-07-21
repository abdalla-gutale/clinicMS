using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Settings;
using ClinicMS.Web.Models.Api.Sms;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class SettingsController : Controller
    {
        private readonly ISettingsApiClient _settingsApiClient;
        private readonly ISmsApiClient _smsApiClient;
        private readonly IConfiguration _configuration;

        public SettingsController(ISettingsApiClient settingsApiClient, ISmsApiClient smsApiClient, IConfiguration configuration)
        {
            _settingsApiClient = settingsApiClient;
            _smsApiClient = smsApiClient;
            _configuration = configuration;
        }

        [RequirePermission("/settings/clinic", PermissionAction.View)]
        public async Task<IActionResult> General(CancellationToken cancellationToken)
        {
            var settings = await _settingsApiClient.GetClinicSettingsAsync(cancellationToken);
            // Kept separate from SettingsJson (below) so Save round-trips the original relative
            // URLs rather than persisting the API-origin-qualified ones used only for the previews.
            ViewBag.IconLogoPreviewUrl = LogoUrlResolver.Resolve(settings?.LogoIconUrl, _configuration);
            ViewBag.LogoPreviewUrl = LogoUrlResolver.Resolve(settings?.LogoUrl, _configuration);
            ViewBag.SidebarLogoPreviewUrl = LogoUrlResolver.Resolve(settings?.SidebarLogoUrl, _configuration);
            ViewBag.ReportLogoPreviewUrl = LogoUrlResolver.Resolve(settings?.ReportLogoUrl, _configuration);
            ViewBag.SettingsJson = ViewJson.Serialize(settings);

            var merchantAccount = await _settingsApiClient.GetMerchantAccountAsync(cancellationToken);
            ViewBag.MerchantAccountJson = ViewJson.Serialize(merchantAccount);

            var discounts = await _settingsApiClient.GetDiscountsAsync(cancellationToken);
            ViewBag.DiscountsJson = ViewJson.Serialize(discounts);

            var paymentAccounts = await _settingsApiClient.GetPaymentAccountsAsync(cancellationToken);
            ViewBag.PaymentAccountsJson = ViewJson.Serialize(paymentAccounts);

            var smsConfigurations = await _smsApiClient.GetConfigurationsAsync(cancellationToken);
            ViewBag.SmsConfigurationsJson = ViewJson.Serialize(smsConfigurations);

            var smsTemplates = await _smsApiClient.GetTemplatesAsync(cancellationToken);
            ViewBag.SmsTemplatesJson = ViewJson.Serialize(smsTemplates.OrderByDescending(t => t.CreatedAt));

            return View();
        }

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Edit)]
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

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Edit)]
        public async Task<IActionResult> SaveMerchantAccount([FromBody] UpsertMerchantAccountRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var account = await _settingsApiClient.UpsertMerchantAccountAsync(request, cancellationToken);
                return Json(account);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Create)]
        public async Task<IActionResult> CreateDiscount([FromBody] CreateDiscountRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var discount = await _settingsApiClient.CreateDiscountAsync(request, cancellationToken);
                return Json(discount);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Edit)]
        public async Task<IActionResult> UpdateDiscount(int id, [FromBody] UpdateDiscountRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var discount = await _settingsApiClient.UpdateDiscountAsync(id, request, cancellationToken);
                return Json(discount);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Delete)]
        public async Task<IActionResult> DeleteDiscount(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _settingsApiClient.DeleteDiscountAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Create)]
        public async Task<IActionResult> CreatePaymentAccount([FromBody] CreatePaymentAccountRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var account = await _settingsApiClient.CreatePaymentAccountAsync(request, cancellationToken);
                return Json(account);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Edit)]
        public async Task<IActionResult> UpdatePaymentAccount(int id, [FromBody] UpdatePaymentAccountRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var account = await _settingsApiClient.UpdatePaymentAccountAsync(id, request, cancellationToken);
                return Json(account);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Delete)]
        public async Task<IActionResult> DeletePaymentAccount(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _settingsApiClient.DeletePaymentAccountAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Create)]
        public async Task<IActionResult> CreateSmsConfiguration([FromBody] CreateSmsConfigurationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var config = await _smsApiClient.CreateConfigurationAsync(request, cancellationToken);
                return Json(config);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Edit)]
        public async Task<IActionResult> UpdateSmsConfiguration(int id, [FromBody] UpdateSmsConfigurationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var config = await _smsApiClient.UpdateConfigurationAsync(id, request, cancellationToken);
                return Json(config);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Delete)]
        public async Task<IActionResult> DeleteSmsConfiguration(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _smsApiClient.DeleteConfigurationAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Create)]
        public async Task<IActionResult> CreateSmsTemplate([FromBody] CreateSmsTemplateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var template = await _smsApiClient.CreateTemplateAsync(request, cancellationToken);
                return Json(template);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Edit)]
        public async Task<IActionResult> UpdateSmsTemplate(int id, [FromBody] UpdateSmsTemplateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var template = await _smsApiClient.UpdateTemplateAsync(id, request, cancellationToken);
                return Json(template);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/settings/clinic", PermissionAction.Delete)]
        public async Task<IActionResult> DeleteSmsTemplate(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _smsApiClient.DeleteTemplateAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
