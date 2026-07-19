namespace ClinicMS.Web.Data;

public class ClinicSettingEntity
{
    public int Id { get; set; }
    public string ClinicName { get; set; } = "";
    public string? LogoIconUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? SidebarLogoUrl { get; set; }
    public string? ReportLogoUrl { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal VatPercentage { get; set; }
    public bool IsVatEnabled { get; set; }
    public string CurrencySymbol { get; set; } = "AED";
    public DateTime? UpdatedAt { get; set; }
}

public class MerchantAccountEntity
{
    public int Id { get; set; }
    public string AccountHolderName { get; set; } = "";
    public string BankName { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string? Iban { get; set; }
    public string? SwiftCode { get; set; }
    public string? Branch { get; set; }
}

public class PatientEntity
{
    public int Id { get; set; }
    public string? PatientCode { get; set; }
    public string FullName { get; set; } = "";
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string Phone { get; set; } = "";
    public string? Email { get; set; }
    public decimal CurrentWalletCredit { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ServiceTypeEntity
{
    public int Id { get; set; }
    public string TypeName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class ServiceEntity
{
    public int Id { get; set; }
    public int ServiceTypeId { get; set; }
    public string ServiceName { get; set; } = "";
    public string? Description { get; set; }
    public decimal StandardPrice { get; set; }
    public bool IsActive { get; set; }
}

public class TreatmentPlanEntity
{
    public int Id { get; set; }
    public string PlanName { get; set; } = "";
    public string PricingModel { get; set; } = "";
    public decimal? FixedPackagePrice { get; set; }
    public int TotalSessions { get; set; }
    public string Frequency { get; set; } = "";
    public bool IsActive { get; set; }
}

public class TreatmentPlanItemEntity
{
    public int Id { get; set; }
    public int TreatmentPlanId { get; set; }
    public string ItemType { get; set; } = "";
    public int? ServiceId { get; set; }
    public int? ProductSkuId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int SessionNumber { get; set; }
}

public class PatientCycleEntity
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int PlanId { get; set; }
    public string CycleName { get; set; } = "";
    public string PricingModel { get; set; } = "";
    public decimal? AgreedTotalPrice { get; set; }
    public string Frequency { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class CycleSessionEntity
{
    public int Id { get; set; }
    public int CycleId { get; set; }
    public int SessionNumber { get; set; }
    public DateOnly OriginalScheduledDate { get; set; }
    public DateOnly ActualScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
}

public class SessionItemEntity
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string ItemType { get; set; } = "";
    public int? ServiceId { get; set; }
    public int? ProductSkuId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class DiscountEntity
{
    public int Id { get; set; }
    public string DiscountName { get; set; } = "";
    public string DiscountType { get; set; } = "";
    public decimal DiscountValue { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class SmsConfigurationEntity
{
    public int Id { get; set; }
    public string ProviderName { get; set; } = "";
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? SenderId { get; set; }
    public bool IsActive { get; set; }
    public string ChannelType { get; set; } = "";
    public string? HostName { get; set; }
    public int? PortNumber { get; set; }
}

public class TemplateTypeEntity
{
    public int Id { get; set; }
    public string TypeName { get; set; } = "";
}

public class SmsTemplateEntity
{
    public int Id { get; set; }
    public string MessageBody { get; set; } = "";
    public bool IsActive { get; set; }
    public string ChannelType { get; set; } = "";
    public int TemplateTypeId { get; set; }
}

public class IdSequenceEntity
{
    public int Id { get; set; }
    public string SequenceKey { get; set; } = "";
    public string? Prefix { get; set; }
    public int NextValue { get; set; }
    public int PaddingLength { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class InvoiceEntity
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public int? PatientId { get; set; }
    public int? SessionId { get; set; }
    public string InvoiceType { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceDue { get; set; }
    public string PaymentStatus { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public decimal VatAmount { get; set; }
}

public class PaymentEntity
{
    public int Id { get; set; }
    public int? InvoiceId { get; set; }
    public int PatientId { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string? ReferenceNumber { get; set; }
    public DateTime PaymentDate { get; set; }
}

public class ProductSkuEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string SkuCode { get; set; } = "";
    public string UnitName { get; set; } = "";
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public bool IsActive { get; set; }
}

public class ProductEntity
{
    public int Id { get; set; }
    public int ProductCategoryId { get; set; }
    public string ProductName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
