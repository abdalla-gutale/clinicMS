using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Audit;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Services.Api.Db;

public class DbAuditApiClient : IAuditApiClient
{
    private readonly ClinicMsDbContext _db;

    public DbAuditApiClient(ClinicMsDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditTrailDto>> GetTrailAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        var totalCount = await _db.AuditTrail.CountAsync(cancellationToken);
        var entries = await _db.AuditTrail
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entries.Select(a => new AuditTrailDto(a.Id, a.UserId, a.TableName, a.RecordId, a.Action, a.OldData, a.NewData, a.IpAddress, a.CreatedAt)).ToList();
        return new PagedResult<AuditTrailDto>(items, page, pageSize, totalCount);
    }

    public async Task<PagedResult<UserLogDto>> GetUserLogsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        var totalCount = await _db.UserLogs.CountAsync(cancellationToken);
        var entries = await _db.UserLogs
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = entries.Select(l => l.UserId).Distinct().ToList();
        var usernames = await _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

        var items = entries.Select(l => new UserLogDto(l.Id, l.UserId, usernames.GetValueOrDefault(l.UserId, ""), l.Action, l.IpAddress, l.UserAgent, l.CreatedAt)).ToList();
        return new PagedResult<UserLogDto>(items, page, pageSize, totalCount);
    }

    public async Task LogUserActionAsync(int userId, string action, string? ipAddress, string? userAgent, CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || !await _db.Users.AnyAsync(u => u.Id == userId, cancellationToken))
        {
            return;
        }

        _db.UserLogs.Add(new UserLogEntity
        {
            UserId = userId,
            Action = action,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            CreatedAt = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static readonly Dictionary<string, string> UserLogMessages = new()
    {
        ["Login_Success"] = "Signed in successfully",
        ["Login_Failed"] = "Failed login attempt",
        ["Logout"] = "Logged out",
        ["Session_Expired"] = "Session expired",
    };

    private sealed record ActivityRawRow(long RawId, string SourceType, string UserName, string DisplayAction, string RawAction, string? TableName, int? RecordId, string? IpAddress, DateTime CreatedAt);

    public async Task<ActivityPageDto> GetActivityFeedAsync(int page, int pageSize, string? search, string? actionFilter, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 1000); // upper bound also covers the feed's CSV export request

        var normalizedFilter = string.IsNullOrWhiteSpace(actionFilter) || string.Equals(actionFilter, "all", StringComparison.OrdinalIgnoreCase)
            ? null
            : actionFilter.Trim().ToUpper();
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLower();

        // Audit trail rows can only ever be CREATE/UPDATE/DELETE and login rows are always LOGIN,
        // so filtering by one of those skips querying the other source table entirely.
        var includeAudit = normalizedFilter is null or "CREATE" or "UPDATE" or "DELETE";
        var includeLogin = normalizedFilter is null or "LOGIN";

        // Login rows are always DisplayAction "LOGIN" so a specific create/update/delete filter
        // never needs to be re-applied there; audit rows span all three, so that filter still needs
        // to narrow within them even after already having picked the right source table above.
        var auditQuery = includeAudit ? ApplySearch(BuildAuditRowQuery(), term) : null;
        if (auditQuery is not null && normalizedFilter is not null)
        {
            auditQuery = auditQuery.Where(r => r.DisplayAction == normalizedFilter);
        }
        var loginQuery = includeLogin ? ApplySearch(BuildLoginRowQuery(), term) : null;

        var totalCount = (auditQuery is null ? 0 : await auditQuery.CountAsync(cancellationToken))
            + (loginQuery is null ? 0 : await loginQuery.CountAsync(cancellationToken));

        // Bounded fan-in merge: each source is asked for at most as many rows as this page could
        // possibly need (never the whole table), then the two small, already-sorted windows are
        // merged and sliced in memory. This also sidesteps EF Core's InMemory provider not
        // supporting further query composition after a Queryable.Concat set operation -- a real
        // SQL Server UNION ALL would allow that, but this approach works identically on both and
        // still never loads the full audit trail regardless of how large it grows.
        var windowSize = page * pageSize;
        var auditWindow = auditQuery is null
            ? new List<ActivityRawRow>()
            : await auditQuery.OrderByDescending(r => r.CreatedAt).Take(windowSize).ToListAsync(cancellationToken);
        var loginWindow = loginQuery is null
            ? new List<ActivityRawRow>()
            : await loginQuery.OrderByDescending(r => r.CreatedAt).Take(windowSize).ToListAsync(cancellationToken);

        var pageRows = auditWindow.Concat(loginWindow)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = pageRows.Select(ToFeedEntry).ToList();
        var stats = await BuildStatsAsync(cancellationToken);

        return new ActivityPageDto(new PagedResult<ActivityFeedEntryDto>(items, page, pageSize, totalCount), stats);
    }

    private IQueryable<ActivityRawRow> BuildAuditRowQuery() =>
        from a in _db.AuditTrail
        join u in _db.Users on a.UserId equals u.Id into userJoin
        from user in userJoin.DefaultIfEmpty()
        select new ActivityRawRow(
            a.Id,
            "Audit",
            user != null ? user.Username : "System",
            a.Action.ToUpper(),
            a.Action,
            a.TableName,
            (int?)a.RecordId,
            a.IpAddress,
            a.CreatedAt);

    private IQueryable<ActivityRawRow> BuildLoginRowQuery() =>
        from l in _db.UserLogs
        join u in _db.Users on l.UserId equals u.Id into userJoin
        from user in userJoin.DefaultIfEmpty()
        select new ActivityRawRow(
            l.Id,
            "Login",
            user != null ? user.Username : "System",
            "LOGIN",
            l.Action,
            null,
            null,
            l.IpAddress,
            l.CreatedAt);

    private static IQueryable<ActivityRawRow> ApplySearch(IQueryable<ActivityRawRow> query, string? term) =>
        term is null
            ? query
            : query.Where(r =>
                r.UserName.ToLower().Contains(term) ||
                r.DisplayAction.ToLower().Contains(term) ||
                (r.TableName != null && r.TableName.ToLower().Contains(term)) ||
                (r.IpAddress != null && r.IpAddress.ToLower().Contains(term)));

    private static ActivityFeedEntryDto ToFeedEntry(ActivityRawRow row)
    {
        var id = $"{(row.SourceType == "Audit" ? "audit" : "login")}-{row.RawId}";
        var message = row.SourceType == "Audit"
            ? $"{(row.DisplayAction == "CREATE" ? "Created" : row.DisplayAction == "UPDATE" ? "Updated" : "Deleted")} {row.TableName} #{row.RecordId}"
            : UserLogMessages.GetValueOrDefault(row.RawAction, row.RawAction);

        return new ActivityFeedEntryDto(id, row.UserName, row.DisplayAction, message, row.IpAddress, row.CreatedAt);
    }

    // KPIs, the 12-week sparklines, and the top-5 users are computed over the WHOLE feed regardless
    // of the current page/search -- but only CreatedAt/Action/UserId are pulled per row (never the
    // audit trail's OldData/NewData JSON blobs), so this stays cheap even as the tables grow.
    private async Task<ActivityStatsDto> BuildStatsAsync(CancellationToken cancellationToken)
    {
        var auditLite = await _db.AuditTrail.Select(a => new { a.Action, a.CreatedAt, a.UserId }).ToListAsync(cancellationToken);
        var loginLite = await _db.UserLogs.Select(l => new { l.UserId, l.CreatedAt }).ToListAsync(cancellationToken);

        var userIds = auditLite.Where(a => a.UserId.HasValue).Select(a => a.UserId!.Value)
            .Concat(loginLite.Select(l => l.UserId))
            .Distinct().ToList();
        var usernames = await _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

        var createCount = auditLite.Count(a => a.Action == "Create");
        var updateCount = auditLite.Count(a => a.Action == "Update");
        var deleteCount = auditLite.Count(a => a.Action == "Delete");
        var loginCount = loginLite.Count;
        var totalCount = auditLite.Count + loginLite.Count;

        const int weeks = 12;
        var now = DateTime.UtcNow;
        int[] BuildSparkline(IEnumerable<DateTime> dates)
        {
            var buckets = new int[weeks];
            foreach (var d in dates)
            {
                var idx = (int)((now - d).TotalDays / 7);
                if (idx >= 0 && idx < weeks) buckets[weeks - 1 - idx]++;
            }
            return buckets;
        }

        var totalSparkline = BuildSparkline(auditLite.Select(a => a.CreatedAt).Concat(loginLite.Select(l => l.CreatedAt)));
        var createSparkline = BuildSparkline(auditLite.Where(a => a.Action == "Create").Select(a => a.CreatedAt));
        var updateSparkline = BuildSparkline(auditLite.Where(a => a.Action == "Update").Select(a => a.CreatedAt));
        var deleteSparkline = BuildSparkline(auditLite.Where(a => a.Action == "Delete").Select(a => a.CreatedAt));

        var topUsers = auditLite.Select(a => a.UserId is int uid ? usernames.GetValueOrDefault(uid, "System") : "System")
            .Concat(loginLite.Select(l => usernames.GetValueOrDefault(l.UserId, "System")))
            .GroupBy(name => name)
            .Select(g => new TopActiveUserDto(g.Key, g.Count()))
            .OrderByDescending(t => t.Count)
            .Take(5)
            .ToList();

        return new ActivityStatsDto(totalCount, createCount, updateCount, deleteCount, loginCount, totalSparkline, createSparkline, updateSparkline, deleteSparkline, topUsers);
    }
}
