namespace ClinicMS.Web.Models.Api;

/// <summary>Generic server-side paging envelope. Page is 1-based; TotalPages is derived rather
/// than stored so callers can't construct one with an inconsistent value.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
