using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Payments;
using ClinicMS.Web.Services.Api;
using ClinicMS.Web.Services.Api.Db;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicMS.Web.Tests;

public class PaymentsApiClientTests
{
    private static async Task<(ClinicMsDbContext Db, DbPaymentsApiClient Client, int PatientId, int InvoiceId)> SeedInvoiceAsync(
        decimal netAmount, decimal paidAmount)
    {
        var db = TestDb.Create();
        var patient = new PatientEntity { FullName = "Nasra Hassan", Phone = "0611112222" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var invoice = new InvoiceEntity
        {
            InvoiceNumber = "INV-2026-1", PatientId = patient.Id, InvoiceType = "Walk-in",
            TotalAmount = netAmount, NetAmount = netAmount, PaidAmount = paidAmount, BalanceDue = netAmount - paidAmount,
            PaymentStatus = paidAmount <= 0 ? "Unpaid" : (netAmount - paidAmount) <= 0 ? "Paid" : "Partial",
            InvoiceDate = DateTime.UtcNow,
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        return (db, new DbPaymentsApiClient(db), patient.Id, invoice.Id);
    }

    [Fact]
    public async Task CreatePayment_PartialAmount_LeavesBalanceDueAndMarksPartial()
    {
        var (db, client, patientId, invoiceId) = await SeedInvoiceAsync(netAmount: 300m, paidAmount: 0m);

        await client.CreatePaymentAsync(new CreatePaymentRequest(invoiceId, patientId, 100m, PaymentMethod.Cash, null, null), default);

        var invoice = await db.Invoices.SingleAsync(i => i.Id == invoiceId);
        Assert.Equal(200m, invoice.BalanceDue);
        Assert.Equal("Partial", invoice.PaymentStatus);
        db.Dispose();
    }

    [Fact]
    public async Task CreatePayment_SettlingFullBalance_MarksInvoicePaid()
    {
        var (db, client, patientId, invoiceId) = await SeedInvoiceAsync(netAmount: 300m, paidAmount: 200m);

        await client.CreatePaymentAsync(new CreatePaymentRequest(invoiceId, patientId, 100m, PaymentMethod.Cash, null, null), default);

        var invoice = await db.Invoices.SingleAsync(i => i.Id == invoiceId);
        Assert.Equal(0m, invoice.BalanceDue);
        Assert.Equal("Paid", invoice.PaymentStatus);
        db.Dispose();
    }

    [Fact]
    public async Task CreatePayment_ZeroOrNegativeAmount_Throws()
    {
        var (db, client, patientId, invoiceId) = await SeedInvoiceAsync(netAmount: 300m, paidAmount: 0m);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            client.CreatePaymentAsync(new CreatePaymentRequest(invoiceId, patientId, 0m, PaymentMethod.Cash, null, null), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task CreatePayment_UnknownInvoiceId_Throws()
    {
        var (db, client, patientId, _) = await SeedInvoiceAsync(netAmount: 300m, paidAmount: 0m);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            client.CreatePaymentAsync(new CreatePaymentRequest(999_999, patientId, 50m, PaymentMethod.Cash, null, null), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task CreatePayment_UnknownPatientId_Throws()
    {
        var (db, client, _, invoiceId) = await SeedInvoiceAsync(netAmount: 300m, paidAmount: 0m);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            client.CreatePaymentAsync(new CreatePaymentRequest(invoiceId, 999_999, 50m, PaymentMethod.Cash, null, null), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task CreatePayment_WithoutAnInvoice_StillRecordsAWalletTopUpPayment()
    {
        var db = TestDb.Create();
        var patient = new PatientEntity { FullName = "Ayan Warsame", Phone = "0699998888" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        var client = new DbPaymentsApiClient(db);

        var payment = await client.CreatePaymentAsync(new CreatePaymentRequest(null, patient.Id, 50m, PaymentMethod.WalletCredit, null, null), default);

        Assert.Null(payment.InvoiceId);
        Assert.Equal(50m, payment.AmountPaid);
        db.Dispose();
    }

    [Fact]
    public async Task GetOutstandingInvoices_OnlyReturnsInvoicesWithAPositiveBalance()
    {
        var (db, client, _, _) = await SeedInvoiceAsync(netAmount: 300m, paidAmount: 0m);
        var patient2 = new PatientEntity { FullName = "Settled Patient", Phone = "0688887777" };
        db.Patients.Add(patient2);
        await db.SaveChangesAsync();
        db.Invoices.Add(new InvoiceEntity
        {
            InvoiceNumber = "INV-2026-2", PatientId = patient2.Id, InvoiceType = "Walk-in",
            TotalAmount = 150m, NetAmount = 150m, PaidAmount = 150m, BalanceDue = 0m, PaymentStatus = "Paid", InvoiceDate = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var outstanding = await client.GetOutstandingInvoicesAsync(default);

        var only = Assert.Single(outstanding);
        Assert.Equal(300m, only.BalanceDue);
        db.Dispose();
    }

    [Fact]
    public async Task GetAccountBreakdown_GroupsIncomeAndExpenseByAccountAndComputesNet()
    {
        var db = TestDb.Create();
        var account = new PaymentAccountEntity { Name = "Main Cash Drawer", AccountType = "Cash", IsActive = true };
        db.PaymentAccounts.Add(account);
        var patient = new PatientEntity { FullName = "Zeinab Omar", Phone = "0677776666" };
        db.Patients.Add(patient);
        var category = new ExpenseCategoryEntity { CategoryName = "Utilities" };
        db.ExpenseCategories.Add(category);
        await db.SaveChangesAsync();

        db.Payments.Add(new PaymentEntity { PatientId = patient.Id, AmountPaid = 500m, PaymentMethod = "Cash", PaymentDate = DateTime.UtcNow, AccountId = account.Id });
        db.Payments.Add(new PaymentEntity { PatientId = patient.Id, AmountPaid = 100m, PaymentMethod = "Cash", PaymentDate = DateTime.UtcNow, AccountId = null });
        db.Expenses.Add(new ExpenseEntity { ExpenseCategoryId = category.Id, Title = "Electricity", Amount = 200m, ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow), PaymentMethod = "Cash", CreatedAt = DateTime.UtcNow, AccountId = account.Id });
        await db.SaveChangesAsync();

        var client = new DbPaymentsApiClient(db);
        var breakdown = await client.GetAccountBreakdownAsync(default);

        var namedAccount = breakdown.Single(a => a.AccountId == account.Id);
        Assert.Equal(500m, namedAccount.TotalIncome);
        Assert.Equal(200m, namedAccount.TotalExpense);
        Assert.Equal(300m, namedAccount.Net);

        var unassigned = breakdown.Single(a => a.AccountId == null);
        Assert.Equal(100m, unassigned.TotalIncome);
        Assert.Equal("Unassigned", unassigned.AccountName);
        db.Dispose();
    }
}
