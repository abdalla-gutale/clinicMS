using ClinicMS.Web.Models.Api.Rbac;

namespace ClinicMS.Web.Services.Api;

public interface IRolesApiClient
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<RoleWithPermissions> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task SetNavPermissionsAsync(int id, IReadOnlyList<PermissionItem> permissions, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModuleDto>> GetModulesAsync(CancellationToken cancellationToken = default);

    Task<ModuleDto> CreateModuleAsync(CreateModuleRequest request, CancellationToken cancellationToken = default);

    Task<ModuleDto> UpdateModuleAsync(int id, UpdateModuleRequest request, CancellationToken cancellationToken = default);

    Task DeleteModuleAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NavPageDto>> GetNavPagesAsync(CancellationToken cancellationToken = default);

    Task<NavPageDto> CreateNavPageAsync(CreateNavPageRequest request, CancellationToken cancellationToken = default);

    Task<NavPageDto> UpdateNavPageAsync(int id, UpdateNavPageRequest request, CancellationToken cancellationToken = default);

    Task DeleteNavPageAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportPageDto>> GetReportPagesAsync(CancellationToken cancellationToken = default);

    Task<ReportPageDto> CreateReportPageAsync(CreateReportPageRequest request, CancellationToken cancellationToken = default);

    Task<ReportPageDto> UpdateReportPageAsync(int id, UpdateReportPageRequest request, CancellationToken cancellationToken = default);

    Task DeleteReportPageAsync(int id, CancellationToken cancellationToken = default);
}
