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
