using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.MedicalServices;
using ClinicMS.Web.Services.Api;
using ClinicMS.Web.Services.Api.Db;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicMS.Web.Tests;

public class SoftDeleteTests
{
    [Fact]
    public async Task DeletePatient_MarksDeletedRatherThanRemovingTheRow()
    {
        var db = TestDb.Create();
        var patient = new PatientEntity { FullName = "Amina Yusuf", Phone = "0611111111" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var client = new DbPatientsApiClient(db);
        await client.DeleteAsync(patient.Id, default);

        // The row still physically exists...
        var stillThere = await db.Patients.IgnoreQueryFilters().SingleAsync(p => p.Id == patient.Id);
        Assert.True(stillThere.IsDeleted);
        Assert.NotNull(stillThere.DeletedAt);

        // ...but every normal read (which goes through the global query filter) no longer sees it.
        var all = await client.GetAllAsync(default);
        Assert.Empty(all);
    }

    [Fact]
    public async Task DeletePatient_RemovedFromPagedResultsToo()
    {
        var db = TestDb.Create();
        var patient = new PatientEntity { FullName = "Hodan Ali", Phone = "0622222222" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var client = new DbPatientsApiClient(db);
        await client.DeleteAsync(patient.Id, default);

        var page = await client.GetPagedAsync(1, 10, null, null, default);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task DeletePatientCycle_SoftDeletesCycleSessionsInvoicesAndPayments()
    {
        var db = TestDb.Create();
        var patient = new PatientEntity { FullName = "Faduma Nur", Phone = "0633333333" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var cycle = new PatientCycleEntity
        {
            PatientId = patient.Id, PlanId = 1, CycleName = "Laser Package", PricingModel = "FixedPackage",
            AgreedTotalPrice = 300m, Frequency = "Weekly", StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "Active", CreatedAt = DateTime.UtcNow,
        };
        db.PatientCycles.Add(cycle);
        await db.SaveChangesAsync();

        var session = new CycleSessionEntity
        {
            CycleId = cycle.Id, SessionNumber = 1,
            OriginalScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow), ActualScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "Completed",
        };
        db.CycleSessions.Add(session);
        await db.SaveChangesAsync();

        var invoice = new InvoiceEntity
        {
            InvoiceNumber = "INV-1", PatientId = patient.Id, SessionId = session.Id, InvoiceType = "Cycle Session",
            TotalAmount = 100m, NetAmount = 100m, PaidAmount = 100m, BalanceDue = 0m, PaymentStatus = "Paid", InvoiceDate = DateTime.UtcNow,
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        db.Payments.Add(new PaymentEntity { InvoiceId = invoice.Id, PatientId = patient.Id, AmountPaid = 100m, PaymentMethod = "Cash", PaymentDate = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var client = new DbMedicalServicesApiClient(db);
        await client.DeletePatientCycleAsync(cycle.Id, default);

        // Every row involved is still physically present...
        Assert.True((await db.PatientCycles.IgnoreQueryFilters().SingleAsync(c => c.Id == cycle.Id)).IsDeleted);
        Assert.True((await db.CycleSessions.IgnoreQueryFilters().SingleAsync(s => s.Id == session.Id)).IsDeleted);
        Assert.True((await db.Invoices.IgnoreQueryFilters().SingleAsync(i => i.Id == invoice.Id)).IsDeleted);
        Assert.True((await db.Payments.IgnoreQueryFilters().SingleAsync(p => p.InvoiceId == invoice.Id)).IsDeleted);

        // ...but none of them show up through the normal (filtered) DbSet anymore.
        Assert.Equal(0, await db.PatientCycles.CountAsync());
        Assert.Equal(0, await db.CycleSessions.CountAsync());
        Assert.Equal(0, await db.Invoices.CountAsync());
        Assert.Equal(0, await db.Payments.CountAsync());
    }

    [Fact]
    public async Task DeletePatientCycle_DeletedInvoiceNoLongerAppearsInOutstandingInvoices()
    {
        var db = TestDb.Create();
        var patient = new PatientEntity { FullName = "Ikran Warsame", Phone = "0644444444" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var cycle = new PatientCycleEntity
        {
            PatientId = patient.Id, PlanId = 1, CycleName = "Deposit Package", PricingModel = "FixedPackage",
            AgreedTotalPrice = 200m, Frequency = "Weekly", StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "Active", CreatedAt = DateTime.UtcNow,
        };
        db.PatientCycles.Add(cycle);
        await db.SaveChangesAsync();

        db.Invoices.Add(new InvoiceEntity
        {
            InvoiceNumber = $"CYCLE-{cycle.Id}", PatientId = patient.Id, InvoiceType = "Cycle Package",
            TotalAmount = 200m, NetAmount = 200m, PaidAmount = 0m, BalanceDue = 200m, PaymentStatus = "Unpaid", InvoiceDate = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var medicalClient = new DbMedicalServicesApiClient(db);
        var paymentsClient = new DbPaymentsApiClient(db);

        var beforeDelete = await paymentsClient.GetOutstandingInvoicesAsync(default);
        Assert.Single(beforeDelete);

        await medicalClient.DeletePatientCycleAsync(cycle.Id, default);

        var afterDelete = await paymentsClient.GetOutstandingInvoicesAsync(default);
        Assert.Empty(afterDelete);
        db.Dispose();
    }
}
