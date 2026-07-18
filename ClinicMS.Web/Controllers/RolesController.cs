using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Rbac;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class RolesController : Controller
    {
        private readonly IRolesApiClient _rolesApiClient;

        public RolesController(IRolesApiClient rolesApiClient)
        {
            _rolesApiClient = rolesApiClient;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var roles = await _rolesApiClient.GetAllAsync(cancellationToken);
            var modules = await _rolesApiClient.GetModulesAsync(cancellationToken);
            var navPages = await _rolesApiClient.GetNavPagesAsync(cancellationToken);

            ViewBag.RolesJson = ViewJson.Serialize(roles);
            ViewBag.ModulesJson = ViewJson.Serialize(modules.Where(m => m.IsActive).OrderBy(m => m.DisplayOrder));
            ViewBag.NavPagesJson = ViewJson.Serialize(navPages.Where(p => p.IsActive).OrderBy(p => p.DisplayOrder));

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var role = await _rolesApiClient.CreateAsync(request, cancellationToken);
                return Json(role);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var role = await _rolesApiClient.UpdateAsync(id, request, cancellationToken);
                return Json(role);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _rolesApiClient.DeleteAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Permissions(int id, CancellationToken cancellationToken)
        {
            try
            {
                var role = await _rolesApiClient.GetByIdAsync(id, cancellationToken);
                return Json(role);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePermissions(int id, [FromBody] List<PermissionItem> permissions, CancellationToken cancellationToken)
        {
            try
            {
                await _rolesApiClient.SetNavPermissionsAsync(id, permissions, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
