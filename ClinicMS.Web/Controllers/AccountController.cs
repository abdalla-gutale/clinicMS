using System.Text.Json;
using ClinicMS.Web.Models.Api.Auth;
using ClinicMS.Web.Models.Api.Settings;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicMS.Web.Controllers
{
    public class AccountController : Controller
    {
        // A code-level login that always works no matter what's in the users table -- so clearing
        // or reseeding real data during testing can never lock the developer out. Grants full
        // SuperAdmin access (see DbAuthApiClient.GetMenuAsync, which checks RoleName rather than
        // re-querying the roles table by id, so this bypass survives even the roles table being wiped).
        private const string MasterUsername = "Raadso";
        private const string MasterEmail = "mailabdallas@gmail.com";
        private const string MasterPassword = "raadso@9090";
        private const int MasterUserId = -1;
        private const string MasterRoleName = "SuperAdmin";

        private const int OtpValidityMinutes = 5;
        private const int ResetCodeValidityMinutes = 10;

        private readonly ISettingsApiClient _settingsApiClient;
        private readonly IUsersApiClient _usersApiClient;
        private readonly ISmsApiClient _smsApiClient;
        private readonly IAuditApiClient _auditApiClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ISettingsApiClient settingsApiClient,
            IUsersApiClient usersApiClient,
            ISmsApiClient smsApiClient,
            IAuditApiClient auditApiClient,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _settingsApiClient = settingsApiClient;
            _usersApiClient = usersApiClient;
            _smsApiClient = smsApiClient;
            _auditApiClient = auditApiClient;
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

        public IActionResult AccessDenied() => View();

        public async Task<IActionResult> ForgotPassword(CancellationToken cancellationToken)
        {
            ViewBag.BrandingJson = ViewJson.Serialize(await GetBrandingWithAbsoluteLogoAsync(cancellationToken));
            return View();
        }

        public async Task<IActionResult> ResetPassword(CancellationToken cancellationToken)
        {
            ViewBag.BrandingJson = ViewJson.Serialize(await GetBrandingWithAbsoluteLogoAsync(cancellationToken));
            return View();
        }

        [HttpPost]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var identifier = (request.Username ?? "").Trim();
            var user = await AuthenticateAsync(identifier, request.Password ?? "", cancellationToken);
            if (user is null)
            {
                return StatusCode(401, new { message = "Invalid username or password." });
            }

            var pending = NewPendingLogin(user);
            try
            {
                await SendOtpEmailAsync(pending, cancellationToken);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = $"Could not send verification code: {ex.Message}" });
            }

            HttpContext.Session.SetString(SessionKeys.PendingLoginUser, JsonSerializer.Serialize(pending));
            return Json(new { maskedEmail = MaskEmail(user.Email), otpExpiresAt = pending.ExpiresAt });
        }

        private async Task<UserSummary?> AuthenticateAsync(string identifier, string password, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(password))
            {
                return null;
            }

            if (string.Equals(identifier, MasterUsername, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(identifier, MasterEmail, StringComparison.OrdinalIgnoreCase))
            {
                return password == MasterPassword
                    ? new UserSummary(MasterUserId, MasterUsername, "Raadso", MasterEmail, MasterUserId, MasterRoleName)
                    : null;
            }

            var dbUser = await _usersApiClient.FindForLoginAsync(identifier, cancellationToken);
            if (dbUser is null || !dbUser.IsActive || !BCrypt.Net.BCrypt.Verify(password, dbUser.PasswordHash))
            {
                return null;
            }

            return new UserSummary(dbUser.Id, dbUser.Username, dbUser.FullName, dbUser.Email, dbUser.RoleId, dbUser.RoleName);
        }

        [HttpPost]
        [EnableRateLimiting("otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpModel model, CancellationToken cancellationToken)
        {
            var pending = GetPendingLogin();
            if (pending is null)
            {
                return StatusCode(400, new { message = "Your login attempt has expired. Please sign in again." });
            }

            if (DateTime.UtcNow > pending.ExpiresAt)
            {
                HttpContext.Session.Remove(SessionKeys.PendingLoginUser);
                return StatusCode(400, new { message = "Your verification code has expired. Please sign in again." });
            }

            if (model.OtpCode != pending.OtpCode)
            {
                return StatusCode(400, new { message = "Invalid verification code." });
            }

            HttpContext.Session.SetString(SessionKeys.AuthToken, "db-session-token");
            HttpContext.Session.SetString(SessionKeys.AuthUser, JsonSerializer.Serialize(pending.User));
            HttpContext.Session.Remove(SessionKeys.PendingLoginUser);

            await _auditApiClient.LogUserActionAsync(pending.User.Id, "Login", ClientIpAddress(), Request.Headers["User-Agent"].ToString(), cancellationToken);

            return Json(new { success = true });
        }

        [HttpPost]
        [EnableRateLimiting("otp")]
        public async Task<IActionResult> ResendOtp(CancellationToken cancellationToken)
        {
            var pending = GetPendingLogin();
            if (pending is null)
            {
                return StatusCode(400, new { message = "Your login attempt has expired. Please sign in again." });
            }

            var refreshed = NewPendingLogin(pending.User);
            try
            {
                await SendOtpEmailAsync(refreshed, cancellationToken);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = $"Could not resend verification code: {ex.Message}" });
            }

            HttpContext.Session.SetString(SessionKeys.PendingLoginUser, JsonSerializer.Serialize(refreshed));
            return Json(new { success = true });
        }

        [HttpPost]
        [EnableRateLimiting("passwordReset")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            var identifier = (request.Identifier ?? "").Trim();
            var dbUser = string.IsNullOrEmpty(identifier) ? null : await _usersApiClient.FindForLoginAsync(identifier, cancellationToken);

            // Same response whether or not the account exists, so this can't be used to enumerate
            // valid usernames/emails. The master bypass login has no DB row and no changeable
            // password, so it's simply never matched here -- nothing to reset for it.
            if (dbUser is not null && dbUser.IsActive)
            {
                var pending = new PendingReset(dbUser.Id, dbUser.Email,
                    Random.Shared.Next(0, 1_000_000).ToString("D6"), DateTime.UtcNow.AddMinutes(ResetCodeValidityMinutes));
                try
                {
                    await _smsApiClient.SendEmailAsync(
                        dbUser.Email,
                        "Reset your ClinicMS password",
                        $"Your ClinicMS password reset code is {pending.Code}. It expires in {ResetCodeValidityMinutes} minutes. If you didn't request this, you can ignore this email.",
                        cancellationToken);
                    HttpContext.Session.SetString(SessionKeys.PendingReset, JsonSerializer.Serialize(pending));
                }
                catch (ApiException)
                {
                    // Swallow -- still return the generic response below so an unauthenticated caller
                    // can't tell the mail gateway is misconfigured either.
                }
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [EnableRateLimiting("passwordReset")]
        public async Task<IActionResult> ResendResetCode(CancellationToken cancellationToken)
        {
            var pending = GetPendingReset();
            if (pending is null)
            {
                return StatusCode(400, new { message = "Your reset request has expired. Please start again." });
            }

            var refreshed = pending with
            {
                Code = Random.Shared.Next(0, 1_000_000).ToString("D6"),
                ExpiresAt = DateTime.UtcNow.AddMinutes(ResetCodeValidityMinutes),
            };
            try
            {
                await _smsApiClient.SendEmailAsync(
                    refreshed.Email,
                    "Reset your ClinicMS password",
                    $"Your ClinicMS password reset code is {refreshed.Code}. It expires in {ResetCodeValidityMinutes} minutes.",
                    cancellationToken);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }

            HttpContext.Session.SetString(SessionKeys.PendingReset, JsonSerializer.Serialize(refreshed));
            return Json(new { success = true });
        }

        [HttpPost]
        [EnableRateLimiting("passwordReset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model, CancellationToken cancellationToken)
        {
            var pending = GetPendingReset();
            if (pending is null)
            {
                return StatusCode(400, new { message = "Your reset request has expired. Please start again." });
            }

            if (DateTime.UtcNow > pending.ExpiresAt)
            {
                HttpContext.Session.Remove(SessionKeys.PendingReset);
                return StatusCode(400, new { message = "Your reset code has expired. Please start again." });
            }

            if (model.Code != pending.Code)
            {
                return StatusCode(400, new { message = "Invalid reset code." });
            }

            try
            {
                await _usersApiClient.ChangePasswordAsync(pending.UserId, model.NewPassword ?? "", cancellationToken);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }

            HttpContext.Session.Remove(SessionKeys.PendingReset);
            return Json(new { success = true });
        }

        private PendingReset? GetPendingReset()
        {
            var json = HttpContext.Session.GetString(SessionKeys.PendingReset);
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<PendingReset>(json);
        }

        [HttpPost]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            var authUser = GetAuthUser();
            if (authUser is not null)
            {
                await _auditApiClient.LogUserActionAsync(authUser.Id, "Logout", ClientIpAddress(), Request.Headers["User-Agent"].ToString(), cancellationToken);
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        private UserSummary? GetAuthUser()
        {
            var json = HttpContext.Session.GetString(SessionKeys.AuthUser);
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<UserSummary>(json);
        }

        private string? ClientIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

        private static PendingLogin NewPendingLogin(UserSummary user) =>
            new(user, Random.Shared.Next(0, 1_000_000).ToString("D6"), DateTime.UtcNow.AddMinutes(OtpValidityMinutes));

        private async Task SendOtpEmailAsync(PendingLogin pending, CancellationToken cancellationToken)
        {
            await _smsApiClient.SendEmailAsync(
                pending.User.Email,
                "Your ClinicMS verification code",
                $"Your ClinicMS verification code is {pending.OtpCode}. It expires in {OtpValidityMinutes} minutes.",
                cancellationToken);
        }

        private PendingLogin? GetPendingLogin()
        {
            var json = HttpContext.Session.GetString(SessionKeys.PendingLoginUser);
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<PendingLogin>(json);
        }

        private static string MaskEmail(string email)
        {
            var atIndex = email.IndexOf('@');
            if (atIndex <= 1)
            {
                return email;
            }
            return $"{email[..2]}***{email[atIndex..]}";
        }

        private record PendingLogin(UserSummary User, string OtpCode, DateTime ExpiresAt);

        private record PendingReset(int UserId, string Email, string Code, DateTime ExpiresAt);
    }

    public record VerifyOtpModel(string OtpCode);

    public record ForgotPasswordRequest(string? Identifier);

    public record ResetPasswordModel(string Code, string? NewPassword);
}
