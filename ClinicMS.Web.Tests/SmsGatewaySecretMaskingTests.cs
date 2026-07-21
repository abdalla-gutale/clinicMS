using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Sms;
using ClinicMS.Web.Services.Api.Db;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicMS.Web.Tests;

public class SmsGatewaySecretMaskingTests
{
    [Fact]
    public async Task GetConfigurations_NeverReturnsTheRealSecretValues()
    {
        var db = TestDb.Create();
        var client = new DbSmsApiClient(db);
        await client.CreateConfigurationAsync(new CreateSmsConfigurationRequest(
            ChannelType.Email, "Gmail SMTP", "info@example.com", "the-real-app-password", "Clinic", "smtp.gmail.com", 587, true), default);

        var listed = Assert.Single(await client.GetConfigurationsAsync(default));

        Assert.NotEqual("info@example.com", listed.ApiKey);
        Assert.NotEqual("the-real-app-password", listed.ApiSecret);
        db.Dispose();
    }

    [Fact]
    public async Task UpdateConfiguration_SubmittingTheMaskedPlaceholderBack_PreservesTheRealSecret()
    {
        var db = TestDb.Create();
        var client = new DbSmsApiClient(db);
        var created = await client.CreateConfigurationAsync(new CreateSmsConfigurationRequest(
            ChannelType.Email, "Gmail SMTP", "info@example.com", "the-real-app-password", "Clinic", "smtp.gmail.com", 587, true), default);

        var maskedFromList = (await client.GetConfigurationsAsync(default)).Single();

        // Simulates the browser: the edit form was pre-filled with the masked value and the admin
        // only changed the provider name, never touching the secret fields.
        await client.UpdateConfigurationAsync(created.Id, new UpdateSmsConfigurationRequest(
            ChannelType.Email, "Gmail SMTP (renamed)", maskedFromList.ApiKey, maskedFromList.ApiSecret, "Clinic", "smtp.gmail.com", 587, true), default);

        var raw = await db.SmsConfigurations.SingleAsync(c => c.Id == created.Id);
        Assert.Equal("info@example.com", raw.ApiKey);
        Assert.Equal("the-real-app-password", raw.ApiSecret);
        db.Dispose();
    }

    [Fact]
    public async Task UpdateConfiguration_SubmittingARealNewSecret_OverwritesTheStoredOne()
    {
        var db = TestDb.Create();
        var client = new DbSmsApiClient(db);
        var created = await client.CreateConfigurationAsync(new CreateSmsConfigurationRequest(
            ChannelType.Email, "Gmail SMTP", "info@example.com", "old-password", "Clinic", "smtp.gmail.com", 587, true), default);

        await client.UpdateConfigurationAsync(created.Id, new UpdateSmsConfigurationRequest(
            ChannelType.Email, "Gmail SMTP", "info@example.com", "brand-new-password", "Clinic", "smtp.gmail.com", 587, true), default);

        var raw = await db.SmsConfigurations.SingleAsync(c => c.Id == created.Id);
        Assert.Equal("brand-new-password", raw.ApiSecret);
        db.Dispose();
    }
}
