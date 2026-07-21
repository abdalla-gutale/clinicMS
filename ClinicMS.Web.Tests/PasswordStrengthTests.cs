using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Users;
using ClinicMS.Web.Services.Api;
using ClinicMS.Web.Services.Api.Db;
using Xunit;

namespace ClinicMS.Web.Tests;

public class PasswordStrengthTests
{
    private static async Task<(ClinicMsDbContext Db, DbUsersApiClient Client, int RoleId)> SeedAsync()
    {
        var db = TestDb.Create();
        var role = new RoleEntity { RoleName = "Receptionist", IsActive = true };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return (db, new DbUsersApiClient(db), role.Id);
    }

    [Theory]
    [InlineData("short1")]       // under 8 characters
    [InlineData("alletters")]    // long enough but no digit
    [InlineData("12345678")]     // long enough but no letter
    public async Task CreateUser_WeakPassword_Throws(string weakPassword)
    {
        var (db, client, roleId) = await SeedAsync();

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.CreateAsync(
            new CreateUserRequest(roleId, "jdoe", weakPassword, "Jane Doe", "jane@example.com", null), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task CreateUser_PasswordWithLetterAndDigitAndEightChars_Succeeds()
    {
        var (db, client, roleId) = await SeedAsync();

        var user = await client.CreateAsync(
            new CreateUserRequest(roleId, "jdoe", "Passw0rd", "Jane Doe", "jane@example.com", null), default);

        Assert.Equal("jdoe", user.Username);
        db.Dispose();
    }

    [Fact]
    public async Task ChangePassword_WeakPassword_Throws()
    {
        var (db, client, roleId) = await SeedAsync();
        var user = await client.CreateAsync(
            new CreateUserRequest(roleId, "jdoe", "Passw0rd", "Jane Doe", "jane@example.com", null), default);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.ChangePasswordAsync(user.Id, "weak", default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task ChangePassword_StrongPassword_Succeeds()
    {
        var (db, client, roleId) = await SeedAsync();
        var user = await client.CreateAsync(
            new CreateUserRequest(roleId, "jdoe", "Passw0rd", "Jane Doe", "jane@example.com", null), default);

        await client.ChangePasswordAsync(user.Id, "NewPass1", default);

        db.Dispose();
    }
}
