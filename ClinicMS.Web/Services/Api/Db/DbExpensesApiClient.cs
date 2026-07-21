using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api;
using ClinicMS.Web.Models.Api.Expenses;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Services.Api.Db;

public class DbExpensesApiClient : IExpensesApiClient
{
    private readonly ClinicMsDbContext _db;

    public DbExpensesApiClient(ClinicMsDbContext db)
    {
        _db = db;
    }

    // ----- Expenses -----

    public async Task<IReadOnlyList<ExpenseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await GenerateDueRecurringExpensesAsync(cancellationToken);

        var expenses = await _db.Expenses.OrderByDescending(e => e.ExpenseDate).ToListAsync(cancellationToken);
        return await BuildExpenseDtosAsync(expenses, cancellationToken);
    }

    public async Task<ExpensesPageDto> GetPagedAsync(int page, int pageSize, string? search, string? category, string? paymentMethod, CancellationToken cancellationToken = default)
    {
        await GenerateDueRecurringExpensesAsync(cancellationToken);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Expenses.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            var matchingCategoryIds = await _db.ExpenseCategories.Where(c => c.CategoryName.ToLower().Contains(term)).Select(c => c.Id).ToListAsync(cancellationToken);
            var matchingVendorIds = await _db.Vendors.Where(v => v.VendorName.ToLower().Contains(term)).Select(v => v.Id).ToListAsync(cancellationToken);
            query = query.Where(e =>
                e.Title.ToLower().Contains(term) ||
                matchingCategoryIds.Contains(e.ExpenseCategoryId) ||
                (e.VendorId != null && matchingVendorIds.Contains(e.VendorId.Value)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var categoryId = await _db.ExpenseCategories.Where(c => c.CategoryName == category).Select(c => (int?)c.Id).FirstOrDefaultAsync(cancellationToken);
            query = categoryId.HasValue ? query.Where(e => e.ExpenseCategoryId == categoryId.Value) : query.Where(e => false);
        }

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            query = query.Where(e => e.PaymentMethod == paymentMethod);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var items = await BuildExpenseDtosAsync(entities, cancellationToken);

        var allTimeTotal = await _db.Expenses.SumAsync(e => e.Amount, cancellationToken);
        var allTimeCount = await _db.Expenses.CountAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayTotal = await _db.Expenses.Where(e => e.ExpenseDate == today).SumAsync(e => e.Amount, cancellationToken);

        return new ExpensesPageDto(new PagedResult<ExpenseDto>(items, page, pageSize, totalCount), allTimeTotal, todayTotal, allTimeCount);
    }

    public async Task<ExpenseDto> CreateAsync(CreateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == request.ExpenseCategoryId, cancellationToken)
            ?? throw new ApiException(400, "Selected category does not exist.");

        if (request.VendorId is int vendorId && !await _db.Vendors.AnyAsync(v => v.Id == vendorId, cancellationToken))
        {
            throw new ApiException(400, "Selected vendor does not exist.");
        }

        if (request.Amount <= 0)
        {
            throw new ApiException(400, "Amount must be greater than zero.");
        }

        if (request.AccountId is int accountId)
        {
            var account = await _db.PaymentAccounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken)
                ?? throw new ApiException(400, "Selected account does not exist.");
            await EnsureWithinBudgetAsync(account, request.Amount, request.ExpenseDate, excludeExpenseId: null, cancellationToken);
        }

        var entity = new ExpenseEntity
        {
            ExpenseCategoryId = category.Id,
            VendorId = request.VendorId,
            ScheduleId = null,
            AccountId = request.AccountId,
            Title = request.Title,
            Amount = request.Amount,
            ExpenseDate = request.ExpenseDate,
            PaymentMethod = request.PaymentMethod,
            ReceiptNumber = request.ReceiptNumber,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        _db.Expenses.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return (await BuildExpenseDtosAsync(new List<ExpenseEntity> { entity }, cancellationToken)).Single();
    }

    public async Task<ExpenseDto> UpdateAsync(int id, UpdateExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Expense not found.");

        var category = await _db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == request.ExpenseCategoryId, cancellationToken)
            ?? throw new ApiException(400, "Selected category does not exist.");

        if (request.VendorId is int vendorId && !await _db.Vendors.AnyAsync(v => v.Id == vendorId, cancellationToken))
        {
            throw new ApiException(400, "Selected vendor does not exist.");
        }

        if (request.Amount <= 0)
        {
            throw new ApiException(400, "Amount must be greater than zero.");
        }

        if (request.AccountId is int accountId)
        {
            var account = await _db.PaymentAccounts.FirstOrDefaultAsync(a => a.Id == accountId, cancellationToken)
                ?? throw new ApiException(400, "Selected account does not exist.");
            await EnsureWithinBudgetAsync(account, request.Amount, request.ExpenseDate, excludeExpenseId: id, cancellationToken);
        }

        entity.ExpenseCategoryId = category.Id;
        entity.VendorId = request.VendorId;
        entity.AccountId = request.AccountId;
        entity.Title = request.Title;
        entity.Amount = request.Amount;
        entity.ExpenseDate = request.ExpenseDate;
        entity.PaymentMethod = request.PaymentMethod;
        entity.ReceiptNumber = request.ReceiptNumber;
        entity.Notes = request.Notes;

        await _db.SaveChangesAsync(cancellationToken);
        return (await BuildExpenseDtosAsync(new List<ExpenseEntity> { entity }, cancellationToken)).Single();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Expense not found.");
        _db.Expenses.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccountBudgetSummaryDto>> GetBudgetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var accounts = await _db.PaymentAccounts.Where(a => a.IsActive).OrderBy(a => a.Id).ToListAsync(cancellationToken);
        var accountIds = accounts.Select(a => a.Id).ToList();

        var spentByAccount = await _db.Expenses
            .Where(e => e.AccountId != null && accountIds.Contains(e.AccountId.Value) && e.ExpenseDate >= monthStart && e.ExpenseDate < monthEnd)
            .GroupBy(e => e.AccountId!.Value)
            .Select(g => new { AccountId = g.Key, Spent = g.Sum(e => e.Amount) })
            .ToListAsync(cancellationToken);

        var spentLookup = spentByAccount.ToDictionary(x => x.AccountId, x => x.Spent);

        return accounts.Select(a =>
        {
            var spent = spentLookup.GetValueOrDefault(a.Id, 0m);
            return new AccountBudgetSummaryDto(a.Id, a.Name, a.MonthlyBudgetEstimate, spent, a.MonthlyBudgetEstimate - spent);
        }).ToList();
    }

    /// <summary>A MonthlyBudgetEstimate of 0 means no cap has been configured for this account yet --
    /// expenses against it are unrestricted until the clinic sets a real estimate in Account Setup.</summary>
    private async Task EnsureWithinBudgetAsync(PaymentAccountEntity account, decimal amount, DateOnly expenseDate, int? excludeExpenseId, CancellationToken cancellationToken)
    {
        if (account.MonthlyBudgetEstimate <= 0)
        {
            return;
        }

        var monthStart = new DateOnly(expenseDate.Year, expenseDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var spent = await _db.Expenses
            .Where(e => e.AccountId == account.Id && e.Id != (excludeExpenseId ?? -1) && e.ExpenseDate >= monthStart && e.ExpenseDate < monthEnd)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        if (spent + amount > account.MonthlyBudgetEstimate)
        {
            var remaining = account.MonthlyBudgetEstimate - spent;
            throw new ApiException(400,
                $"This expense exceeds {account.Name}'s remaining budget for {expenseDate:MMMM yyyy} (remaining: {remaining:0.00}, this expense: {amount:0.00}).");
        }
    }

    /// <summary>Recurring schedules have no account of their own (the real schema doesn't carry one),
    /// so auto-generated expenses start unassigned to any account/budget -- staff pick the account
    /// when they review the generated row, at which point budget validation kicks in via UpdateAsync.
    /// Called on every GetAllAsync so the Expense page always reflects newly-due schedules without a
    /// separate manual trigger; if the app was closed for a while, this catches up one expense per
    /// missed period rather than silently skipping to just the latest. Also exposed publicly so the
    /// UI can trigger it on demand (returns how many rows were generated).</summary>
    public async Task<int> GenerateDueRecurringExpensesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueSchedules = await _db.RecurringExpenseSchedules
            .Where(s => s.IsActive && s.AutoGenerate && s.NextDueDate <= today)
            .ToListAsync(cancellationToken);

        if (dueSchedules.Count == 0)
        {
            return 0;
        }

        var generatedCount = 0;
        foreach (var schedule in dueSchedules)
        {
            var guard = 0;
            while (schedule.NextDueDate <= today && guard++ < 500)
            {
                _db.Expenses.Add(new ExpenseEntity
                {
                    ExpenseCategoryId = schedule.ExpenseCategoryId,
                    VendorId = schedule.VendorId,
                    ScheduleId = schedule.Id,
                    AccountId = null,
                    Title = schedule.Title,
                    Amount = schedule.Amount,
                    ExpenseDate = schedule.NextDueDate,
                    PaymentMethod = "Unassigned",
                    ReceiptNumber = null,
                    Notes = "Auto-generated from recurring schedule.",
                    CreatedAt = DateTime.UtcNow,
                });
                generatedCount++;

                schedule.NextDueDate = AdvanceByFrequency(schedule.NextDueDate, schedule.Frequency);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return generatedCount;
    }

    private static DateOnly AdvanceByFrequency(DateOnly date, string frequency) => frequency switch
    {
        "Weekly" => date.AddDays(7),
        "Quarterly" => date.AddMonths(3),
        "Yearly" => date.AddYears(1),
        _ => date.AddMonths(1),
    };

    private async Task<List<ExpenseDto>> BuildExpenseDtosAsync(List<ExpenseEntity> expenses, CancellationToken cancellationToken)
    {
        if (expenses.Count == 0)
        {
            return new List<ExpenseDto>();
        }

        var categoryIds = expenses.Select(e => e.ExpenseCategoryId).Distinct().ToList();
        var vendorIds = expenses.Where(e => e.VendorId.HasValue).Select(e => e.VendorId!.Value).Distinct().ToList();
        var accountIds = expenses.Where(e => e.AccountId.HasValue).Select(e => e.AccountId!.Value).Distinct().ToList();

        var categories = await _db.ExpenseCategories.Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        var vendors = vendorIds.Count > 0
            ? await _db.Vendors.Where(v => vendorIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, cancellationToken)
            : new Dictionary<int, VendorEntity>();
        var accounts = accountIds.Count > 0
            ? await _db.PaymentAccounts.Where(a => accountIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, cancellationToken)
            : new Dictionary<int, PaymentAccountEntity>();

        return expenses.Select(e => new ExpenseDto(
            e.Id,
            e.ExpenseCategoryId,
            categories.GetValueOrDefault(e.ExpenseCategoryId)?.CategoryName ?? "",
            e.VendorId,
            e.VendorId is int vId ? vendors.GetValueOrDefault(vId)?.VendorName : null,
            e.ScheduleId,
            e.Title,
            e.Amount,
            e.ExpenseDate,
            e.PaymentMethod,
            e.ReceiptNumber,
            e.Notes,
            e.CreatedAt,
            e.AccountId,
            e.AccountId is int aId ? accounts.GetValueOrDefault(aId)?.Name : null)).ToList();
    }

    // ----- Expense Categories -----

    public async Task<IReadOnlyList<ExpenseCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _db.ExpenseCategories.OrderBy(c => c.Id).ToListAsync(cancellationToken);
        return categories.Select(ToDto).ToList();
    }

    public async Task<ExpenseCategoryDto> CreateCategoryAsync(CreateExpenseCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new ExpenseCategoryEntity { CategoryName = request.CategoryName, Description = request.Description, IsActive = request.IsActive };
        _db.ExpenseCategories.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<ExpenseCategoryDto> UpdateCategoryAsync(int id, UpdateExpenseCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Category not found.");

        entity.CategoryName = request.CategoryName;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.Expenses.AnyAsync(e => e.ExpenseCategoryId == id, cancellationToken) ||
            await _db.RecurringExpenseSchedules.AnyAsync(s => s.ExpenseCategoryId == id, cancellationToken))
        {
            throw new ApiException(400, "Cannot delete a category that still has expenses or recurring schedules on record.");
        }

        var entity = await _db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Category not found.");
        _db.ExpenseCategories.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Vendors -----

    public async Task<IReadOnlyList<VendorDto>> GetVendorsAsync(CancellationToken cancellationToken = default)
    {
        var vendors = await _db.Vendors.OrderBy(v => v.Id).ToListAsync(cancellationToken);
        return vendors.Select(ToDto).ToList();
    }

    public async Task<VendorDto> CreateVendorAsync(CreateVendorRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new VendorEntity
        {
            VendorName = request.VendorName,
            ContactPerson = request.ContactPerson,
            Phone = request.Phone,
            Email = request.Email,
            IsActive = request.IsActive,
        };
        _db.Vendors.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<VendorDto> UpdateVendorAsync(int id, UpdateVendorRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Vendor not found.");

        entity.VendorName = request.VendorName;
        entity.ContactPerson = request.ContactPerson;
        entity.Phone = request.Phone;
        entity.Email = request.Email;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteVendorAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.RecurringExpenseSchedules.AnyAsync(s => s.VendorId == id, cancellationToken))
        {
            throw new ApiException(400, "Cannot delete a vendor that has recurring expense schedules on record.");
        }

        var entity = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Vendor not found.");
        _db.Vendors.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Recurring Expense Schedules -----

    public async Task<IReadOnlyList<RecurringExpenseDto>> GetRecurringSchedulesAsync(CancellationToken cancellationToken = default)
    {
        var schedules = await _db.RecurringExpenseSchedules.OrderBy(s => s.NextDueDate).ToListAsync(cancellationToken);
        var categoryIds = schedules.Select(s => s.ExpenseCategoryId).Distinct().ToList();
        var vendorIds = schedules.Where(s => s.VendorId.HasValue).Select(s => s.VendorId!.Value).Distinct().ToList();

        var categories = await _db.ExpenseCategories.Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        var vendors = vendorIds.Count > 0
            ? await _db.Vendors.Where(v => vendorIds.Contains(v.Id)).ToDictionaryAsync(v => v.Id, cancellationToken)
            : new Dictionary<int, VendorEntity>();

        return schedules.Select(s => ToDto(s, categories.GetValueOrDefault(s.ExpenseCategoryId)?.CategoryName ?? "",
            s.VendorId is int vId ? vendors.GetValueOrDefault(vId)?.VendorName : null)).ToList();
    }

    public async Task<RecurringExpenseDto> CreateRecurringScheduleAsync(CreateRecurringExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == request.ExpenseCategoryId, cancellationToken)
            ?? throw new ApiException(400, "Selected category does not exist.");

        VendorEntity? vendor = null;
        if (request.VendorId is int vendorId)
        {
            vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId, cancellationToken)
                ?? throw new ApiException(400, "Selected vendor does not exist.");
        }

        if (request.Amount <= 0)
        {
            throw new ApiException(400, "Amount must be greater than zero.");
        }

        var entity = new RecurringExpenseScheduleEntity
        {
            ExpenseCategoryId = category.Id,
            VendorId = request.VendorId,
            Title = request.Title,
            Amount = request.Amount,
            Frequency = request.Frequency.ToString(),
            NextDueDate = request.NextDueDate,
            AutoGenerate = request.AutoGenerate,
            IsActive = request.IsActive,
        };
        _db.RecurringExpenseSchedules.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity, category.CategoryName, vendor?.VendorName);
    }

    public async Task<RecurringExpenseDto> UpdateRecurringScheduleAsync(int id, UpdateRecurringExpenseRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.RecurringExpenseSchedules.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Recurring expense schedule not found.");

        var category = await _db.ExpenseCategories.FirstOrDefaultAsync(c => c.Id == request.ExpenseCategoryId, cancellationToken)
            ?? throw new ApiException(400, "Selected category does not exist.");

        VendorEntity? vendor = null;
        if (request.VendorId is int vendorId)
        {
            vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId, cancellationToken)
                ?? throw new ApiException(400, "Selected vendor does not exist.");
        }

        if (request.Amount <= 0)
        {
            throw new ApiException(400, "Amount must be greater than zero.");
        }

        entity.ExpenseCategoryId = category.Id;
        entity.VendorId = request.VendorId;
        entity.Title = request.Title;
        entity.Amount = request.Amount;
        entity.Frequency = request.Frequency.ToString();
        entity.NextDueDate = request.NextDueDate;
        entity.AutoGenerate = request.AutoGenerate;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity, category.CategoryName, vendor?.VendorName);
    }

    public async Task DeleteRecurringScheduleAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.RecurringExpenseSchedules.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Recurring expense schedule not found.");

        // Historical expenses already generated from this schedule stay on record -- only detach
        // the link so they don't dangle on a schedule id that no longer exists.
        var linkedExpenses = await _db.Expenses.Where(e => e.ScheduleId == id).ToListAsync(cancellationToken);
        foreach (var expense in linkedExpenses)
        {
            expense.ScheduleId = null;
        }

        _db.RecurringExpenseSchedules.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static ExpenseCategoryDto ToDto(ExpenseCategoryEntity e) => new(e.Id, e.CategoryName, e.Description, e.IsActive);

    private static VendorDto ToDto(VendorEntity e) => new(e.Id, e.VendorName, e.ContactPerson, e.Phone, e.Email, e.IsActive);

    private static RecurringExpenseDto ToDto(RecurringExpenseScheduleEntity e, string categoryName, string? vendorName) => new(
        e.Id, e.ExpenseCategoryId, categoryName, e.VendorId, vendorName, e.Title, e.Amount,
        Enum.TryParse<RecurringFrequency>(e.Frequency, out var freq) ? freq : RecurringFrequency.Monthly,
        e.NextDueDate, e.AutoGenerate, e.IsActive);
}
