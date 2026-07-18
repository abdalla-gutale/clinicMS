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

    Task<IReadOnlyList<NavPageDto>> GetNavPagesAsync(CancellationToken cancellationToken = default);
}
