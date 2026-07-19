using ClinicMS.Web.Models.Api.Payments;

namespace ClinicMS.Web.Services.Api;

public interface IPaymentsApiClient
{
    Task<IReadOnlyList<OutstandingInvoiceDto>> GetOutstandingInvoicesAsync(CancellationToken cancellationToken = default);

    Task<InvoiceDto> GetInvoiceByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<RevenueSummaryDto> GetRevenueSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountBreakdownDto>> GetAccountBreakdownAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductRefundDto>> GetProductRefundsAsync(CancellationToken cancellationToken = default);

    Task<ProductRefundDto> CreateProductRefundAsync(CreateProductRefundRequest request, CancellationToken cancellationToken = default);
}
