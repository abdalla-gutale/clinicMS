using ClinicMS.Web.Models.Api.Settings;

namespace ClinicMS.Web.Services.Api;

public class SettingsApiClient : ApiClientBase, ISettingsApiClient
{
    public SettingsApiClient(HttpClient http) : base(http)
    {
    }

    public Task<ClinicSettingDto?> GetClinicSettingsAsync(CancellationToken cancellationToken = default) =>
        GetOrDefaultAsync<ClinicSettingDto>("api/clinicsettings", cancellationToken);

    public Task<ClinicSettingDto> UpsertClinicSettingsAsync(UpsertClinicSettingRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<ClinicSettingDto>("api/clinicsettings", request, cancellationToken);

    public Task<PublicBrandingDto?> GetPublicBrandingAsync(CancellationToken cancellationToken = default) =>
        GetOptionalAsync<PublicBrandingDto>("api/clinicsettings/public-branding", cancellationToken);
}
