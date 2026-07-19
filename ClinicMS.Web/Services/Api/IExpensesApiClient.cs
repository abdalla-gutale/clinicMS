using ClinicMS.Web.Models.Api.Expenses;

namespace ClinicMS.Web.Services.Api;

public interface IExpensesApiClient
{
    Task<IReadOnlyList<ExpenseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<ExpenseCategoryDto> CreateCategoryAsync(CreateExpenseCategoryRequest request, CancellationToken cancellationToken = default);

    Task<ExpenseCategoryDto> UpdateCategoryAsync(int id, UpdateExpenseCategoryRequest request, CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VendorDto>> GetVendorsAsync(CancellationToken cancellationToken = default);

    Task<VendorDto> CreateVendorAsync(CreateVendorRequest request, CancellationToken cancellationToken = default);

    Task<VendorDto> UpdateVendorAsync(int id, UpdateVendorRequest request, CancellationToken cancellationToken = default);

    Task DeleteVendorAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecurringExpenseDto>> GetRecurringSchedulesAsync(CancellationToken cancellationToken = default);

    Task<RecurringExpenseDto> CreateRecurringScheduleAsync(CreateRecurringExpenseRequest request, CancellationToken cancellationToken = default);

    Task<RecurringExpenseDto> UpdateRecurringScheduleAsync(int id, UpdateRecurringExpenseRequest request, CancellationToken cancellationToken = default);

    Task DeleteRecurringScheduleAsync(int id, CancellationToken cancellationToken = default);
}
