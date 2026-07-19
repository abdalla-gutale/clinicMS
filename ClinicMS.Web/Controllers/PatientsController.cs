using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Patients;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class PatientsController : Controller
    {
        private readonly IPatientsApiClient _patientsApiClient;

        public PatientsController(IPatientsApiClient patientsApiClient)
        {
            _patientsApiClient = patientsApiClient;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var patients = await _patientsApiClient.GetAllAsync(cancellationToken);
            ViewBag.PatientsJson = ViewJson.Serialize(patients);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var patient = await _patientsApiClient.CreateAsync(request, cancellationToken);
                return Json(patient);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePatientRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var patient = await _patientsApiClient.UpdateAsync(id, request, cancellationToken);
                return Json(patient);
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
                await _patientsApiClient.DeleteAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
