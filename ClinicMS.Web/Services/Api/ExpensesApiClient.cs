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

    public Task<VendorDto> CreateVendorAsync(CreateVendorRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<VendorDto>("api/vendors", request, cancellationToken);

    public Task<VendorDto> UpdateVendorAsync(int id, UpdateVendorRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<VendorDto>($"api/vendors/{id}", request, cancellationToken);

    public Task DeleteVendorAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/vendors/{id}", cancellationToken);

    public Task<IReadOnlyList<RecurringExpenseDto>> GetRecurringSchedulesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<RecurringExpenseDto>>("api/recurringexpenseschedules", cancellationToken);

    public Task<RecurringExpenseDto> CreateRecurringScheduleAsync(CreateRecurringExpenseRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<RecurringExpenseDto>("api/recurringexpenseschedules", request, cancellationToken);

    public Task<RecurringExpenseDto> UpdateRecurringScheduleAsync(int id, UpdateRecurringExpenseRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<RecurringExpenseDto>($"api/recurringexpenseschedules/{id}", request, cancellationToken);

    public Task DeleteRecurringScheduleAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/recurringexpenseschedules/{id}", cancellationToken);
}
