namespace ClinicMS.Web.Models.Api.Rbac;

public record RoleDto(int Id, string RoleName, string? Description, bool IsActive);

public record CreateRoleRequest(string RoleName, string? Description, bool IsActive);

public record UpdateRoleRequest(string RoleName, string? Description, bool IsActive);

public record PermissionItem(int NavPageId, bool CanView, bool CanCreate, bool CanEdit, bool CanDelete);

public record PermissionDetail(int NavPageId, string PageName, bool CanView, bool CanCreate, bool CanEdit, bool CanDelete);

public record ReportPermissionItem(int ReportPageId, bool CanAccess);

public record ReportPermissionDetail(int ReportPageId, string ReportName, bool CanAccess);

public record RoleWithPermissions(
    int Id,
    string RoleName,
    string? Description,
    bool IsActive,
    IReadOnlyList<PermissionDetail> NavPermissions,
    IReadOnlyList<ReportPermissionDetail> ReportPermissions);

public record ModuleDto(int Id, string ModuleName, int DisplayOrder, bool IsActive);

public record NavPageDto(int Id, int ModuleId, string PageName, string PageUrl, int DisplayOrder, bool IsActive, int? ParentPageId);
