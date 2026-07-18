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
}
