using ClinicMS.Web.Models.Api.Settings;

namespace ClinicMS.Web.Services.Api.Mocks;

public class MockSettingsApiClient : ISettingsApiClient
{
    public Task<ClinicSettingDto?> GetClinicSettingsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<ClinicSettingDto?>(MockStore.ClinicSettings);

    public Task<ClinicSettingDto> UpsertClinicSettingsAsync(UpsertClinicSettingRequest request, CancellationToken cancellationToken = default)
    {
        MockStore.ClinicSettings = MockStore.ClinicSettings with
        {
            ClinicName = request.ClinicName,
            LogoIconUrl = request.LogoIconUrl,
            LogoUrl = request.LogoUrl,
            SidebarLogoUrl = request.SidebarLogoUrl,
            ReportLogoUrl = request.ReportLogoUrl,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            VatPercentage = request.VatPercentage,
            IsVatEnabled = request.IsVatEnabled,
            CurrencySymbol = request.CurrencySymbol,
        };

        return Task.FromResult(MockStore.ClinicSettings);
    }

    public Task<PublicBrandingDto?> GetPublicBrandingAsync(CancellationToken cancellationToken = default)
    {
        var settings = MockStore.ClinicSettings;
        // The icon logo (not the general LogoUrl) is what login/OTP and the browser tab show.
        return Task.FromResult<PublicBrandingDto?>(new PublicBrandingDto(settings.ClinicName, settings.Email, settings.LogoIconUrl));
    }

    public Task<MerchantAccountDto?> GetMerchantAccountAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<MerchantAccountDto?>(MockStore.MerchantAccount);

    public Task<MerchantAccountDto> UpsertMerchantAccountAsync(UpsertMerchantAccountRequest request, CancellationToken cancellationToken = default)
    {
        MockStore.MerchantAccount = MockStore.MerchantAccount with
        {
            AccountHolderName = request.AccountHolderName,
            BankName = request.BankName,
            AccountNumber = request.AccountNumber,
            Iban = request.Iban,
            SwiftCode = request.SwiftCode,
            Branch = request.Branch,
        };

        return Task.FromResult(MockStore.MerchantAccount);
    }

    public Task<IReadOnlyList<DiscountDto>> GetDiscountsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DiscountDto>>(MockStore.Discounts.ToList());

    public Task<DiscountDto> CreateDiscountAsync(CreateDiscountRequest request, CancellationToken cancellationToken = default)
    {
        EnsureNoActiveOverlap(excludeId: null, request.StartDate, request.EndDate, request.IsActive);

        var discount = new DiscountDto(
            MockStore.NextDiscountId++,
            request.DiscountName,
            request.DiscountType,
            request.DiscountValue,
            request.StartDate,
            request.EndDate,
            request.IsActive);

        MockStore.Discounts.Add(discount);
        return Task.FromResult(discount);
    }

    public Task<DiscountDto> UpdateDiscountAsync(int id, UpdateDiscountRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.Discounts.FindIndex(d => d.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Discount not found.");
        }

        EnsureNoActiveOverlap(excludeId: id, request.StartDate, request.EndDate, request.IsActive);

        var updated = new DiscountDto(id, request.DiscountName, request.DiscountType, request.DiscountValue, request.StartDate, request.EndDate, request.IsActive);
        MockStore.Discounts[index] = updated;
        return Task.FromResult(updated);
    }

    /// <summary>Only one active discount may run at a time -- two active discounts' [StartDate,
    /// EndDate] ranges may never overlap, even by a single day. Inactive discounts are exempt
    /// since they aren't actually applied.</summary>
    private static void EnsureNoActiveOverlap(int? excludeId, DateOnly startDate, DateOnly endDate, bool isActive)
    {
        if (endDate < startDate)
        {
            throw new ApiException(400, "End date cannot be before start date.");
        }

        if (!isActive)
        {
            return;
        }

        var conflict = MockStore.Discounts.FirstOrDefault(d =>
            d.Id != excludeId &&
            d.IsActive &&
            startDate <= d.EndDate &&
            d.StartDate <= endDate);

        if (conflict is not null)
        {
            throw new ApiException(400,
                $"'{conflict.DiscountName}' is already active from {conflict.StartDate:MMM d, yyyy} to {conflict.EndDate:MMM d, yyyy}, which overlaps this date range.");
        }
    }

    public Task DeleteDiscountAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = MockStore.Discounts.RemoveAll(d => d.Id == id);
        if (removed == 0)
        {
            throw new ApiException(404, "Discount not found.");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PaymentAccountDto>> GetPaymentAccountsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PaymentAccountDto>>(MockStore.PaymentAccounts.ToList());

    public Task<PaymentAccountDto> CreatePaymentAccountAsync(CreatePaymentAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = new PaymentAccountDto(MockStore.NextPaymentAccountId++, request.Name, request.AccountType, request.PhoneOrAccountNumber, request.IsActive);
        MockStore.PaymentAccounts.Add(account);
        return Task.FromResult(account);
    }

    public Task<PaymentAccountDto> UpdatePaymentAccountAsync(int id, UpdatePaymentAccountRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.PaymentAccounts.FindIndex(a => a.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Payment account not found.");
        }

        var updated = new PaymentAccountDto(id, request.Name, request.AccountType, request.PhoneOrAccountNumber, request.IsActive);
        MockStore.PaymentAccounts[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeletePaymentAccountAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = MockStore.PaymentAccounts.RemoveAll(a => a.Id == id);
        if (removed == 0)
        {
            throw new ApiException(404, "Payment account not found.");
        }

        return Task.CompletedTask;
    }
}
