using System.Text.Json;
using ClinicMS.Web.Models.Api.Auth;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace ClinicMS.Web.Data;

/// <summary>Maps directly onto the existing clinicMS.vera schema (columns are camelCase in SQL
/// Server) -- this project does not own migrations for these tables, so no model changes should be
/// pushed from here without updating the source schema script too.</summary>
public class ClinicMsDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public ClinicMsDbContext(DbContextOptions<ClinicMsDbContext> options, IHttpContextAccessor? httpContextAccessor = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<ClinicSettingEntity> ClinicSettings => Set<ClinicSettingEntity>();
    public DbSet<MerchantAccountEntity> MerchantAccounts => Set<MerchantAccountEntity>();
    public DbSet<PatientEntity> Patients => Set<PatientEntity>();
    public DbSet<ServiceTypeEntity> ServiceTypes => Set<ServiceTypeEntity>();
    public DbSet<ServiceEntity> Services => Set<ServiceEntity>();
    public DbSet<TreatmentPlanEntity> TreatmentPlans => Set<TreatmentPlanEntity>();
    public DbSet<TreatmentPlanItemEntity> TreatmentPlanItems => Set<TreatmentPlanItemEntity>();
    public DbSet<PatientCycleEntity> PatientCycles => Set<PatientCycleEntity>();
    public DbSet<CycleSessionEntity> CycleSessions => Set<CycleSessionEntity>();
    public DbSet<SessionItemEntity> SessionItems => Set<SessionItemEntity>();
    public DbSet<DiscountEntity> Discounts => Set<DiscountEntity>();
    public DbSet<SmsConfigurationEntity> SmsConfigurations => Set<SmsConfigurationEntity>();
    public DbSet<SmsTemplateEntity> SmsTemplates => Set<SmsTemplateEntity>();
    public DbSet<IdSequenceEntity> IdSequences => Set<IdSequenceEntity>();
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();
    public DbSet<ProductSkuEntity> ProductSkus => Set<ProductSkuEntity>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<ProductCategoryEntity> ProductCategories => Set<ProductCategoryEntity>();
    public DbSet<SupplierEntity> Suppliers => Set<SupplierEntity>();
    public DbSet<PurchaseOrderEntity> PurchaseOrders => Set<PurchaseOrderEntity>();
    public DbSet<PurchaseOrderItemEntity> PurchaseOrderItems => Set<PurchaseOrderItemEntity>();
    public DbSet<StockMovementEntity> StockMovements => Set<StockMovementEntity>();
    public DbSet<PurchaseReturnEntity> PurchaseReturns => Set<PurchaseReturnEntity>();
    public DbSet<PurchaseReturnItemEntity> PurchaseReturnItems => Set<PurchaseReturnItemEntity>();
    public DbSet<ExpenseCategoryEntity> ExpenseCategories => Set<ExpenseCategoryEntity>();
    public DbSet<VendorEntity> Vendors => Set<VendorEntity>();
    public DbSet<RecurringExpenseScheduleEntity> RecurringExpenseSchedules => Set<RecurringExpenseScheduleEntity>();
    public DbSet<ExpenseEntity> Expenses => Set<ExpenseEntity>();
    public DbSet<PaymentAccountEntity> PaymentAccounts => Set<PaymentAccountEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<ModuleEntity> Modules => Set<ModuleEntity>();
    public DbSet<NavPageEntity> NavPages => Set<NavPageEntity>();
    public DbSet<PermissionEntity> Permissions => Set<PermissionEntity>();
    public DbSet<ReportPageEntity> ReportPages => Set<ReportPageEntity>();
    public DbSet<ReportPermissionEntity> ReportPermissions => Set<ReportPermissionEntity>();
    public DbSet<UserLogEntity> UserLogs => Set<UserLogEntity>();
    public DbSet<AuditTrailEntity> AuditTrail => Set<AuditTrailEntity>();
    public DbSet<InvoiceItemEntity> InvoiceItems => Set<InvoiceItemEntity>();
    public DbSet<ProductRefundEntity> ProductRefunds => Set<ProductRefundEntity>();
    public DbSet<RefundItemEntity> RefundItems => Set<RefundItemEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClinicSettingEntity>(e =>
        {
            e.ToTable("clinicSettings");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ClinicName).HasColumnName("clinicName");
            e.Property(x => x.LogoIconUrl).HasColumnName("logoIconUrl");
            e.Property(x => x.LogoUrl).HasColumnName("logoUrl");
            e.Property(x => x.SidebarLogoUrl).HasColumnName("sidebarLogoUrl");
            e.Property(x => x.ReportLogoUrl).HasColumnName("reportLogoUrl");
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.Address).HasColumnName("address");
            e.Property(x => x.VatPercentage).HasColumnName("vatPercentage");
            e.Property(x => x.IsVatEnabled).HasColumnName("isVatEnabled");
            e.Property(x => x.CurrencySymbol).HasColumnName("currencySymbol");
            e.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
        });

        modelBuilder.Entity<MerchantAccountEntity>(e =>
        {
            e.ToTable("merchantAccounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.AccountHolderName).HasColumnName("accountHolderName");
            e.Property(x => x.BankName).HasColumnName("bankName");
            e.Property(x => x.AccountNumber).HasColumnName("accountNumber");
            e.Property(x => x.Iban).HasColumnName("iban");
            e.Property(x => x.SwiftCode).HasColumnName("swiftCode");
            e.Property(x => x.Branch).HasColumnName("branch");
        });

        modelBuilder.Entity<PatientEntity>(e =>
        {
            e.ToTable("patients");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PatientCode).HasColumnName("patientCode");
            e.Property(x => x.FullName).HasColumnName("fullName");
            e.Property(x => x.Gender).HasColumnName("gender");
            e.Property(x => x.DateOfBirth).HasColumnName("dateOfBirth");
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.ImageUrl).HasColumnName("imageUrl");
            e.Property(x => x.CurrentWalletCredit).HasColumnName("currentWalletCredit");
            e.Property(x => x.CreatedAt).HasColumnName("createdAt");
            e.Property(x => x.IsDeleted).HasColumnName("isDeleted");
            e.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<ServiceTypeEntity>(e =>
        {
            e.ToTable("serviceTypes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TypeName).HasColumnName("typeName");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<ServiceEntity>(e =>
        {
            e.ToTable("services");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ServiceTypeId).HasColumnName("serviceTypeId");
            e.Property(x => x.ServiceName).HasColumnName("serviceName");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.StandardPrice).HasColumnName("standardPrice");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<TreatmentPlanEntity>(e =>
        {
            e.ToTable("treatmentPlans");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PlanName).HasColumnName("planName");
            e.Property(x => x.PricingModel).HasColumnName("pricingModel");
            e.Property(x => x.FixedPackagePrice).HasColumnName("fixedPackagePrice");
            e.Property(x => x.TotalSessions).HasColumnName("totalSessions");
            e.Property(x => x.Frequency).HasColumnName("frequency");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<TreatmentPlanItemEntity>(e =>
        {
            e.ToTable("treatmentPlanItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TreatmentPlanId).HasColumnName("treatmentPlanId");
            e.Property(x => x.ItemType).HasColumnName("itemType");
            e.Property(x => x.ServiceId).HasColumnName("serviceId");
            e.Property(x => x.ProductSkuId).HasColumnName("productSkuId");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.UnitPrice).HasColumnName("unitPrice");
            e.Property(x => x.SessionNumber).HasColumnName("sessionNumber");
        });

        modelBuilder.Entity<PatientCycleEntity>(e =>
        {
            e.ToTable("patientCycles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PatientId).HasColumnName("patientId");
            e.Property(x => x.PlanId).HasColumnName("planId");
            e.Property(x => x.CycleName).HasColumnName("cycleName");
            e.Property(x => x.PricingModel).HasColumnName("pricingModel");
            e.Property(x => x.AgreedTotalPrice).HasColumnName("agreedTotalPrice");
            e.Property(x => x.Frequency).HasColumnName("frequency");
            e.Property(x => x.StartDate).HasColumnName("startDate");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.CreatedAt).HasColumnName("createdAt");
            e.Property(x => x.IsDeleted).HasColumnName("isDeleted");
            e.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<CycleSessionEntity>(e =>
        {
            e.ToTable("cycleSessions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CycleId).HasColumnName("cycleId");
            e.Property(x => x.SessionNumber).HasColumnName("sessionNumber");
            e.Property(x => x.OriginalScheduledDate).HasColumnName("originalScheduledDate");
            e.Property(x => x.ActualScheduledDate).HasColumnName("actualScheduledDate");
            e.Property(x => x.CompletedDate).HasColumnName("completedDate");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.IsDeleted).HasColumnName("isDeleted");
            e.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<SessionItemEntity>(e =>
        {
            e.ToTable("sessionItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SessionId).HasColumnName("sessionId");
            e.Property(x => x.ItemType).HasColumnName("itemType");
            e.Property(x => x.ServiceId).HasColumnName("serviceId");
            e.Property(x => x.ProductSkuId).HasColumnName("productSkuId");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.UnitPrice).HasColumnName("unitPrice");
        });

        modelBuilder.Entity<DiscountEntity>(e =>
        {
            e.ToTable("discounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.DiscountName).HasColumnName("discountName");
            e.Property(x => x.DiscountType).HasColumnName("discountType");
            e.Property(x => x.DiscountValue).HasColumnName("discountValue");
            e.Property(x => x.StartDate).HasColumnName("startDate");
            e.Property(x => x.EndDate).HasColumnName("endDate");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<SmsConfigurationEntity>(e =>
        {
            e.ToTable("smsConfigurations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ProviderName).HasColumnName("providerName");
            e.Property(x => x.ApiKey).HasColumnName("apiKey");
            e.Property(x => x.ApiSecret).HasColumnName("apiSecret");
            e.Property(x => x.SenderId).HasColumnName("senderId");
            e.Property(x => x.IsActive).HasColumnName("isActive");
            e.Property(x => x.ChannelType).HasColumnName("channelType");
            e.Property(x => x.HostName).HasColumnName("hostName");
            e.Property(x => x.PortNumber).HasColumnName("portNumber");
        });

        modelBuilder.Entity<SmsTemplateEntity>(e =>
        {
            e.ToTable("smsTemplates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TemplateName).HasColumnName("templateName");
            e.Property(x => x.MessageBody).HasColumnName("messageBody");
            e.Property(x => x.IsActive).HasColumnName("isActive");
            e.Property(x => x.ChannelType).HasColumnName("channelType");
            e.Property(x => x.CreatedAt).HasColumnName("createdAt");
        });

        modelBuilder.Entity<IdSequenceEntity>(e =>
        {
            e.ToTable("idSequences");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SequenceKey).HasColumnName("sequenceKey");
            e.Property(x => x.Prefix).HasColumnName("prefix");
            e.Property(x => x.NextValue).HasColumnName("nextValue");
            e.Property(x => x.PaddingLength).HasColumnName("paddingLength");
            e.Property(x => x.UpdatedAt).HasColumnName("updatedAt");
        });

        modelBuilder.Entity<InvoiceEntity>(e =>
        {
            e.ToTable("invoices");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.InvoiceNumber).HasColumnName("invoiceNumber");
            e.Property(x => x.PatientId).HasColumnName("patientId");
            e.Property(x => x.SessionId).HasColumnName("sessionId");
            e.Property(x => x.InvoiceType).HasColumnName("invoiceType");
            e.Property(x => x.TotalAmount).HasColumnName("totalAmount");
            e.Property(x => x.DiscountAmount).HasColumnName("discountAmount");
            e.Property(x => x.NetAmount).HasColumnName("netAmount");
            e.Property(x => x.PaidAmount).HasColumnName("paidAmount");
            e.Property(x => x.BalanceDue).HasColumnName("balanceDue");
            e.Property(x => x.PaymentStatus).HasColumnName("paymentStatus");
            e.Property(x => x.InvoiceDate).HasColumnName("invoiceDate");
            e.Property(x => x.VatAmount).HasColumnName("vatAmount");
            e.Property(x => x.IsDeleted).HasColumnName("isDeleted");
            e.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<PaymentEntity>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.InvoiceId).HasColumnName("invoiceId");
            e.Property(x => x.PatientId).HasColumnName("patientId");
            e.Property(x => x.AmountPaid).HasColumnName("amountPaid");
            e.Property(x => x.PaymentMethod).HasColumnName("paymentMethod");
            e.Property(x => x.ReferenceNumber).HasColumnName("referenceNumber");
            e.Property(x => x.PaymentDate).HasColumnName("paymentDate");
            e.Property(x => x.AccountId).HasColumnName("accountId");
            e.Property(x => x.IsDeleted).HasColumnName("isDeleted");
            e.Property(x => x.DeletedAt).HasColumnName("deletedAt");
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<ProductSkuEntity>(e =>
        {
            e.ToTable("productSkus");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ProductId).HasColumnName("productId");
            e.Property(x => x.SkuCode).HasColumnName("skuCode");
            e.Property(x => x.UnitName).HasColumnName("unitName");
            e.Property(x => x.CostPrice).HasColumnName("costPrice");
            e.Property(x => x.SellingPrice).HasColumnName("sellingPrice");
            e.Property(x => x.StockQuantity).HasColumnName("stockQuantity");
            e.Property(x => x.ReorderLevel).HasColumnName("reorderLevel");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<ProductEntity>(e =>
        {
            e.ToTable("products");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ProductCategoryId).HasColumnName("productCategoryId");
            e.Property(x => x.ProductName).HasColumnName("productName");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<ProductCategoryEntity>(e =>
        {
            e.ToTable("productCategories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CategoryName).HasColumnName("categoryName");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<SupplierEntity>(e =>
        {
            e.ToTable("suppliers");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.SupplierName).HasColumnName("supplierName");
            e.Property(x => x.ContactPerson).HasColumnName("contactPerson");
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.Address).HasColumnName("address");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<PurchaseOrderEntity>(e =>
        {
            e.ToTable("purchaseOrders");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PoNumber).HasColumnName("poNumber");
            e.Property(x => x.SupplierId).HasColumnName("supplierId");
            e.Property(x => x.OrderDate).HasColumnName("orderDate");
            e.Property(x => x.ExpectedDeliveryDate).HasColumnName("expectedDeliveryDate");
            e.Property(x => x.TotalAmount).HasColumnName("totalAmount");
            e.Property(x => x.Status).HasColumnName("status");
            e.Property(x => x.Notes).HasColumnName("notes");
        });

        modelBuilder.Entity<PurchaseOrderItemEntity>(e =>
        {
            e.ToTable("purchaseOrderItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PurchaseOrderId).HasColumnName("purchaseOrderId");
            e.Property(x => x.ProductSkuId).HasColumnName("productSkuId");
            e.Property(x => x.QuantityOrdered).HasColumnName("quantityOrdered");
            e.Property(x => x.QuantityReceived).HasColumnName("quantityReceived");
            e.Property(x => x.UnitCost).HasColumnName("unitCost");
            e.Property(x => x.TotalCost).HasColumnName("totalCost");
        });

        modelBuilder.Entity<StockMovementEntity>(e =>
        {
            e.ToTable("stockMovements");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ProductSkuId).HasColumnName("productSkuId");
            e.Property(x => x.MovementType).HasColumnName("movementType");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.ReferenceId).HasColumnName("referenceId");
            e.Property(x => x.MovementDate).HasColumnName("movementDate");
            e.Property(x => x.Notes).HasColumnName("notes");
        });

        modelBuilder.Entity<PurchaseReturnEntity>(e =>
        {
            e.ToTable("purchaseReturns");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PurchaseOrderId).HasColumnName("purchaseOrderId");
            e.Property(x => x.SupplierId).HasColumnName("supplierId");
            e.Property(x => x.ReturnDate).HasColumnName("returnDate");
            e.Property(x => x.TotalAmount).HasColumnName("totalAmount");
            e.Property(x => x.Reason).HasColumnName("reason");
        });

        modelBuilder.Entity<PurchaseReturnItemEntity>(e =>
        {
            e.ToTable("purchaseReturnItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.PurchaseReturnId).HasColumnName("purchaseReturnId");
            e.Property(x => x.ProductSkuId).HasColumnName("productSkuId");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.UnitCost).HasColumnName("unitCost");
            e.Property(x => x.TotalCost).HasColumnName("totalCost");
        });

        modelBuilder.Entity<ExpenseCategoryEntity>(e =>
        {
            e.ToTable("expenseCategories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.CategoryName).HasColumnName("categoryName");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<VendorEntity>(e =>
        {
            e.ToTable("vendors");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.VendorName).HasColumnName("vendorName");
            e.Property(x => x.ContactPerson).HasColumnName("contactPerson");
            e.Property(x => x.Phone).HasColumnName("phone");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<RecurringExpenseScheduleEntity>(e =>
        {
            e.ToTable("recurringExpenseSchedules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ExpenseCategoryId).HasColumnName("expenseCategoryId");
            e.Property(x => x.VendorId).HasColumnName("vendorId");
            e.Property(x => x.Title).HasColumnName("title");
            e.Property(x => x.Amount).HasColumnName("amount");
            e.Property(x => x.Frequency).HasColumnName("frequency");
            e.Property(x => x.NextDueDate).HasColumnName("nextDueDate");
            e.Property(x => x.AutoGenerate).HasColumnName("autoGenerate");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<ExpenseEntity>(e =>
        {
            e.ToTable("expenses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ExpenseCategoryId).HasColumnName("expenseCategoryId");
            e.Property(x => x.VendorId).HasColumnName("vendorId");
            e.Property(x => x.ScheduleId).HasColumnName("scheduleId");
            e.Property(x => x.AccountId).HasColumnName("accountId");
            e.Property(x => x.Title).HasColumnName("title");
            e.Property(x => x.Amount).HasColumnName("amount");
            e.Property(x => x.ExpenseDate).HasColumnName("expenseDate");
            e.Property(x => x.PaymentMethod).HasColumnName("paymentMethod");
            e.Property(x => x.ReceiptNumber).HasColumnName("receiptNumber");
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.CreatedAt).HasColumnName("createdAt");
        });

        modelBuilder.Entity<InvoiceItemEntity>(e =>
        {
            e.ToTable("invoiceItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.InvoiceId).HasColumnName("invoiceId");
            e.Property(x => x.ItemType).HasColumnName("itemType");
            e.Property(x => x.ServiceId).HasColumnName("serviceId");
            e.Property(x => x.ProductSkuId).HasColumnName("productSkuId");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.UnitPrice).HasColumnName("unitPrice");
            e.Property(x => x.TotalPrice).HasColumnName("totalPrice");
        });

        modelBuilder.Entity<ProductRefundEntity>(e =>
        {
            e.ToTable("productRefunds");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.InvoiceId).HasColumnName("invoiceId");
            e.Property(x => x.PatientId).HasColumnName("patientId");
            e.Property(x => x.TotalRefundAmount).HasColumnName("totalRefundAmount");
            e.Property(x => x.RefundType).HasColumnName("refundType");
            e.Property(x => x.Reason).HasColumnName("reason");
            e.Property(x => x.RefundDate).HasColumnName("refundDate");
        });

        modelBuilder.Entity<RefundItemEntity>(e =>
        {
            e.ToTable("refundItems");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RefundId).HasColumnName("refundId");
            e.Property(x => x.ProductSkuId).HasColumnName("productSkuId");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.RefundUnitPrice).HasColumnName("refundUnitPrice");
            e.Property(x => x.RestockItem).HasColumnName("restockItem");
        });

        modelBuilder.Entity<PaymentAccountEntity>(e =>
        {
            e.ToTable("paymentAccounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.AccountType).HasColumnName("accountType");
            e.Property(x => x.AccountTypeSub).HasColumnName("accountTypeSub");
            e.Property(x => x.Number).HasColumnName("number");
            e.Property(x => x.MonthlyBudgetEstimate).HasColumnName("monthlyBudgetEstimate");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<UserEntity>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RoleId).HasColumnName("roleId");
            e.Property(x => x.Username).HasColumnName("username");
            e.Property(x => x.PasswordHash).HasColumnName("passwordHash");
            e.Property(x => x.FullName).HasColumnName("fullName");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.PhoneNumber).HasColumnName("phoneNumber");
            e.Property(x => x.IsActive).HasColumnName("isActive");
            e.Property(x => x.CreatedAt).HasColumnName("createdAt");
        });

        modelBuilder.Entity<RoleEntity>(e =>
        {
            e.ToTable("roles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RoleName).HasColumnName("roleName");
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<ModuleEntity>(e =>
        {
            e.ToTable("modules");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ModuleName).HasColumnName("moduleName");
            e.Property(x => x.ModuleIcon).HasColumnName("moduleIcon");
            e.Property(x => x.DisplayOrder).HasColumnName("displayOrder");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<NavPageEntity>(e =>
        {
            e.ToTable("navPages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ModuleId).HasColumnName("moduleId");
            e.Property(x => x.PageName).HasColumnName("pageName");
            e.Property(x => x.PageUrl).HasColumnName("pageUrl");
            e.Property(x => x.DisplayOrder).HasColumnName("displayOrder");
            e.Property(x => x.IsActive).HasColumnName("isActive");
            e.Property(x => x.ParentPageId).HasColumnName("parentPageId");
        });

        modelBuilder.Entity<PermissionEntity>(e =>
        {
            e.ToTable("permissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RoleId).HasColumnName("roleId");
            e.Property(x => x.NavPageId).HasColumnName("navPageId");
            e.Property(x => x.CanView).HasColumnName("canView");
            e.Property(x => x.CanCreate).HasColumnName("canCreate");
            e.Property(x => x.CanEdit).HasColumnName("canEdit");
            e.Property(x => x.CanDelete).HasColumnName("canDelete");
        });

        modelBuilder.Entity<ReportPageEntity>(e =>
        {
            e.ToTable("reportPages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ModuleId).HasColumnName("moduleId");
            e.Property(x => x.ReportName).HasColumnName("reportName");
            e.Property(x => x.ReportUrl).HasColumnName("reportUrl");
            e.Property(x => x.DisplayOrder).HasColumnName("displayOrder");
            e.Property(x => x.IsActive).HasColumnName("isActive");
        });

        modelBuilder.Entity<ReportPermissionEntity>(e =>
        {
            e.ToTable("reportPermissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.RoleId).HasColumnName("roleId");
            e.Property(x => x.ReportPageId).HasColumnName("reportPageId");
            e.Property(x => x.CanAccess).HasColumnName("canAccess");
        });

        modelBuilder.Entity<UserLogEntity>(e =>
        {
            e.ToTable("userLogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("userId");
            e.Property(x => x.Action).HasColumnName("action");
            e.Property(x => x.IpAddress).HasColumnName("ipAddress");
            e.Property(x => x.UserAgent).HasColumnName("userAgent");
            e.Property(x => x.CreatedAt).HasColumnName("createdAt");
        });

        modelBuilder.Entity<AuditTrailEntity>(e =>
        {
            e.ToTable("fullAuditTrailLog");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("userId");
            e.Property(x => x.TableName).HasColumnName("tableName");
            e.Property(x => x.RecordId).HasColumnName("recordId");
            e.Property(x => x.Action).HasColumnName("action");
            e.Property(x => x.OldData).HasColumnName("oldData");
            e.Property(x => x.NewData).HasColumnName("newData");
            e.Property(x => x.IpAddress).HasColumnName("ipAddress");
            e.Property(x => x.CreatedAt).HasColumnName("createdAt");
        });
    }

    /// <summary>Auto-logs every Add/Modify/Delete to fullAuditTrailLog by inspecting the change
    /// tracker on every save -- so every mutation across every Db*ApiClient gets an audit row for
    /// free, with no per-call-site logging code to remember to add. Runs as a second SaveChanges
    /// pass because Added entities don't have their generated id until after the first save.
    /// AuditTrailEntity/UserLogEntity themselves are never audited (nothing to attribute a log
    /// entry about a log entry to).</summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var pending = CaptureAuditableChanges();
        var result = await base.SaveChangesAsync(cancellationToken);

        if (pending.Count > 0)
        {
            var userId = GetCurrentUserId();
            var ipAddress = _httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress?.ToString();

            foreach (var change in pending)
            {
                // Added entities have no real id (and EF fills their key properties with a negative
                // placeholder) until after the first save above, so their NewData can only be
                // serialized now -- doing it during CaptureAuditableChanges would bake in that
                // placeholder instead of the generated id.
                var newData = change.Action == "Create" ? SerializeCurrent(change.Entry) : change.NewData;

                AuditTrail.Add(new AuditTrailEntity
                {
                    UserId = userId,
                    TableName = change.TableName,
                    RecordId = GetPrimaryKeyValue(change.Entry),
                    Action = change.Action,
                    OldData = change.OldData,
                    NewData = newData,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await base.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private List<AuditableChange> CaptureAuditableChanges()
    {
        var changes = new List<AuditableChange>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditTrailEntity or UserLogEntity)
            {
                continue;
            }

            var tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;

            switch (entry.State)
            {
                case EntityState.Added:
                    changes.Add(new AuditableChange(entry, tableName, "Create", null, SerializeCurrent(entry)));
                    break;
                case EntityState.Modified:
                    changes.Add(new AuditableChange(entry, tableName, "Update", SerializeOriginal(entry), SerializeCurrent(entry)));
                    break;
                case EntityState.Deleted:
                    changes.Add(new AuditableChange(entry, tableName, "Delete", SerializeOriginal(entry), null));
                    break;
            }
        }

        return changes;
    }

    private int? GetCurrentUserId()
    {
        var json = _httpContextAccessor?.HttpContext?.Session.GetString(SessionKeys.AuthUser);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        var user = JsonSerializer.Deserialize<UserSummary>(json);
        // The code-level master login has no row in the users table (see AccountController) --
        // its synthetic id can't satisfy fullAuditTrailLog's FK to users, so it logs as "no user".
        return user is { Id: > 0 } ? user.Id : null;
    }

    private static int GetPrimaryKeyValue(EntityEntry entry)
    {
        var keyProperty = entry.Metadata.FindPrimaryKey()?.Properties.FirstOrDefault();
        if (keyProperty is null)
        {
            return 0;
        }

        var value = entry.Property(keyProperty.Name).CurrentValue;
        return value switch
        {
            int i => i,
            long l => (int)l,
            _ => 0,
        };
    }

    private static string SerializeCurrent(EntityEntry entry) =>
        JsonSerializer.Serialize(entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));

    private static string SerializeOriginal(EntityEntry entry) =>
        JsonSerializer.Serialize(entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));

    private record AuditableChange(EntityEntry Entry, string TableName, string Action, string? OldData, string? NewData);
}
