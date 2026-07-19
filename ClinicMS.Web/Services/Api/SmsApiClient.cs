using ClinicMS.Web.Models.Api.Sms;

namespace ClinicMS.Web.Services.Api;

public class SmsApiClient : ApiClientBase, ISmsApiClient
{
    public SmsApiClient(HttpClient http) : base(http)
    {
    }

    public Task<IReadOnlyList<SmsTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<SmsTemplateDto>>("api/smstemplates", cancellationToken);

    public Task<SmsTemplateDto> CreateTemplateAsync(CreateSmsTemplateRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<SmsTemplateDto>("api/smstemplates", request, cancellationToken);

    public Task<SmsTemplateDto> UpdateTemplateAsync(int id, UpdateSmsTemplateRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<SmsTemplateDto>($"api/smstemplates/{id}", request, cancellationToken);

    public Task DeleteTemplateAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/smstemplates/{id}", cancellationToken);

    public Task<IReadOnlyList<TemplateTypeDto>> GetTemplateTypesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<TemplateTypeDto>>("api/templatetypes", cancellationToken);

    public Task<IReadOnlyList<SmsConfigurationDto>> GetConfigurationsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<SmsConfigurationDto>>("api/smsconfigurations", cancellationToken);

    public Task<SmsConfigurationDto> CreateConfigurationAsync(CreateSmsConfigurationRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<SmsConfigurationDto>("api/smsconfigurations", request, cancellationToken);

    public Task<SmsConfigurationDto> UpdateConfigurationAsync(int id, UpdateSmsConfigurationRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<SmsConfigurationDto>($"api/smsconfigurations/{id}", request, cancellationToken);

    public Task DeleteConfigurationAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/smsconfigurations/{id}", cancellationToken);
}
