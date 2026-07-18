using System.Net.Http.Headers;

namespace ClinicMS.Web.Services.Api;

/// <summary>Attaches the signed-in user's JWT (stashed in session by AccountController after a
/// successful OTP verification) to every outgoing ClinicMS.API request.</summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.Session.GetString(SessionKeys.AuthToken);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
