using ClinicMS.Web.Models.Api.Payments;

namespace ClinicMS.Web.Services.Api;

public class PaymentsApiClient : ApiClientBase, IPaymentsApiClient
{
    public PaymentsApiClient(HttpClient http) : base(http)
    {
    }

    public Task<IReadOnlyList<OutstandingInvoiceDto>> GetOutstandingInvoicesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<OutstandingInvoiceDto>>("api/invoices/outstanding", cancellationToken);

    public Task<InvoiceDto> GetInvoiceByIdAsync(int id, CancellationToken cancellationToken = default) =>
        GetAsync<InvoiceDto>($"api/invoices/{id}", cancellationToken);

    public Task<RevenueSummaryDto> GetRevenueSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={from.Value:yyyy-MM-dd}");
        if (to.HasValue) query.Add($"to={to.Value:yyyy-MM-dd}");
        var queryString = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
        return GetAsync<RevenueSummaryDto>($"api/invoices/revenue-summary{queryString}", cancellationToken);
    }

    public Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<PaymentDto>("api/payments", request, cancellationToken);

    public Task<IReadOnlyList<AccountBreakdownDto>> GetAccountBreakdownAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<AccountBreakdownDto>>("api/payments/account-breakdown", cancellationToken);

    public Task<IReadOnlyList<ProductRefundDto>> GetProductRefundsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ProductRefundDto>>("api/productrefunds", cancellationToken);

    public Task<ProductRefundDto> CreateProductRefundAsync(CreateProductRefundRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<ProductRefundDto>("api/productrefunds", request, cancellationToken);
}
