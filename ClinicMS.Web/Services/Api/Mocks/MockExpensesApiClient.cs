using ClinicMS.Web.Models.Api.Expenses;

namespace ClinicMS.Web.Services.Api.Mocks;

public class MockExpensesApiClient : IExpensesApiClient
{
    public Task<IReadOnlyList<ExpenseDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ExpenseDto>>(MockStore.Expenses.OrderByDescending(e => e.ExpenseDate).ToList());

    public Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var category = MockStore.ExpenseCategories.FirstOrDefault(c => c.Id == request.ExpenseCategoryId)
            ?? throw new ApiException(400, "Selected category does not exist.");
        var vendorName = request.VendorId is int vendorId
            ? MockStore.Vendors.FirstOrDefault(v => v.Id == vendorId)?.VendorName
            : null;
        var accountName = request.AccountId is int accountId
            ? MockStore.PaymentAccounts.FirstOrDefault(a => a.Id == accountId)?.Name
            : null;

        var expense = new ExpenseDto(
            MockStore.NextExpenseId++,
            category.Id,
            category.CategoryName,
            request.VendorId,
            vendorName,
            null,
            request.Title,
            request.Amount,
            request.ExpenseDate,
            request.PaymentMethod,
            request.ReceiptNumber,
            request.Notes,
            DateTime.UtcNow,
            request.AccountId,
            accountName);

        MockStore.Expenses.Add(expense);
        return Task.FromResult(expense);
    }

    public Task<IReadOnlyList<ExpenseCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ExpenseCategoryDto>>(MockStore.ExpenseCategories.ToList());

    public Task<ExpenseCategoryDto> CreateCategoryAsync(CreateExpenseCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = new ExpenseCategoryDto(MockStore.NextExpenseCategoryId++, request.CategoryName, request.Description, request.IsActive);
        MockStore.ExpenseCategories.Add(category);
        return Task.FromResult(category);
    }

    public Task<ExpenseCategoryDto> UpdateCategoryAsync(int id, UpdateExpenseCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.ExpenseCategories.FindIndex(c => c.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Category not found.");
        }

        var updated = new ExpenseCategoryDto(id, request.CategoryName, request.Description, request.IsActive);
        MockStore.ExpenseCategories[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = MockStore.ExpenseCategories.RemoveAll(c => c.Id == id);
        if (removed == 0)
        {
            throw new ApiException(404, "Category not found.");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VendorDto>> GetVendorsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<VendorDto>>(MockStore.Vendors.ToList());

    public Task<VendorDto> CreateVendorAsync(CreateVendorRequest request, CancellationToken cancellationToken = default)
    {
        var vendor = new VendorDto(MockStore.NextVendorId++, request.VendorName, request.ContactPerson, request.Phone, request.Email, request.IsActive);
        MockStore.Vendors.Add(vendor);
        return Task.FromResult(vendor);
    }

    public Task<VendorDto> UpdateVendorAsync(int id, UpdateVendorRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.Vendors.FindIndex(v => v.Id == id);
        if (index < 0) throw new ApiException(404, "Vendor not found.");

        var updated = new VendorDto(id, request.VendorName, request.ContactPerson, request.Phone, request.Email, request.IsActive);
        MockStore.Vendors[index] = updated;

        for (var i = 0; i < MockStore.RecurringExpenseSchedules.Count; i++)
        {
            var schedule = MockStore.RecurringExpenseSchedules[i];
            if (schedule.VendorId == id && schedule.VendorName != updated.VendorName)
            {
                MockStore.RecurringExpenseSchedules[i] = schedule with { VendorName = updated.VendorName };
            }
        }

        return Task.FromResult(updated);
    }

    public Task DeleteVendorAsync(int id, CancellationToken cancellationToken = default)
    {
        if (MockStore.RecurringExpenseSchedules.Any(s => s.VendorId == id))
        {
            throw new ApiException(400, "Cannot delete a vendor that has recurring expense schedules on record.");
        }
        if (MockStore.Vendors.RemoveAll(v => v.Id == id) == 0)
        {
            throw new ApiException(404, "Vendor not found.");
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RecurringExpenseDto>> GetRecurringSchedulesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<RecurringExpenseDto>>(MockStore.RecurringExpenseSchedules.OrderBy(s => s.NextDueDate).ToList());

    public Task<RecurringExpenseDto> CreateRecurringScheduleAsync(CreateRecurringExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var category = MockStore.ExpenseCategories.FirstOrDefault(c => c.Id == request.ExpenseCategoryId)
            ?? throw new ApiException(400, "Selected category does not exist.");
        var vendorName = request.VendorId is int vendorId
            ? MockStore.Vendors.FirstOrDefault(v => v.Id == vendorId)?.VendorName
            : null;

        var schedule = new RecurringExpenseDto(
            MockStore.NextRecurringExpenseId++, category.Id, category.CategoryName, request.VendorId, vendorName,
            request.Title, request.Amount, request.Frequency, request.NextDueDate, request.AutoGenerate, request.IsActive);
        MockStore.RecurringExpenseSchedules.Add(schedule);
        return Task.FromResult(schedule);
    }

    public Task<RecurringExpenseDto> UpdateRecurringScheduleAsync(int id, UpdateRecurringExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.RecurringExpenseSchedules.FindIndex(s => s.Id == id);
        if (index < 0) throw new ApiException(404, "Recurring expense schedule not found.");

        var category = MockStore.ExpenseCategories.FirstOrDefault(c => c.Id == request.ExpenseCategoryId)
            ?? throw new ApiException(400, "Selected category does not exist.");
        var vendorName = request.VendorId is int vendorId
            ? MockStore.Vendors.FirstOrDefault(v => v.Id == vendorId)?.VendorName
            : null;

        var updated = new RecurringExpenseDto(
            id, category.Id, category.CategoryName, request.VendorId, vendorName,
            request.Title, request.Amount, request.Frequency, request.NextDueDate, request.AutoGenerate, request.IsActive);
        MockStore.RecurringExpenseSchedules[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteRecurringScheduleAsync(int id, CancellationToken cancellationToken = default)
    {
        if (MockStore.RecurringExpenseSchedules.RemoveAll(s => s.Id == id) == 0)
        {
            throw new ApiException(404, "Recurring expense schedule not found.");
        }
        return Task.CompletedTask;
    }
}
