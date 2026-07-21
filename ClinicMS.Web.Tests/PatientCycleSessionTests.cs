using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.MedicalServices;
using ClinicMS.Web.Models.Api.Payments;
using ClinicMS.Web.Services.Api;
using ClinicMS.Web.Services.Api.Db;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicMS.Web.Tests;

public class PatientCycleSessionTests
{
    private static async Task<(ClinicMsDbContext Db, DbMedicalServicesApiClient Client, int CycleId, int ServiceId)> SeedPerVisitCycleAsync(
        bool vatEnabled = true, decimal vatPercentage = 5m, int sessionCount = 1)
    {
        var db = TestDb.Create();
        db.ClinicSettings.Add(new ClinicSettingEntity { IsVatEnabled = vatEnabled, VatPercentage = vatPercentage });
        var patient = new PatientEntity { FullName = "Amina Yusuf", Phone = "0611111111" };
        var service = new ServiceEntity { ServiceName = "Consultation", StandardPrice = 100m, IsActive = true };
        db.Patients.Add(patient);
        db.Services.Add(service);
        await db.SaveChangesAsync();

        var cycle = new PatientCycleEntity
        {
            PatientId = patient.Id, PlanId = 1, CycleName = "Consultation Plan", PricingModel = "PerVisit",
            Frequency = "Weekly", StartDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Active", CreatedAt = DateTime.UtcNow,
        };
        db.PatientCycles.Add(cycle);
        await db.SaveChangesAsync();

        for (var i = 1; i <= sessionCount; i++)
        {
            var session = new CycleSessionEntity
            {
                CycleId = cycle.Id, SessionNumber = i,
                OriginalScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow), ActualScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "Upcoming",
            };
            db.CycleSessions.Add(session);
            await db.SaveChangesAsync();
            db.SessionItems.Add(new SessionItemEntity { SessionId = session.Id, ItemType = "Service", ServiceId = service.Id, Quantity = 1, UnitPrice = service.StandardPrice });
        }
        await db.SaveChangesAsync();

        return (db, new DbMedicalServicesApiClient(db), cycle.Id, service.Id);
    }

    [Fact]
    public async Task CompleteSession_PerVisit_RecomputesVatFromClinicSettingsRegardlessOfClient()
    {
        var (db, client, cycleId, _) = await SeedPerVisitCycleAsync(vatEnabled: true, vatPercentage: 5m);

        var result = await client.CompletePatientCycleSessionAsync(cycleId,
            new CompletePatientCycleSessionRequest(1, DiscountAmount: 0m, PaidAmount: 105m, PaymentMethod.Cash, null, null), default);

        var session = Assert.Single(result.Sessions);
        Assert.Equal(PatientSessionStatus.Completed, session.Status);
        Assert.Equal(100m, session.ChargeAmount);
        Assert.Equal(5m, session.VatAmount);
        db.Dispose();
    }

    [Fact]
    public async Task CompleteSession_VatDisabledInSettings_ChargesNoVatEvenIfServicePriceIsRound()
    {
        var (db, client, cycleId, _) = await SeedPerVisitCycleAsync(vatEnabled: false);

        var result = await client.CompletePatientCycleSessionAsync(cycleId,
            new CompletePatientCycleSessionRequest(1, DiscountAmount: 0m, PaidAmount: 100m, PaymentMethod.Cash, null, null), default);

        Assert.Equal(0m, Assert.Single(result.Sessions).VatAmount);
        db.Dispose();
    }

    [Fact]
    public async Task CompleteSession_DiscountExceedingSessionTotal_Throws()
    {
        var (db, client, cycleId, _) = await SeedPerVisitCycleAsync();

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.CompletePatientCycleSessionAsync(cycleId,
            new CompletePatientCycleSessionRequest(1, DiscountAmount: 200m, PaidAmount: 1m, PaymentMethod.Cash, null, null), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task CompleteSession_PaidAmountExceedingNetAmount_Throws()
    {
        var (db, client, cycleId, _) = await SeedPerVisitCycleAsync(vatEnabled: false);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.CompletePatientCycleSessionAsync(cycleId,
            new CompletePatientCycleSessionRequest(1, DiscountAmount: 0m, PaidAmount: 999m, PaymentMethod.Cash, null, null), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task CompleteSession_ZeroPaidAmount_Throws()
    {
        var (db, client, cycleId, _) = await SeedPerVisitCycleAsync(vatEnabled: false);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.CompletePatientCycleSessionAsync(cycleId,
            new CompletePatientCycleSessionRequest(1, DiscountAmount: 0m, PaidAmount: 0m, PaymentMethod.Cash, null, null), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task CompleteSession_AlreadyCompleted_Throws()
    {
        var (db, client, cycleId, _) = await SeedPerVisitCycleAsync(vatEnabled: false);
        await client.CompletePatientCycleSessionAsync(cycleId,
            new CompletePatientCycleSessionRequest(1, DiscountAmount: 0m, PaidAmount: 100m, PaymentMethod.Cash, null, null), default);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.CompletePatientCycleSessionAsync(cycleId,
            new CompletePatientCycleSessionRequest(1, DiscountAmount: 0m, PaidAmount: 100m, PaymentMethod.Cash, null, null), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task CompleteSession_MissingPaymentMethod_Throws()
    {
        var (db, client, cycleId, _) = await SeedPerVisitCycleAsync(vatEnabled: false);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.CompletePatientCycleSessionAsync(cycleId,
            new CompletePatientCycleSessionRequest(1, DiscountAmount: 0m, PaidAmount: 100m, PaymentMethod: null, null, null), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    private static async Task<(ClinicMsDbContext Db, DbMedicalServicesApiClient Client, int CycleId)> SeedFixedPackagePerSessionCycleAsync(
        decimal agreedTotalPrice, int sessionCount)
    {
        var db = TestDb.Create();
        db.ClinicSettings.Add(new ClinicSettingEntity { IsVatEnabled = false });
        var patient = new PatientEntity { FullName = "Hodan Ali", Phone = "0622222222" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var cycle = new PatientCycleEntity
        {
            PatientId = patient.Id, PlanId = 1, CycleName = "Laser Package", PricingModel = "FixedPackage",
            AgreedTotalPrice = agreedTotalPrice, Frequency = "Weekly",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Active", CreatedAt = DateTime.UtcNow,
        };
        db.PatientCycles.Add(cycle);
        await db.SaveChangesAsync();

        for (var i = 1; i <= sessionCount; i++)
        {
            db.CycleSessions.Add(new CycleSessionEntity
            {
                CycleId = cycle.Id, SessionNumber = i,
                OriginalScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow), ActualScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "Upcoming",
            });
        }
        await db.SaveChangesAsync();

        return (db, new DbMedicalServicesApiClient(db), cycle.Id);
    }

    [Fact]
    public async Task CompleteSession_FixedPackagePerSession_SplitsPriceEvenlyAcrossSessions()
    {
        var (db, client, cycleId) = await SeedFixedPackagePerSessionCycleAsync(agreedTotalPrice: 300m, sessionCount: 3);

        var result = await client.CompletePatientCycleSessionAsync(cycleId,
            new CompletePatientCycleSessionRequest(1, DiscountAmount: 0m, PaidAmount: 100m, PaymentMethod.Cash, null, null), default);

        var session = result.Sessions.Single(s => s.SessionNumber == 1);
        Assert.Equal(100m, session.ChargeAmount);
        db.Dispose();
    }

    [Fact]
    public async Task CompleteSession_FixedPackagePerSession_LastSessionAbsorbsRoundingRemainder()
    {
        // 100 / 3 = 33.33 (rounded) per session; the first two sessions charge 33.33 each (66.66
        // total), so the last session must charge the true remainder (33.34) rather than another
        // rounded 33.33 -- otherwise the package would always undercharge by a cent on 3-way splits.
        var (db, client, cycleId) = await SeedFixedPackagePerSessionCycleAsync(agreedTotalPrice: 100m, sessionCount: 3);

        await client.CompletePatientCycleSessionAsync(cycleId,
            new CompletePatientCycleSessionRequest(1, DiscountAmount: 0m, PaidAmount: 33.33m, PaymentMethod.Cash, null, null), default);
        await client.CompletePatientCycleSessionAsync(cycleId,
            new CompletePatientCycleSessionRequest(2, DiscountAmount: 0m, PaidAmount: 33.33m, PaymentMethod.Cash, null, null), default);
        var result = await client.CompletePatientCycleSessionAsync(cycleId,
            new CompletePatientCycleSessionRequest(3, DiscountAmount: 0m, PaidAmount: 33.34m, PaymentMethod.Cash, null, null), default);

        var lastSession = result.Sessions.Single(s => s.SessionNumber == 3);
        Assert.Equal(33.34m, lastSession.ChargeAmount);
        db.Dispose();
    }

    [Fact]
    public async Task CompleteSession_FixedPackageDepositBalanceMode_MarksCompletedWithoutChargingAgain()
    {
        var db = TestDb.Create();
        db.ClinicSettings.Add(new ClinicSettingEntity { IsVatEnabled = false });
        var patient = new PatientEntity { FullName = "Faduma Nur", Phone = "0633333333" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var cycle = new PatientCycleEntity
        {
            PatientId = patient.Id, PlanId = 1, CycleName = "Deposit Package", PricingModel = "FixedPackage",
            AgreedTotalPrice = 500m, Frequency = "Weekly",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Active", CreatedAt = DateTime.UtcNow,
        };
        db.PatientCycles.Add(cycle);
        await db.SaveChangesAsync();

        // A deposit-balance package already has its single invoice created at assignment time
        // (InvoiceNumber == "CYCLE-{id}") -- its presence is exactly what tells CompletePatientCycleSessionAsync
        // not to create a second per-session charge.
        db.Invoices.Add(new InvoiceEntity
        {
            InvoiceNumber = $"CYCLE-{cycle.Id}", PatientId = patient.Id, InvoiceType = "Cycle Package",
            TotalAmount = 500m, NetAmount = 500m, PaidAmount = 200m, BalanceDue = 300m, PaymentStatus = "Partial", InvoiceDate = DateTime.UtcNow,
        });
        db.CycleSessions.Add(new CycleSessionEntity
        {
            CycleId = cycle.Id, SessionNumber = 1,
            OriginalScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow), ActualScheduledDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Status = "Upcoming",
        });
        await db.SaveChangesAsync();

        var client = new DbMedicalServicesApiClient(db);
        var result = await client.CompletePatientCycleSessionAsync(cycle.Id,
            new CompletePatientCycleSessionRequest(1, DiscountAmount: 0m, PaidAmount: 0m, PaymentMethod: null, null, null), default);

        Assert.Equal(PatientSessionStatus.Completed, Assert.Single(result.Sessions).Status);
        Assert.Equal(1, await db.Invoices.CountAsync());
        db.Dispose();
    }

    [Fact]
    public async Task RecordCyclePayment_ReducesBalanceAndFlipsStatusToPaidWhenSettled()
    {
        var db = TestDb.Create();
        var patient = new PatientEntity { FullName = "Ikran Warsame", Phone = "0644444444" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();

        var cycle = new PatientCycleEntity
        {
            PatientId = patient.Id, PlanId = 1, CycleName = "Deposit Package", PricingModel = "FixedPackage",
            AgreedTotalPrice = 200m, Frequency = "Weekly",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Active", CreatedAt = DateTime.UtcNow,
        };
        db.PatientCycles.Add(cycle);
        await db.SaveChangesAsync();
        db.Invoices.Add(new InvoiceEntity
        {
            InvoiceNumber = $"CYCLE-{cycle.Id}", PatientId = patient.Id, InvoiceType = "Cycle Package",
            TotalAmount = 200m, NetAmount = 200m, PaidAmount = 0m, BalanceDue = 200m, PaymentStatus = "Unpaid", InvoiceDate = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var client = new DbMedicalServicesApiClient(db);
        await client.RecordCyclePaymentAsync(cycle.Id, new RecordCyclePaymentRequest(120m, PaymentMethod.Cash, null, null), default);
        var invoiceAfterFirst = await db.Invoices.SingleAsync(i => i.InvoiceNumber == $"CYCLE-{cycle.Id}");
        Assert.Equal(80m, invoiceAfterFirst.BalanceDue);
        Assert.Equal("Partial", invoiceAfterFirst.PaymentStatus);

        await client.RecordCyclePaymentAsync(cycle.Id, new RecordCyclePaymentRequest(80m, PaymentMethod.Cash, null, null), default);
        var invoiceAfterSecond = await db.Invoices.SingleAsync(i => i.InvoiceNumber == $"CYCLE-{cycle.Id}");
        Assert.Equal(0m, invoiceAfterSecond.BalanceDue);
        Assert.Equal("Paid", invoiceAfterSecond.PaymentStatus);
        db.Dispose();
    }

    [Fact]
    public async Task RecordCyclePayment_ExceedingRemainingBalance_Throws()
    {
        var db = TestDb.Create();
        var patient = new PatientEntity { FullName = "Sagal Dahir", Phone = "0655555555" };
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        var cycle = new PatientCycleEntity
        {
            PatientId = patient.Id, PlanId = 1, CycleName = "Deposit Package", PricingModel = "FixedPackage",
            AgreedTotalPrice = 200m, Frequency = "Weekly",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Active", CreatedAt = DateTime.UtcNow,
        };
        db.PatientCycles.Add(cycle);
        await db.SaveChangesAsync();
        db.Invoices.Add(new InvoiceEntity
        {
            InvoiceNumber = $"CYCLE-{cycle.Id}", PatientId = patient.Id, InvoiceType = "Cycle Package",
            TotalAmount = 200m, NetAmount = 200m, PaidAmount = 0m, BalanceDue = 200m, PaymentStatus = "Unpaid", InvoiceDate = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var client = new DbMedicalServicesApiClient(db);
        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            client.RecordCyclePaymentAsync(cycle.Id, new RecordCyclePaymentRequest(250m, PaymentMethod.Cash, null, null), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }

    [Fact]
    public async Task RecordCyclePayment_OnAPerVisitCycleWithNoPackageInvoice_Throws()
    {
        var (db, client, cycleId, _) = await SeedPerVisitCycleAsync();

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            client.RecordCyclePaymentAsync(cycleId, new RecordCyclePaymentRequest(10m, PaymentMethod.Cash, null, null), default));

        Assert.Equal(400, ex.StatusCode);
        db.Dispose();
    }
}
