using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Rbac;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class AdministrationController : Controller
    {
        private readonly IRolesApiClient _rolesApiClient;

        public AdministrationController(IRolesApiClient rolesApiClient)
        {
            _rolesApiClient = rolesApiClient;
        }

        [RequirePermission("/admin/modules", PermissionAction.View)]
        public async Task<IActionResult> Modules(CancellationToken cancellationToken)
        {
            var modules = await _rolesApiClient.GetModulesAsync(cancellationToken);
            ViewBag.ModulesJson = ViewJson.Serialize(modules);
            return View();
        }

        [HttpPost]
        [RequirePermission("/admin/modules", PermissionAction.Create)]
        public async Task<IActionResult> CreateModule([FromBody] CreateModuleRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _rolesApiClient.CreateModuleAsync(request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        [RequirePermission("/admin/modules", PermissionAction.Edit)]
        public async Task<IActionResult> UpdateModule(int id, [FromBody] UpdateModuleRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _rolesApiClient.UpdateModuleAsync(id, request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        [RequirePermission("/admin/modules", PermissionAction.Delete)]
        public async Task<IActionResult> DeleteModule(int id, CancellationToken cancellationToken)
        {
            try { await _rolesApiClient.DeleteModuleAsync(id, cancellationToken); return Json(new { success = true }); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [RequirePermission("/admin/nav-pages", PermissionAction.View)]
        public async Task<IActionResult> NavPages(CancellationToken cancellationToken)
        {
            var navPages = await _rolesApiClient.GetNavPagesAsync(cancellationToken);
            var modules = await _rolesApiClient.GetModulesAsync(cancellationToken);
            ViewBag.NavPagesJson = ViewJson.Serialize(navPages);
            ViewBag.ModulesJson = ViewJson.Serialize(modules.Where(m => m.IsActive));
            return View();
        }

        [HttpPost]
        [RequirePermission("/admin/nav-pages", PermissionAction.Create)]
        public async Task<IActionResult> CreateNavPage([FromBody] CreateNavPageRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _rolesApiClient.CreateNavPageAsync(request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        [RequirePermission("/admin/nav-pages", PermissionAction.Edit)]
        public async Task<IActionResult> UpdateNavPage(int id, [FromBody] UpdateNavPageRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _rolesApiClient.UpdateNavPageAsync(id, request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        [RequirePermission("/admin/nav-pages", PermissionAction.Delete)]
        public async Task<IActionResult> DeleteNavPage(int id, CancellationToken cancellationToken)
        {
            try { await _rolesApiClient.DeleteNavPageAsync(id, cancellationToken); return Json(new { success = true }); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [RequirePermission("/admin/report-pages", PermissionAction.View)]
        public async Task<IActionResult> ReportPages(CancellationToken cancellationToken)
        {
            var reportPages = await _rolesApiClient.GetReportPagesAsync(cancellationToken);
            var modules = await _rolesApiClient.GetModulesAsync(cancellationToken);
            ViewBag.ReportPagesJson = ViewJson.Serialize(reportPages);
            ViewBag.ModulesJson = ViewJson.Serialize(modules.Where(m => m.IsActive));
            return View();
        }

        [HttpPost]
        [RequirePermission("/admin/report-pages", PermissionAction.Create)]
        public async Task<IActionResult> CreateReportPage([FromBody] CreateReportPageRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _rolesApiClient.CreateReportPageAsync(request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        [RequirePermission("/admin/report-pages", PermissionAction.Edit)]
        public async Task<IActionResult> UpdateReportPage(int id, [FromBody] UpdateReportPageRequest request, CancellationToken cancellationToken)
        {
            try { return Json(await _rolesApiClient.UpdateReportPageAsync(id, request, cancellationToken)); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }

        [HttpPost]
        [RequirePermission("/admin/report-pages", PermissionAction.Delete)]
        public async Task<IActionResult> DeleteReportPage(int id, CancellationToken cancellationToken)
        {
            try { await _rolesApiClient.DeleteReportPageAsync(id, cancellationToken); return Json(new { success = true }); }
            catch (ApiException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        }
    }
}
