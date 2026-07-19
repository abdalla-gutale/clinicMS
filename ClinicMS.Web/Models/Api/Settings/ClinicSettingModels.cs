namespace ClinicMS.Web.Models.Api.Settings;

public record ClinicSettingDto(
    int Id,
    string ClinicName,
    string? LogoIconUrl,
    string? LogoUrl,
    string? SidebarLogoUrl,
    string? ReportLogoUrl,
    string? Phone,
    string? Email,
    string? Address,
    decimal VatPercentage,
    bool IsVatEnabled,
    string CurrencySymbol);

/// <summary>Safe-for-anonymous subset shown on the login/OTP pages before the user has a token.</summary>
public record PublicBrandingDto(string ClinicName, string? Email, string? LogoUrl);

public record UpsertClinicSettingRequest(
    string ClinicName,
    string? LogoIconUrl,
    string? LogoUrl,
    string? SidebarLogoUrl,
    string? ReportLogoUrl,
    string? Phone,
    string? Email,
    string? Address,
    decimal VatPercentage,
    bool IsVatEnabled,
    string CurrencySymbol);

/// <summary>Bank details a clinic gives out to receive payments -- not a payment gateway
/// integration, just the account info shown/printed for bank transfers.</summary>
public record MerchantAccountDto(
    int Id,
    string AccountHolderName,
    string BankName,
    string AccountNumber,
    string? Iban,
    string? SwiftCode,
    string? Branch);

public record UpsertMerchantAccountRequest(
    string AccountHolderName,
    string BankName,
    string AccountNumber,
    string? Iban,
    string? SwiftCode,
    string? Branch);
