using ClinicMS.Web.Models.Api.Payments;
using ClinicMS.Web.Models.Api.SupplyChain;

namespace ClinicMS.Web.Services.Api.Mocks;

public class MockPaymentsApiClient : IPaymentsApiClient
{
    public Task<IReadOnlyList<OutstandingInvoiceDto>> GetOutstandingInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var outstanding = MockStore.Invoices
            .Where(i => i.BalanceDue > 0)
            .Select(ToOutstandingDto)
            .ToList();

        return Task.FromResult<IReadOnlyList<OutstandingInvoiceDto>>(outstanding);
    }

    public Task<InvoiceDto> GetInvoiceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = MockStore.Invoices.FirstOrDefault(i => i.Id == id)
            ?? throw new ApiException(404, "Invoice not found.");
        return Task.FromResult(ToInvoiceDto(invoice));
    }

    public Task<RevenueSummaryDto> GetRevenueSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rangeFrom = from ?? new DateOnly(today.Year, today.Month, 1);
        var rangeTo = to ?? today;

        var inRange = MockStore.Invoices
            .Where(i =>
            {
                var date = DateOnly.FromDateTime(i.InvoiceDate);
                return date >= rangeFrom && date <= rangeTo;
            })
            .ToList();

        var summary = new RevenueSummaryDto(
            rangeFrom,
            rangeTo,
            inRange.Sum(i => i.NetAmount),
            inRange.Sum(i => i.DiscountAmount));

        return Task.FromResult(summary);
    }

    public Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var patientName = MockStore.Patients.FirstOrDefault(p => p.Id == request.PatientId)?.FullName
            ?? throw new ApiException(400, "Selected patient does not exist.");

        if (request.InvoiceId is int invoiceId)
        {
            var invoice = MockStore.Invoices.FirstOrDefault(i => i.Id == invoiceId)
                ?? throw new ApiException(400, "Selected invoice does not exist.");
            invoice.PaidAmount += request.AmountPaid;
        }

        var accountName = request.AccountId is int accountId
            ? MockStore.PaymentAccounts.FirstOrDefault(a => a.Id == accountId)?.Name
            : null;

        var payment = new PaymentDto(
            MockStore.NextPaymentId++,
            request.InvoiceId,
            request.PatientId,
            patientName,
            request.AmountPaid,
            request.PaymentMethod,
            request.ReferenceNumber,
            DateTime.UtcNow,
            request.AccountId,
            accountName);

        MockStore.Payments.Add(payment);
        return Task.FromResult(payment);
    }

    public Task<IReadOnlyList<AccountBreakdownDto>> GetAccountBreakdownAsync(CancellationToken cancellationToken = default)
    {
        var incomeByAccount = MockStore.Payments
            .GroupBy(p => (p.AccountId, Name: p.AccountName ?? "Unassigned"))
            .ToDictionary(g => g.Key, g => g.Sum(p => p.AmountPaid));

        var expenseByAccount = MockStore.Expenses
            .GroupBy(e => (e.AccountId, Name: e.AccountName ?? "Unassigned"))
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var keys = incomeByAccount.Keys.Concat(expenseByAccount.Keys).Distinct();

        var result = keys
            .Select(k =>
            {
                var income = incomeByAccount.GetValueOrDefault(k, 0m);
                var expense = expenseByAccount.GetValueOrDefault(k, 0m);
                return new AccountBreakdownDto(k.AccountId, k.Name, income, expense, income - expense);
            })
            .OrderByDescending(a => a.TotalIncome)
            .ToList();

        return Task.FromResult<IReadOnlyList<AccountBreakdownDto>>(result);
    }

    public Task<IReadOnlyList<ProductRefundDto>> GetProductRefundsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProductRefundDto>>(MockStore.ProductRefunds.OrderByDescending(r => r.RefundDate).ToList());

    public Task<ProductRefundDto> CreateProductRefundAsync(CreateProductRefundRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = MockStore.Invoices.FirstOrDefault(i => i.Id == request.InvoiceId)
            ?? throw new ApiException(400, "Selected invoice does not exist.");
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ApiException(400, "A refund must have at least one item.");
        }

        var items = new List<RefundItemDto>();
        foreach (var itemRequest in request.Items)
        {
            var invoiceItem = invoice.Items.FirstOrDefault(i => i.ProductSkuId == itemRequest.ProductSkuId)
                ?? throw new ApiException(400, "One of the selected items is not part of this invoice.");
            if (itemRequest.Quantity <= 0 || itemRequest.Quantity > invoiceItem.Quantity)
            {
                throw new ApiException(400, $"Refund quantity for '{invoiceItem.ProductName}' must be between 1 and {invoiceItem.Quantity}.");
            }
            if (itemRequest.RefundUnitPrice < 0)
            {
                throw new ApiException(400, "Refund unit price cannot be negative.");
            }

            var skuIndex = MockStore.ProductSkus.FindIndex(s => s.Id == itemRequest.ProductSkuId);
            if (itemRequest.RestockItem && skuIndex >= 0)
            {
                var sku = MockStore.ProductSkus[skuIndex];
                MockStore.ProductSkus[skuIndex] = sku with { StockQuantity = sku.StockQuantity + itemRequest.Quantity };
                MockStore.StockMovements.Add(new StockMovementDto(
                    MockStore.NextStockMovementId++, sku.Id, sku.SkuCode, sku.ProductName, StockMovementType.In,
                    itemRequest.Quantity, invoice.InvoiceNumber, DateTime.UtcNow, "Restocked from product refund"));
            }

            items.Add(new RefundItemDto(itemRequest.ProductSkuId, invoiceItem.SkuCode ?? string.Empty, invoiceItem.ProductName ?? string.Empty, itemRequest.Quantity, itemRequest.RefundUnitPrice, itemRequest.RestockItem));
        }

        var refund = new ProductRefundDto(
            MockStore.NextProductRefundId++, invoice.Id, invoice.InvoiceNumber, invoice.PatientId, invoice.PatientName,
            items.Sum(i => i.Quantity * i.RefundUnitPrice), request.RefundType, request.Reason, DateTime.UtcNow, items);
        MockStore.ProductRefunds.Add(refund);
        return Task.FromResult(refund);
    }

    private static OutstandingInvoiceDto ToOutstandingDto(MockStore.InvoiceRecord i) => new(
        i.Id, i.PatientId, i.PatientName, i.PatientPhone, i.InvoiceDate, i.NetAmount, i.PaidAmount, i.BalanceDue, i.Status);

    private static InvoiceDto ToInvoiceDto(MockStore.InvoiceRecord i) => new(
        i.Id, i.InvoiceNumber, i.PatientId, i.PatientName, null, i.InvoiceType,
        i.TotalAmount, i.DiscountAmount, i.VatAmount, i.NetAmount, i.PaidAmount, i.BalanceDue, i.Status, i.InvoiceDate, i.Items);
}
