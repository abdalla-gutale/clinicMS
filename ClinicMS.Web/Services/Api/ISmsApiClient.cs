using ClinicMS.Web.Models.Api.Sms;

namespace ClinicMS.Web.Services.Api;

public interface ISmsApiClient
{
    Task<IReadOnlyList<SmsTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TemplateTypeDto>> GetTemplateTypesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SmsConfigurationDto>> GetConfigurationsAsync(CancellationToken cancellationToken = default);
}
