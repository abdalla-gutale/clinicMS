using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Payments;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class PaymentsController : Controller
    {
        private readonly IPaymentsApiClient _paymentsApiClient;

        public PaymentsController(IPaymentsApiClient paymentsApiClient)
        {
            _paymentsApiClient = paymentsApiClient;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var outstanding = await _paymentsApiClient.GetOutstandingInvoicesAsync(cancellationToken);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var monthSummary = await _paymentsApiClient.GetRevenueSummaryAsync(null, null, cancellationToken);
            var todaySummary = await _paymentsApiClient.GetRevenueSummaryAsync(today, today, cancellationToken);

            ViewBag.OutstandingJson = ViewJson.Serialize(outstanding);
            ViewBag.MonthTotal = monthSummary.TotalNetAmount;
            ViewBag.TodayTotal = todaySummary.TotalNetAmount;

            return View();
        }

        public async Task<IActionResult> Invoices(CancellationToken cancellationToken)
        {
            var outstanding = await _paymentsApiClient.GetOutstandingInvoicesAsync(cancellationToken);
            ViewBag.OutstandingJson = ViewJson.Serialize(outstanding);
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoice(int id, CancellationToken cancellationToken)
        {
            try
            {
                var invoice = await _paymentsApiClient.GetInvoiceByIdAsync(id, cancellationToken);
                return Json(invoice);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RecordPayment([FromBody] CreatePaymentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var payment = await _paymentsApiClient.CreatePaymentAsync(request, cancellationToken);
                return Json(payment);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }
    }
}
