using ClinicMS.Web.Models.Api.Settings;

namespace ClinicMS.Web.Services.Api;

public class SettingsApiClient : ApiClientBase, ISettingsApiClient
{
    public SettingsApiClient(HttpClient http) : base(http)
    {
    }

    public Task<ClinicSettingDto?> GetClinicSettingsAsync(CancellationToken cancellationToken = default) =>
        GetOrDefaultAsync<ClinicSettingDto>("api/clinicsettings", cancellationToken);

    public Task<ClinicSettingDto> UpsertClinicSettingsAsync(UpsertClinicSettingRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<ClinicSettingDto>("api/clinicsettings", request, cancellationToken);

    public Task<PublicBrandingDto?> GetPublicBrandingAsync(CancellationToken cancellationToken = default) =>
        GetOptionalAsync<PublicBrandingDto>("api/clinicsettings/public-branding", cancellationToken);

    public Task<MerchantAccountDto?> GetMerchantAccountAsync(CancellationToken cancellationToken = default) =>
        GetOrDefaultAsync<MerchantAccountDto>("api/clinicsettings/merchant-account", cancellationToken);

    public Task<MerchantAccountDto> UpsertMerchantAccountAsync(UpsertMerchantAccountRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<MerchantAccountDto>("api/clinicsettings/merchant-account", request, cancellationToken);

    public Task<IReadOnlyList<DiscountDto>> GetDiscountsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<DiscountDto>>("api/clinicsettings/discounts", cancellationToken);

    public Task<DiscountDto> CreateDiscountAsync(CreateDiscountRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<DiscountDto>("api/clinicsettings/discounts", request, cancellationToken);

    public Task<DiscountDto> UpdateDiscountAsync(int id, UpdateDiscountRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<DiscountDto>($"api/clinicsettings/discounts/{id}", request, cancellationToken);

    public Task DeleteDiscountAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/clinicsettings/discounts/{id}", cancellationToken);

    public Task<IReadOnlyList<PaymentAccountDto>> GetPaymentAccountsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<PaymentAccountDto>>("api/clinicsettings/payment-accounts", cancellationToken);

    public Task<PaymentAccountDto> CreatePaymentAccountAsync(CreatePaymentAccountRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<PaymentAccountDto>("api/clinicsettings/payment-accounts", request, cancellationToken);

    public Task<PaymentAccountDto> UpdatePaymentAccountAsync(int id, UpdatePaymentAccountRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<PaymentAccountDto>($"api/clinicsettings/payment-accounts/{id}", request, cancellationToken);

    public Task DeletePaymentAccountAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/clinicsettings/payment-accounts/{id}", cancellationToken);
}
