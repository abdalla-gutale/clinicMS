using ClinicMS.Web.Models.Api.Rbac;

namespace ClinicMS.Web.Services.Api.Mocks;

public class MockRolesApiClient : IRolesApiClient
{
    public Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RoleDto>>(MockStore.Roles.ToList());

    public Task<RoleWithPermissions> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = MockStore.Roles.FirstOrDefault(r => r.Id == id)
            ?? throw new ApiException(404, "Role not found.");

        var permissions = MockStore.RoleNavPermissions.TryGetValue(id, out var byPage) ? byPage : new Dictionary<int, PermissionItem>();
        var navPermissions = MockStore.NavPages
            .Select(p => permissions.TryGetValue(p.Id, out var perm)
                ? new PermissionDetail(p.Id, p.PageName, perm.CanView, perm.CanCreate, perm.CanEdit, perm.CanDelete)
                : new PermissionDetail(p.Id, p.PageName, false, false, false, false))
            .ToList();

        return Task.FromResult(new RoleWithPermissions(
            role.Id, role.RoleName, role.Description, role.IsActive,
            navPermissions, Array.Empty<ReportPermissionDetail>()));
    }

    public Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var nextId = MockStore.Roles.Count == 0 ? 1 : MockStore.Roles.Max(r => r.Id) + 1;
        var role = new RoleDto(nextId, request.RoleName, request.Description, request.IsActive);
        MockStore.Roles.Add(role);
        MockStore.RoleNavPermissions[nextId] = new Dictionary<int, PermissionItem>();
        return Task.FromResult(role);
    }

    public Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.Roles.FindIndex(r => r.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Role not found.");
        }

        var updated = new RoleDto(id, request.RoleName, request.Description, request.IsActive);
        MockStore.Roles[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = MockStore.Roles.RemoveAll(r => r.Id == id);
        if (removed == 0)
        {
            throw new ApiException(404, "Role not found.");
        }

        MockStore.RoleNavPermissions.Remove(id);
        return Task.CompletedTask;
    }

    public Task SetNavPermissionsAsync(int id, IReadOnlyList<PermissionItem> permissions, CancellationToken cancellationToken = default)
    {
        if (!MockStore.Roles.Any(r => r.Id == id))
        {
            throw new ApiException(404, "Role not found.");
        }

        MockStore.RoleNavPermissions[id] = permissions.ToDictionary(p => p.NavPageId, p => p);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ModuleDto>> GetModulesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ModuleDto>>(MockStore.Modules.OrderBy(m => m.DisplayOrder).ToList());

    public Task<ModuleDto> CreateModuleAsync(CreateModuleRequest request, CancellationToken cancellationToken = default)
    {
        var nextId = MockStore.Modules.Count == 0 ? 1 : MockStore.Modules.Max(m => m.Id) + 1;
        var module = new ModuleDto(nextId, request.ModuleName, request.DisplayOrder, request.IsActive);
        MockStore.Modules.Add(module);
        return Task.FromResult(module);
    }

    public Task<ModuleDto> UpdateModuleAsync(int id, UpdateModuleRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.Modules.FindIndex(m => m.Id == id);
        if (index < 0) throw new ApiException(404, "Module not found.");

        var updated = new ModuleDto(id, request.ModuleName, request.DisplayOrder, request.IsActive);
        MockStore.Modules[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteModuleAsync(int id, CancellationToken cancellationToken = default)
    {
        if (MockStore.NavPages.Any(p => p.ModuleId == id))
        {
            throw new ApiException(400, "Cannot delete a module that still has nav pages assigned to it.");
        }
        if (MockStore.Modules.RemoveAll(m => m.Id == id) == 0)
        {
            throw new ApiException(404, "Module not found.");
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NavPageDto>> GetNavPagesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NavPageDto>>(MockStore.NavPages.OrderBy(p => p.DisplayOrder).ToList());

    public Task<NavPageDto> CreateNavPageAsync(CreateNavPageRequest request, CancellationToken cancellationToken = default)
    {
        if (!MockStore.Modules.Any(m => m.Id == request.ModuleId))
        {
            throw new ApiException(400, "Selected module does not exist.");
        }
        if (request.ParentPageId is int parentId && !MockStore.NavPages.Any(p => p.Id == parentId))
        {
            throw new ApiException(400, "Selected parent page does not exist.");
        }

        var nextId = MockStore.NavPages.Count == 0 ? 1 : MockStore.NavPages.Max(p => p.Id) + 1;
        var page = new NavPageDto(nextId, request.ModuleId, request.PageName, request.PageUrl, request.DisplayOrder, request.IsActive, request.ParentPageId);
        MockStore.NavPages.Add(page);
        return Task.FromResult(page);
    }

    public Task<NavPageDto> UpdateNavPageAsync(int id, UpdateNavPageRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.NavPages.FindIndex(p => p.Id == id);
        if (index < 0) throw new ApiException(404, "Nav page not found.");
        if (!MockStore.Modules.Any(m => m.Id == request.ModuleId))
        {
            throw new ApiException(400, "Selected module does not exist.");
        }
        if (request.ParentPageId == id)
        {
            throw new ApiException(400, "A nav page cannot be its own parent.");
        }
        if (request.ParentPageId is int parentId && !MockStore.NavPages.Any(p => p.Id == parentId))
        {
            throw new ApiException(400, "Selected parent page does not exist.");
        }

        var updated = new NavPageDto(id, request.ModuleId, request.PageName, request.PageUrl, request.DisplayOrder, request.IsActive, request.ParentPageId);
        MockStore.NavPages[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteNavPageAsync(int id, CancellationToken cancellationToken = default)
    {
        if (MockStore.NavPages.Any(p => p.ParentPageId == id))
        {
            throw new ApiException(400, "Cannot delete a nav page that still has sub-pages assigned to it.");
        }
        if (MockStore.NavPages.RemoveAll(p => p.Id == id) == 0)
        {
            throw new ApiException(404, "Nav page not found.");
        }
        foreach (var permissions in MockStore.RoleNavPermissions.Values)
        {
            permissions.Remove(id);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ReportPageDto>> GetReportPagesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReportPageDto>>(MockStore.ReportPages.OrderBy(r => r.DisplayOrder).ToList());

    public Task<ReportPageDto> CreateReportPageAsync(CreateReportPageRequest request, CancellationToken cancellationToken = default)
    {
        var module = MockStore.Modules.FirstOrDefault(m => m.Id == request.ModuleId)
            ?? throw new ApiException(400, "Selected module does not exist.");

        var report = new ReportPageDto(MockStore.NextReportPageId++, module.Id, module.ModuleName, request.ReportName, request.ReportUrl, request.DisplayOrder, request.IsActive);
        MockStore.ReportPages.Add(report);
        return Task.FromResult(report);
    }

    public Task<ReportPageDto> UpdateReportPageAsync(int id, UpdateReportPageRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.ReportPages.FindIndex(r => r.Id == id);
        if (index < 0) throw new ApiException(404, "Report page not found.");

        var module = MockStore.Modules.FirstOrDefault(m => m.Id == request.ModuleId)
            ?? throw new ApiException(400, "Selected module does not exist.");

        var updated = new ReportPageDto(id, module.Id, module.ModuleName, request.ReportName, request.ReportUrl, request.DisplayOrder, request.IsActive);
        MockStore.ReportPages[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteReportPageAsync(int id, CancellationToken cancellationToken = default)
    {
        if (MockStore.ReportPages.RemoveAll(r => r.Id == id) == 0)
        {
            throw new ApiException(404, "Report page not found.");
        }
        return Task.CompletedTask;
    }
}
