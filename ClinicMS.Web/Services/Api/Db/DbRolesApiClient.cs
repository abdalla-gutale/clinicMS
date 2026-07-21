using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Rbac;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Services.Api.Db;

public class DbRolesApiClient : IRolesApiClient
{
    private readonly ClinicMsDbContext _db;

    public DbRolesApiClient(ClinicMsDbContext db)
    {
        _db = db;
    }

    // ----- Roles -----

    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _db.Roles.OrderBy(r => r.Id).ToListAsync(cancellationToken);
        return roles.Select(ToDto).ToList();
    }

    public async Task<RoleWithPermissions> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Role not found.");

        var navPages = await _db.NavPages.OrderBy(p => p.DisplayOrder).ToListAsync(cancellationToken);
        var permissions = await _db.Permissions.Where(p => p.RoleId == id).ToDictionaryAsync(p => p.NavPageId, cancellationToken);
        var navPermissions = navPages.Select(p => permissions.TryGetValue(p.Id, out var perm)
            ? new PermissionDetail(p.Id, p.PageName, perm.CanView, perm.CanCreate, perm.CanEdit, perm.CanDelete)
            : new PermissionDetail(p.Id, p.PageName, false, false, false, false)).ToList();

        var reportPages = await _db.ReportPages.OrderBy(r => r.DisplayOrder).ToListAsync(cancellationToken);
        var reportPermissions = await _db.ReportPermissions.Where(p => p.RoleId == id).ToDictionaryAsync(p => p.ReportPageId, cancellationToken);
        var reportPerms = reportPages.Select(r => reportPermissions.TryGetValue(r.Id, out var perm)
            ? new ReportPermissionDetail(r.Id, r.ReportName, perm.CanAccess)
            : new ReportPermissionDetail(r.Id, r.ReportName, false)).ToList();

        return new RoleWithPermissions(role.Id, role.RoleName, role.Description, role.IsActive, navPermissions, reportPerms);
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new RoleEntity { RoleName = request.RoleName, Description = request.Description, IsActive = request.IsActive };
        _db.Roles.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<RoleDto> UpdateAsync(int id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Role not found.");

        entity.RoleName = request.RoleName;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.Users.AnyAsync(u => u.RoleId == id, cancellationToken))
        {
            throw new ApiException(400, "Cannot delete a role that still has users assigned to it.");
        }

        var entity = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Role not found.");

        var permissions = await _db.Permissions.Where(p => p.RoleId == id).ToListAsync(cancellationToken);
        _db.Permissions.RemoveRange(permissions);
        var reportPermissions = await _db.ReportPermissions.Where(p => p.RoleId == id).ToListAsync(cancellationToken);
        _db.ReportPermissions.RemoveRange(reportPermissions);

        _db.Roles.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetNavPermissionsAsync(int id, IReadOnlyList<PermissionItem> permissions, CancellationToken cancellationToken = default)
    {
        if (!await _db.Roles.AnyAsync(r => r.Id == id, cancellationToken))
        {
            throw new ApiException(404, "Role not found.");
        }

        var existing = await _db.Permissions.Where(p => p.RoleId == id).ToListAsync(cancellationToken);
        _db.Permissions.RemoveRange(existing);

        foreach (var item in permissions)
        {
            _db.Permissions.Add(new PermissionEntity
            {
                RoleId = id,
                NavPageId = item.NavPageId,
                CanView = item.CanView,
                CanCreate = item.CanCreate,
                CanEdit = item.CanEdit,
                CanDelete = item.CanDelete,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Modules -----

    public async Task<IReadOnlyList<ModuleDto>> GetModulesAsync(CancellationToken cancellationToken = default)
    {
        var modules = await _db.Modules.OrderBy(m => m.DisplayOrder).ToListAsync(cancellationToken);
        return modules.Select(m => new ModuleDto(m.Id, m.ModuleName, m.ModuleIcon, m.DisplayOrder, m.IsActive)).ToList();
    }

    public async Task<ModuleDto> CreateModuleAsync(CreateModuleRequest request, CancellationToken cancellationToken = default)
    {
        var icon = ValidateModuleIcon(request.ModuleIcon);
        var entity = new ModuleEntity { ModuleName = request.ModuleName, ModuleIcon = icon, DisplayOrder = request.DisplayOrder, IsActive = request.IsActive };
        _db.Modules.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new ModuleDto(entity.Id, entity.ModuleName, entity.ModuleIcon, entity.DisplayOrder, entity.IsActive);
    }

    public async Task<ModuleDto> UpdateModuleAsync(int id, UpdateModuleRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Modules.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Module not found.");

        entity.ModuleName = request.ModuleName;
        entity.ModuleIcon = ValidateModuleIcon(request.ModuleIcon);
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return new ModuleDto(entity.Id, entity.ModuleName, entity.ModuleIcon, entity.DisplayOrder, entity.IsActive);
    }

    // The icon is rendered straight into an HTML class attribute in the sidebar, so beyond just
    // requiring it, characters that could break out of that attribute are rejected outright rather
    // than silently stripped -- a RemixIcon class name (e.g. "ri-folder-3-line") never needs them.
    private static string ValidateModuleIcon(string? moduleIcon)
    {
        var icon = moduleIcon?.Trim() ?? "";
        if (icon.Length == 0)
        {
            throw new ApiException(400, "Module icon is required.");
        }
        if (icon.Length > 50 || icon.IndexOfAny(['<', '>', '"', '\'', '&']) >= 0)
        {
            throw new ApiException(400, "Module icon must be a valid icon class name (e.g. ri-folder-3-line).");
        }
        return icon;
    }

    public async Task DeleteModuleAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.NavPages.AnyAsync(p => p.ModuleId == id, cancellationToken) ||
            await _db.ReportPages.AnyAsync(r => r.ModuleId == id, cancellationToken))
        {
            throw new ApiException(400, "Cannot delete a module that still has nav pages or report pages assigned to it.");
        }

        var entity = await _db.Modules.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Module not found.");
        _db.Modules.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Nav Pages -----

    public async Task<IReadOnlyList<NavPageDto>> GetNavPagesAsync(CancellationToken cancellationToken = default)
    {
        var pages = await _db.NavPages.OrderBy(p => p.DisplayOrder).ToListAsync(cancellationToken);
        return pages.Select(ToDto).ToList();
    }

    public async Task<NavPageDto> CreateNavPageAsync(CreateNavPageRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _db.Modules.AnyAsync(m => m.Id == request.ModuleId, cancellationToken))
        {
            throw new ApiException(400, "Selected module does not exist.");
        }
        if (request.ParentPageId is int parentId && !await _db.NavPages.AnyAsync(p => p.Id == parentId, cancellationToken))
        {
            throw new ApiException(400, "Selected parent page does not exist.");
        }

        var entity = new NavPageEntity
        {
            ModuleId = request.ModuleId,
            PageName = request.PageName,
            PageUrl = request.PageUrl,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            ParentPageId = request.ParentPageId,
        };
        _db.NavPages.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<NavPageDto> UpdateNavPageAsync(int id, UpdateNavPageRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.NavPages.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Nav page not found.");

        if (!await _db.Modules.AnyAsync(m => m.Id == request.ModuleId, cancellationToken))
        {
            throw new ApiException(400, "Selected module does not exist.");
        }
        if (request.ParentPageId == id)
        {
            throw new ApiException(400, "A nav page cannot be its own parent.");
        }
        if (request.ParentPageId is int parentId && !await _db.NavPages.AnyAsync(p => p.Id == parentId, cancellationToken))
        {
            throw new ApiException(400, "Selected parent page does not exist.");
        }

        entity.ModuleId = request.ModuleId;
        entity.PageName = request.PageName;
        entity.PageUrl = request.PageUrl;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
        entity.ParentPageId = request.ParentPageId;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteNavPageAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.NavPages.AnyAsync(p => p.ParentPageId == id, cancellationToken))
        {
            throw new ApiException(400, "Cannot delete a nav page that still has sub-pages assigned to it.");
        }

        var entity = await _db.NavPages.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Nav page not found.");

        var permissions = await _db.Permissions.Where(p => p.NavPageId == id).ToListAsync(cancellationToken);
        _db.Permissions.RemoveRange(permissions);

        _db.NavPages.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Report Pages -----

    public async Task<IReadOnlyList<ReportPageDto>> GetReportPagesAsync(CancellationToken cancellationToken = default)
    {
        var pages = await _db.ReportPages.OrderBy(r => r.DisplayOrder).ToListAsync(cancellationToken);
        var modules = await _db.Modules.ToDictionaryAsync(m => m.Id, m => m.ModuleName, cancellationToken);
        return pages.Select(r => new ReportPageDto(r.Id, r.ModuleId, modules.GetValueOrDefault(r.ModuleId, ""), r.ReportName, r.ReportUrl, r.DisplayOrder, r.IsActive)).ToList();
    }

    public async Task<ReportPageDto> CreateReportPageAsync(CreateReportPageRequest request, CancellationToken cancellationToken = default)
    {
        var module = await _db.Modules.FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken)
            ?? throw new ApiException(400, "Selected module does not exist.");

        var entity = new ReportPageEntity
        {
            ModuleId = module.Id,
            ReportName = request.ReportName,
            ReportUrl = request.ReportUrl,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
        };
        _db.ReportPages.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new ReportPageDto(entity.Id, module.Id, module.ModuleName, entity.ReportName, entity.ReportUrl, entity.DisplayOrder, entity.IsActive);
    }

    public async Task<ReportPageDto> UpdateReportPageAsync(int id, UpdateReportPageRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ReportPages.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Report page not found.");

        var module = await _db.Modules.FirstOrDefaultAsync(m => m.Id == request.ModuleId, cancellationToken)
            ?? throw new ApiException(400, "Selected module does not exist.");

        entity.ModuleId = module.Id;
        entity.ReportName = request.ReportName;
        entity.ReportUrl = request.ReportUrl;
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return new ReportPageDto(entity.Id, module.Id, module.ModuleName, entity.ReportName, entity.ReportUrl, entity.DisplayOrder, entity.IsActive);
    }

    public async Task DeleteReportPageAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ReportPages.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Report page not found.");

        var permissions = await _db.ReportPermissions.Where(p => p.ReportPageId == id).ToListAsync(cancellationToken);
        _db.ReportPermissions.RemoveRange(permissions);

        _db.ReportPages.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static RoleDto ToDto(RoleEntity e) => new(e.Id, e.RoleName, e.Description, e.IsActive);

    private static NavPageDto ToDto(NavPageEntity e) => new(e.Id, e.ModuleId, e.PageName, e.PageUrl, e.DisplayOrder, e.IsActive, e.ParentPageId);
}
