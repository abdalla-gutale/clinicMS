using ClinicMS.Web.Models.Api;

namespace ClinicMS.Web.Models.Api.Expenses;

public record VendorDto(int Id, string VendorName, string? ContactPerson, string? Phone, string? Email, bool IsActive);

public record CreateVendorRequest(string VendorName, string? ContactPerson, string? Phone, string? Email, bool IsActive);

public record UpdateVendorRequest(string VendorName, string? ContactPerson, string? Phone, string? Email, bool IsActive);

public enum RecurringFrequency
{
    Weekly,
    Monthly,
    Quarterly,
    Yearly
}

public record RecurringExpenseDto(
    int Id, int ExpenseCategoryId, string ExpenseCategoryName, int? VendorId, string? VendorName,
    string Title, decimal Amount, RecurringFrequency Frequency, DateOnly NextDueDate, bool AutoGenerate, bool IsActive);

public record CreateRecurringExpenseRequest(
    int ExpenseCategoryId, int? VendorId, string Title, decimal Amount, RecurringFrequency Frequency,
    DateOnly NextDueDate, bool AutoGenerate, bool IsActive);

public record UpdateRecurringExpenseRequest(
    int ExpenseCategoryId, int? VendorId, string Title, decimal Amount, RecurringFrequency Frequency,
    DateOnly NextDueDate, bool AutoGenerate, bool IsActive);

public record ExpenseCategoryDto(int Id, string CategoryName, string? Description, bool IsActive);

public record CreateExpenseCategoryRequest(string CategoryName, string? Description, bool IsActive);

public record UpdateExpenseCategoryRequest(string CategoryName, string? Description, bool IsActive);

public record ExpenseDto(
    int Id, int ExpenseCategoryId, string ExpenseCategoryName, int? VendorId, string? VendorName, int? ScheduleId,
    string Title, decimal Amount, DateOnly ExpenseDate, string PaymentMethod, string? ReceiptNumber, string? Notes, DateTime CreatedAt,
    int? AccountId, string? AccountName);

/// <summary>The Expenses list's header stats (all-time total, today's total, all-time count) are
/// unaffected by the current page/search -- they're computed once here rather than re-derived from
/// whatever page of rows happens to be loaded client-side.</summary>
public record ExpensesPageDto(PagedResult<ExpenseDto> Page, decimal AllTimeTotalAmount, decimal TodayTotalAmount, int AllTimeCount);

/// <summary>PaymentMethod is a plain string here (not an enum) to match the real API's
/// CreateExpenseDto exactly -- unlike most other domains, expenses take/return it as free-form text.</summary>
public record CreateExpenseRequest(
    int ExpenseCategoryId, int? VendorId, string Title, decimal Amount,
    DateOnly ExpenseDate, string PaymentMethod, string? ReceiptNumber, string? Notes, int? AccountId);

public record UpdateExpenseRequest(
    int ExpenseCategoryId, int? VendorId, string Title, decimal Amount,
    DateOnly ExpenseDate, string PaymentMethod, string? ReceiptNumber, string? Notes, int? AccountId);

/// <summary>Per-account monthly budget snapshot for the current calendar month, used by the Expense
/// page to show how much of each account's estimate is left before recording a new expense.</summary>
public record AccountBudgetSummaryDto(
    int AccountId, string AccountName, decimal MonthlyBudgetEstimate, decimal SpentThisMonth, decimal RemainingThisMonth);
