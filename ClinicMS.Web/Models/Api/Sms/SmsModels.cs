namespace ClinicMS.Web.Models.Api.Sms;

public enum ChannelType
{
    SMS,
    WhatsApp,
    Email
}

public record SmsTemplateDto(int Id, int TemplateTypeId, string TemplateTypeName, ChannelType ChannelType, string MessageBody, bool IsActive);

public record CreateSmsTemplateRequest(int TemplateTypeId, ChannelType ChannelType, string MessageBody, bool IsActive);

public record UpdateSmsTemplateRequest(int TemplateTypeId, ChannelType ChannelType, string MessageBody, bool IsActive);

public record TemplateTypeDto(int Id, string TypeName);

public record SmsConfigurationDto(
    int Id, ChannelType ChannelType, string ProviderName, string? ApiKey, string? ApiSecret,
    string? SenderId, string? HostName, int? PortNumber, bool IsActive);
