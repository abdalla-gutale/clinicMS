namespace ClinicMS.Web.Models.Api.Expenses;

public record VendorDto(int Id, string VendorName, string? ContactPerson, string? Phone, string? Email, bool IsActive);

public record ExpenseCategoryDto(int Id, string CategoryName, string? Description, bool IsActive);

public record CreateExpenseCategoryRequest(string CategoryName, string? Description, bool IsActive);

public record UpdateExpenseCategoryRequest(string CategoryName, string? Description, bool IsActive);

public record ExpenseDto(
    int Id, int ExpenseCategoryId, string ExpenseCategoryName, int? VendorId, string? VendorName, int? ScheduleId,
    string Title, decimal Amount, DateOnly ExpenseDate, string PaymentMethod, string? ReceiptNumber, string? Notes, DateTime CreatedAt);

/// <summary>PaymentMethod is a plain string here (not an enum) to match the real API's
/// CreateExpenseDto exactly -- unlike most other domains, expenses take/return it as free-form text.</summary>
public record CreateExpenseRequest(
    int ExpenseCategoryId, int? VendorId, string Title, decimal Amount,
    DateOnly ExpenseDate, string PaymentMethod, string? ReceiptNumber, string? Notes);
