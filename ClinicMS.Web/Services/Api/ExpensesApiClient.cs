using ClinicMS.Web.Models.Api.Expenses;

namespace ClinicMS.Web.Services.Api;

public class ExpensesApiClient : ApiClientBase, IExpensesApiClient
{
    public ExpensesApiClient(HttpClient http) : base(http)
    {
    }

    public Task<IReadOnlyList<ExpenseDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ExpenseDto>>("api/expenses", cancellationToken);

    public Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<ExpenseDto>("api/expenses", request, cancellationToken);

    public Task<IReadOnlyList<ExpenseCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ExpenseCategoryDto>>("api/expensecategories", cancellationToken);

    public Task<ExpenseCategoryDto> CreateCategoryAsync(CreateExpenseCategoryRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<ExpenseCategoryDto>("api/expensecategories", request, cancellationToken);

    public Task<ExpenseCategoryDto> UpdateCategoryAsync(int id, UpdateExpenseCategoryRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<ExpenseCategoryDto>($"api/expensecategories/{id}", request, cancellationToken);

    public Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/expensecategories/{id}", cancellationToken);

    public Task<IReadOnlyList<VendorDto>> GetVendorsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<VendorDto>>("api/vendors", cancellationToken);
}
