using ClinicMS.Web.Models.Api.Auth;

namespace ClinicMS.Web.Services.Api;

public interface IAuthApiClient
{
    Task<LoginChallenge> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    Task<LoginResponse> VerifyLoginOtpAsync(int userId, string otpCode, CancellationToken cancellationToken = default);

    Task RequestOtpAsync(int userId, OtpPurpose purpose, CancellationToken cancellationToken = default);

    Task<MenuDto> GetMenuAsync(CancellationToken cancellationToken = default);
}
