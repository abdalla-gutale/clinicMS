using ClinicMS.Web.Models.Api.Settings;

namespace ClinicMS.Web.Services.Api;

public interface ISettingsApiClient
{
    Task<ClinicSettingDto?> GetClinicSettingsAsync(CancellationToken cancellationToken = default);

    Task<ClinicSettingDto> UpsertClinicSettingsAsync(UpsertClinicSettingRequest request, CancellationToken cancellationToken = default);

    Task<PublicBrandingDto?> GetPublicBrandingAsync(CancellationToken cancellationToken = default);

    Task<MerchantAccountDto?> GetMerchantAccountAsync(CancellationToken cancellationToken = default);

    Task<MerchantAccountDto> UpsertMerchantAccountAsync(UpsertMerchantAccountRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscountDto>> GetDiscountsAsync(CancellationToken cancellationToken = default);

    Task<DiscountDto> CreateDiscountAsync(CreateDiscountRequest request, CancellationToken cancellationToken = default);

    Task<DiscountDto> UpdateDiscountAsync(int id, UpdateDiscountRequest request, CancellationToken cancellationToken = default);

    Task DeleteDiscountAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentAccountDto>> GetPaymentAccountsAsync(CancellationToken cancellationToken = default);

    Task<PaymentAccountDto> CreatePaymentAccountAsync(CreatePaymentAccountRequest request, CancellationToken cancellationToken = default);

    Task<PaymentAccountDto> UpdatePaymentAccountAsync(int id, UpdatePaymentAccountRequest request, CancellationToken cancellationToken = default);

    Task DeletePaymentAccountAsync(int id, CancellationToken cancellationToken = default);
}
