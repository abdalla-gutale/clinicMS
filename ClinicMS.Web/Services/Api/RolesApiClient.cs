using ClinicMS.Web.Models.Api.Rbac;

namespace ClinicMS.Web.Services.Api;

public class RolesApiClient : ApiClientBase, IRolesApiClient
{
    public RolesApiClient(HttpClient http) : base(http)
    {
    }

    public Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<RoleDto>>("api/roles", cancellationToken);

    public Task<RoleWithPermissions> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        GetAsync<RoleWithPermissions>($"api/roles/{id}", cancellationToken);

    public Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<RoleDto>("api/roles", request, cancellationToken);

    public Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<RoleDto>($"api/roles/{id}", request, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/roles/{id}", cancellationToken);

    public Task SetNavPermissionsAsync(int id, IReadOnlyList<PermissionItem> permissions, CancellationToken cancellationToken = default) =>
        PutAsync($"api/roles/{id}/nav-permissions", permissions, cancellationToken);

    public Task<IReadOnlyList<ModuleDto>> GetModulesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ModuleDto>>("api/modules", cancellationToken);

    public Task<IReadOnlyList<NavPageDto>> GetNavPagesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<NavPageDto>>("api/navpages", cancellationToken);
}
