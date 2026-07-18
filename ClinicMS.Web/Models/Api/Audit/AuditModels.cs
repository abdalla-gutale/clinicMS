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
