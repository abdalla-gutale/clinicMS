using ClinicMS.Web.Models.Api.Audit;
using ClinicMS.Web.Models.Api.Expenses;
using ClinicMS.Web.Models.Api.MedicalServices;
using ClinicMS.Web.Models.Api.Notifications;
using ClinicMS.Web.Models.Api.Patients;
using ClinicMS.Web.Models.Api.Payments;
using ClinicMS.Web.Models.Api.Rbac;
using ClinicMS.Web.Models.Api.Settings;
using ClinicMS.Web.Models.Api.Sms;
using ClinicMS.Web.Models.Api.SupplyChain;
using ClinicMS.Web.Models.Api.Users;

namespace ClinicMS.Web.Services.Api.Mocks;

/// <summary>In-memory seed data + mutable state for every Mock*ApiClient, standing in for
/// ClinicMS.API while the frontend is designed without a running backend. Single process-lifetime
/// singleton so edits made through one controller (e.g. adding a user) are visible to others.</summary>
public static class MockStore
{
    public static readonly List<ModuleDto> Modules = new()
    {
        new ModuleDto(1, "Registration", 1, true),
        new ModuleDto(2, "Finance", 2, true),
        new ModuleDto(3, "Supply Chain", 3, true),
        new ModuleDto(4, "Configuration", 4, true),
        new ModuleDto(5, "Administration", 5, true),
        new ModuleDto(6, "Reports", 6, true),
    };

    public static readonly List<NavPageDto> NavPages = new()
    {
        // Registration
        new NavPageDto(1, 1, "Patient", "/patients", 1, true, null),
        new NavPageDto(2, 1, "Medical Services", "/medical-services", 2, true, null),
        new NavPageDto(3, 1, "Service Types", "/service-types", 1, true, 2),
        new NavPageDto(4, 1, "Services", "/services", 2, true, 2),
        new NavPageDto(5, 1, "Treatment Plans", "/treatment-plans", 3, true, 2),
        new NavPageDto(6, 1, "Patient Cycles", "/patient-cycles", 4, true, 2),
        new NavPageDto(7, 1, "Walk-in Sale", "/walk-in-sale", 5, true, 2),

        // Finance
        new NavPageDto(10, 2, "Billing & Revenue", "/billing-revenue", 1, true, null),
        new NavPageDto(11, 2, "Invoices", "/invoices", 1, true, 10),
        new NavPageDto(12, 2, "Payments", "/payments", 2, true, 10),
        new NavPageDto(13, 2, "Product Refunds", "/product-refunds", 3, true, 10),
        new NavPageDto(14, 2, "Expense Management", "/expense-management", 2, true, null),
        new NavPageDto(15, 2, "Expense Categories", "/expense-categories", 1, true, 14),
        new NavPageDto(16, 2, "Expenses", "/expenses", 2, true, 14),
        new NavPageDto(17, 2, "Recurring Expenses", "/recurring-expenses", 3, true, 14),

        // Supply Chain
        new NavPageDto(40, 3, "Inventory Control", "/inventory-control", 1, true, null),
        new NavPageDto(41, 3, "Product Categories", "/product-categories", 1, true, 40),
        new NavPageDto(42, 3, "Products", "/products", 2, true, 40),
        new NavPageDto(43, 3, "Product SKUs", "/product-skus", 3, true, 40),
        new NavPageDto(44, 3, "Stock Movements", "/stock-movements", 4, true, 40),
        new NavPageDto(45, 3, "Procurement", "/procurement", 2, true, null),
        new NavPageDto(46, 3, "Suppliers", "/suppliers", 1, true, 45),
        new NavPageDto(47, 3, "Purchase Orders", "/purchase-orders", 2, true, 45),
        new NavPageDto(48, 3, "Vendors", "/vendors", 3, true, 45),

        // Configuration
        new NavPageDto(30, 4, "Clinic Settings", "/settings/clinic", 1, true, null),

        // Administration
        new NavPageDto(50, 5, "Access Control", "/access-control", 1, true, null),
        new NavPageDto(51, 5, "Users", "/admin/users", 1, true, 50),
        new NavPageDto(52, 5, "Roles", "/admin/roles", 2, true, 50),
        new NavPageDto(53, 5, "System Utilities", "/system-utilities", 2, true, null),
        new NavPageDto(54, 5, "Modules", "/admin/modules", 1, true, 53),
        new NavPageDto(55, 5, "Nav Pages", "/admin/nav-pages", 2, true, 53),
        new NavPageDto(56, 5, "Report Pages", "/admin/report-pages", 3, true, 53),
        new NavPageDto(57, 5, "Audit log", "/admin/audit", 4, true, 53),
    };

    public static readonly List<ReportPageDto> ReportPages = new()
    {
        new ReportPageDto(1, 6, "Reports", "Revenue Summary", "/reports/revenue-summary", 1, true),
        new ReportPageDto(2, 6, "Reports", "Expense Summary", "/reports/expense-summary", 2, true),
        new ReportPageDto(3, 6, "Reports", "Inventory Valuation", "/reports/inventory-valuation", 3, true),
    };
    public static int NextReportPageId = 4;

    public static readonly List<RoleDto> Roles = new()
    {
        new RoleDto(1, "Administrator", "Full system access", true),
        new RoleDto(2, "Front Desk", "Reception, scheduling and patient intake", true),
        new RoleDto(3, "Accountant", "Billing, invoicing and expense tracking", true),
    };

    /// <summary>NavPageId -> CanView/CanCreate/CanEdit/CanDelete, per role.</summary>
    public static readonly Dictionary<int, Dictionary<int, PermissionItem>> RoleNavPermissions = new()
    {
        [1] = NavPages.ToDictionary(p => p.Id, p => new PermissionItem(p.Id, true, true, true, true)),
        [2] = new()
        {
            [1] = new PermissionItem(1, true, true, true, false),
            [12] = new PermissionItem(12, true, true, false, false), // Payments
            [11] = new PermissionItem(11, true, false, false, false), // Invoices
        },
        [3] = new()
        {
            [12] = new PermissionItem(12, true, true, false, false), // Payments
            [11] = new PermissionItem(11, true, true, false, false), // Invoices
            [16] = new PermissionItem(16, true, true, true, true), // Expenses
            [15] = new PermissionItem(15, true, true, true, true), // Expense Categories
        },
    };

    public static readonly List<UserDto> Users = new()
    {
        new UserDto(1, 1, "Administrator", "admin", "Admin User", "admin@clinicms.com", "+971-50-100-0001", true, DateTime.UtcNow.AddMonths(-8)),
        new UserDto(2, 2, "Front Desk", "sara.ahmed", "Sara Ahmed", "sara.ahmed@clinicms.com", "+971-50-100-0002", true, DateTime.UtcNow.AddMonths(-5)),
        new UserDto(3, 3, "Accountant", "omar.khaled", "Omar Khaled", "omar.khaled@clinicms.com", "+971-50-100-0003", true, DateTime.UtcNow.AddMonths(-3)),
    };
    public static int NextUserId = 4;

    public static readonly List<VendorDto> Vendors = new()
    {
        new VendorDto(1, "MedSupply Co.", "Fatima Noor", "+971-4-200-1000", "sales@medsupply.example", true),
        new VendorDto(2, "PharmaLink LLC", "Rashid Al-Marri", "+971-4-200-2000", "orders@pharmalink.example", true),
        new VendorDto(3, "Gulf Facilities Mgmt", "Aisha Rahman", "+971-4-200-3000", "billing@gulffm.example", true),
    };
    public static int NextVendorId = 4;

    public static readonly List<ExpenseCategoryDto> ExpenseCategories = new()
    {
        new ExpenseCategoryDto(1, "Rent", "Monthly clinic space rent", true),
        new ExpenseCategoryDto(2, "Utilities", "Electricity, water, internet", true),
        new ExpenseCategoryDto(3, "Medical Supplies", "Consumables and disposables", true),
        new ExpenseCategoryDto(4, "Salaries", "Staff payroll", true),
    };
    public static int NextExpenseCategoryId = 5;

    public static readonly List<ExpenseDto> Expenses = new()
    {
        new ExpenseDto(1, 1, "Rent", null, null, null, "July clinic rent", 15000m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)), "BankTransfer", "RCPT-1001", null, DateTime.UtcNow.AddDays(-14), 3, "POS Merchant Terminal"),
        new ExpenseDto(2, 2, "Utilities", null, null, null, "DEWA bill", 1280.50m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)), "BankTransfer", "RCPT-1002", null, DateTime.UtcNow.AddDays(-10), 3, "POS Merchant Terminal"),
        new ExpenseDto(3, 3, "Medical Supplies", 1, "MedSupply Co.", null, "Gloves and syringes restock", 3420.00m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)), "Cash", "RCPT-1003", "Monthly restock", DateTime.UtcNow.AddDays(-6), 1, "Front Desk Cash"),
        new ExpenseDto(4, 4, "Salaries", null, null, null, "July payroll", 42000m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), "BankTransfer", "RCPT-1004", null, DateTime.UtcNow.AddDays(-2), 3, "POS Merchant Terminal"),
    };
    public static int NextExpenseId = 5;

    public static readonly List<RecurringExpenseDto> RecurringExpenseSchedules = new()
    {
        new RecurringExpenseDto(1, 1, "Rent", null, null, "Monthly clinic space rent", 15000m, RecurringFrequency.Monthly, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(16)), true, true),
        new RecurringExpenseDto(2, 2, "Utilities", 3, "Gulf Facilities Mgmt", "DEWA bill", 1280.50m, RecurringFrequency.Monthly, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)), true, true),
        new RecurringExpenseDto(3, 4, "Salaries", null, null, "Staff payroll", 42000m, RecurringFrequency.Monthly, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(28)), false, true),
    };
    public static int NextRecurringExpenseId = 4;

    public static readonly List<PatientOption> Patients = new()
    {
        new PatientOption(1, "Layla Hassan", "+971-50-300-0001", "layla.hassan@example.com"),
        new PatientOption(2, "Yusuf Al-Amin", "+971-50-300-0002", "yusuf.alamin@example.com"),
        new PatientOption(3, "Mona Said", "+971-50-300-0003", null),
    };

    // Full patient registration records for the Patients page -- deliberately separate from the
    // lightweight PatientOption list above, which only backs the Notifications patient picker.
    public static readonly List<PatientDto> PatientRecords = new()
    {
        new PatientDto(1, null, "Layla Hassan", PatientGender.Female, new DateOnly(1994, 3, 12), "+971-50-300-0001", "layla.hassan@example.com", DateTime.UtcNow.AddMonths(-8)),
        new PatientDto(2, null, "Yusuf Al-Amin", PatientGender.Male, new DateOnly(1988, 7, 25), "+971-50-300-0002", "yusuf.alamin@example.com", DateTime.UtcNow.AddMonths(-5)),
        new PatientDto(3, null, "Mona Said", PatientGender.Female, new DateOnly(2001, 11, 3), "+971-50-300-0003", null, DateTime.UtcNow.AddMonths(-2)),
        new PatientDto(4, null, "Karim Talaat", PatientGender.Male, new DateOnly(1976, 1, 30), "+971-50-300-0004", "karim.talaat@example.com", DateTime.UtcNow.AddDays(-10)),
    };
    public static int NextPatientRecordId = 5;

    public static readonly List<ServiceTypeDto> ServiceTypes = new()
    {
        new ServiceTypeDto(1, "Consultation", "Initial and follow-up consultations", true),
        new ServiceTypeDto(2, "Dermatology", "Skin treatments and procedures", true),
        new ServiceTypeDto(3, "Laser Therapy", "Laser-based cosmetic treatments", true),
        new ServiceTypeDto(4, "Hair Restoration", "Hair loss treatments", true),
        new ServiceTypeDto(5, "Wellness", "General wellness and spa services", false),
    };
    public static int NextServiceTypeId = 6;

    public static readonly List<ServiceDto> Services = new()
    {
        new ServiceDto(1, 1, "Consultation", "General Consultation", 150m, "Initial patient assessment", true),
        new ServiceDto(2, 1, "Consultation", "Follow-up Consultation", 80m, null, true),
        new ServiceDto(3, 2, "Dermatology", "Chemical Peel", 350m, null, true),
        new ServiceDto(4, 2, "Dermatology", "Acne Treatment", 400m, null, true),
        new ServiceDto(5, 3, "Laser Therapy", "Laser Hair Removal", 500m, "Per treatment area", true),
        new ServiceDto(6, 3, "Laser Therapy", "Skin Resurfacing", 600m, null, true),
        new ServiceDto(7, 4, "Hair Restoration", "PRP Hair Treatment", 700m, null, true),
        new ServiceDto(8, 5, "Wellness", "Relaxation Massage", 250m, null, false),
    };
    public static int NextServiceId = 9;

    public static readonly List<ProductOption> ProductOptions = new()
    {
        new ProductOption(1, "Vitamin C Serum", 120m),
        new ProductOption(2, "Sunscreen SPF50", 90m),
        new ProductOption(3, "Post-Laser Aftercare Kit", 150m),
        new ProductOption(4, "Hair Growth Serum", 180m),
        new ProductOption(5, "Moisturizing Cream", 75m),
        new ProductOption(6, "Hydrating Face Mask", 60m),
    };

    public static readonly List<ProductCategoryDto> ProductCategories = new()
    {
        new ProductCategoryDto(1, "Skincare", "Retail skincare products", true),
        new ProductCategoryDto(2, "Hair Care", "Hair growth and restoration products", true),
        new ProductCategoryDto(3, "Aftercare Kits", "Post-treatment aftercare bundles", true),
    };
    public static int NextProductCategoryId = 4;

    public static readonly List<ProductDto> Products = new()
    {
        new ProductDto(1, 1, "Skincare", "Vitamin C Serum", "Brightening antioxidant serum", true),
        new ProductDto(2, 1, "Skincare", "Sunscreen SPF50", "Broad-spectrum sun protection", true),
        new ProductDto(3, 3, "Aftercare Kits", "Post-Laser Aftercare Kit", "Soothing kit for post-laser care", true),
        new ProductDto(4, 2, "Hair Care", "Hair Growth Serum", "Topical hair regrowth serum", true),
        new ProductDto(5, 1, "Skincare", "Moisturizing Cream", "Daily hydrating moisturizer", true),
    };
    public static int NextProductId = 6;

    // SKU ids intentionally start at 201 to line up with the ProductSkuId already referenced by the
    // seeded invoice item below ("Vitamin C Serum", SKU-VCS-30) from before this catalog existed.
    public static readonly List<ProductSkuDto> ProductSkus = new()
    {
        new ProductSkuDto(201, 1, "Vitamin C Serum", "SKU-VCS-30", "30ml Bottle", 60m, 120m, 42, 10, true),
        new ProductSkuDto(202, 2, "Sunscreen SPF50", "SKU-SUN-50", "50ml Tube", 45m, 90m, 35, 10, true),
        new ProductSkuDto(203, 3, "Post-Laser Aftercare Kit", "SKU-PLK-01", "Kit", 75m, 150m, 18, 5, true),
        new ProductSkuDto(204, 4, "Hair Growth Serum", "SKU-HGS-60", "60ml Bottle", 90m, 180m, 24, 8, true),
        new ProductSkuDto(205, 5, "Moisturizing Cream", "SKU-MC-100", "100ml Jar", 38m, 75m, 8, 10, true),
    };
    public static int NextProductSkuId = 206;

    public static readonly List<StockMovementDto> StockMovements = new()
    {
        new StockMovementDto(1, 201, "SKU-VCS-30", "Vitamin C Serum", StockMovementType.In, 50, "PO-1001", DateTime.UtcNow.AddDays(-20), "Initial stock"),
        new StockMovementDto(2, 201, "SKU-VCS-30", "Vitamin C Serum", StockMovementType.Out, 8, "INV-2026-0003", DateTime.UtcNow.AddDays(-2), "Sold to patient"),
        new StockMovementDto(3, 205, "SKU-MC-100", "Moisturizing Cream", StockMovementType.In, 20, "PO-1002", DateTime.UtcNow.AddDays(-15), "Initial stock"),
        new StockMovementDto(4, 205, "SKU-MC-100", "Moisturizing Cream", StockMovementType.Out, 12, null, DateTime.UtcNow.AddDays(-3), "Front desk retail sales"),
    };
    public static int NextStockMovementId = 5;

    public static readonly List<SupplierDto> Suppliers = new()
    {
        new SupplierDto(1, "MedSupply Co.", "Fatima Noor", "+971-4-200-1000", "sales@medsupply.example", "Al Quoz Industrial Area, Dubai", true),
        new SupplierDto(2, "PharmaLink LLC", "Rashid Al-Marri", "+971-4-200-2000", "orders@pharmalink.example", "Sheikh Zayed Road, Dubai", true),
        new SupplierDto(3, "GlowCare Distributors", "Huda Farouk", "+971-4-200-4000", "info@glowcare.example", "Deira, Dubai", true),
    };
    public static int NextSupplierId = 4;

    public static readonly List<PurchaseOrderDto> PurchaseOrders = new()
    {
        new PurchaseOrderDto(1, "PO-1001", 1, "MedSupply Co.", DateTime.UtcNow.AddDays(-20), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)), 3000m, PurchaseOrderStatus.Received, "Initial stock order",
            new List<PurchaseOrderItemDto> { new(201, "SKU-VCS-30", "Vitamin C Serum", 50, 50, 60m, 3000m) }),
        new PurchaseOrderDto(2, "PO-1002", 3, "GlowCare Distributors", DateTime.UtcNow.AddDays(-15), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-9)), 760m, PurchaseOrderStatus.Received, "Initial stock order",
            new List<PurchaseOrderItemDto> { new(205, "SKU-MC-100", "Moisturizing Cream", 20, 20, 38m, 760m) }),
        new PurchaseOrderDto(3, "PO-1003", 2, "PharmaLink LLC", DateTime.UtcNow.AddDays(-2), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), 1800m, PurchaseOrderStatus.Ordered, "Restock hair care line",
            new List<PurchaseOrderItemDto> { new(204, "SKU-HGS-60", "Hair Growth Serum", 20, 0, 90m, 1800m) }),
    };
    public static int NextPurchaseOrderId = 4;
    public static int NextPurchaseOrderNumber = 1004;

    public static readonly List<TreatmentPlanDto> TreatmentPlans = new()
    {
        new TreatmentPlanDto(1, "Acne Clearance Program", PricingModelType.FixedPackage, 1800m, PlanFrequency.Weekly, 6, new List<TreatmentPlanSessionDto>
        {
            new(1, "Week 1", new List<int> { 4 }, new List<int> { 3 }),
            new(2, "Week 2", new List<int> { 4 }, new List<int>()),
            new(3, "Week 3", new List<int> { 3, 4 }, new List<int> { 1 }),
            new(4, "Week 4", new List<int> { 4 }, new List<int>()),
            new(5, "Week 5", new List<int> { 4 }, new List<int> { 1 }),
            new(6, "Week 6", new List<int> { 3, 4 }, new List<int> { 1, 5 }),
        }),
        new TreatmentPlanDto(2, "Laser Hair Removal Package", PricingModelType.PerVisit, null, PlanFrequency.Monthly, 4, new List<TreatmentPlanSessionDto>
        {
            new(1, "Month 1", new List<int> { 5 }, new List<int>()),
            new(2, "Month 2", new List<int> { 5 }, new List<int>()),
            new(3, "Month 3", new List<int> { 5 }, new List<int> { 2 }),
            new(4, "Month 4", new List<int> { 5 }, new List<int> { 2 }),
        }),
    };
    public static int NextTreatmentPlanId = 3;

    public static readonly List<PatientCycleDto> PatientCycles = new();
    public static int NextPatientCycleId = 1;

    public sealed class InvoiceRecord
    {
        public required int Id { get; init; }
        public required string InvoiceNumber { get; init; }
        public required int PatientId { get; init; }
        public required string PatientName { get; init; }
        public required string PatientPhone { get; init; }
        public required string InvoiceType { get; init; }
        public required DateTime InvoiceDate { get; init; }
        public required decimal DiscountAmount { get; init; }
        public required List<InvoiceItemDto> Items { get; init; }
        public decimal PaidAmount { get; set; }

        public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
        public decimal VatAmount => Math.Round((TotalAmount - DiscountAmount) * 0.05m, 2);
        public decimal NetAmount => TotalAmount - DiscountAmount + VatAmount;
        public decimal BalanceDue => NetAmount - PaidAmount;
        public PaymentStatus Status => BalanceDue <= 0 ? PaymentStatus.Paid : PaidAmount > 0 ? PaymentStatus.Partial : PaymentStatus.Unpaid;
    }

    public static readonly List<InvoiceRecord> Invoices = new()
    {
        new InvoiceRecord
        {
            Id = 5001, InvoiceNumber = "INV-2026-0001", PatientId = 1, PatientName = "Layla Hassan", PatientPhone = "+971-50-300-0001",
            InvoiceType = "Service", InvoiceDate = DateTime.UtcNow.AddDays(-10), DiscountAmount = 50m, PaidAmount = 787.5m,
            Items = new()
            {
                new InvoiceItemDto(1, "Service", 101, "Consultation", null, null, null, 1, 300m, 300m),
                new InvoiceItemDto(2, "Service", 102, "Skin Treatment", null, null, null, 1, 500m, 500m),
            },
        },
        new InvoiceRecord
        {
            Id = 5002, InvoiceNumber = "INV-2026-0002", PatientId = 2, PatientName = "Yusuf Al-Amin", PatientPhone = "+971-50-300-0002",
            InvoiceType = "Service", InvoiceDate = DateTime.UtcNow.AddDays(-5), DiscountAmount = 0m, PaidAmount = 600m,
            Items = new()
            {
                new InvoiceItemDto(3, "Service", 103, "Laser Therapy", null, null, null, 1, 1200m, 1200m),
            },
        },
        new InvoiceRecord
        {
            Id = 5003, InvoiceNumber = "INV-2026-0003", PatientId = 3, PatientName = "Mona Said", PatientPhone = "+971-50-300-0003",
            InvoiceType = "Mixed", InvoiceDate = DateTime.UtcNow.AddDays(-2), DiscountAmount = 0m, PaidAmount = 0m,
            Items = new()
            {
                new InvoiceItemDto(4, "Service", 101, "Consultation", null, null, null, 1, 150m, 150m),
                new InvoiceItemDto(5, "Product", null, null, 201, "Vitamin C Serum", "SKU-VCS-30", 1, 300m, 300m),
            },
        },
    };
    public static int NextInvoiceItemId = 6;
    public static int NextInvoiceId = 5004;

    public static readonly List<PaymentDto> Payments = new()
    {
        new PaymentDto(9001, 5001, 1, "Layla Hassan", 787.5m, PaymentMethod.CreditCard, "TXN-0001", DateTime.UtcNow.AddDays(-10), 3, "POS Merchant Terminal"),
        new PaymentDto(9002, 5002, 2, "Yusuf Al-Amin", 600m, PaymentMethod.Cash, null, DateTime.UtcNow.AddDays(-5), 1, "Front Desk Cash"),
    };
    public static int NextPaymentId = 9003;

    public static readonly List<ProductRefundDto> ProductRefunds = new();
    public static int NextProductRefundId = 1;

    public static readonly List<TemplateTypeDto> TemplateTypes = new()
    {
        new TemplateTypeDto(1, "Appointment Reminder"),
        new TemplateTypeDto(2, "Payment Receipt"),
        new TemplateTypeDto(3, "Welcome Message"),
        new TemplateTypeDto(4, "Custom"),
    };

    public static readonly List<SmsTemplateDto> SmsTemplates = new()
    {
        new SmsTemplateDto(1, "Expiring Membership", 1, "Appointment Reminder", ChannelType.SMS, "Hi {name}, your ClinicMS membership expires on {date}. Renew now to keep your access!", true, DateTime.UtcNow),
        new SmsTemplateDto(2, "Welcome New Member", 3, "Welcome Message", ChannelType.SMS, "Welcome to ClinicMS, {name}! Your membership is now active. We are thrilled to have you.", true, DateTime.UtcNow.AddHours(-3)),
        new SmsTemplateDto(3, "Payment Received", 2, "Payment Receipt", ChannelType.SMS, "Hi {name}, we received your payment of {amount} for {package}. Invoice #{invoice} — thank you!", true, DateTime.UtcNow.AddDays(-1)),
        new SmsTemplateDto(4, "Birthday Greeting", 4, "Custom", ChannelType.SMS, "Happy Birthday, {name}! Enjoy 10% off your next visit as a gift from us — ClinicMS.", true, DateTime.UtcNow.AddDays(-1).AddHours(-4)),
        new SmsTemplateDto(5, "Overdue Payment", 2, "Payment Receipt", ChannelType.SMS, "Hi {name}, your account has an overdue balance of {amount}. Please settle it soon — {branch}.", true, DateTime.UtcNow.AddDays(-3)),
    };
    public static int NextSmsTemplateId = 6;

    public static readonly List<SmsConfigurationDto> SmsConfigurations = new()
    {
        new SmsConfigurationDto(1, ChannelType.Email, "Gmail SMTP", "info@raadsotech.com", "xlycprlkwcekiebr", "Raadso Hair Clinic", "smtp.gmail.com", 587, true),
        new SmsConfigurationDto(2, ChannelType.WhatsApp, "WasenderAPI", "28cbca7bdf4c1a0e9e0907903e34d34b0b0b8f36622c6f0e91a2b3c4d5e6f70", null, "615052652", null, null, true),
    };
    public static int NextSmsConfigurationId = 3;

    public static ClinicSettingDto ClinicSettings = new(
        Id: 1,
        ClinicName: "ClinicMS Demo Clinic",
        LogoIconUrl: "/assets/images/logo-sm.png",
        LogoUrl: "/assets/images/logo-dark.png",
        SidebarLogoUrl: "/assets/images/logo-dark.png",
        ReportLogoUrl: "/assets/images/logo-dark.png",
        Phone: "+971-4-123-4567",
        Email: "info@clinicms.example",
        Address: "Dubai Healthcare City, Dubai, UAE",
        VatPercentage: 5m,
        IsVatEnabled: true,
        CurrencySymbol: "AED");

    public static MerchantAccountDto MerchantAccount = new(
        Id: 1,
        AccountHolderName: "ClinicMS Demo Clinic LLC",
        BankName: "Emirates NBD",
        AccountNumber: "1015-2233-4455-001",
        Iban: "AE07 0331 2345 6789 0123 456",
        SwiftCode: "EBILAEAD",
        Branch: "Dubai Healthcare City Branch");

    // Active discounts' date ranges must never overlap (enforced by MockSettingsApiClient), so
    // these are deliberately staggered: #1 and #3 are the only two live ranges and don't touch;
    // #2 is active but already closed (fully in the past); #4/#5 are inactive so their ranges are
    // free to overlap anything.
    public static readonly List<DiscountDto> Discounts = new()
    {
        new DiscountDto(1, "New Patient Welcome", DiscountType.ServiceOnly, 10m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)), true),
        new DiscountDto(2, "Summer Promo", DiscountType.ProductOnly, 20m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-15)), true),
        new DiscountDto(3, "Loyalty Members", DiscountType.Both, 15m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(25)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(400)), true),
        new DiscountDto(4, "Holiday Special", DiscountType.Both, 25m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(40)), false),
        new DiscountDto(5, "Retired Promo", DiscountType.ServiceOnly, 5m, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), false),
    };

    public static readonly List<PaymentAccountDto> PaymentAccounts = new()
    {
        new PaymentAccountDto(1, "Front Desk Cash", PaymentAccountType.Cash, null, true),
        new PaymentAccountDto(2, "EVC Plus", PaymentAccountType.Evc, "+252-61-234-5678", true),
        new PaymentAccountDto(3, "POS Merchant Terminal", PaymentAccountType.Merchant, "MERCH-00123", true),
    };
    public static int NextPaymentAccountId = 4;
    public static int NextDiscountId = 6;

    public static readonly List<AuditTrailDto> AuditTrail = new()
    {
        new AuditTrailDto(1, 1, "Users", 2, "Create", null, "{\"username\":\"sara.ahmed\"}", "10.0.0.12", DateTime.UtcNow.AddDays(-20)),
        new AuditTrailDto(2, 3, "Expenses", 4, "Create", null, "{\"title\":\"July payroll\"}", "10.0.0.14", DateTime.UtcNow.AddDays(-2)),
        new AuditTrailDto(3, 1, "Roles", 3, "Update", "{\"isActive\":false}", "{\"isActive\":true}", "10.0.0.12", DateTime.UtcNow.AddDays(-1)),
    };

    public static readonly List<UserLogDto> UserLogs = new()
    {
        new UserLogDto(1, 1, "admin", "Login", "10.0.0.12", "Mozilla/5.0", DateTime.UtcNow.AddDays(-1).AddHours(-2)),
        new UserLogDto(2, 2, "sara.ahmed", "Login", "10.0.0.13", "Mozilla/5.0", DateTime.UtcNow.AddHours(-6)),
        new UserLogDto(3, 3, "omar.khaled", "Login", "10.0.0.14", "Mozilla/5.0", DateTime.UtcNow.AddHours(-1)),
    };
}
