using ClinicMS.Web.Models.Api.Sms;

namespace ClinicMS.Web.Services.Api.Mocks;

public class MockSmsApiClient : ISmsApiClient
{
    public Task<IReadOnlyList<SmsTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SmsTemplateDto>>(MockStore.SmsTemplates.ToList());

    public Task<SmsTemplateDto> CreateTemplateAsync(CreateSmsTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var type = MockStore.TemplateTypes.FirstOrDefault(t => t.Id == request.TemplateTypeId)
            ?? throw new ApiException(400, "Selected template type does not exist.");

        var template = new SmsTemplateDto(
            MockStore.NextSmsTemplateId++,
            request.TemplateName,
            type.Id,
            type.TypeName,
            request.ChannelType,
            request.MessageBody,
            request.IsActive,
            DateTime.UtcNow);

        MockStore.SmsTemplates.Add(template);
        return Task.FromResult(template);
    }

    public Task<SmsTemplateDto> UpdateTemplateAsync(int id, UpdateSmsTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.SmsTemplates.FindIndex(t => t.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Template not found.");
        }

        var type = MockStore.TemplateTypes.FirstOrDefault(t => t.Id == request.TemplateTypeId)
            ?? throw new ApiException(400, "Selected template type does not exist.");

        var existing = MockStore.SmsTemplates[index];
        var updated = existing with
        {
            TemplateName = request.TemplateName,
            TemplateTypeId = type.Id,
            TemplateTypeName = type.TypeName,
            ChannelType = request.ChannelType,
            MessageBody = request.MessageBody,
            IsActive = request.IsActive,
        };

        MockStore.SmsTemplates[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteTemplateAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = MockStore.SmsTemplates.RemoveAll(t => t.Id == id);
        if (removed == 0)
        {
            throw new ApiException(404, "Template not found.");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TemplateTypeDto>> GetTemplateTypesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TemplateTypeDto>>(MockStore.TemplateTypes.ToList());

    public Task<IReadOnlyList<SmsConfigurationDto>> GetConfigurationsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SmsConfigurationDto>>(MockStore.SmsConfigurations.ToList());

    public Task<SmsConfigurationDto> CreateConfigurationAsync(CreateSmsConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var config = new SmsConfigurationDto(
            MockStore.NextSmsConfigurationId++,
            request.ChannelType,
            request.ProviderName,
            request.ApiKey,
            request.ApiSecret,
            request.SenderId,
            request.HostName,
            request.PortNumber,
            request.IsActive);

        MockStore.SmsConfigurations.Add(config);
        return Task.FromResult(config);
    }

    public Task<SmsConfigurationDto> UpdateConfigurationAsync(int id, UpdateSmsConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.SmsConfigurations.FindIndex(c => c.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Configuration not found.");
        }

        var updated = new SmsConfigurationDto(
            id, request.ChannelType, request.ProviderName, request.ApiKey, request.ApiSecret,
            request.SenderId, request.HostName, request.PortNumber, request.IsActive);

        MockStore.SmsConfigurations[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteConfigurationAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = MockStore.SmsConfigurations.RemoveAll(c => c.Id == id);
        if (removed == 0)
        {
            throw new ApiException(404, "Configuration not found.");
        }

        return Task.CompletedTask;
    }
}
