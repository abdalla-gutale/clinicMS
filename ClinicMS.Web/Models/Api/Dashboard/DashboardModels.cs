namespace ClinicMS.Web.Models.Api.Dashboard;

public record RevenuePointDto(DateOnly Date, decimal NetAmount);

public record ExpenseCategoryBreakdownDto(string CategoryName, decimal Amount);

public record LowStockItemDto(int ProductSkuId, string SkuCode, string ProductName, int StockQuantity, int ReorderLevel);

public record TodaySessionDto(int PatientCycleId, string PatientName, string PlanName, string SessionLabel, string Status);

public record RecentActivityDto(DateTime CreatedAt, string Description);

public record TopPatientDto(int PatientId, string PatientName, decimal TotalPaid, int PaymentCount);

public record DashboardSummaryDto(
    int PatientCount,
    decimal TodayRevenue,
    decimal MonthRevenue,
    decimal OutstandingBalance,
    int ActivePatientCycleCount,
    int LowStockProductCount,
    int PendingPurchaseOrderCount,
    decimal MonthExpenses,
    IReadOnlyList<RevenuePointDto> RevenueTrend,
    IReadOnlyList<ExpenseCategoryBreakdownDto> ExpenseBreakdown,
    IReadOnlyList<LowStockItemDto> LowStockItems,
    IReadOnlyList<TodaySessionDto> TodaySessions,
    IReadOnlyList<RecentActivityDto> RecentActivity,
    IReadOnlyList<TopPatientDto> TopPatients);
