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

    public Task<ModuleDto> CreateModuleAsync(CreateModuleRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<ModuleDto>("api/modules", request, cancellationToken);

    public Task<ModuleDto> UpdateModuleAsync(int id, UpdateModuleRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<ModuleDto>($"api/modules/{id}", request, cancellationToken);

    public Task DeleteModuleAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/modules/{id}", cancellationToken);

    public Task<IReadOnlyList<NavPageDto>> GetNavPagesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<NavPageDto>>("api/navpages", cancellationToken);

    public Task<NavPageDto> CreateNavPageAsync(CreateNavPageRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<NavPageDto>("api/navpages", request, cancellationToken);

    public Task<NavPageDto> UpdateNavPageAsync(int id, UpdateNavPageRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<NavPageDto>($"api/navpages/{id}", request, cancellationToken);

    public Task DeleteNavPageAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/navpages/{id}", cancellationToken);

    public Task<IReadOnlyList<ReportPageDto>> GetReportPagesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ReportPageDto>>("api/reportpages", cancellationToken);

    public Task<ReportPageDto> CreateReportPageAsync(CreateReportPageRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<ReportPageDto>("api/reportpages", request, cancellationToken);

    public Task<ReportPageDto> UpdateReportPageAsync(int id, UpdateReportPageRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<ReportPageDto>($"api/reportpages/{id}", request, cancellationToken);

    public Task DeleteReportPageAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/reportpages/{id}", cancellationToken);
}
