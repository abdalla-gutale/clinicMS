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

public class PaymentAccountEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string AccountType { get; set; } = "";
    public string? AccountTypeSub { get; set; }
    public string? Number { get; set; }
    public decimal MonthlyBudgetEstimate { get; set; }
    public bool IsActive { get; set; }
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
    public string? ImageUrl { get; set; }
    public decimal CurrentWalletCredit { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
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
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
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
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
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

public class SmsTemplateEntity
{
    public int Id { get; set; }
    public string TemplateName { get; set; } = "";
    public string MessageBody { get; set; } = "";
    public bool IsActive { get; set; }
    public string ChannelType { get; set; } = "";
    public DateTime CreatedAt { get; set; }
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
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
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
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? AccountId { get; set; }
}

public class InvoiceItemEntity
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string ItemType { get; set; } = "";
    public int? ServiceId { get; set; }
    public int? ProductSkuId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class ProductRefundEntity
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int? PatientId { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public string RefundType { get; set; } = "";
    public string? Reason { get; set; }
    public DateTime RefundDate { get; set; }
}

public class RefundItemEntity
{
    public int Id { get; set; }
    public int RefundId { get; set; }
    public int ProductSkuId { get; set; }
    public int Quantity { get; set; }
    public decimal RefundUnitPrice { get; set; }
    public bool RestockItem { get; set; }
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

public class ProductCategoryEntity
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class SupplierEntity
{
    public int Id { get; set; }
    public string SupplierName { get; set; } = "";
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
}

public class PurchaseOrderEntity
{
    public int Id { get; set; }
    public string PoNumber { get; set; } = "";
    public int SupplierId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    public string? Notes { get; set; }
}

public class PurchaseOrderItemEntity
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ProductSkuId { get; set; }
    public int QuantityOrdered { get; set; }
    public int QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

public class StockMovementEntity
{
    public int Id { get; set; }
    public int ProductSkuId { get; set; }
    public string MovementType { get; set; } = "";
    public int Quantity { get; set; }
    public string? ReferenceId { get; set; }
    public DateTime MovementDate { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseReturnEntity
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int SupplierId { get; set; }
    public DateTime ReturnDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Reason { get; set; }
}

public class PurchaseReturnItemEntity
{
    public int Id { get; set; }
    public int PurchaseReturnId { get; set; }
    public int ProductSkuId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

public class ExpenseCategoryEntity
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class VendorEntity
{
    public int Id { get; set; }
    public string VendorName { get; set; } = "";
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}

public class RecurringExpenseScheduleEntity
{
    public int Id { get; set; }
    public int ExpenseCategoryId { get; set; }
    public int? VendorId { get; set; }
    public string Title { get; set; } = "";
    public decimal Amount { get; set; }
    public string Frequency { get; set; } = "";
    public DateOnly NextDueDate { get; set; }
    public bool AutoGenerate { get; set; }
    public bool IsActive { get; set; }
}

public class UserEntity
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RoleEntity
{
    public int Id { get; set; }
    public string RoleName { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class ModuleEntity
{
    public int Id { get; set; }
    public string ModuleName { get; set; } = "";
    public string ModuleIcon { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class NavPageEntity
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string PageName { get; set; } = "";
    public string PageUrl { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public int? ParentPageId { get; set; }
}

public class PermissionEntity
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int NavPageId { get; set; }
    public bool CanView { get; set; }
    public bool CanCreate { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class ReportPageEntity
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public string ReportName { get; set; } = "";
    public string ReportUrl { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class ReportPermissionEntity
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int ReportPageId { get; set; }
    public bool CanAccess { get; set; }
}

public class UserLogEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = "";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditTrailEntity
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string TableName { get; set; } = "";
    public int RecordId { get; set; }
    public string Action { get; set; } = "";
    public string? OldData { get; set; }
    public string? NewData { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ExpenseEntity
{
    public int Id { get; set; }
    public int ExpenseCategoryId { get; set; }
    public int? VendorId { get; set; }
    public int? ScheduleId { get; set; }
    public int? AccountId { get; set; }
    public string Title { get; set; } = "";
    public decimal Amount { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string? ReceiptNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
