using ClinicMS.Web.Models.Api.Auth;
using ClinicMS.Web.Models.Api.Settings;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthApiClient _authApiClient;
        private readonly ISettingsApiClient _settingsApiClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthApiClient authApiClient, ISettingsApiClient settingsApiClient, IConfiguration configuration, ILogger<AccountController> logger)
        {
            _authApiClient = authApiClient;
            _settingsApiClient = settingsApiClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> Login(CancellationToken cancellationToken)
        {
            ViewBag.BrandingJson = ViewJson.Serialize(await GetBrandingWithAbsoluteLogoAsync(cancellationToken));
            return View();
        }

        public async Task<IActionResult> Otp(CancellationToken cancellationToken)
        {
            ViewBag.BrandingJson = ViewJson.Serialize(await GetBrandingWithAbsoluteLogoAsync(cancellationToken));
            return View();
        }

        private async Task<PublicBrandingDto?> GetBrandingWithAbsoluteLogoAsync(CancellationToken cancellationToken)
        {
            PublicBrandingDto? branding;
            try
            {
                branding = await _settingsApiClient.GetPublicBrandingAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ApiException)
            {
                _logger.LogWarning(ex, "Could not fetch clinic branding; falling back to defaults.");
                return null;
            }

            if (branding is null)
            {
                return null;
            }

            return branding with { LogoUrl = LogoUrlResolver.Resolve(branding.LogoUrl, _configuration) };
        }

        public IActionResult Profile() => View();

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var challenge = await _authApiClient.LoginAsync(request.Username, request.Password, cancellationToken);
                HttpContext.Session.SetInt32(SessionKeys.PendingLoginUserId, challenge.UserId);
                return Json(new { maskedEmail = challenge.MaskedEmail, otpExpiresAt = challenge.OtpExpiresAt });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return ServiceUnavailable(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpModel model, CancellationToken cancellationToken)
        {
            var userId = HttpContext.Session.GetInt32(SessionKeys.PendingLoginUserId);
            if (userId is null)
            {
                return StatusCode(400, new { message = "Your login attempt has expired. Please sign in again." });
            }

            try
            {
                var result = await _authApiClient.VerifyLoginOtpAsync(userId.Value, model.OtpCode, cancellationToken);

                HttpContext.Session.SetString(SessionKeys.AuthToken, result.AccessToken);
                HttpContext.Session.SetString(SessionKeys.AuthUser, System.Text.Json.JsonSerializer.Serialize(result.User));
                HttpContext.Session.Remove(SessionKeys.PendingLoginUserId);

                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return ServiceUnavailable(ex);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResendOtp(CancellationToken cancellationToken)
        {
            var userId = HttpContext.Session.GetInt32(SessionKeys.PendingLoginUserId);
            if (userId is null)
            {
                return StatusCode(400, new { message = "Your login attempt has expired. Please sign in again." });
            }

            try
            {
                await _authApiClient.RequestOtpAsync(userId.Value, OtpPurpose.Login, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return ServiceUnavailable(ex);
            }
        }

        private IActionResult ServiceUnavailable(Exception ex)
        {
            _logger.LogWarning(ex, "ClinicMS.API is unreachable.");
            return StatusCode(503, new { message = "The server is temporarily unavailable. Please try again shortly." });
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }

    public record VerifyOtpModel(string OtpCode);
}
