using ClinicMS.Web.Data;
using ClinicMS.Web.Services.Api.Db;
using Xunit;

namespace ClinicMS.Web.Tests;

// ClinicMsDbContext.SaveChangesAsync auto-logs an AuditTrail row for every entity insert/update/
// delete (except AuditTrailEntity/UserLogEntity themselves) -- so seeding a Role/User in these
// tests also produces real "System"-attributed audit rows of its own. Every assertion here is
// therefore written against the DELTA the test itself introduces, not an absolute count, since an
// absolute-zero baseline is never actually available once any other entity has been seeded.
public class ActivityFeedTests
{
    private static async Task<(ClinicMsDbContext Db, DbAuditApiClient Client, int UserId)> SeedAsync()
    {
        var db = TestDb.Create();
        var role = new RoleEntity { RoleName = "Admin", IsActive = true };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        var user = new UserEntity { RoleId = role.Id, Username = "jdoe", PasswordHash = "x", FullName = "Jane Doe", Email = "jane@example.com", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (db, new DbAuditApiClient(db), user.Id);
    }

    [Fact]
    public async Task GetActivityFeed_MergesAuditTrailAndUserLogsIntoOneChronologicalFeed()
    {
        var (db, client, userId) = await SeedAsync();
        var baseline = await client.GetActivityFeedAsync(1, 10, null, null, default);

        // Seeding's own auto-audit rows are timestamped at real insert time, which is strictly
        // before this point -- so both explicit rows below (anchored to "now") sort after them,
        // and the 1-minute gap between the two orders them correctly relative to each other.
        var now = DateTime.UtcNow;
        db.AuditTrail.Add(new AuditTrailEntity { UserId = userId, TableName = "patients", RecordId = 5, Action = "Create", CreatedAt = now });
        db.UserLogs.Add(new UserLogEntity { UserId = userId, Action = "Login_Success", CreatedAt = now.AddMinutes(1) });
        await db.SaveChangesAsync();

        var result = await client.GetActivityFeedAsync(1, 10, null, null, default);

        Assert.Equal(baseline.Feed.TotalCount + 2, result.Feed.TotalCount);
        // Most recent first.
        Assert.Equal("LOGIN", result.Feed.Items[0].Action);
        Assert.Equal("CREATE", result.Feed.Items[1].Action);
        db.Dispose();
    }

    [Fact]
    public async Task GetActivityFeed_BuildsHumanReadableMessagesForEachSourceType()
    {
        var (db, client, userId) = await SeedAsync();
        db.AuditTrail.Add(new AuditTrailEntity { UserId = userId, TableName = "invoices", RecordId = 42, Action = "Update", CreatedAt = DateTime.UtcNow });
        db.UserLogs.Add(new UserLogEntity { UserId = userId, Action = "Login_Failed", CreatedAt = DateTime.UtcNow.AddSeconds(-1) });
        await db.SaveChangesAsync();

        var result = await client.GetActivityFeedAsync(1, 10, null, null, default);

        Assert.Contains(result.Feed.Items, i => i.Message == "Updated invoices #42");
        Assert.Contains(result.Feed.Items, i => i.Message == "Failed login attempt");
        db.Dispose();
    }

    [Fact]
    public async Task GetActivityFeed_UnattributedAuditRowsFallBackToSystemAsUserName()
    {
        var db = TestDb.Create();
        db.AuditTrail.Add(new AuditTrailEntity { UserId = null, TableName = "patients", RecordId = 1, Action = "Delete", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var client = new DbAuditApiClient(db);
        var result = await client.GetActivityFeedAsync(1, 10, null, null, default);

        Assert.Equal("System", Assert.Single(result.Feed.Items).UserName);
        db.Dispose();
    }

    [Fact]
    public async Task GetActivityFeed_ActionFilterNarrowsToOneSourceType()
    {
        var (db, client, userId) = await SeedAsync();
        db.AuditTrail.Add(new AuditTrailEntity { UserId = userId, TableName = "patients", RecordId = 1, Action = "Create", CreatedAt = DateTime.UtcNow });
        db.AuditTrail.Add(new AuditTrailEntity { UserId = userId, TableName = "patients", RecordId = 2, Action = "Delete", CreatedAt = DateTime.UtcNow });
        db.UserLogs.Add(new UserLogEntity { UserId = userId, Action = "Login_Success", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await client.GetActivityFeedAsync(1, 10, null, "login", default);

        var only = Assert.Single(result.Feed.Items);
        Assert.Equal("LOGIN", only.Action);
        db.Dispose();
    }

    [Fact]
    public async Task GetActivityFeed_SearchMatchesUsernameOrTableNameOrIp()
    {
        var (db, client, userId) = await SeedAsync();
        db.AuditTrail.Add(new AuditTrailEntity { UserId = userId, TableName = "invoices", RecordId = 1, Action = "Create", IpAddress = "10.0.0.5", CreatedAt = DateTime.UtcNow });
        db.AuditTrail.Add(new AuditTrailEntity { UserId = userId, TableName = "patients", RecordId = 2, Action = "Create", IpAddress = "10.0.0.6", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await client.GetActivityFeedAsync(1, 10, "invoices", null, default);

        var only = Assert.Single(result.Feed.Items);
        Assert.Equal("Created invoices #1", only.Message);
        db.Dispose();
    }

    [Fact]
    public async Task GetActivityFeed_StatsCountEachActionTypeCorrectly()
    {
        var (db, client, userId) = await SeedAsync();
        var baseline = await client.GetActivityFeedAsync(1, 10, null, null, default);

        db.AuditTrail.Add(new AuditTrailEntity { UserId = userId, TableName = "a", RecordId = 1, Action = "Create", CreatedAt = DateTime.UtcNow });
        db.AuditTrail.Add(new AuditTrailEntity { UserId = userId, TableName = "a", RecordId = 2, Action = "Create", CreatedAt = DateTime.UtcNow });
        db.AuditTrail.Add(new AuditTrailEntity { UserId = userId, TableName = "a", RecordId = 3, Action = "Update", CreatedAt = DateTime.UtcNow });
        db.AuditTrail.Add(new AuditTrailEntity { UserId = userId, TableName = "a", RecordId = 4, Action = "Delete", CreatedAt = DateTime.UtcNow });
        db.UserLogs.Add(new UserLogEntity { UserId = userId, Action = "Login_Success", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await client.GetActivityFeedAsync(1, 10, null, null, default);

        Assert.Equal(baseline.Stats.CreateCount + 2, result.Stats.CreateCount);
        Assert.Equal(baseline.Stats.UpdateCount + 1, result.Stats.UpdateCount);
        Assert.Equal(baseline.Stats.DeleteCount + 1, result.Stats.DeleteCount);
        Assert.Equal(baseline.Stats.LoginCount + 1, result.Stats.LoginCount);
        Assert.Equal(baseline.Stats.TotalCount + 5, result.Stats.TotalCount);
        db.Dispose();
    }

    [Fact]
    public async Task GetActivityFeed_SparklineBucketsRecentActivityIntoThisWeek()
    {
        var (db, client, userId) = await SeedAsync();
        var baseline = await client.GetActivityFeedAsync(1, 10, null, null, default);

        db.AuditTrail.Add(new AuditTrailEntity { UserId = userId, TableName = "a", RecordId = 1, Action = "Create", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await client.GetActivityFeedAsync(1, 10, null, null, default);

        Assert.Equal(baseline.Stats.TotalSparkline[^1] + 1, result.Stats.TotalSparkline[^1]);
        Assert.Equal(baseline.Stats.CreateSparkline[^1] + 1, result.Stats.CreateSparkline[^1]);
        db.Dispose();
    }

    [Fact]
    public async Task GetActivityFeed_TopUsersRanksByActivityCountDescending()
    {
        var db = TestDb.Create();
        var role = new RoleEntity { RoleName = "Admin", IsActive = true };
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        var active = new UserEntity { RoleId = role.Id, Username = "busy", PasswordHash = "x", FullName = "Busy User", Email = "busy@example.com", IsActive = true, CreatedAt = DateTime.UtcNow };
        var quiet = new UserEntity { RoleId = role.Id, Username = "quiet", PasswordHash = "x", FullName = "Quiet User", Email = "quiet@example.com", IsActive = true, CreatedAt = DateTime.UtcNow };
        db.Users.AddRange(active, quiet);
        await db.SaveChangesAsync();

        db.AuditTrail.Add(new AuditTrailEntity { UserId = active.Id, TableName = "a", RecordId = 1, Action = "Create", CreatedAt = DateTime.UtcNow });
        db.AuditTrail.Add(new AuditTrailEntity { UserId = active.Id, TableName = "a", RecordId = 2, Action = "Update", CreatedAt = DateTime.UtcNow });
        db.AuditTrail.Add(new AuditTrailEntity { UserId = quiet.Id, TableName = "a", RecordId = 3, Action = "Create", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var client = new DbAuditApiClient(db);
        var result = await client.GetActivityFeedAsync(1, 10, null, null, default);

        var busyCount = result.Stats.TopUsers.Single(u => u.UserName == "busy").Count;
        var quietCount = result.Stats.TopUsers.Single(u => u.UserName == "quiet").Count;
        Assert.Equal(2, busyCount);
        Assert.Equal(1, quietCount);
        Assert.True(busyCount > quietCount);
        db.Dispose();
    }

    [Fact]
    public async Task GetActivityFeed_PagesAcrossTheMergedFeedCorrectly()
    {
        var (db, client, userId) = await SeedAsync();
        var baseline = await client.GetActivityFeedAsync(1, 10, null, null, default);

        for (var i = 1; i <= 15; i++)
        {
            db.AuditTrail.Add(new AuditTrailEntity { UserId = userId, TableName = "a", RecordId = i, Action = "Create", CreatedAt = DateTime.UtcNow.AddMinutes(-i) });
        }
        await db.SaveChangesAsync();

        var expectedTotal = baseline.Feed.TotalCount + 15;
        var expectedPage2Count = Math.Min(10, Math.Max(0, expectedTotal - 10));

        var page1 = await client.GetActivityFeedAsync(1, 10, null, null, default);
        var page2 = await client.GetActivityFeedAsync(2, 10, null, null, default);

        Assert.Equal(10, page1.Feed.Items.Count);
        Assert.Equal(expectedTotal, page1.Feed.TotalCount);
        Assert.Equal(expectedPage2Count, page2.Feed.Items.Count);

        var seenIds = page1.Feed.Items.Select(i => i.Id).Concat(page2.Feed.Items.Select(i => i.Id)).ToHashSet();
        Assert.Equal(page1.Feed.Items.Count + page2.Feed.Items.Count, seenIds.Count); // no duplicates across pages
        db.Dispose();
    }
}
