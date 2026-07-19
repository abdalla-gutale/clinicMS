using ClinicMS.Web.Models.Api.Sms;

namespace ClinicMS.Web.Services.Api;

public interface ISmsApiClient
{
    Task<IReadOnlyList<SmsTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    Task<SmsTemplateDto> CreateTemplateAsync(CreateSmsTemplateRequest request, CancellationToken cancellationToken = default);

    Task<SmsTemplateDto> UpdateTemplateAsync(int id, UpdateSmsTemplateRequest request, CancellationToken cancellationToken = default);

    Task DeleteTemplateAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TemplateTypeDto>> GetTemplateTypesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SmsConfigurationDto>> GetConfigurationsAsync(CancellationToken cancellationToken = default);

    Task<SmsConfigurationDto> CreateConfigurationAsync(CreateSmsConfigurationRequest request, CancellationToken cancellationToken = default);

    Task<SmsConfigurationDto> UpdateConfigurationAsync(int id, UpdateSmsConfigurationRequest request, CancellationToken cancellationToken = default);

    Task DeleteConfigurationAsync(int id, CancellationToken cancellationToken = default);
}
