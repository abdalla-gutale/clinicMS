using ClinicMS.Web.Models.Api.Auth;
using ClinicMS.Web.Models.Api.Settings;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    public class AccountController : Controller
    {
        // Backend is not wired up yet -- login/OTP is stubbed against these fixed values so the
        // frontend can be designed and clicked through without ClinicMS.API running.
        private const string StaticUsername = "admin";
        private const string StaticPassword = "Admin@123";
        private const string StaticOtpCode = "123456";

        private readonly ISettingsApiClient _settingsApiClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ISettingsApiClient settingsApiClient, IConfiguration configuration, ILogger<AccountController> logger)
        {
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
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (!string.Equals(request.Username, StaticUsername, StringComparison.OrdinalIgnoreCase) ||
                request.Password != StaticPassword)
            {
                return StatusCode(401, new { message = "Invalid username or password." });
            }

            const int staticUserId = 1;
            HttpContext.Session.SetInt32(SessionKeys.PendingLoginUserId, staticUserId);
            return Json(new { maskedEmail = "ad***@clinic.com", otpExpiresAt = DateTime.UtcNow.AddMinutes(5) });
        }

        [HttpPost]
        public IActionResult VerifyOtp([FromBody] VerifyOtpModel model)
        {
            var userId = HttpContext.Session.GetInt32(SessionKeys.PendingLoginUserId);
            if (userId is null)
            {
                return StatusCode(400, new { message = "Your login attempt has expired. Please sign in again." });
            }

            if (model.OtpCode != StaticOtpCode)
            {
                return StatusCode(400, new { message = "Invalid verification code." });
            }

            var user = new UserSummary(userId.Value, StaticUsername, "Admin User", "admin@clinic.com", 1, "Administrator");
            HttpContext.Session.SetString(SessionKeys.AuthToken, "static-dev-token");
            HttpContext.Session.SetString(SessionKeys.AuthUser, System.Text.Json.JsonSerializer.Serialize(user));
            HttpContext.Session.Remove(SessionKeys.PendingLoginUserId);

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ResendOtp()
        {
            var userId = HttpContext.Session.GetInt32(SessionKeys.PendingLoginUserId);
            if (userId is null)
            {
                return StatusCode(400, new { message = "Your login attempt has expired. Please sign in again." });
            }

            return Json(new { success = true });
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
