using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Data;

/// <summary>Maps directly onto the existing clinicMS.vera schema (columns are camelCase in SQL
/// Server) -- this project does not own migrations for these tables, so no model changes should be
/// pushed from here without updating the source schema script too.</summary>
public class ClinicMsDbContext : DbContext
{
    public ClinicMsDbContext(DbContextOptions<ClinicMsDbContext> options) : base(options)
    {
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
    public DbSet<TemplateTypeEntity> TemplateTypes => Set<TemplateTypeEntity>();
    public DbSet<SmsTemplateEntity> SmsTemplates => Set<SmsTemplateEntity>();
    public DbSet<IdSequenceEntity> IdSequences => Set<IdSequenceEntity>();
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();
    public DbSet<ProductSkuEntity> ProductSkus => Set<ProductSkuEntity>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

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
            e.Property(x => x.CurrentWalletCredit).HasColumnName("currentWalletCredit");
            e.Property(x => x.CreatedAt).HasColumnName("createdAt");
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

        modelBuilder.Entity<TemplateTypeEntity>(e =>
        {
            e.ToTable("templateTypes");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.TypeName).HasColumnName("typeName");
        });

        modelBuilder.Entity<SmsTemplateEntity>(e =>
        {
            e.ToTable("smsTemplates");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.MessageBody).HasColumnName("messageBody");
            e.Property(x => x.IsActive).HasColumnName("isActive");
            e.Property(x => x.ChannelType).HasColumnName("channelType");
            e.Property(x => x.TemplateTypeId).HasColumnName("templateTypeId");
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
    }
}
