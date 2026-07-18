using ClinicMS.Web.Models.Api.Settings;

namespace ClinicMS.Web.Services.Api;

public interface ISettingsApiClient
{
    Task<ClinicSettingDto?> GetClinicSettingsAsync(CancellationToken cancellationToken = default);

    Task<ClinicSettingDto> UpsertClinicSettingsAsync(UpsertClinicSettingRequest request, CancellationToken cancellationToken = default);

    Task<PublicBrandingDto?> GetPublicBrandingAsync(CancellationToken cancellationToken = default);
}
