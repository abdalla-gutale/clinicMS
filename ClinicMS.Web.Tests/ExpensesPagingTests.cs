using ClinicMS.Web.Data;
using ClinicMS.Web.Services.Api.Db;
using Xunit;

namespace ClinicMS.Web.Tests;

public class ExpensesPagingTests
{
    private static async Task<DbExpensesApiClient> SeedAsync(int count)
    {
        var db = TestDb.Create();
        var category = new ExpenseCategoryEntity { CategoryName = "Utilities", IsActive = true };
        db.ExpenseCategories.Add(category);
        await db.SaveChangesAsync();

        for (var i = 1; i <= count; i++)
        {
            db.Expenses.Add(new ExpenseEntity
            {
                ExpenseCategoryId = category.Id,
                Title = $"Expense {i:D3}",
                Amount = 10m,
                ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
                PaymentMethod = "Cash",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
            });
        }
        await db.SaveChangesAsync();
        return new DbExpensesApiClient(db);
    }

    [Fact]
    public async Task GetPaged_ReturnsRequestedPageSizeAndTotalCount()
    {
        var client = await SeedAsync(25);

        var result = await client.GetPagedAsync(1, 10, null, null, null, default);

        Assert.Equal(10, result.Page.Items.Count);
        Assert.Equal(25, result.Page.TotalCount);
        Assert.Equal(25, result.AllTimeCount);
    }

    [Fact]
    public async Task GetPaged_AllTimeTotalReflectsEveryExpenseNotJustCurrentPage()
    {
        var client = await SeedAsync(25);

        var result = await client.GetPagedAsync(1, 10, null, null, null, default);

        Assert.Equal(250m, result.AllTimeTotalAmount);
    }

    [Fact]
    public async Task GetPaged_TodayTotalOnlySumsExpensesDatedToday()
    {
        var db = TestDb.Create();
        var category = new ExpenseCategoryEntity { CategoryName = "Utilities", IsActive = true };
        db.ExpenseCategories.Add(category);
        await db.SaveChangesAsync();

        db.Expenses.Add(new ExpenseEntity { ExpenseCategoryId = category.Id, Title = "Today Expense", Amount = 40m, ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow), PaymentMethod = "Cash", CreatedAt = DateTime.UtcNow });
        db.Expenses.Add(new ExpenseEntity { ExpenseCategoryId = category.Id, Title = "Yesterday Expense", Amount = 15m, ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), PaymentMethod = "Cash", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var client = new DbExpensesApiClient(db);
        var result = await client.GetPagedAsync(1, 10, null, null, null, default);

        Assert.Equal(40m, result.TodayTotalAmount);
    }

    [Fact]
    public async Task GetPaged_SearchFiltersByTitle()
    {
        var client = await SeedAsync(5);

        var result = await client.GetPagedAsync(1, 10, "003", null, null, default);

        var only = Assert.Single(result.Page.Items);
        Assert.Equal("Expense 003", only.Title);
    }

    [Fact]
    public async Task GetPaged_CategoryFilterNarrowsResults()
    {
        var db = TestDb.Create();
        var utilities = new ExpenseCategoryEntity { CategoryName = "Utilities", IsActive = true };
        var supplies = new ExpenseCategoryEntity { CategoryName = "Supplies", IsActive = true };
        db.ExpenseCategories.AddRange(utilities, supplies);
        await db.SaveChangesAsync();

        db.Expenses.Add(new ExpenseEntity { ExpenseCategoryId = utilities.Id, Title = "Electric bill", Amount = 20m, ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow), PaymentMethod = "Cash", CreatedAt = DateTime.UtcNow });
        db.Expenses.Add(new ExpenseEntity { ExpenseCategoryId = supplies.Id, Title = "Gloves", Amount = 30m, ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow), PaymentMethod = "Cash", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var client = new DbExpensesApiClient(db);
        var result = await client.GetPagedAsync(1, 10, null, "Supplies", null, default);

        var only = Assert.Single(result.Page.Items);
        Assert.Equal("Gloves", only.Title);
    }

    [Fact]
    public async Task GetPaged_PaymentMethodFilterNarrowsResults()
    {
        var db = TestDb.Create();
        var category = new ExpenseCategoryEntity { CategoryName = "Utilities", IsActive = true };
        db.ExpenseCategories.Add(category);
        await db.SaveChangesAsync();

        db.Expenses.Add(new ExpenseEntity { ExpenseCategoryId = category.Id, Title = "Cash expense", Amount = 20m, ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow), PaymentMethod = "Cash", CreatedAt = DateTime.UtcNow });
        db.Expenses.Add(new ExpenseEntity { ExpenseCategoryId = category.Id, Title = "Card expense", Amount = 30m, ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow), PaymentMethod = "CreditCard", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var client = new DbExpensesApiClient(db);
        var result = await client.GetPagedAsync(1, 10, null, null, "CreditCard", default);

        var only = Assert.Single(result.Page.Items);
        Assert.Equal("Card expense", only.Title);
    }
}
