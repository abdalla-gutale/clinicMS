using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.MedicalServices;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class MedicalServicesController : Controller
    {
        private readonly IMedicalServicesApiClient _medicalServicesApiClient;
        private readonly IPatientsApiClient _patientsApiClient;
        private readonly ISettingsApiClient _settingsApiClient;

        public MedicalServicesController(IMedicalServicesApiClient medicalServicesApiClient, IPatientsApiClient patientsApiClient, ISettingsApiClient settingsApiClient)
        {
            _medicalServicesApiClient = medicalServicesApiClient;
            _patientsApiClient = patientsApiClient;
            _settingsApiClient = settingsApiClient;
        }

        public async Task<IActionResult> ServiceTypes(CancellationToken cancellationToken)
        {
            var types = await _medicalServicesApiClient.GetServiceTypesAsync(cancellationToken);
            ViewBag.ServiceTypesJson = ViewJson.Serialize(types);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateServiceType([FromBody] CreateServiceTypeRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var type = await _medicalServicesApiClient.CreateServiceTypeAsync(request, cancellationToken);
                return Json(type);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateServiceType(int id, [FromBody] UpdateServiceTypeRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var type = await _medicalServicesApiClient.UpdateServiceTypeAsync(id, request, cancellationToken);
                return Json(type);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteServiceType(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _medicalServicesApiClient.DeleteServiceTypeAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        public async Task<IActionResult> Services(CancellationToken cancellationToken)
        {
            var services = await _medicalServicesApiClient.GetServicesAsync(cancellationToken);
            var types = await _medicalServicesApiClient.GetServiceTypesAsync(cancellationToken);
            ViewBag.ServicesJson = ViewJson.Serialize(services);
            ViewBag.ServiceTypesJson = ViewJson.Serialize(types.Where(t => t.IsActive));
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateService([FromBody] CreateServiceRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var service = await _medicalServicesApiClient.CreateServiceAsync(request, cancellationToken);
                return Json(service);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateService(int id, [FromBody] UpdateServiceRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var service = await _medicalServicesApiClient.UpdateServiceAsync(id, request, cancellationToken);
                return Json(service);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteService(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _medicalServicesApiClient.DeleteServiceAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        public async Task<IActionResult> TreatmentPlans(CancellationToken cancellationToken)
        {
            var plans = await _medicalServicesApiClient.GetTreatmentPlansAsync(cancellationToken);
            var services = await _medicalServicesApiClient.GetServicesAsync(cancellationToken);
            var products = await _medicalServicesApiClient.GetProductOptionsAsync(cancellationToken);
            var patients = await _patientsApiClient.GetAllAsync(cancellationToken);

            ViewBag.TreatmentPlansJson = ViewJson.Serialize(plans);
            ViewBag.ServicesJson = ViewJson.Serialize(services.Where(s => s.IsActive));
            ViewBag.ProductOptionsJson = ViewJson.Serialize(products);
            ViewBag.PlanPatientsJson = ViewJson.Serialize(patients);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateTreatmentPlan([FromBody] CreateTreatmentPlanRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var plan = await _medicalServicesApiClient.CreateTreatmentPlanAsync(request, cancellationToken);
                return Json(plan);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTreatmentPlan(int id, [FromBody] UpdateTreatmentPlanRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var plan = await _medicalServicesApiClient.UpdateTreatmentPlanAsync(id, request, cancellationToken);
                return Json(plan);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTreatmentPlan(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _medicalServicesApiClient.DeleteTreatmentPlanAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        public async Task<IActionResult> PatientCycles(CancellationToken cancellationToken)
        {
            var cycles = await _medicalServicesApiClient.GetPatientCyclesAsync(cancellationToken);
            var patients = await _patientsApiClient.GetAllAsync(cancellationToken);
            var plans = await _medicalServicesApiClient.GetTreatmentPlansAsync(cancellationToken);
            var services = await _medicalServicesApiClient.GetServicesAsync(cancellationToken);
            var products = await _medicalServicesApiClient.GetProductOptionsAsync(cancellationToken);
            var accounts = await _settingsApiClient.GetPaymentAccountsAsync(cancellationToken);
            var discounts = await _settingsApiClient.GetDiscountsAsync(cancellationToken);
            var clinicSettings = await _settingsApiClient.GetClinicSettingsAsync(cancellationToken);

            ViewBag.PatientCyclesJson = ViewJson.Serialize(cycles);
            ViewBag.CyclePatientsJson = ViewJson.Serialize(patients);
            ViewBag.CycleTreatmentPlansJson = ViewJson.Serialize(plans);
            ViewBag.CycleServicesJson = ViewJson.Serialize(services.Where(s => s.IsActive));
            ViewBag.CycleProductOptionsJson = ViewJson.Serialize(products);
            ViewBag.CyclePaymentAccountsJson = ViewJson.Serialize(accounts.Where(a => a.IsActive));
            ViewBag.CycleDiscountsJson = ViewJson.Serialize(discounts);
            ViewBag.CycleVatPercentage = clinicSettings?.IsVatEnabled == true ? clinicSettings.VatPercentage : 0m;
            ViewBag.CycleCurrencySymbol = clinicSettings?.CurrencySymbol ?? "AED";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignPatientCycle([FromBody] AssignPatientCycleRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var cycle = await _medicalServicesApiClient.AssignPatientCycleAsync(request, cancellationToken);
                return Json(cycle);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePatientCycleSessions(int id, [FromBody] UpdatePatientCycleSessionsRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var cycle = await _medicalServicesApiClient.UpdatePatientCycleSessionsAsync(id, request, cancellationToken);
                return Json(cycle);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReschedulePatientCycleSession(int id, [FromBody] ReschedulePatientCycleSessionRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var cycle = await _medicalServicesApiClient.ReschedulePatientCycleSessionAsync(id, request, cancellationToken);
                return Json(cycle);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CompletePatientCycleSession(int id, [FromBody] CompletePatientCycleSessionRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var cycle = await _medicalServicesApiClient.CompletePatientCycleSessionAsync(id, request, cancellationToken);
                return Json(cycle);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RecordCyclePayment(int id, [FromBody] RecordCyclePaymentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var cycle = await _medicalServicesApiClient.RecordCyclePaymentAsync(id, request, cancellationToken);
                return Json(cycle);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePatientCycle(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _medicalServicesApiClient.DeletePatientCycleAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        public async Task<IActionResult> WalkInSale(CancellationToken cancellationToken)
        {
            var patients = await _patientsApiClient.GetAllAsync(cancellationToken);
            var services = await _medicalServicesApiClient.GetServicesAsync(cancellationToken);
            var products = await _medicalServicesApiClient.GetProductOptionsAsync(cancellationToken);
            var accounts = await _settingsApiClient.GetPaymentAccountsAsync(cancellationToken);
            var settings = await _settingsApiClient.GetClinicSettingsAsync(cancellationToken);

            ViewBag.WalkInPatientsJson = ViewJson.Serialize(patients);
            ViewBag.WalkInServicesJson = ViewJson.Serialize(services.Where(s => s.IsActive));
            ViewBag.WalkInProductsJson = ViewJson.Serialize(products);
            ViewBag.WalkInPaymentAccountsJson = ViewJson.Serialize(accounts.Where(a => a.IsActive));
            ViewBag.VatPercentage = settings?.IsVatEnabled == true ? settings.VatPercentage : 0m;
            ViewBag.CurrencySymbol = settings?.CurrencySymbol ?? "AED";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateWalkInSale([FromBody] CreateWalkInSaleRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var invoice = await _medicalServicesApiClient.CreateWalkInSaleAsync(request, cancellationToken);
                return Json(invoice);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
