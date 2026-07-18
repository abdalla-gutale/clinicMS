namespace ClinicMS.Web.Services.Api;

/// <summary>Logo URLs from ClinicSettings are stored relative to ClinicMS.API's own wwwroot/uploads
/// (e.g. "/uploads/branding/logo.png"), not ClinicMS.Web's -- resolve them against the API's base
/// URL before handing them to a view, or they 404.</summary>
public static class LogoUrlResolver
{
    public static string? Resolve(string? logoUrl, IConfiguration configuration)
    {
        if (logoUrl is null || Uri.IsWellFormedUriString(logoUrl, UriKind.Absolute))
        {
            return logoUrl;
        }

        var apiBaseUrl = configuration["Api:BaseUrl"]?.TrimEnd('/') ?? "";
        return apiBaseUrl + logoUrl;
    }
}
