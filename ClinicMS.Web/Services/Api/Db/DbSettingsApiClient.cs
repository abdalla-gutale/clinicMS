using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Settings;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Services.Api.Db;

public class DbSettingsApiClient : ISettingsApiClient
{
    private readonly ClinicMsDbContext _db;

    public DbSettingsApiClient(ClinicMsDbContext db)
    {
        _db = db;
    }

    public async Task<ClinicSettingDto?> GetClinicSettingsAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _db.ClinicSettings.FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<ClinicSettingDto> UpsertClinicSettingsAsync(UpsertClinicSettingRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ClinicSettings.FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            entity = new ClinicSettingEntity();
            _db.ClinicSettings.Add(entity);
        }

        entity.ClinicName = request.ClinicName;
        entity.LogoIconUrl = request.LogoIconUrl;
        entity.LogoUrl = request.LogoUrl;
        entity.SidebarLogoUrl = request.SidebarLogoUrl;
        entity.ReportLogoUrl = request.ReportLogoUrl;
        entity.Phone = request.Phone;
        entity.Email = request.Email;
        entity.Address = request.Address;
        entity.VatPercentage = request.VatPercentage;
        entity.IsVatEnabled = request.IsVatEnabled;
        entity.CurrencySymbol = request.CurrencySymbol;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<PublicBrandingDto?> GetPublicBrandingAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _db.ClinicSettings.FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : new PublicBrandingDto(entity.ClinicName, entity.Email, entity.LogoIconUrl);
    }

    public async Task<MerchantAccountDto?> GetMerchantAccountAsync(CancellationToken cancellationToken = default)
    {
        var entity = await _db.MerchantAccounts.FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<MerchantAccountDto> UpsertMerchantAccountAsync(UpsertMerchantAccountRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.MerchantAccounts.FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            entity = new MerchantAccountEntity();
            _db.MerchantAccounts.Add(entity);
        }

        entity.AccountHolderName = request.AccountHolderName;
        entity.BankName = request.BankName;
        entity.AccountNumber = request.AccountNumber;
        entity.Iban = request.Iban;
        entity.SwiftCode = request.SwiftCode;
        entity.Branch = request.Branch;

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<DiscountDto>> GetDiscountsAsync(CancellationToken cancellationToken = default)
    {
        var discounts = await _db.Discounts.ToListAsync(cancellationToken);
        return discounts.Select(ToDto).ToList();
    }

    public async Task<DiscountDto> CreateDiscountAsync(CreateDiscountRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureNoActiveOverlapAsync(excludeId: null, request.StartDate, request.EndDate, request.IsActive, cancellationToken);

        var entity = new DiscountEntity
        {
            DiscountName = request.DiscountName,
            DiscountType = request.DiscountType.ToString(),
            DiscountValue = request.DiscountValue,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.IsActive,
        };

        _db.Discounts.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<DiscountDto> UpdateDiscountAsync(int id, UpdateDiscountRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Discounts.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Discount not found.");

        await EnsureNoActiveOverlapAsync(excludeId: id, request.StartDate, request.EndDate, request.IsActive, cancellationToken);

        entity.DiscountName = request.DiscountName;
        entity.DiscountType = request.DiscountType.ToString();
        entity.DiscountValue = request.DiscountValue;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    /// <summary>Only one active discount may run at a time -- two active discounts' [StartDate,
    /// EndDate] ranges may never overlap, even by a single day. Inactive discounts are exempt
    /// since they aren't actually applied.</summary>
    private async Task EnsureNoActiveOverlapAsync(int? excludeId, DateOnly startDate, DateOnly endDate, bool isActive, CancellationToken cancellationToken)
    {
        if (endDate < startDate)
        {
            throw new ApiException(400, "End date cannot be before start date.");
        }

        if (!isActive)
        {
            return;
        }

        var conflict = await _db.Discounts.FirstOrDefaultAsync(d =>
            d.Id != (excludeId ?? -1) &&
            d.IsActive &&
            startDate <= d.EndDate &&
            d.StartDate <= endDate,
            cancellationToken);

        if (conflict is not null)
        {
            throw new ApiException(400,
                $"'{conflict.DiscountName}' is already active from {conflict.StartDate:MMM d, yyyy} to {conflict.EndDate:MMM d, yyyy}, which overlaps this date range.");
        }
    }

    public async Task DeleteDiscountAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Discounts.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Discount not found.");
        _db.Discounts.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentAccountDto>> GetPaymentAccountsAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _db.PaymentAccounts.OrderBy(a => a.Id).ToListAsync(cancellationToken);
        return accounts.Select(ToDto).ToList();
    }

    public async Task<PaymentAccountDto> CreatePaymentAccountAsync(CreatePaymentAccountRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePaymentAccount(request.AccountType, request.AccountTypeSub, request.Number, request.MonthlyBudgetEstimate);

        var accountTypeSub = request.AccountType == PaymentAccountType.MasterCard ? PaymentAccountTypeSub.None : request.AccountTypeSub;
        var entity = new PaymentAccountEntity
        {
            Name = request.Name,
            AccountType = request.AccountType.ToString(),
            AccountTypeSub = accountTypeSub.ToString(),
            Number = RequiresNumber(request.AccountType, accountTypeSub) ? request.Number : null,
            MonthlyBudgetEstimate = request.MonthlyBudgetEstimate,
            IsActive = request.IsActive,
        };

        _db.PaymentAccounts.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<PaymentAccountDto> UpdatePaymentAccountAsync(int id, UpdatePaymentAccountRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.PaymentAccounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Payment account not found.");

        ValidatePaymentAccount(request.AccountType, request.AccountTypeSub, request.Number, request.MonthlyBudgetEstimate);

        var accountTypeSub = request.AccountType == PaymentAccountType.MasterCard ? PaymentAccountTypeSub.None : request.AccountTypeSub;
        entity.Name = request.Name;
        entity.AccountType = request.AccountType.ToString();
        entity.AccountTypeSub = accountTypeSub.ToString();
        entity.Number = RequiresNumber(request.AccountType, accountTypeSub) ? request.Number : null;
        entity.MonthlyBudgetEstimate = request.MonthlyBudgetEstimate;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeletePaymentAccountAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.PaymentAccounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Payment account not found.");

        if (await _db.Expenses.AnyAsync(e => e.AccountId == id, cancellationToken))
        {
            throw new ApiException(400, "Cannot delete an account that already has expenses recorded against it.");
        }

        _db.PaymentAccounts.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>MasterCard always needs a card number. Cash/Merchant only need a number when tied to
    /// a mobile-money carrier (Evc/Zaad/Somtel) so the payment can actually be traced back to a real
    /// wallet -- a plain cash drawer or generic merchant terminal (sub = None) needs none.</summary>
    private static bool RequiresNumber(PaymentAccountType accountType, PaymentAccountTypeSub accountTypeSub) =>
        accountType == PaymentAccountType.MasterCard || accountTypeSub != PaymentAccountTypeSub.None;

    private static void ValidatePaymentAccount(PaymentAccountType accountType, PaymentAccountTypeSub accountTypeSub, string? number, decimal monthlyBudgetEstimate)
    {
        if (RequiresNumber(accountType, accountTypeSub) && string.IsNullOrWhiteSpace(number))
        {
            var label = accountType == PaymentAccountType.MasterCard ? "MasterCard" : accountTypeSub.ToString();
            throw new ApiException(400, $"A number is required for {label} accounts.");
        }

        if (monthlyBudgetEstimate < 0)
        {
            throw new ApiException(400, "Monthly budget estimate cannot be negative.");
        }
    }

    private static PaymentAccountDto ToDto(PaymentAccountEntity e) => new(
        e.Id,
        e.Name,
        Enum.TryParse<PaymentAccountType>(e.AccountType, out var type) ? type : PaymentAccountType.Cash,
        Enum.TryParse<PaymentAccountTypeSub>(e.AccountTypeSub, out var sub) ? sub : PaymentAccountTypeSub.None,
        e.Number,
        e.MonthlyBudgetEstimate,
        e.IsActive);

    private static ClinicSettingDto ToDto(ClinicSettingEntity e) => new(
        e.Id, e.ClinicName, e.LogoIconUrl, e.LogoUrl, e.SidebarLogoUrl, e.ReportLogoUrl,
        e.Phone, e.Email, e.Address, e.VatPercentage, e.IsVatEnabled, e.CurrencySymbol);

    private static MerchantAccountDto ToDto(MerchantAccountEntity e) => new(
        e.Id, e.AccountHolderName, e.BankName, e.AccountNumber, e.Iban, e.SwiftCode, e.Branch);

    private static DiscountDto ToDto(DiscountEntity e) => new(
        e.Id,
        e.DiscountName,
        Enum.TryParse<DiscountType>(e.DiscountType, out var type) ? type : DiscountType.Both,
        e.DiscountValue,
        e.StartDate ?? default,
        e.EndDate ?? default,
        e.IsActive);
}
