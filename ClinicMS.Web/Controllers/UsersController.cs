using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Users;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class UsersController : Controller
    {
        private readonly IUsersApiClient _usersApiClient;
        private readonly IRolesApiClient _rolesApiClient;

        public UsersController(IUsersApiClient usersApiClient, IRolesApiClient rolesApiClient)
        {
            _usersApiClient = usersApiClient;
            _rolesApiClient = rolesApiClient;
        }

        [RequirePermission("/admin/users", PermissionAction.View)]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var users = await _usersApiClient.GetAllAsync(cancellationToken);
            var roles = await _rolesApiClient.GetAllAsync(cancellationToken);

            ViewBag.UsersJson = ViewJson.Serialize(users);
            ViewBag.RolesJson = ViewJson.Serialize(roles);

            return View();
        }

        [HttpPost]
        [RequirePermission("/admin/users", PermissionAction.Create)]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _usersApiClient.CreateAsync(request, cancellationToken);
                return Json(user);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/admin/users", PermissionAction.Edit)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _usersApiClient.UpdateAsync(id, request, cancellationToken);
                return Json(user);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/admin/users", PermissionAction.Edit)]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordModel model, CancellationToken cancellationToken)
        {
            try
            {
                await _usersApiClient.ChangePasswordAsync(id, model.NewPassword, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/admin/users", PermissionAction.Delete)]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _usersApiClient.DeleteAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }

    public record ChangePasswordModel(string NewPassword);
}
