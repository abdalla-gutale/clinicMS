using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Payments;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class PaymentsController : Controller
    {
        private const int DefaultPageSize = 8;

        private readonly IPaymentsApiClient _paymentsApiClient;
        private readonly ISettingsApiClient _settingsApiClient;
        private readonly IPatientsApiClient _patientsApiClient;
        private readonly IConfiguration _configuration;

        public PaymentsController(IPaymentsApiClient paymentsApiClient, ISettingsApiClient settingsApiClient, IPatientsApiClient patientsApiClient, IConfiguration configuration)
        {
            _paymentsApiClient = paymentsApiClient;
            _settingsApiClient = settingsApiClient;
            _patientsApiClient = patientsApiClient;
            _configuration = configuration;
        }

        [RequirePermission("/payments", PermissionAction.View)]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var initialPage = await _paymentsApiClient.GetOutstandingInvoicesPagedAsync(1, DefaultPageSize, null, cancellationToken);
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var monthSummary = await _paymentsApiClient.GetRevenueSummaryAsync(null, null, cancellationToken);
            var todaySummary = await _paymentsApiClient.GetRevenueSummaryAsync(today, today, cancellationToken);
            var accounts = await _settingsApiClient.GetPaymentAccountsAsync(cancellationToken);
            var accountBreakdown = await _paymentsApiClient.GetAccountBreakdownAsync(cancellationToken);
            var allPatients = await _patientsApiClient.GetAllAsync(cancellationToken);

            ViewBag.OutstandingPageJson = ViewJson.Serialize(initialPage);
            ViewBag.MonthTotal = monthSummary.TotalNetAmount;
            ViewBag.TodayTotal = todaySummary.TotalNetAmount;
            ViewBag.PaymentAccountsJson = ViewJson.Serialize(accounts.Where(a => a.IsActive));
            ViewBag.AccountBreakdownJson = ViewJson.Serialize(accountBreakdown);
            ViewBag.AllPatientsJson = ViewJson.Serialize(allPatients);

            return View();
        }

        [HttpGet]
        [RequirePermission("/payments", PermissionAction.View)]
        public async Task<IActionResult> GetOutstandingPage(int page, int pageSize, string? search, CancellationToken cancellationToken)
        {
            var result = await _paymentsApiClient.GetOutstandingInvoicesPagedAsync(page, pageSize, search, cancellationToken);
            return Json(result);
        }

        [RequirePermission("/invoices", PermissionAction.View)]
        public async Task<IActionResult> Invoices(CancellationToken cancellationToken)
        {
            var initialPage = await _paymentsApiClient.GetOutstandingInvoicesPagedAsync(1, DefaultPageSize, null, cancellationToken);
            ViewBag.OutstandingPageJson = ViewJson.Serialize(initialPage);

            var settings = await _settingsApiClient.GetClinicSettingsAsync(cancellationToken);
            ViewBag.ReportLogoUrl = LogoUrlResolver.Resolve(settings?.ReportLogoUrl, _configuration);
            ViewBag.ClinicName = settings?.ClinicName ?? "ClinicMS";

            return View();
        }

        [HttpGet]
        [RequirePermission("/invoices", PermissionAction.View)]
        public async Task<IActionResult> GetInvoicesPage(int page, int pageSize, string? search, CancellationToken cancellationToken)
        {
            var result = await _paymentsApiClient.GetOutstandingInvoicesPagedAsync(page, pageSize, search, cancellationToken);
            return Json(result);
        }

        [RequirePermission("/product-refunds", PermissionAction.View)]
        public async Task<IActionResult> ProductRefunds(CancellationToken cancellationToken)
        {
            var refunds = await _paymentsApiClient.GetProductRefundsAsync(cancellationToken);
            ViewBag.ProductRefundsJson = ViewJson.Serialize(refunds);
            return View();
        }

        [HttpGet]
        [RequirePermission("/invoices", PermissionAction.View)]
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
        [RequirePermission("/product-refunds", PermissionAction.Create)]
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
        [RequirePermission("/payments", PermissionAction.Create)]
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
