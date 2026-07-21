using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api;
using ClinicMS.Web.Models.Api.Payments;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Services.Api.Db;

public class DbPaymentsApiClient : IPaymentsApiClient
{
    private readonly ClinicMsDbContext _db;

    public DbPaymentsApiClient(ClinicMsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<OutstandingInvoiceDto>> GetOutstandingInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await _db.Invoices.Where(i => i.BalanceDue > 0).OrderByDescending(i => i.InvoiceDate).ToListAsync(cancellationToken);
        var patientIds = invoices.Where(i => i.PatientId.HasValue).Select(i => i.PatientId!.Value).Distinct().ToList();
        var patients = await _db.Patients.Where(p => patientIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);

        return invoices.Select(i =>
        {
            var patient = i.PatientId.HasValue ? patients.GetValueOrDefault(i.PatientId.Value) : null;
            return new OutstandingInvoiceDto(i.Id, i.PatientId, patient?.FullName, patient?.Phone, i.InvoiceDate, i.NetAmount, i.PaidAmount, i.BalanceDue, ParseStatus(i.PaymentStatus));
        }).ToList();
    }

    public async Task<PagedResult<OutstandingInvoiceDto>> GetOutstandingInvoicesPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query =
            from i in _db.Invoices
            where i.BalanceDue > 0
            join p in _db.Patients on i.PatientId equals p.Id into patientJoin
            from patient in patientJoin.DefaultIfEmpty()
            select new { Invoice = i, Patient = patient };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Patient != null && x.Patient.FullName.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.Invoice.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(x => new OutstandingInvoiceDto(
            x.Invoice.Id, x.Invoice.PatientId, x.Patient?.FullName, x.Patient?.Phone,
            x.Invoice.InvoiceDate, x.Invoice.NetAmount, x.Invoice.PaidAmount, x.Invoice.BalanceDue, ParseStatus(x.Invoice.PaymentStatus))).ToList();

        return new PagedResult<OutstandingInvoiceDto>(items, page, pageSize, totalCount);
    }

    public async Task<InvoiceDto> GetInvoiceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Invoice not found.");

        var patient = invoice.PatientId is int patientId
            ? await _db.Patients.FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken)
            : null;

        var items = await _db.InvoiceItems.Where(i => i.InvoiceId == id).ToListAsync(cancellationToken);
        var itemDtos = await BuildInvoiceItemDtosAsync(items, cancellationToken);

        return new InvoiceDto(
            invoice.Id, invoice.InvoiceNumber, invoice.PatientId, patient?.FullName, invoice.SessionId, invoice.InvoiceType,
            invoice.TotalAmount, invoice.DiscountAmount, invoice.VatAmount, invoice.NetAmount, invoice.PaidAmount, invoice.BalanceDue,
            ParseStatus(invoice.PaymentStatus), invoice.InvoiceDate, itemDtos);
    }

    public async Task<RevenueSummaryDto> GetRevenueSummaryAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rangeFrom = from ?? new DateOnly(today.Year, today.Month, 1);
        var rangeTo = to ?? today;
        var fromDt = rangeFrom.ToDateTime(TimeOnly.MinValue);
        var toDt = rangeTo.ToDateTime(TimeOnly.MaxValue);

        var inRange = await _db.Invoices.Where(i => i.InvoiceDate >= fromDt && i.InvoiceDate <= toDt).ToListAsync(cancellationToken);
        return new RevenueSummaryDto(rangeFrom, rangeTo, inRange.Sum(i => i.NetAmount), inRange.Sum(i => i.DiscountAmount));
    }

    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new ApiException(400, "Selected patient does not exist.");

        if (request.AmountPaid <= 0)
        {
            throw new ApiException(400, "Amount paid must be greater than zero.");
        }

        if (request.InvoiceId is int invoiceId)
        {
            var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken)
                ?? throw new ApiException(400, "Selected invoice does not exist.");

            invoice.PaidAmount += request.AmountPaid;
            invoice.BalanceDue = invoice.NetAmount - invoice.PaidAmount;
            invoice.PaymentStatus = invoice.BalanceDue <= 0 ? "Paid" : invoice.PaidAmount > 0 ? "Partial" : "Unpaid";
        }

        PaymentAccountEntity? account = null;
        if (request.AccountId is int accountId)
        {
            account = await _db.PaymentAccounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken)
                ?? throw new ApiException(400, "Selected account does not exist.");
        }

        var entity = new PaymentEntity
        {
            InvoiceId = request.InvoiceId,
            PatientId = patient.Id,
            AmountPaid = request.AmountPaid,
            PaymentMethod = request.PaymentMethod.ToString(),
            ReferenceNumber = request.ReferenceNumber,
            PaymentDate = DateTime.UtcNow,
            AccountId = request.AccountId,
        };
        _db.Payments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return new PaymentDto(
            entity.Id, entity.InvoiceId, entity.PatientId, patient.FullName, entity.AmountPaid,
            request.PaymentMethod, entity.ReferenceNumber, entity.PaymentDate, entity.AccountId, account?.Name);
    }

    public async Task<IReadOnlyList<AccountBreakdownDto>> GetAccountBreakdownAsync(CancellationToken cancellationToken = default)
    {
        var payments = await _db.Payments.ToListAsync(cancellationToken);
        var expenses = await _db.Expenses.ToListAsync(cancellationToken);
        var accounts = await _db.PaymentAccounts.ToDictionaryAsync(a => a.Id, cancellationToken);

        // Dictionary<int?, T> throws ArgumentNullException on a genuinely-null key (Nullable<T> with
        // HasValue false boxes to an actual null reference) -- group on a non-null sentinel instead
        // and map it back to "no account" only when building the final DTOs.
        const int unassignedKey = -1;
        var incomeByAccount = payments.GroupBy(p => p.AccountId ?? unassignedKey).ToDictionary(g => g.Key, g => g.Sum(p => p.AmountPaid));
        var expenseByAccount = expenses.GroupBy(e => e.AccountId ?? unassignedKey).ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
        var keys = incomeByAccount.Keys.Concat(expenseByAccount.Keys).Distinct();

        return keys.Select(key =>
        {
            int? accountId = key == unassignedKey ? null : key;
            var name = accountId is int aid ? accounts.GetValueOrDefault(aid)?.Name ?? "Unassigned" : "Unassigned";
            var income = incomeByAccount.GetValueOrDefault(key, 0m);
            var expense = expenseByAccount.GetValueOrDefault(key, 0m);
            return new AccountBreakdownDto(accountId, name, income, expense, income - expense);
        }).OrderByDescending(a => a.TotalIncome).ToList();
    }

    public async Task<IReadOnlyList<ProductRefundDto>> GetProductRefundsAsync(CancellationToken cancellationToken = default)
    {
        var refunds = await _db.ProductRefunds.OrderByDescending(r => r.RefundDate).ToListAsync(cancellationToken);
        return await BuildRefundDtosAsync(refunds, cancellationToken);
    }

    public async Task<ProductRefundDto> CreateProductRefundAsync(CreateProductRefundRequest request, CancellationToken cancellationToken = default)
    {
        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new ApiException(400, "Selected invoice does not exist.");

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ApiException(400, "A refund must have at least one item.");
        }

        var invoiceItems = await _db.InvoiceItems.Where(i => i.InvoiceId == invoice.Id).ToListAsync(cancellationToken);
        var skuIds = request.Items.Select(i => i.ProductSkuId).ToList();
        var skus = await _db.ProductSkus.Where(s => skuIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);

        var refund = new ProductRefundEntity
        {
            InvoiceId = invoice.Id,
            PatientId = invoice.PatientId,
            TotalRefundAmount = 0m,
            RefundType = request.RefundType.ToString(),
            Reason = request.Reason,
            RefundDate = DateTime.UtcNow,
        };
        _db.ProductRefunds.Add(refund);
        await _db.SaveChangesAsync(cancellationToken);

        var totalRefund = 0m;
        foreach (var itemRequest in request.Items)
        {
            var invoiceItem = invoiceItems.FirstOrDefault(i => i.ProductSkuId == itemRequest.ProductSkuId)
                ?? throw new ApiException(400, "One of the selected items is not part of this invoice.");

            if (itemRequest.Quantity <= 0 || itemRequest.Quantity > invoiceItem.Quantity)
            {
                throw new ApiException(400, $"Refund quantity must be between 1 and {invoiceItem.Quantity}.");
            }

            if (itemRequest.RefundUnitPrice < 0)
            {
                throw new ApiException(400, "Refund unit price cannot be negative.");
            }

            if (itemRequest.RestockItem && skus.TryGetValue(itemRequest.ProductSkuId, out var sku))
            {
                sku.StockQuantity += itemRequest.Quantity;
                _db.StockMovements.Add(new StockMovementEntity
                {
                    ProductSkuId = sku.Id,
                    MovementType = "In",
                    Quantity = itemRequest.Quantity,
                    ReferenceId = invoice.InvoiceNumber,
                    MovementDate = DateTime.UtcNow,
                    Notes = "Restocked from product refund",
                });
            }

            _db.RefundItems.Add(new RefundItemEntity
            {
                RefundId = refund.Id,
                ProductSkuId = itemRequest.ProductSkuId,
                Quantity = itemRequest.Quantity,
                RefundUnitPrice = itemRequest.RefundUnitPrice,
                RestockItem = itemRequest.RestockItem,
            });
            totalRefund += itemRequest.Quantity * itemRequest.RefundUnitPrice;
        }

        refund.TotalRefundAmount = totalRefund;
        await _db.SaveChangesAsync(cancellationToken);

        return (await BuildRefundDtosAsync(new List<ProductRefundEntity> { refund }, cancellationToken)).Single();
    }

    private async Task<List<InvoiceItemDto>> BuildInvoiceItemDtosAsync(List<InvoiceItemEntity> items, CancellationToken cancellationToken)
    {
        var serviceIds = items.Where(i => i.ServiceId.HasValue).Select(i => i.ServiceId!.Value).Distinct().ToList();
        var skuIds = items.Where(i => i.ProductSkuId.HasValue).Select(i => i.ProductSkuId!.Value).Distinct().ToList();
        var services = await _db.Services.Where(s => serviceIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var skus = await _db.ProductSkus.Where(s => skuIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var productIds = skus.Values.Select(s => s.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.ProductName, cancellationToken);

        return items.Select(i =>
        {
            var service = i.ServiceId is int sid ? services.GetValueOrDefault(sid) : null;
            var sku = i.ProductSkuId is int pskuId ? skus.GetValueOrDefault(pskuId) : null;
            var productName = sku is not null ? products.GetValueOrDefault(sku.ProductId) : null;
            return new InvoiceItemDto(i.Id, i.ItemType, i.ServiceId, service?.ServiceName, i.ProductSkuId, productName, sku?.SkuCode, i.Quantity, i.UnitPrice, i.TotalPrice);
        }).ToList();
    }

    private async Task<List<ProductRefundDto>> BuildRefundDtosAsync(List<ProductRefundEntity> refunds, CancellationToken cancellationToken)
    {
        if (refunds.Count == 0)
        {
            return new List<ProductRefundDto>();
        }

        var refundIds = refunds.Select(r => r.Id).ToList();
        var invoiceIds = refunds.Select(r => r.InvoiceId).Distinct().ToList();
        var patientIds = refunds.Where(r => r.PatientId.HasValue).Select(r => r.PatientId!.Value).Distinct().ToList();

        var invoices = await _db.Invoices.Where(i => invoiceIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, cancellationToken);
        var patients = await _db.Patients.Where(p => patientIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, cancellationToken);
        var items = await _db.RefundItems.Where(i => refundIds.Contains(i.RefundId)).ToListAsync(cancellationToken);
        var skuIds = items.Select(i => i.ProductSkuId).Distinct().ToList();
        var skus = await _db.ProductSkus.Where(s => skuIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var productIds = skus.Values.Select(s => s.ProductId).Distinct().ToList();
        var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.ProductName, cancellationToken);

        return refunds.Select(r =>
        {
            var invoice = invoices.GetValueOrDefault(r.InvoiceId);
            var refundItems = items.Where(i => i.RefundId == r.Id).Select(i =>
            {
                var sku = skus.GetValueOrDefault(i.ProductSkuId);
                var productName = sku is not null ? products.GetValueOrDefault(sku.ProductId, "") : "";
                return new RefundItemDto(i.ProductSkuId, sku?.SkuCode ?? "", productName, i.Quantity, i.RefundUnitPrice, i.RestockItem);
            }).ToList();

            return new ProductRefundDto(
                r.Id, r.InvoiceId, invoice?.InvoiceNumber ?? "", r.PatientId,
                r.PatientId is int pid ? patients.GetValueOrDefault(pid) : null,
                r.TotalRefundAmount, ParseRefundType(r.RefundType), r.Reason, r.RefundDate, refundItems);
        }).ToList();
    }

    private static PaymentStatus ParseStatus(string status) =>
        Enum.TryParse<PaymentStatus>(status, out var parsed) ? parsed : PaymentStatus.Unpaid;

    private static RefundType ParseRefundType(string type) =>
        Enum.TryParse<RefundType>(type, out var parsed) ? parsed : RefundType.Partial;
}
