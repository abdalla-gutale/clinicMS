namespace ClinicMS.Web.Models.Api.Dashboard;

public record DashboardSummaryDto(
    int PatientCount,
    decimal TodayRevenue,
    decimal MonthRevenue,
    decimal OutstandingBalance,
    int ActivePatientCycleCount,
    int LowStockProductCount,
    int PendingPurchaseOrderCount,
    decimal MonthExpenses);
