using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Rbac;
using ClinicMS.Web.Services.Api;
using ClinicMS.Web.Services.Api.Db;
using Xunit;

namespace ClinicMS.Web.Tests;

public class ModuleIconValidationTests
{
    private static DbRolesApiClient NewClient() => new(TestDb.Create());

    [Fact]
    public async Task CreateModule_WithoutIcon_Throws()
    {
        var client = NewClient();

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            client.CreateModuleAsync(new CreateModuleRequest("Registrations", "", 1, true), default));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateModule_WhitespaceOnlyIcon_Throws()
    {
        var client = NewClient();

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            client.CreateModuleAsync(new CreateModuleRequest("Registrations", "   ", 1, true), default));

        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [InlineData("ri-folder\"line")]
    [InlineData("ri-folder<line")]
    [InlineData("ri-folder&line")]
    public async Task CreateModule_IconWithUnsafeCharacters_Throws(string unsafeIcon)
    {
        var client = NewClient();

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            client.CreateModuleAsync(new CreateModuleRequest("Registrations", unsafeIcon, 1, true), default));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateModule_WithValidIcon_Succeeds()
    {
        var client = NewClient();

        var module = await client.CreateModuleAsync(new CreateModuleRequest("Registrations", "ri-file-list-3-line", 1, true), default);

        Assert.Equal("ri-file-list-3-line", module.ModuleIcon);
    }

    [Fact]
    public async Task UpdateModule_ClearingIcon_Throws()
    {
        var client = NewClient();
        var module = await client.CreateModuleAsync(new CreateModuleRequest("Registrations", "ri-file-list-3-line", 1, true), default);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            client.UpdateModuleAsync(module.Id, new UpdateModuleRequest("Registrations", "", 1, true), default));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateModule_WithValidIcon_UpdatesIt()
    {
        var client = NewClient();
        var module = await client.CreateModuleAsync(new CreateModuleRequest("Registrations", "ri-file-list-3-line", 1, true), default);

        var updated = await client.UpdateModuleAsync(module.Id, new UpdateModuleRequest("Registrations", "ri-user-line", 1, true), default);

        Assert.Equal("ri-user-line", updated.ModuleIcon);
    }

    [Fact]
    public async Task GetModules_ReturnsTheStoredIcon()
    {
        var client = NewClient();
        await client.CreateModuleAsync(new CreateModuleRequest("Finance", "ri-bank-card-line", 1, true), default);

        var modules = await client.GetModulesAsync(default);

        Assert.Equal("ri-bank-card-line", Assert.Single(modules).ModuleIcon);
    }
}
