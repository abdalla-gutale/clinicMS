using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Reports;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Services.Api.Db;

public class DbReportsApiClient : IReportsApiClient
{
    private readonly ClinicMsDbContext _db;

    public DbReportsApiClient(ClinicMsDbContext db)
    {
        _db = db;
    }

    public async Task<BalanceSheetDto> GetBalanceSheetAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _db.PaymentAccounts.OrderBy(a => a.Id).ToListAsync(cancellationToken);
        var accountIds = accounts.Select(a => a.Id).ToList();

        var paymentsIn = await _db.Payments
            .Where(p => p.AccountId != null && accountIds.Contains(p.AccountId.Value))
            .GroupBy(p => p.AccountId!.Value)
            .Select(g => new { AccountId = g.Key, Total = g.Sum(p => p.AmountPaid) })
            .ToDictionaryAsync(x => x.AccountId, x => x.Total, cancellationToken);

        var expensesOut = await _db.Expenses
            .Where(e => e.AccountId != null && accountIds.Contains(e.AccountId.Value))
            .GroupBy(e => e.AccountId!.Value)
            .Select(g => new { AccountId = g.Key, Total = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.AccountId, x => x.Total, cancellationToken);

        var accountBalances = accounts.Select(a =>
        {
            var inflow = paymentsIn.GetValueOrDefault(a.Id, 0m);
            var outflow = expensesOut.GetValueOrDefault(a.Id, 0m);
            return new BalanceSheetAccountDto(a.Id, a.Name, inflow - outflow);
        }).ToList();

        var totalCash = accountBalances.Sum(a => a.Balance);

        var inventoryValue = await _db.ProductSkus.SumAsync(s => (decimal?)(s.StockQuantity * s.CostPrice), cancellationToken) ?? 0m;

        var patientWalletCredits = await _db.Patients.SumAsync(p => (decimal?)p.CurrentWalletCredit, cancellationToken) ?? 0m;

        var totalAssets = totalCash + inventoryValue + patientWalletCredits;

        // "Ordered" = committed to a supplier but goods not yet received -- money effectively
        // spoken for. Draft (including any legacy/unrecognized status) isn't a real commitment yet,
        // and Received/Cancelled purchase orders no longer represent outstanding liability.
        var purchaseOrdersOutstanding = await _db.PurchaseOrders
            .Where(o => o.Status == "Ordered")
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

        var totalLiabilities = purchaseOrdersOutstanding;

        return new BalanceSheetDto(
            DateTime.UtcNow,
            accountBalances,
            totalCash,
            inventoryValue,
            patientWalletCredits,
            totalAssets,
            purchaseOrdersOutstanding,
            totalLiabilities,
            totalAssets - totalLiabilities);
    }
}
