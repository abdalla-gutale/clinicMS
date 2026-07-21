using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Sms;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace ClinicMS.Web.Services.Api.Db;

public class DbSmsApiClient : ISmsApiClient
{
    // Stands in for a real ApiKey/ApiSecret value in every response sent to the browser -- gateway
    // credentials (Gmail app passwords, provider tokens) must never round-trip to the client in
    // plain text. UpdateConfigurationAsync recognizes this exact value coming back on save as
    // "field left untouched" and keeps the real stored secret instead of overwriting it with dots.
    private const string MaskedSecret = "••••••••";

    private readonly ClinicMsDbContext _db;

    public DbSmsApiClient(ClinicMsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SmsTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var templates = await _db.SmsTemplates.ToListAsync(cancellationToken);
        return templates.Select(ToDto).ToList();
    }

    public async Task<SmsTemplateDto> CreateTemplateAsync(CreateSmsTemplateRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureChannelHasActiveGatewayAsync(request.ChannelType, cancellationToken);

        var entity = new SmsTemplateEntity
        {
            TemplateName = request.TemplateName,
            MessageBody = request.MessageBody,
            IsActive = request.IsActive,
            ChannelType = request.ChannelType.ToString(),
            CreatedAt = DateTime.UtcNow,
        };

        _db.SmsTemplates.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SmsTemplateDto> UpdateTemplateAsync(int id, UpdateSmsTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SmsTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Template not found.");

        await EnsureChannelHasActiveGatewayAsync(request.ChannelType, cancellationToken);

        entity.TemplateName = request.TemplateName;
        entity.MessageBody = request.MessageBody;
        entity.IsActive = request.IsActive;
        entity.ChannelType = request.ChannelType.ToString();

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteTemplateAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SmsTemplates.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Template not found.");
        _db.SmsTemplates.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SmsConfigurationDto>> GetConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        var configs = await _db.SmsConfigurations.ToListAsync(cancellationToken);
        return configs.Select(ToDto).ToList();
    }

    public async Task<SmsConfigurationDto> CreateConfigurationAsync(CreateSmsConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new SmsConfigurationEntity
        {
            ChannelType = request.ChannelType.ToString(),
            ProviderName = request.ProviderName,
            ApiKey = request.ApiKey,
            ApiSecret = request.ApiSecret,
            SenderId = request.SenderId,
            HostName = request.HostName,
            PortNumber = request.PortNumber,
            IsActive = request.IsActive,
        };

        _db.SmsConfigurations.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<SmsConfigurationDto> UpdateConfigurationAsync(int id, UpdateSmsConfigurationRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SmsConfigurations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Configuration not found.");

        entity.ChannelType = request.ChannelType.ToString();
        entity.ProviderName = request.ProviderName;
        entity.ApiKey = request.ApiKey == MaskedSecret ? entity.ApiKey : request.ApiKey;
        entity.ApiSecret = request.ApiSecret == MaskedSecret ? entity.ApiSecret : request.ApiSecret;
        entity.SenderId = request.SenderId;
        entity.HostName = request.HostName;
        entity.PortNumber = request.PortNumber;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteConfigurationAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.SmsConfigurations.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Configuration not found.");

        // A template referencing this channel with no other active gateway left would become
        // unsendable, so block removing the last active gateway for a channel still in use.
        var stillHasTemplates = await _db.SmsTemplates.AnyAsync(t => t.ChannelType == entity.ChannelType, cancellationToken);
        if (stillHasTemplates)
        {
            var otherActiveGateway = await _db.SmsConfigurations.AnyAsync(
                c => c.Id != id && c.ChannelType == entity.ChannelType && c.IsActive, cancellationToken);
            if (!otherActiveGateway)
            {
                throw new ApiException(400, $"Cannot remove the only {entity.ChannelType} gateway while {entity.ChannelType} templates exist.");
            }
        }

        _db.SmsConfigurations.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var gateway = await _db.SmsConfigurations.FirstOrDefaultAsync(
            c => c.ChannelType == ChannelType.Email.ToString() && c.IsActive, cancellationToken)
            ?? throw new ApiException(400, "No active Email gateway is configured. Configure one under SMS Gateway first.");

        if (string.IsNullOrWhiteSpace(gateway.HostName) || gateway.PortNumber is null)
        {
            throw new ApiException(400, "The active Email gateway is missing an SMTP host or port.");
        }

        using var client = new SmtpClient(gateway.HostName, gateway.PortNumber.Value)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(gateway.ApiKey, gateway.ApiSecret),
        };

        using var message = new MailMessage
        {
            From = new MailAddress(gateway.ApiKey ?? gateway.SenderId ?? toEmail, gateway.SenderId),
            Subject = subject,
            Body = body,
        };
        message.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (SmtpException ex)
        {
            throw new ApiException(502, $"Could not send email: {ex.Message}");
        }
    }

    /// <summary>Templates can only be created/edited for a channel that has at least one active
    /// gateway configured under SMS Gateway -- otherwise the template could never actually be sent.</summary>
    private async Task EnsureChannelHasActiveGatewayAsync(ChannelType channelType, CancellationToken cancellationToken)
    {
        var hasActiveGateway = await _db.SmsConfigurations.AnyAsync(
            c => c.ChannelType == channelType.ToString() && c.IsActive, cancellationToken);
        if (!hasActiveGateway)
        {
            throw new ApiException(400, $"No active {channelType} gateway is configured. Configure one under SMS Gateway first.");
        }
    }

    private static SmsTemplateDto ToDto(SmsTemplateEntity e) => new(
        e.Id,
        e.TemplateName,
        Enum.TryParse<ChannelType>(e.ChannelType, out var channel) ? channel : ChannelType.SMS,
        e.MessageBody,
        e.IsActive,
        e.CreatedAt);

    private static SmsConfigurationDto ToDto(SmsConfigurationEntity e) => new(
        e.Id,
        Enum.TryParse<ChannelType>(e.ChannelType, out var channel) ? channel : ChannelType.SMS,
        e.ProviderName,
        MaskIfPresent(e.ApiKey),
        MaskIfPresent(e.ApiSecret),
        e.SenderId,
        e.HostName,
        e.PortNumber,
        e.IsActive);

    private static string? MaskIfPresent(string? value) => string.IsNullOrEmpty(value) ? value : MaskedSecret;
}
