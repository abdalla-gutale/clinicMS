using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Services.Api.Db;

public class DbDashboardApiClient : IDashboardApiClient
{
    private readonly ClinicMsDbContext _db;

    public DbDashboardApiClient(ClinicMsDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var todayDateOnly = DateOnly.FromDateTime(today);
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthStartDateOnly = DateOnly.FromDateTime(monthStart);
        var trendStart = today.AddDays(-13);

        var patientCount = await _db.Patients.CountAsync(cancellationToken);

        var todayRevenue = await _db.Invoices.Where(i => i.InvoiceDate.Date == today).SumAsync(i => (decimal?)i.NetAmount, cancellationToken) ?? 0m;
        var monthRevenue = await _db.Invoices.Where(i => i.InvoiceDate >= monthStart).SumAsync(i => (decimal?)i.NetAmount, cancellationToken) ?? 0m;
        var outstandingBalance = await _db.Invoices.SumAsync(i => (decimal?)i.BalanceDue, cancellationToken) ?? 0m;
        var monthExpenses = await _db.Expenses.Where(e => e.ExpenseDate >= monthStartDateOnly).SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        // "Active" here means not yet fully completed (mirrors DbMedicalServicesApiClient's
        // Active/Paused vs Completed split) -- a cycle with any non-Completed session still counts.
        var activePatientCycleCount = await _db.PatientCycles
            .Where(c => _db.CycleSessions.Any(s => s.CycleId == c.Id && s.Status != "Completed"))
            .CountAsync(cancellationToken);

        var lowStockProductCount = await _db.ProductSkus
            .Where(s => s.IsActive && s.StockQuantity <= s.ReorderLevel)
            .CountAsync(cancellationToken);

        // Draft + Ordered both still need attention (not yet Received or Cancelled).
        var pendingPurchaseOrderCount = await _db.PurchaseOrders
            .Where(o => o.Status == "Draft" || o.Status == "Ordered")
            .CountAsync(cancellationToken);

        var revenueTrend = await _db.Invoices
            .Where(i => i.InvoiceDate >= trendStart)
            .GroupBy(i => i.InvoiceDate.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(i => i.NetAmount) })
            .ToListAsync(cancellationToken);
        var revenueByDate = revenueTrend.ToDictionary(x => DateOnly.FromDateTime(x.Date), x => x.Total);
        var revenuePoints = Enumerable.Range(0, 14)
            .Select(offset => DateOnly.FromDateTime(trendStart).AddDays(offset))
            .Select(d => new RevenuePointDto(d, revenueByDate.GetValueOrDefault(d, 0m)))
            .ToList();

        var expenseBreakdown = await _db.Expenses
            .Where(e => e.ExpenseDate >= monthStartDateOnly)
            .GroupBy(e => e.ExpenseCategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync(cancellationToken);
        var categoryNames = await _db.ExpenseCategories.ToDictionaryAsync(c => c.Id, c => c.CategoryName, cancellationToken);
        var expenseBreakdownDtos = expenseBreakdown
            .OrderByDescending(x => x.Total)
            .Take(6)
            .Select(x => new ExpenseCategoryBreakdownDto(categoryNames.GetValueOrDefault(x.CategoryId, "Other"), x.Total))
            .ToList();

        var lowStockItems = await _db.ProductSkus
            .Where(s => s.IsActive && s.StockQuantity <= s.ReorderLevel)
            .OrderBy(s => s.StockQuantity - s.ReorderLevel)
            .Take(8)
            .ToListAsync(cancellationToken);
        var lowStockProductNames = await _db.Products.ToDictionaryAsync(p => p.Id, p => p.ProductName, cancellationToken);
        var lowStockDtos = lowStockItems
            .Select(s => new LowStockItemDto(s.Id, s.SkuCode, lowStockProductNames.GetValueOrDefault(s.ProductId, ""), s.StockQuantity, s.ReorderLevel))
            .ToList();

        var todaySessions = await _db.CycleSessions
            .Where(s => s.ActualScheduledDate == todayDateOnly && s.Status != "Completed")
            .ToListAsync(cancellationToken);
        var cycleIds = todaySessions.Select(s => s.CycleId).Distinct().ToList();
        var cycles = await _db.PatientCycles.Where(c => cycleIds.Contains(c.Id)).ToListAsync(cancellationToken);
        var cyclesById = cycles.ToDictionary(c => c.Id);
        var patientIds = cycles.Select(c => c.PatientId).Distinct().ToList();
        var patientNames = await _db.Patients.Where(p => patientIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, cancellationToken);
        var todaySessionDtos = todaySessions.Select(s =>
        {
            var cycle = cyclesById.GetValueOrDefault(s.CycleId);
            return new TodaySessionDto(
                s.CycleId,
                cycle is not null ? patientNames.GetValueOrDefault(cycle.PatientId, "") : "",
                cycle?.CycleName ?? "",
                SessionLabel(cycle?.Frequency ?? "Weekly", s.SessionNumber),
                s.Status);
        }).OrderBy(s => s.PatientName).ToList();

        var recentActivity = await _db.AuditTrail
            .OrderByDescending(a => a.CreatedAt)
            .Take(8)
            .ToListAsync(cancellationToken);
        var recentActivityDtos = recentActivity
            .Select(a => new RecentActivityDto(a.CreatedAt, $"{a.Action} {a.TableName} #{a.RecordId}"))
            .ToList();

        var monthPayments = await _db.Payments
            .Where(p => p.PaymentDate >= monthStart)
            .GroupBy(p => p.PatientId)
            .Select(g => new { PatientId = g.Key, Total = g.Sum(p => p.AmountPaid), Count = g.Count() })
            .OrderByDescending(x => x.Total)
            .Take(5)
            .ToListAsync(cancellationToken);
        var topPatientIds = monthPayments.Select(x => x.PatientId).ToList();
        var topPatientNames = await _db.Patients.Where(p => topPatientIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName, cancellationToken);
        var topPatientDtos = monthPayments
            .Select(x => new TopPatientDto(x.PatientId, topPatientNames.GetValueOrDefault(x.PatientId, ""), x.Total, x.Count))
            .ToList();

        return new DashboardSummaryDto(
            patientCount,
            todayRevenue,
            monthRevenue,
            outstandingBalance,
            activePatientCycleCount,
            lowStockProductCount,
            pendingPurchaseOrderCount,
            monthExpenses,
            revenuePoints,
            expenseBreakdownDtos,
            lowStockDtos,
            todaySessionDtos,
            recentActivityDtos,
            topPatientDtos);
    }

    private static string SessionLabel(string frequency, int sessionNumber) => frequency switch
    {
        "Daily" => $"Day {sessionNumber}",
        "Monthly" => $"Month {sessionNumber}",
        _ => $"Week {sessionNumber}",
    };
}
