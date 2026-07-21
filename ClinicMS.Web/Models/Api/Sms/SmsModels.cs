namespace ClinicMS.Web.Models.Api.Sms;

public enum ChannelType
{
    SMS,
    WhatsApp,
    Email
}

public record SmsTemplateDto(
    int Id,
    string TemplateName,
    ChannelType ChannelType,
    string MessageBody,
    bool IsActive,
    DateTime CreatedAt);

public record CreateSmsTemplateRequest(string TemplateName, ChannelType ChannelType, string MessageBody, bool IsActive);

public record UpdateSmsTemplateRequest(string TemplateName, ChannelType ChannelType, string MessageBody, bool IsActive);

public record SmsConfigurationDto(
    int Id, ChannelType ChannelType, string ProviderName, string? ApiKey, string? ApiSecret,
    string? SenderId, string? HostName, int? PortNumber, bool IsActive);

public record CreateSmsConfigurationRequest(
    ChannelType ChannelType, string ProviderName, string? ApiKey, string? ApiSecret,
    string? SenderId, string? HostName, int? PortNumber, bool IsActive);

public record UpdateSmsConfigurationRequest(
    ChannelType ChannelType, string ProviderName, string? ApiKey, string? ApiSecret,
    string? SenderId, string? HostName, int? PortNumber, bool IsActive);
