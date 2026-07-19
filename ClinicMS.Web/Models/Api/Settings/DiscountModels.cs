namespace ClinicMS.Web.Models.Api.Settings;

/// <summary>What a discount applies to -- services, products, or both.</summary>
public enum DiscountType
{
    ServiceOnly,
    ProductOnly,
    Both
}

public record DiscountDto(
    int Id,
    string DiscountName,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive);

public record CreateDiscountRequest(
    string DiscountName,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive);

public record UpdateDiscountRequest(
    string DiscountName,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive);
