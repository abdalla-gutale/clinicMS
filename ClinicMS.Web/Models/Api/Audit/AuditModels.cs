namespace ClinicMS.Web.Models.Api.Audit;

public record AuditTrailDto(
    long Id,
    int? UserId,
    string TableName,
    int RecordId,
    string Action,
    string? OldData,
    string? NewData,
    string? IpAddress,
    DateTime CreatedAt);

public record UserLogDto(
    int Id,
    int UserId,
    string Username,
    string Action,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAt);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

/// <summary>A single row of the unified Activity Log feed -- audit trail entries (create/update/
/// delete) and login/logout events merged into one chronological stream. Id is a composite string
/// ("audit-123"/"login-45") since the two source tables have independent, colliding numeric ids.</summary>
public record ActivityFeedEntryDto(string Id, string UserName, string Action, string Message, string? IpAddress, DateTime CreatedAt);

public record TopActiveUserDto(string UserName, int Count);

/// <summary>Header stats (KPI counts, 12-week sparklines, top-5 users) computed once over the
/// entire feed -- unaffected by the current page or search, so the client doesn't need the full
/// dataset loaded just to render them.</summary>
public record ActivityStatsDto(
    int TotalCount, int CreateCount, int UpdateCount, int DeleteCount, int LoginCount,
    IReadOnlyList<int> TotalSparkline, IReadOnlyList<int> CreateSparkline, IReadOnlyList<int> UpdateSparkline, IReadOnlyList<int> DeleteSparkline,
    IReadOnlyList<TopActiveUserDto> TopUsers);

public record ActivityPageDto(PagedResult<ActivityFeedEntryDto> Feed, ActivityStatsDto Stats);
