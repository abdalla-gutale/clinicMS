using ClinicMS.Web.Data;
using ClinicMS.Web.Services.Api.Db;
using Xunit;

namespace ClinicMS.Web.Tests;

public class OutstandingInvoicesPagingTests
{
    private static async Task<DbPaymentsApiClient> SeedAsync(int outstandingCount, int settledCount = 0)
    {
        var db = TestDb.Create();
        for (var i = 1; i <= outstandingCount; i++)
        {
            var patient = new PatientEntity { FullName = $"Patient {i:D3}", Phone = $"06{i:D8}" };
            db.Patients.Add(patient);
            await db.SaveChangesAsync();

            db.Invoices.Add(new InvoiceEntity
            {
                InvoiceNumber = $"INV-{i}", PatientId = patient.Id, InvoiceType = "Walk-in",
                TotalAmount = 100m, NetAmount = 100m, PaidAmount = 0m, BalanceDue = 100m,
                PaymentStatus = "Unpaid", InvoiceDate = DateTime.UtcNow.AddMinutes(-i),
            });
        }
        for (var i = 1; i <= settledCount; i++)
        {
            db.Invoices.Add(new InvoiceEntity
            {
                InvoiceNumber = $"SETTLED-{i}", InvoiceType = "Walk-in",
                TotalAmount = 50m, NetAmount = 50m, PaidAmount = 50m, BalanceDue = 0m,
                PaymentStatus = "Paid", InvoiceDate = DateTime.UtcNow,
            });
        }
        await db.SaveChangesAsync();
        return new DbPaymentsApiClient(db);
    }

    [Fact]
    public async Task GetOutstandingPaged_ExcludesSettledInvoices()
    {
        var client = await SeedAsync(outstandingCount: 5, settledCount: 3);

        var result = await client.GetOutstandingInvoicesPagedAsync(1, 100, null, default);

        Assert.Equal(5, result.TotalCount);
    }

    [Fact]
    public async Task GetOutstandingPaged_ReturnsRequestedPageSize()
    {
        var client = await SeedAsync(25);

        var page1 = await client.GetOutstandingInvoicesPagedAsync(1, 10, null, default);
        var page3 = await client.GetOutstandingInvoicesPagedAsync(3, 10, null, default);

        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(5, page3.Items.Count);
        Assert.Equal(25, page1.TotalCount);
    }

    [Fact]
    public async Task GetOutstandingPaged_SearchFiltersByPatientName()
    {
        var client = await SeedAsync(5);

        var result = await client.GetOutstandingInvoicesPagedAsync(1, 10, "003", default);

        var only = Assert.Single(result.Items);
        Assert.Equal("Patient 003", only.PatientName);
    }
}
