using ClinicMS.Web.Models.Api.Auth;

namespace ClinicMS.Web.Services.Api;

public class AuthApiClient : ApiClientBase, IAuthApiClient
{
    public AuthApiClient(HttpClient http) : base(http)
    {
    }

    public Task<LoginChallenge> LoginAsync(string username, string password, CancellationToken cancellationToken = default) =>
        PostAsync<LoginChallenge>("api/auth/login", new LoginRequest(username, password), cancellationToken);

    public Task<LoginResponse> VerifyLoginOtpAsync(int userId, string otpCode, CancellationToken cancellationToken = default) =>
        PostAsync<LoginResponse>("api/auth/verify-login-otp", new VerifyLoginOtpRequest(userId, otpCode), cancellationToken);

    public Task RequestOtpAsync(int userId, OtpPurpose purpose, CancellationToken cancellationToken = default) =>
        PostAsync("api/auth/otp/request", new RequestOtpRequest(userId, purpose), cancellationToken);

    public Task<MenuDto> GetMenuAsync(CancellationToken cancellationToken = default) =>
        GetAsync<MenuDto>("api/auth/menu", cancellationToken);
}
