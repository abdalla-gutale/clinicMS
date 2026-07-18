using ClinicMS.Web.Models.Api.Sms;

namespace ClinicMS.Web.Services.Api;

public class SmsApiClient : ApiClientBase, ISmsApiClient
{
    public SmsApiClient(HttpClient http) : base(http)
    {
    }

    public Task<IReadOnlyList<SmsTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<SmsTemplateDto>>("api/smstemplates", cancellationToken);

    public Task<IReadOnlyList<TemplateTypeDto>> GetTemplateTypesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<TemplateTypeDto>>("api/templatetypes", cancellationToken);

    public Task<IReadOnlyList<SmsConfigurationDto>> GetConfigurationsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<SmsConfigurationDto>>("api/smsconfigurations", cancellationToken);
}
