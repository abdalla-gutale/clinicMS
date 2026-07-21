using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Expenses;
using ClinicMS.Web.Services.Api;
using ClinicMS.Web.Services.Api.Db;
using Xunit;

namespace ClinicMS.Web.Tests;

public class ExpensesBudgetTests
{
    private static async Task<(ClinicMsDbContext Db, DbExpensesApiClient Client, int CategoryId, int AccountId)> SeedAsync(decimal monthlyBudget)
    {
        var db = TestDb.Create();
        var category = new ExpenseCategoryEntity { CategoryName = "Rent", IsActive = true };
        var account = new PaymentAccountEntity { Name = "Main Cash", AccountType = "Cash", MonthlyBudgetEstimate = monthlyBudget, IsActive = true };
        db.ExpenseCategories.Add(category);
        db.PaymentAccounts.Add(account);
        await db.SaveChangesAsync();

        return (db, new DbExpensesApiClient(db), category.Id, account.Id);
    }

    [Fact]
    public async Task CreateAsync_WithinBudget_Succeeds()
    {
        var (db, client, categoryId, accountId) = await SeedAsync(1000m);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var expense = await client.CreateAsync(new CreateExpenseRequest(categoryId, null, "Electricity", 600m, today, "Cash", null, null, accountId), default);

        Assert.Equal(600m, expense.Amount);
        db.Dispose();
    }

    [Fact]
    public async Task CreateAsync_ExceedingRemainingBudget_Throws()
    {
        var (db, client, categoryId, accountId) = await SeedAsync(1000m);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await client.CreateAsync(new CreateExpenseRequest(categoryId, null, "Electricity", 600m, today, "Cash", null, null, accountId), default);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            client.CreateAsync(new CreateExpenseRequest(categoryId, null, "Water", 500m, today, "Cash", null, null, accountId), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task CreateAsync_ExactlyAtRemainingBudget_Succeeds()
    {
        var (db, client, categoryId, accountId) = await SeedAsync(1000m);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        await client.CreateAsync(new CreateExpenseRequest(categoryId, null, "Electricity", 600m, today, "Cash", null, null, accountId), default);
        var second = await client.CreateAsync(new CreateExpenseRequest(categoryId, null, "Water", 400m, today, "Cash", null, null, accountId), default);

        Assert.Equal(400m, second.Amount);
        db.Dispose();
    }

    [Fact]
    public async Task CreateAsync_ZeroBudget_IsUnrestricted()
    {
        var (db, client, categoryId, accountId) = await SeedAsync(0m);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var expense = await client.CreateAsync(new CreateExpenseRequest(categoryId, null, "Big purchase", 999_999m, today, "Cash", null, null, accountId), default);

        Assert.Equal(999_999m, expense.Amount);
        db.Dispose();
    }

    [Fact]
    public async Task CreateAsync_NoAccountSelected_SkipsBudgetCheckEntirely()
    {
        var (db, client, categoryId, _) = await SeedAsync(100m);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var expense = await client.CreateAsync(new CreateExpenseRequest(categoryId, null, "Unassigned expense", 5000m, today, "Cash", null, null, null), default);

        Assert.Equal(5000m, expense.Amount);
        Assert.Null(expense.AccountId);
        db.Dispose();
    }

    [Fact]
    public async Task UpdateAsync_ExcludesItsOwnPriorAmountFromTheBudgetCheck()
    {
        var (db, client, categoryId, accountId) = await SeedAsync(1000m);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var expense = await client.CreateAsync(new CreateExpenseRequest(categoryId, null, "Electricity", 600m, today, "Cash", null, null, accountId), default);

        // Raising this same expense to 900 should be allowed (900 total, not 600+900) since the
        // budget check must exclude the expense being edited, not double-count it.
        var updated = await client.UpdateAsync(expense.Id, new UpdateExpenseRequest(categoryId, null, "Electricity", 900m, today, "Cash", null, null, accountId), default);

        Assert.Equal(900m, updated.Amount);
        db.Dispose();
    }
}
