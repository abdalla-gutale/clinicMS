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
        private readonly ISettingsApiClient _settingsApiClient;
        private readonly IConfiguration _configuration;

        public PaymentsController(IPaymentsApiClient paymentsApiClient, ISettingsApiClient settingsApiClient, IConfiguration configuration)
        {
            _paymentsApiClient = paymentsApiClient;
            _settingsApiClient = settingsApiClient;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var outstanding = await _paymentsApiClient.GetOutstandingInvoicesAsync(cancellationToken);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var monthSummary = await _paymentsApiClient.GetRevenueSummaryAsync(null, null, cancellationToken);
            var todaySummary = await _paymentsApiClient.GetRevenueSummaryAsync(today, today, cancellationToken);
            var accounts = await _settingsApiClient.GetPaymentAccountsAsync(cancellationToken);
            var accountBreakdown = await _paymentsApiClient.GetAccountBreakdownAsync(cancellationToken);

            ViewBag.OutstandingJson = ViewJson.Serialize(outstanding);
            ViewBag.MonthTotal = monthSummary.TotalNetAmount;
            ViewBag.TodayTotal = todaySummary.TotalNetAmount;
            ViewBag.PaymentAccountsJson = ViewJson.Serialize(accounts.Where(a => a.IsActive));
            ViewBag.AccountBreakdownJson = ViewJson.Serialize(accountBreakdown);

            return View();
        }

        public async Task<IActionResult> Invoices(CancellationToken cancellationToken)
        {
            var outstanding = await _paymentsApiClient.GetOutstandingInvoicesAsync(cancellationToken);
            ViewBag.OutstandingJson = ViewJson.Serialize(outstanding);

            var settings = await _settingsApiClient.GetClinicSettingsAsync(cancellationToken);
            ViewBag.ReportLogoUrl = LogoUrlResolver.Resolve(settings?.ReportLogoUrl, _configuration);
            ViewBag.ClinicName = settings?.ClinicName ?? "ClinicMS";

            return View();
        }

        public async Task<IActionResult> ProductRefunds(CancellationToken cancellationToken)
        {
            var refunds = await _paymentsApiClient.GetProductRefundsAsync(cancellationToken);
            ViewBag.ProductRefundsJson = ViewJson.Serialize(refunds);
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
        public async Task<IActionResult> CreateProductRefund([FromBody] CreateProductRefundRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var refund = await _paymentsApiClient.CreateProductRefundAsync(request, cancellationToken);
                return Json(refund);
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
