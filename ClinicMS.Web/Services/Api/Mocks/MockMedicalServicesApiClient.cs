using ClinicMS.Web.Models.Api.MedicalServices;
using ClinicMS.Web.Models.Api.Payments;

namespace ClinicMS.Web.Services.Api.Mocks;

public class MockMedicalServicesApiClient : IMedicalServicesApiClient
{
    private readonly ISettingsApiClient _settingsApiClient;

    public MockMedicalServicesApiClient(ISettingsApiClient settingsApiClient)
    {
        _settingsApiClient = settingsApiClient;
    }

    public Task<IReadOnlyList<ServiceTypeDto>> GetServiceTypesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceTypeDto>>(MockStore.ServiceTypes.ToList());

    public Task<ServiceTypeDto> CreateServiceTypeAsync(CreateServiceTypeRequest request, CancellationToken cancellationToken = default)
    {
        var type = new ServiceTypeDto(MockStore.NextServiceTypeId++, request.TypeName, request.Description, request.IsActive);
        MockStore.ServiceTypes.Add(type);
        return Task.FromResult(type);
    }

    public Task<ServiceTypeDto> UpdateServiceTypeAsync(int id, UpdateServiceTypeRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.ServiceTypes.FindIndex(t => t.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Service type not found.");
        }

        var updated = new ServiceTypeDto(id, request.TypeName, request.Description, request.IsActive);
        MockStore.ServiceTypes[index] = updated;

        // Keep denormalized ServiceTypeName in sync on any services under this type.
        for (var i = 0; i < MockStore.Services.Count; i++)
        {
            if (MockStore.Services[i].ServiceTypeId == id)
            {
                MockStore.Services[i] = MockStore.Services[i] with { ServiceTypeName = request.TypeName };
            }
        }

        return Task.FromResult(updated);
    }

    public Task DeleteServiceTypeAsync(int id, CancellationToken cancellationToken = default)
    {
        if (MockStore.Services.Any(s => s.ServiceTypeId == id))
        {
            throw new ApiException(400, "Cannot delete a service type that still has services under it.");
        }

        var removed = MockStore.ServiceTypes.RemoveAll(t => t.Id == id);
        if (removed == 0)
        {
            throw new ApiException(404, "Service type not found.");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ServiceDto>>(MockStore.Services.ToList());

    public Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var type = MockStore.ServiceTypes.FirstOrDefault(t => t.Id == request.ServiceTypeId)
            ?? throw new ApiException(400, "Selected service type does not exist.");

        var service = new ServiceDto(
            MockStore.NextServiceId++, type.Id, type.TypeName, request.ServiceName, request.Price, request.Description, request.IsActive);

        MockStore.Services.Add(service);
        return Task.FromResult(service);
    }

    public Task<ServiceDto> UpdateServiceAsync(int id, UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.Services.FindIndex(s => s.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Service not found.");
        }

        var type = MockStore.ServiceTypes.FirstOrDefault(t => t.Id == request.ServiceTypeId)
            ?? throw new ApiException(400, "Selected service type does not exist.");

        var updated = new ServiceDto(id, type.Id, type.TypeName, request.ServiceName, request.Price, request.Description, request.IsActive);
        MockStore.Services[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteServiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = MockStore.Services.RemoveAll(s => s.Id == id);
        if (removed == 0)
        {
            throw new ApiException(404, "Service not found.");
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ProductOption>> GetProductOptionsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProductOption>>(MockStore.ProductOptions.ToList());

    public Task<IReadOnlyList<TreatmentPlanDto>> GetTreatmentPlansAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TreatmentPlanDto>>(MockStore.TreatmentPlans.ToList());

    public Task<TreatmentPlanDto> CreateTreatmentPlanAsync(CreateTreatmentPlanRequest request, CancellationToken cancellationToken = default)
    {
        ValidateTreatmentPlan(request.PricingModelType, request.FixedPackagePrice, request.TotalSessions, request.Sessions);

        var plan = new TreatmentPlanDto(
            MockStore.NextTreatmentPlanId++,
            request.PlanName,
            request.PricingModelType,
            request.PricingModelType == PricingModelType.FixedPackage ? request.FixedPackagePrice : null,
            request.Frequency,
            request.TotalSessions,
            request.Sessions.Select(s => new TreatmentPlanSessionDto(s.SessionNumber, s.Label, s.ServiceIds, s.ProductIds)).ToList());

        MockStore.TreatmentPlans.Add(plan);
        return Task.FromResult(plan);
    }

    public Task<TreatmentPlanDto> UpdateTreatmentPlanAsync(int id, UpdateTreatmentPlanRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.TreatmentPlans.FindIndex(p => p.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Treatment plan not found.");
        }

        ValidateTreatmentPlan(request.PricingModelType, request.FixedPackagePrice, request.TotalSessions, request.Sessions);

        var updated = new TreatmentPlanDto(
            id,
            request.PlanName,
            request.PricingModelType,
            request.PricingModelType == PricingModelType.FixedPackage ? request.FixedPackagePrice : null,
            request.Frequency,
            request.TotalSessions,
            request.Sessions.Select(s => new TreatmentPlanSessionDto(s.SessionNumber, s.Label, s.ServiceIds, s.ProductIds)).ToList());

        MockStore.TreatmentPlans[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteTreatmentPlanAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = MockStore.TreatmentPlans.RemoveAll(p => p.Id == id);
        if (removed == 0)
        {
            throw new ApiException(404, "Treatment plan not found.");
        }

        return Task.CompletedTask;
    }

    private static void ValidateTreatmentPlan(
        PricingModelType pricingModelType, decimal? fixedPackagePrice, int totalSessions, IReadOnlyList<TreatmentPlanSessionRequest> sessions)
    {
        if (pricingModelType == PricingModelType.FixedPackage && (fixedPackagePrice is null || fixedPackagePrice <= 0))
        {
            throw new ApiException(400, "Fixed package price is required when the pricing model is Fixed Package.");
        }

        if (totalSessions != sessions.Count)
        {
            throw new ApiException(400, "The number of sessions does not match the total sessions specified.");
        }

        var emptySession = sessions.FirstOrDefault(s => s.ServiceIds.Count == 0 && s.ProductIds.Count == 0);
        if (emptySession is not null)
        {
            throw new ApiException(400, $"\"{emptySession.Label}\" must have at least one service or product selected.");
        }
    }

    public Task<IReadOnlyList<PatientCycleDto>> GetPatientCyclesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PatientCycleDto>>(MockStore.PatientCycles.Select(WithComputedStatus).ToList());

    public Task<PatientCycleDto> AssignPatientCycleAsync(AssignPatientCycleRequest request, CancellationToken cancellationToken = default)
    {
        var patient = MockStore.PatientRecords.FirstOrDefault(p => p.Id == request.PatientId)
            ?? throw new ApiException(400, "Selected patient does not exist.");

        var plan = MockStore.TreatmentPlans.FirstOrDefault(p => p.Id == request.TreatmentPlanId)
            ?? throw new ApiException(400, "Selected treatment plan does not exist.");

        // A patient may only have one unfinished cycle at a time -- Paused counts as unfinished
        // since it's just an Active cycle with an overdue session, not a completed one.
        var hasUnfinishedCycle = MockStore.PatientCycles
            .Where(c => c.PatientId == request.PatientId)
            .Any(c => ComputeCycleStatus(c.Sessions) != PatientCycleStatus.Completed);
        if (hasUnfinishedCycle)
        {
            throw new ApiException(400, "This patient already has an active treatment cycle. Complete or finish it before assigning a new one.");
        }

        if (request.StartDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ApiException(400, "Start date cannot be in the past.");
        }

        IReadOnlyList<PatientCycleSessionRequest> sessionSource;
        if (plan.PricingModelType == PricingModelType.FixedPackage)
        {
            // Fixed package sessions are not editable at assign time -- always clone the plan's own template.
            sessionSource = plan.Sessions.Select(s => new PatientCycleSessionRequest(s.SessionNumber, s.ServiceIds, s.ProductIds)).ToList();
        }
        else
        {
            if (request.Sessions.Count != plan.TotalSessions)
            {
                throw new ApiException(400, "The number of sessions does not match the total sessions specified.");
            }

            var emptySession = request.Sessions.FirstOrDefault(s => s.ServiceIds.Count == 0 && s.ProductIds.Count == 0);
            if (emptySession is not null)
            {
                throw new ApiException(400, $"Session {emptySession.SessionNumber} must have at least one service or product selected.");
            }

            sessionSource = request.Sessions;
        }

        FixedPackagePaymentMode? paymentMode = null;
        decimal? totalPrice = null;
        decimal? balanceDue = null;
        var paidAmount = 0m;

        if (plan.PricingModelType == PricingModelType.FixedPackage)
        {
            paymentMode = request.PaymentMode ?? FixedPackagePaymentMode.PerSession;
            totalPrice = plan.FixedPackagePrice ?? 0m;
            balanceDue = totalPrice;

            if (paymentMode == FixedPackagePaymentMode.DepositBalance && request.DepositAmount is > 0)
            {
                if (request.DepositAmount > totalPrice)
                {
                    throw new ApiException(400, "Deposit cannot exceed the package price.");
                }

                var depositMethod = request.DepositPaymentMethod
                    ?? throw new ApiException(400, "A payment method is required to record the deposit.");

                var depositAccountName = request.DepositAccountId is int depositAccountId
                    ? MockStore.PaymentAccounts.FirstOrDefault(a => a.Id == depositAccountId)?.Name
                    : null;

                var deposit = new PaymentDto(
                    MockStore.NextPaymentId++, null, patient.Id, patient.FullName, request.DepositAmount.Value, depositMethod, "Initial deposit", DateTime.UtcNow,
                    request.DepositAccountId, depositAccountName);
                MockStore.Payments.Add(deposit);

                paidAmount = request.DepositAmount.Value;
                balanceDue = totalPrice - paidAmount;
            }
        }

        var cycle = new PatientCycleDto(
            MockStore.NextPatientCycleId++,
            patient.Id,
            patient.FullName,
            plan.Id,
            plan.PlanName,
            plan.PricingModelType,
            plan.Frequency,
            plan.TotalSessions,
            plan.Sessions.Select(planSession =>
            {
                var match = sessionSource.First(s => s.SessionNumber == planSession.SessionNumber);
                return new PatientCycleSessionDto(
                    planSession.SessionNumber,
                    planSession.Label,
                    ComputeSessionDate(request.StartDate, plan.Frequency, planSession.SessionNumber - 1),
                    PatientSessionStatus.Upcoming,
                    match.ServiceIds,
                    match.ProductIds,
                    null,
                    null,
                    null,
                    null,
                    null);
            }).ToList(),
            DateTime.UtcNow,
            PatientCycleStatus.Active,
            paymentMode,
            totalPrice,
            paidAmount,
            balanceDue);

        var withStatus = WithComputedStatus(cycle);
        MockStore.PatientCycles.Add(withStatus);
        return Task.FromResult(withStatus);
    }

    private static DateOnly ComputeSessionDate(DateOnly startDate, PlanFrequency frequency, int sessionIndex) => frequency switch
    {
        PlanFrequency.Daily => startDate.AddDays(sessionIndex),
        PlanFrequency.Weekly => startDate.AddDays(sessionIndex * 7),
        PlanFrequency.Monthly => startDate.AddMonths(sessionIndex),
        _ => startDate
    };

    private static PatientCycleStatus ComputeCycleStatus(IReadOnlyList<PatientCycleSessionDto> sessions)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (sessions.Any(s => s.Status != PatientSessionStatus.Completed && s.ScheduledDate < today))
        {
            return PatientCycleStatus.Paused;
        }

        if (sessions.All(s => s.Status == PatientSessionStatus.Completed))
        {
            return PatientCycleStatus.Completed;
        }

        return PatientCycleStatus.Active;
    }

    private static PatientCycleDto WithComputedStatus(PatientCycleDto cycle) =>
        cycle with { Status = ComputeCycleStatus(cycle.Sessions) };

    public Task<PatientCycleDto> UpdatePatientCycleSessionsAsync(int id, UpdatePatientCycleSessionsRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.PatientCycles.FindIndex(c => c.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Patient cycle not found.");
        }

        var cycle = MockStore.PatientCycles[index];

        if (cycle.PricingModelType == PricingModelType.FixedPackage)
        {
            throw new ApiException(400, "Sessions cannot be changed for fixed package plans.");
        }

        // Completed sessions are historical -- ignore any submitted changes for them instead of failing the whole save.
        var editableRequests = request.Sessions
            .Where(rs => cycle.Sessions.First(s => s.SessionNumber == rs.SessionNumber).Status != PatientSessionStatus.Completed)
            .ToList();

        var emptySession = editableRequests.FirstOrDefault(s => s.ServiceIds.Count == 0 && s.ProductIds.Count == 0);
        if (emptySession is not null)
        {
            throw new ApiException(400, $"Session {emptySession.SessionNumber} must have at least one service or product selected.");
        }

        var updatedSessions = cycle.Sessions.Select(existing =>
        {
            var match = editableRequests.FirstOrDefault(s => s.SessionNumber == existing.SessionNumber);
            return match is null ? existing : existing with { ServiceIds = match.ServiceIds, ProductIds = match.ProductIds };
        }).ToList();

        var updated = WithComputedStatus(cycle with { Sessions = updatedSessions });
        MockStore.PatientCycles[index] = updated;
        return Task.FromResult(updated);
    }

    public Task<PatientCycleDto> ReschedulePatientCycleSessionAsync(int id, ReschedulePatientCycleSessionRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.PatientCycles.FindIndex(c => c.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Patient cycle not found.");
        }

        var cycle = MockStore.PatientCycles[index];

        var target = cycle.Sessions.FirstOrDefault(s => s.SessionNumber == request.SessionNumber)
            ?? throw new ApiException(404, "Session not found on this patient cycle.");

        if (target.Status == PatientSessionStatus.Completed)
        {
            throw new ApiException(400, "Completed sessions cannot be rescheduled.");
        }

        if (request.NewDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ApiException(400, "Cannot reschedule a session to a past date.");
        }

        // Keep sessions in chronological order: never move before the previous session, and (unless
        // shifting everything after it too) never land on or after the next session's date.
        var previousSession = cycle.Sessions
            .Where(s => s.SessionNumber < request.SessionNumber)
            .OrderByDescending(s => s.SessionNumber)
            .FirstOrDefault();
        if (previousSession is not null && request.NewDate < previousSession.ScheduledDate)
        {
            throw new ApiException(400, $"Cannot reschedule before '{previousSession.Label}' ({previousSession.ScheduledDate:MMM d, yyyy}).");
        }

        if (!request.CascadeToFollowing)
        {
            var nextSession = cycle.Sessions
                .Where(s => s.SessionNumber > request.SessionNumber)
                .OrderBy(s => s.SessionNumber)
                .FirstOrDefault();
            if (nextSession is not null && request.NewDate >= nextSession.ScheduledDate)
            {
                throw new ApiException(400, $"Cannot reschedule on or after '{nextSession.Label}' ({nextSession.ScheduledDate:MMM d, yyyy}) unless following sessions are shifted too.");
            }
        }

        var deltaDays = request.NewDate.DayNumber - target.ScheduledDate.DayNumber;

        List<PatientCycleSessionDto> updatedSessions;
        if (request.CascadeToFollowing)
        {
            updatedSessions = cycle.Sessions.Select(s =>
            {
                if (s.SessionNumber < request.SessionNumber || s.Status == PatientSessionStatus.Completed)
                {
                    return s;
                }

                return s with { ScheduledDate = s.ScheduledDate.AddDays(deltaDays), Status = PatientSessionStatus.Rescheduled };
            }).ToList();
        }
        else
        {
            updatedSessions = cycle.Sessions
                .Select(s => s.SessionNumber == request.SessionNumber
                    ? s with { ScheduledDate = request.NewDate, Status = PatientSessionStatus.Rescheduled }
                    : s)
                .ToList();
        }

        var updated = WithComputedStatus(cycle with { Sessions = updatedSessions });
        MockStore.PatientCycles[index] = updated;
        return Task.FromResult(updated);
    }

    public async Task<PatientCycleDto> CompletePatientCycleSessionAsync(int id, CompletePatientCycleSessionRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.PatientCycles.FindIndex(c => c.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Patient cycle not found.");
        }

        var cycle = MockStore.PatientCycles[index];

        var target = cycle.Sessions.FirstOrDefault(s => s.SessionNumber == request.SessionNumber)
            ?? throw new ApiException(404, "Session not found on this patient cycle.");

        if (target.Status == PatientSessionStatus.Completed)
        {
            throw new ApiException(400, "This session is already completed.");
        }

        decimal? totalAmount = null;

        if (cycle.PricingModelType == PricingModelType.PerVisit)
        {
            var servicesTotal = target.ServiceIds.Sum(sid => MockStore.Services.FirstOrDefault(s => s.Id == sid)?.Price ?? 0m);
            var productsTotal = target.ProductIds.Sum(pid => MockStore.ProductOptions.FirstOrDefault(p => p.Id == pid)?.Price ?? 0m);
            totalAmount = servicesTotal + productsTotal;
        }
        else if (cycle.PaymentMode == FixedPackagePaymentMode.PerSession)
        {
            var total = cycle.TotalPrice ?? 0m;
            var perSession = cycle.TotalSessions > 0 ? Math.Round(total / cycle.TotalSessions, 2) : 0m;
            var completedSoFar = cycle.Sessions.Count(s => s.Status == PatientSessionStatus.Completed);
            var isLastPayableSession = completedSoFar == cycle.TotalSessions - 1;
            totalAmount = isLastPayableSession ? total - (perSession * (cycle.TotalSessions - 1)) : perSession;
        }
        // Fixed package + DepositBalance mode: no per-session charge, payments are tracked at the cycle level.

        int? paymentId = null;
        decimal? discountAmount = null;
        decimal? vatAmount = null;
        decimal? paidAmount = null;
        var paidAmountDelta = 0m;

        if (totalAmount is > 0)
        {
            if (request.DiscountAmount < 0 || request.DiscountAmount > totalAmount)
            {
                throw new ApiException(400, "Discount amount cannot be negative or exceed the session's total amount.");
            }
            discountAmount = request.DiscountAmount;

            // VAT is never trusted from the client -- it's always recomputed here from the clinic's
            // own settings so a tampered request can't under- or over-charge tax.
            var settings = await _settingsApiClient.GetClinicSettingsAsync(cancellationToken);
            vatAmount = settings?.IsVatEnabled == true
                ? Math.Round((totalAmount.Value - discountAmount.Value) * (settings.VatPercentage / 100m), 2)
                : 0m;

            var netAmount = totalAmount.Value - discountAmount.Value + vatAmount.Value;

            if (request.PaidAmount <= 0 || request.PaidAmount > netAmount)
            {
                throw new ApiException(400, "Paid amount must be greater than zero and cannot exceed the net amount.");
            }

            if (request.PaymentMethod is null)
            {
                throw new ApiException(400, "A payment method is required to complete this session.");
            }

            var accountName = request.AccountId is int accountId
                ? MockStore.PaymentAccounts.FirstOrDefault(a => a.Id == accountId)?.Name
                : null;

            var payment = new PaymentDto(
                MockStore.NextPaymentId++, null, cycle.PatientId, cycle.PatientName, request.PaidAmount, request.PaymentMethod.Value, request.ReferenceNumber, DateTime.UtcNow,
                request.AccountId, accountName);
            MockStore.Payments.Add(payment);

            paymentId = payment.Id;
            paidAmount = request.PaidAmount;
            paidAmountDelta = request.PaidAmount;
        }

        var updatedSessions = cycle.Sessions
            .Select(s => s.SessionNumber == request.SessionNumber
                ? s with { Status = PatientSessionStatus.Completed, ChargeAmount = totalAmount, DiscountAmount = discountAmount, VatAmount = vatAmount, PaidAmount = paidAmount, PaymentId = paymentId }
                : s)
            .ToList();

        var newPaidAmount = cycle.PaidAmount + paidAmountDelta;
        var newBalance = cycle.TotalPrice.HasValue ? cycle.TotalPrice - newPaidAmount : (decimal?)null;

        var updated = WithComputedStatus(cycle with { Sessions = updatedSessions, PaidAmount = newPaidAmount, BalanceDue = newBalance });
        MockStore.PatientCycles[index] = updated;
        return updated;
    }

    public Task<PatientCycleDto> RecordCyclePaymentAsync(int id, RecordCyclePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.PatientCycles.FindIndex(c => c.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Patient cycle not found.");
        }

        var cycle = MockStore.PatientCycles[index];

        if (cycle.PricingModelType != PricingModelType.FixedPackage || cycle.PaymentMode != FixedPackagePaymentMode.DepositBalance)
        {
            throw new ApiException(400, "Balance payments are only available for Fixed Package cycles using the deposit + balance payment mode.");
        }

        if (request.Amount <= 0)
        {
            throw new ApiException(400, "Payment amount must be greater than zero.");
        }

        if (request.Amount > (cycle.BalanceDue ?? 0m))
        {
            throw new ApiException(400, "Payment amount exceeds the remaining balance.");
        }

        var accountName = request.AccountId is int accountId
            ? MockStore.PaymentAccounts.FirstOrDefault(a => a.Id == accountId)?.Name
            : null;

        var payment = new PaymentDto(
            MockStore.NextPaymentId++, null, cycle.PatientId, cycle.PatientName, request.Amount, request.PaymentMethod, request.ReferenceNumber, DateTime.UtcNow,
            request.AccountId, accountName);
        MockStore.Payments.Add(payment);

        var newPaidAmount = cycle.PaidAmount + request.Amount;
        var newBalance = cycle.TotalPrice - newPaidAmount;

        var updated = cycle with { PaidAmount = newPaidAmount, BalanceDue = newBalance };
        MockStore.PatientCycles[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeletePatientCycleAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = MockStore.PatientCycles.RemoveAll(c => c.Id == id);
        if (removed == 0)
        {
            throw new ApiException(404, "Patient cycle not found.");
        }

        return Task.CompletedTask;
    }

    public Task<InvoiceDto> CreateWalkInSaleAsync(CreateWalkInSaleRequest request, CancellationToken cancellationToken = default)
    {
        var patient = MockStore.PatientRecords.FirstOrDefault(p => p.Id == request.PatientId)
            ?? throw new ApiException(400, "Selected patient does not exist.");

        if (request.ServiceIds.Count == 0 && request.ProductIds.Count == 0)
        {
            throw new ApiException(400, "Select at least one service or product.");
        }

        if (request.DiscountAmount < 0)
        {
            throw new ApiException(400, "Discount amount cannot be negative.");
        }

        var items = new List<InvoiceItemDto>();

        foreach (var serviceId in request.ServiceIds)
        {
            var service = MockStore.Services.FirstOrDefault(s => s.Id == serviceId)
                ?? throw new ApiException(400, "Selected service does not exist.");
            items.Add(new InvoiceItemDto(MockStore.NextInvoiceItemId++, "Service", service.Id, service.ServiceName, null, null, null, 1, service.Price, service.Price));
        }

        foreach (var productId in request.ProductIds)
        {
            var product = MockStore.ProductOptions.FirstOrDefault(p => p.Id == productId)
                ?? throw new ApiException(400, "Selected product does not exist.");
            items.Add(new InvoiceItemDto(MockStore.NextInvoiceItemId++, "Product", null, null, product.Id, product.Name, null, 1, product.Price, product.Price));
        }

        var invoiceType = request.ServiceIds.Count > 0 && request.ProductIds.Count > 0 ? "Mixed"
            : request.ServiceIds.Count > 0 ? "Service" : "Product";

        var invoiceId = MockStore.NextInvoiceId++;
        var record = new MockStore.InvoiceRecord
        {
            Id = invoiceId,
            InvoiceNumber = $"INV-{DateTime.UtcNow.Year}-{invoiceId}",
            PatientId = patient.Id,
            PatientName = patient.FullName,
            PatientPhone = patient.Phone,
            InvoiceType = invoiceType,
            InvoiceDate = DateTime.UtcNow,
            DiscountAmount = request.DiscountAmount,
            Items = items,
        };

        if (request.DiscountAmount > record.TotalAmount)
        {
            throw new ApiException(400, "Discount cannot exceed the invoice total.");
        }

        MockStore.Invoices.Add(record);

        var accountName = request.AccountId is int accountId
            ? MockStore.PaymentAccounts.FirstOrDefault(a => a.Id == accountId)?.Name
            : null;

        var payment = new PaymentDto(
            MockStore.NextPaymentId++, record.Id, patient.Id, patient.FullName, record.NetAmount, request.PaymentMethod, request.ReferenceNumber, DateTime.UtcNow,
            request.AccountId, accountName);
        MockStore.Payments.Add(payment);
        record.PaidAmount = record.NetAmount;

        return Task.FromResult(ToInvoiceDto(record));
    }

    private static InvoiceDto ToInvoiceDto(MockStore.InvoiceRecord i) => new(
        i.Id, i.InvoiceNumber, i.PatientId, i.PatientName, null, i.InvoiceType,
        i.TotalAmount, i.DiscountAmount, i.VatAmount, i.NetAmount, i.PaidAmount, i.BalanceDue, i.Status, i.InvoiceDate, i.Items);
}
