using ClinicMS.Web.Models.Api.Dashboard;

namespace ClinicMS.Web.Services.Api.Mocks;

public class MockDashboardApiClient : IDashboardApiClient
{
    public Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var todayRevenue = MockStore.Invoices.Where(i => i.InvoiceDate.Date == today).Sum(i => i.NetAmount);
        var monthRevenue = MockStore.Invoices.Where(i => i.InvoiceDate >= monthStart).Sum(i => i.NetAmount);
        var outstanding = MockStore.Invoices.Sum(i => i.BalanceDue);
        var monthExpenses = MockStore.Expenses
            .Where(e => e.ExpenseDate.ToDateTime(TimeOnly.MinValue) >= monthStart)
            .Sum(e => e.Amount);

        var summary = new DashboardSummaryDto(
            PatientCount: 128,
            TodayRevenue: todayRevenue,
            MonthRevenue: monthRevenue,
            OutstandingBalance: outstanding,
            ActivePatientCycleCount: 34,
            LowStockProductCount: 6,
            PendingPurchaseOrderCount: 3,
            MonthExpenses: monthExpenses);

        return Task.FromResult(summary);
    }
}
