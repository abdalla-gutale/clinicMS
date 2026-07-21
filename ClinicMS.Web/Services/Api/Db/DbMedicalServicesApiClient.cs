using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.MedicalServices;
using ClinicMS.Web.Models.Api.Payments;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Services.Api.Db;

public class DbMedicalServicesApiClient : IMedicalServicesApiClient
{
    private readonly ClinicMsDbContext _db;

    public DbMedicalServicesApiClient(ClinicMsDbContext db)
    {
        _db = db;
    }

    // ----- Service Types -----

    public async Task<IReadOnlyList<ServiceTypeDto>> GetServiceTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await _db.ServiceTypes.OrderBy(t => t.Id).ToListAsync(cancellationToken);
        return types.Select(ToDto).ToList();
    }

    public async Task<ServiceTypeDto> CreateServiceTypeAsync(CreateServiceTypeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new ServiceTypeEntity { TypeName = request.TypeName, Description = request.Description, IsActive = request.IsActive };
        _db.ServiceTypes.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<ServiceTypeDto> UpdateServiceTypeAsync(int id, UpdateServiceTypeRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.ServiceTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Service type not found.");

        entity.TypeName = request.TypeName;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task DeleteServiceTypeAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _db.Services.AnyAsync(s => s.ServiceTypeId == id, cancellationToken))
        {
            throw new ApiException(400, "Cannot delete a service type that still has services under it.");
        }

        var entity = await _db.ServiceTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Service type not found.");
        _db.ServiceTypes.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Services -----

    public async Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        var services = await _db.Services.OrderBy(s => s.Id).ToListAsync(cancellationToken);
        var typeNames = await _db.ServiceTypes.ToDictionaryAsync(t => t.Id, t => t.TypeName, cancellationToken);
        return services.Select(s => ToDto(s, typeNames.GetValueOrDefault(s.ServiceTypeId, ""))).ToList();
    }

    public async Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var type = await _db.ServiceTypes.FirstOrDefaultAsync(t => t.Id == request.ServiceTypeId, cancellationToken)
            ?? throw new ApiException(400, "Selected service type does not exist.");

        var entity = new ServiceEntity
        {
            ServiceTypeId = type.Id,
            ServiceName = request.ServiceName,
            StandardPrice = request.Price,
            Description = request.Description,
            IsActive = request.IsActive,
        };
        _db.Services.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity, type.TypeName);
    }

    public async Task<ServiceDto> UpdateServiceAsync(int id, UpdateServiceRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Service not found.");

        var type = await _db.ServiceTypes.FirstOrDefaultAsync(t => t.Id == request.ServiceTypeId, cancellationToken)
            ?? throw new ApiException(400, "Selected service type does not exist.");

        entity.ServiceTypeId = type.Id;
        entity.ServiceName = request.ServiceName;
        entity.StandardPrice = request.Price;
        entity.Description = request.Description;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity, type.TypeName);
    }

    public async Task DeleteServiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Service not found.");
        _db.Services.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ----- Product options (lightweight catalog for the session grid) -----

    public async Task<IReadOnlyList<ProductOption>> GetProductOptionsAsync(CancellationToken cancellationToken = default)
    {
        var skus = await _db.ProductSkus.Where(s => s.IsActive).OrderBy(s => s.Id).ToListAsync(cancellationToken);
        var products = await _db.Products.ToDictionaryAsync(p => p.Id, cancellationToken);
        return skus.Select(s => new ProductOption(s.Id, products.GetValueOrDefault(s.ProductId)?.ProductName ?? s.SkuCode, s.SellingPrice)).ToList();
    }

    // ----- Treatment Plans -----

    public async Task<IReadOnlyList<TreatmentPlanDto>> GetTreatmentPlansAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _db.TreatmentPlans.OrderBy(p => p.Id).ToListAsync(cancellationToken);
        var planIds = plans.Select(p => p.Id).ToList();
        var items = await _db.TreatmentPlanItems.Where(i => planIds.Contains(i.TreatmentPlanId)).ToListAsync(cancellationToken);
        return plans.Select(p => ToDto(p, items.Where(i => i.TreatmentPlanId == p.Id).ToList())).ToList();
    }

    public async Task<TreatmentPlanDto> CreateTreatmentPlanAsync(CreateTreatmentPlanRequest request, CancellationToken cancellationToken = default)
    {
        ValidateTreatmentPlan(request.PricingModelType, request.FixedPackagePrice, request.TotalSessions, request.Sessions);

        var entity = new TreatmentPlanEntity
        {
            PlanName = request.PlanName,
            PricingModel = request.PricingModelType.ToString(),
            FixedPackagePrice = request.PricingModelType == PricingModelType.FixedPackage ? request.FixedPackagePrice : null,
            TotalSessions = request.TotalSessions,
            Frequency = request.Frequency.ToString(),
            IsActive = true,
        };
        _db.TreatmentPlans.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var items = await BuildPlanItemsAsync(entity.Id, request.Sessions, cancellationToken);
        _db.TreatmentPlanItems.AddRange(items);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(entity, items);
    }

    public async Task<TreatmentPlanDto> UpdateTreatmentPlanAsync(int id, UpdateTreatmentPlanRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.TreatmentPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Treatment plan not found.");

        ValidateTreatmentPlan(request.PricingModelType, request.FixedPackagePrice, request.TotalSessions, request.Sessions);

        entity.PlanName = request.PlanName;
        entity.PricingModel = request.PricingModelType.ToString();
        entity.FixedPackagePrice = request.PricingModelType == PricingModelType.FixedPackage ? request.FixedPackagePrice : null;
        entity.TotalSessions = request.TotalSessions;
        entity.Frequency = request.Frequency.ToString();

        var existingItems = await _db.TreatmentPlanItems.Where(i => i.TreatmentPlanId == id).ToListAsync(cancellationToken);
        _db.TreatmentPlanItems.RemoveRange(existingItems);
        await _db.SaveChangesAsync(cancellationToken);

        var items = await BuildPlanItemsAsync(entity.Id, request.Sessions, cancellationToken);
        _db.TreatmentPlanItems.AddRange(items);
        await _db.SaveChangesAsync(cancellationToken);

        return ToDto(entity, items);
    }

    public async Task DeleteTreatmentPlanAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.TreatmentPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Treatment plan not found.");

        var items = await _db.TreatmentPlanItems.Where(i => i.TreatmentPlanId == id).ToListAsync(cancellationToken);
        _db.TreatmentPlanItems.RemoveRange(items);
        _db.TreatmentPlans.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<TreatmentPlanItemEntity>> BuildPlanItemsAsync(
        int treatmentPlanId, IReadOnlyList<TreatmentPlanSessionRequest> sessions, CancellationToken cancellationToken)
    {
        var allServiceIds = sessions.SelectMany(s => s.ServiceIds).Distinct().ToList();
        var allProductIds = sessions.SelectMany(s => s.ProductIds).Distinct().ToList();
        var services = await _db.Services.Where(s => allServiceIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var skus = await _db.ProductSkus.Where(s => allProductIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);

        var result = new List<TreatmentPlanItemEntity>();
        foreach (var session in sessions)
        {
            foreach (var serviceId in session.ServiceIds)
            {
                result.Add(new TreatmentPlanItemEntity
                {
                    TreatmentPlanId = treatmentPlanId,
                    ItemType = "Service",
                    ServiceId = serviceId,
                    Quantity = 1,
                    UnitPrice = services.GetValueOrDefault(serviceId)?.StandardPrice ?? 0m,
                    SessionNumber = session.SessionNumber,
                });
            }

            foreach (var productId in session.ProductIds)
            {
                result.Add(new TreatmentPlanItemEntity
                {
                    TreatmentPlanId = treatmentPlanId,
                    ItemType = "Product",
                    ProductSkuId = productId,
                    Quantity = 1,
                    UnitPrice = skus.GetValueOrDefault(productId)?.SellingPrice ?? 0m,
                    SessionNumber = session.SessionNumber,
                });
            }
        }

        return result;
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

    // ----- Patient Cycles -----

    public async Task<IReadOnlyList<PatientCycleDto>> GetPatientCyclesAsync(CancellationToken cancellationToken = default)
    {
        var cycles = await _db.PatientCycles.ToListAsync(cancellationToken);
        return await BuildCycleDtosAsync(cycles, cancellationToken);
    }

    public async Task<PatientCycleDto> AssignPatientCycleAsync(AssignPatientCycleRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new ApiException(400, "Selected patient does not exist.");

        var plan = await _db.TreatmentPlans.FirstOrDefaultAsync(p => p.Id == request.TreatmentPlanId, cancellationToken)
            ?? throw new ApiException(400, "Selected treatment plan does not exist.");

        var planItems = await _db.TreatmentPlanItems.Where(i => i.TreatmentPlanId == plan.Id).ToListAsync(cancellationToken);

        // A patient may only have one unfinished cycle at a time -- Paused counts as unfinished
        // since it's just an Active cycle with an overdue session, not a completed one.
        var existingCycles = await _db.PatientCycles.Where(c => c.PatientId == request.PatientId).ToListAsync(cancellationToken);
        if (existingCycles.Count > 0)
        {
            var existingDtos = await BuildCycleDtosAsync(existingCycles, cancellationToken);
            if (existingDtos.Any(c => c.Status != PatientCycleStatus.Completed))
            {
                throw new ApiException(400, "This patient already has an active treatment cycle. Complete or finish it before assigning a new one.");
            }
        }

        if (request.StartDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ApiException(400, "Start date cannot be in the past.");
        }

        var pricingModel = ParsePricingModel(plan.PricingModel);

        List<PatientCycleSessionRequest> sessionSource;
        if (pricingModel == PricingModelType.FixedPackage)
        {
            // Fixed package sessions are not editable at assign time -- always clone the plan's own template.
            sessionSource = Enumerable.Range(1, plan.TotalSessions).Select(n =>
            {
                var sessionItems = planItems.Where(i => i.SessionNumber == n).ToList();
                return new PatientCycleSessionRequest(
                    n,
                    sessionItems.Where(i => i.ItemType == "Service" && i.ServiceId.HasValue).Select(i => i.ServiceId!.Value).ToList(),
                    sessionItems.Where(i => i.ItemType == "Product" && i.ProductSkuId.HasValue).Select(i => i.ProductSkuId!.Value).ToList());
            }).ToList();
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

            sessionSource = request.Sessions.ToList();
        }

        var cycleEntity = new PatientCycleEntity
        {
            PatientId = patient.Id,
            PlanId = plan.Id,
            CycleName = plan.PlanName,
            PricingModel = plan.PricingModel,
            AgreedTotalPrice = pricingModel == PricingModelType.FixedPackage ? plan.FixedPackagePrice : null,
            Frequency = plan.Frequency,
            StartDate = request.StartDate,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
        };
        _db.PatientCycles.Add(cycleEntity);
        await _db.SaveChangesAsync(cancellationToken);

        var sessionEntities = new List<CycleSessionEntity>();
        for (var n = 1; n <= plan.TotalSessions; n++)
        {
            var date = ComputeSessionDate(request.StartDate, plan.Frequency, n - 1);
            sessionEntities.Add(new CycleSessionEntity
            {
                CycleId = cycleEntity.Id,
                SessionNumber = n,
                OriginalScheduledDate = date,
                ActualScheduledDate = date,
                Status = "Pending",
            });
        }
        _db.CycleSessions.AddRange(sessionEntities);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var sessionEntity in sessionEntities)
        {
            var match = sessionSource.First(s => s.SessionNumber == sessionEntity.SessionNumber);
            _db.SessionItems.AddRange(await BuildSessionItemsAsync(sessionEntity.Id, match.ServiceIds, match.ProductIds, cancellationToken));
        }
        await _db.SaveChangesAsync(cancellationToken);

        if (pricingModel == PricingModelType.FixedPackage)
        {
            var paymentMode = request.PaymentMode ?? FixedPackagePaymentMode.PerSession;
            if (paymentMode == FixedPackagePaymentMode.DepositBalance)
            {
                var totalPrice = plan.FixedPackagePrice ?? 0m;

                // FixedPackage + DepositBalance cycles are the one case with no per-session charge --
                // money is tracked at the cycle level instead. There's no CycleId column on invoices,
                // so a single package invoice is created here and found again later by its
                // InvoiceNumber ("CYCLE-{cycleId}"), which also doubles as how a reload tells
                // DepositBalance mode apart from PerSession (see BuildCycleDtosAsync).
                var packageInvoice = new InvoiceEntity
                {
                    InvoiceNumber = PackageInvoiceNumber(cycleEntity.Id),
                    PatientId = patient.Id,
                    SessionId = null,
                    InvoiceType = "Package",
                    TotalAmount = totalPrice,
                    DiscountAmount = 0m,
                    VatAmount = 0m,
                    NetAmount = totalPrice,
                    PaidAmount = 0m,
                    BalanceDue = totalPrice,
                    PaymentStatus = "Unpaid",
                    InvoiceDate = DateTime.UtcNow,
                };

                if (request.DepositAmount is > 0)
                {
                    if (request.DepositAmount > totalPrice)
                    {
                        throw new ApiException(400, "Deposit cannot exceed the package price.");
                    }

                    var depositMethod = request.DepositPaymentMethod
                        ?? throw new ApiException(400, "A payment method is required to record the deposit.");

                    packageInvoice.PaidAmount = request.DepositAmount.Value;
                    packageInvoice.BalanceDue = totalPrice - request.DepositAmount.Value;
                    packageInvoice.PaymentStatus = packageInvoice.BalanceDue <= 0 ? "Paid" : "Partial";

                    _db.Invoices.Add(packageInvoice);
                    await _db.SaveChangesAsync(cancellationToken);

                    _db.Payments.Add(new PaymentEntity
                    {
                        InvoiceId = packageInvoice.Id,
                        PatientId = patient.Id,
                        AmountPaid = request.DepositAmount.Value,
                        PaymentMethod = depositMethod.ToString(),
                        ReferenceNumber = "Initial deposit",
                        PaymentDate = DateTime.UtcNow,
                        AccountId = request.DepositAccountId,
                    });
                }
                else
                {
                    _db.Invoices.Add(packageInvoice);
                }

                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        return (await BuildCycleDtosAsync(new List<PatientCycleEntity> { cycleEntity }, cancellationToken)).Single();
    }

    public async Task<PatientCycleDto> UpdatePatientCycleSessionsAsync(int id, UpdatePatientCycleSessionsRequest request, CancellationToken cancellationToken = default)
    {
        var cycleEntity = await _db.PatientCycles.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Patient cycle not found.");

        if (ParsePricingModel(cycleEntity.PricingModel) == PricingModelType.FixedPackage)
        {
            throw new ApiException(400, "Sessions cannot be changed for fixed package plans.");
        }

        var sessionEntities = await _db.CycleSessions.Where(s => s.CycleId == id).ToListAsync(cancellationToken);

        // Completed sessions are historical -- ignore any submitted changes for them instead of failing the whole save.
        var editableRequests = request.Sessions
            .Where(rs => sessionEntities.First(s => s.SessionNumber == rs.SessionNumber).Status != "Completed")
            .ToList();

        var emptySession = editableRequests.FirstOrDefault(s => s.ServiceIds.Count == 0 && s.ProductIds.Count == 0);
        if (emptySession is not null)
        {
            throw new ApiException(400, $"Session {emptySession.SessionNumber} must have at least one service or product selected.");
        }

        foreach (var req in editableRequests)
        {
            var sessionEntity = sessionEntities.First(s => s.SessionNumber == req.SessionNumber);
            var oldItems = await _db.SessionItems.Where(i => i.SessionId == sessionEntity.Id).ToListAsync(cancellationToken);
            _db.SessionItems.RemoveRange(oldItems);
            _db.SessionItems.AddRange(await BuildSessionItemsAsync(sessionEntity.Id, req.ServiceIds, req.ProductIds, cancellationToken));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (await BuildCycleDtosAsync(new List<PatientCycleEntity> { cycleEntity }, cancellationToken)).Single();
    }

    public async Task<PatientCycleDto> ReschedulePatientCycleSessionAsync(int id, ReschedulePatientCycleSessionRequest request, CancellationToken cancellationToken = default)
    {
        var cycleEntity = await _db.PatientCycles.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Patient cycle not found.");

        var sessionEntities = await _db.CycleSessions.Where(s => s.CycleId == id).OrderBy(s => s.SessionNumber).ToListAsync(cancellationToken);

        var target = sessionEntities.FirstOrDefault(s => s.SessionNumber == request.SessionNumber)
            ?? throw new ApiException(404, "Session not found on this patient cycle.");

        if (target.Status == "Completed")
        {
            throw new ApiException(400, "Completed sessions cannot be rescheduled.");
        }

        if (request.NewDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new ApiException(400, "Cannot reschedule a session to a past date.");
        }

        var previousSession = sessionEntities.Where(s => s.SessionNumber < request.SessionNumber).OrderByDescending(s => s.SessionNumber).FirstOrDefault();
        if (previousSession is not null && request.NewDate < previousSession.ActualScheduledDate)
        {
            throw new ApiException(400, $"Cannot reschedule before '{SessionLabel(cycleEntity.Frequency, previousSession.SessionNumber)}' ({previousSession.ActualScheduledDate:MMM d, yyyy}).");
        }

        if (!request.CascadeToFollowing)
        {
            var nextSession = sessionEntities.Where(s => s.SessionNumber > request.SessionNumber).OrderBy(s => s.SessionNumber).FirstOrDefault();
            if (nextSession is not null && request.NewDate >= nextSession.ActualScheduledDate)
            {
                throw new ApiException(400, $"Cannot reschedule on or after '{SessionLabel(cycleEntity.Frequency, nextSession.SessionNumber)}' ({nextSession.ActualScheduledDate:MMM d, yyyy}) unless following sessions are shifted too.");
            }
        }

        var deltaDays = request.NewDate.DayNumber - target.ActualScheduledDate.DayNumber;

        if (request.CascadeToFollowing)
        {
            foreach (var s in sessionEntities)
            {
                if (s.SessionNumber < request.SessionNumber || s.Status == "Completed")
                {
                    continue;
                }

                s.ActualScheduledDate = s.ActualScheduledDate.AddDays(deltaDays);
            }
        }
        else
        {
            target.ActualScheduledDate = request.NewDate;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return (await BuildCycleDtosAsync(new List<PatientCycleEntity> { cycleEntity }, cancellationToken)).Single();
    }

    public async Task<PatientCycleDto> CompletePatientCycleSessionAsync(int id, CompletePatientCycleSessionRequest request, CancellationToken cancellationToken = default)
    {
        var cycleEntity = await _db.PatientCycles.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Patient cycle not found.");

        var sessionEntities = await _db.CycleSessions.Where(s => s.CycleId == id).ToListAsync(cancellationToken);
        var target = sessionEntities.FirstOrDefault(s => s.SessionNumber == request.SessionNumber)
            ?? throw new ApiException(404, "Session not found on this patient cycle.");

        if (target.Status == "Completed")
        {
            throw new ApiException(400, "This session is already completed.");
        }

        var pricingModel = ParsePricingModel(cycleEntity.PricingModel);
        var totalSessions = sessionEntities.Count;

        var packageInvoice = pricingModel == PricingModelType.FixedPackage
            ? await _db.Invoices.FirstOrDefaultAsync(i => i.InvoiceNumber == PackageInvoiceNumber(id), cancellationToken)
            : null;

        decimal? totalAmount = null;
        var invoiceItemsToCreate = new List<InvoiceItemEntity>();

        if (pricingModel == PricingModelType.PerVisit)
        {
            var items = await _db.SessionItems.Where(i => i.SessionId == target.Id).ToListAsync(cancellationToken);
            var serviceIds = items.Where(i => i.ItemType == "Service" && i.ServiceId.HasValue).Select(i => i.ServiceId!.Value).ToList();
            var productIds = items.Where(i => i.ItemType == "Product" && i.ProductSkuId.HasValue).Select(i => i.ProductSkuId!.Value).ToList();
            var services = await _db.Services.Where(s => serviceIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
            var skus = await _db.ProductSkus.Where(s => productIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
            var servicesTotal = serviceIds.Sum(sid => services.GetValueOrDefault(sid)?.StandardPrice ?? 0m);
            var productsTotal = productIds.Sum(pid => skus.GetValueOrDefault(pid)?.SellingPrice ?? 0m);
            totalAmount = servicesTotal + productsTotal;

            // Itemize using each session item's real price -- these always sum to totalAmount exactly.
            foreach (var serviceId in serviceIds)
            {
                var price = services.GetValueOrDefault(serviceId)?.StandardPrice ?? 0m;
                invoiceItemsToCreate.Add(new InvoiceItemEntity { ItemType = "Service", ServiceId = serviceId, Quantity = 1, UnitPrice = price, TotalPrice = price });
            }
            foreach (var productId in productIds)
            {
                var price = skus.GetValueOrDefault(productId)?.SellingPrice ?? 0m;
                invoiceItemsToCreate.Add(new InvoiceItemEntity { ItemType = "Product", ProductSkuId = productId, Quantity = 1, UnitPrice = price, TotalPrice = price });
            }
        }
        else if (packageInvoice is null) // FixedPackage + PerSession
        {
            var total = cycleEntity.AgreedTotalPrice ?? 0m;
            var perSession = totalSessions > 0 ? Math.Round(total / totalSessions, 2) : 0m;
            var completedSoFar = sessionEntities.Count(s => s.Status == "Completed");
            var isLastPayableSession = completedSoFar == totalSessions - 1;
            totalAmount = isLastPayableSession ? total - (perSession * (totalSessions - 1)) : perSession;

            // A package's per-session share isn't itemized by service/product (that's the nature of a
            // flat package deal), so a single descriptive line stands in for the session's charge.
            invoiceItemsToCreate.Add(new InvoiceItemEntity { ItemType = "Package", Quantity = 1, UnitPrice = totalAmount.Value, TotalPrice = totalAmount.Value });
        }
        // FixedPackage + DepositBalance: no per-session charge, payments are tracked at the cycle level.

        if (totalAmount is > 0)
        {
            if (request.DiscountAmount < 0 || request.DiscountAmount > totalAmount)
            {
                throw new ApiException(400, "Discount amount cannot be negative or exceed the session's total amount.");
            }

            var discountAmount = request.DiscountAmount;

            // VAT is never trusted from the client -- it's always recomputed here from the clinic's
            // own settings so a tampered request can't under- or over-charge tax.
            var settings = await _db.ClinicSettings.FirstOrDefaultAsync(cancellationToken);
            var vatAmount = settings?.IsVatEnabled == true
                ? Math.Round((totalAmount.Value - discountAmount) * (settings.VatPercentage / 100m), 2)
                : 0m;

            var netAmount = totalAmount.Value - discountAmount + vatAmount;

            if (request.PaidAmount <= 0 || request.PaidAmount > netAmount)
            {
                throw new ApiException(400, "Paid amount must be greater than zero and cannot exceed the net amount.");
            }

            if (request.PaymentMethod is null)
            {
                throw new ApiException(400, "A payment method is required to complete this session.");
            }

            var balanceDue = netAmount - request.PaidAmount;
            var sessionInvoice = new InvoiceEntity
            {
                InvoiceNumber = "",
                PatientId = cycleEntity.PatientId,
                SessionId = target.Id,
                InvoiceType = "Cycle Session",
                TotalAmount = totalAmount.Value,
                DiscountAmount = discountAmount,
                VatAmount = vatAmount,
                NetAmount = netAmount,
                PaidAmount = request.PaidAmount,
                BalanceDue = balanceDue,
                PaymentStatus = balanceDue <= 0 ? "Paid" : "Partial",
                InvoiceDate = DateTime.UtcNow,
            };
            _db.Invoices.Add(sessionInvoice);
            await _db.SaveChangesAsync(cancellationToken);
            sessionInvoice.InvoiceNumber = $"INV-{sessionInvoice.InvoiceDate.Year}-{sessionInvoice.Id}";

            foreach (var item in invoiceItemsToCreate)
            {
                item.InvoiceId = sessionInvoice.Id;
            }
            _db.InvoiceItems.AddRange(invoiceItemsToCreate);

            _db.Payments.Add(new PaymentEntity
            {
                InvoiceId = sessionInvoice.Id,
                PatientId = cycleEntity.PatientId,
                AmountPaid = request.PaidAmount,
                PaymentMethod = request.PaymentMethod.Value.ToString(),
                ReferenceNumber = request.ReferenceNumber,
                PaymentDate = DateTime.UtcNow,
                AccountId = request.AccountId,
            });
        }

        target.Status = "Completed";
        target.CompletedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return (await BuildCycleDtosAsync(new List<PatientCycleEntity> { cycleEntity }, cancellationToken)).Single();
    }

    public async Task<PatientCycleDto> RecordCyclePaymentAsync(int id, RecordCyclePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var cycleEntity = await _db.PatientCycles.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Patient cycle not found.");

        var packageInvoice = ParsePricingModel(cycleEntity.PricingModel) == PricingModelType.FixedPackage
            ? await _db.Invoices.FirstOrDefaultAsync(i => i.InvoiceNumber == PackageInvoiceNumber(id), cancellationToken)
            : null;

        if (packageInvoice is null)
        {
            throw new ApiException(400, "Balance payments are only available for Fixed Package cycles using the deposit + balance payment mode.");
        }

        if (request.Amount <= 0)
        {
            throw new ApiException(400, "Payment amount must be greater than zero.");
        }

        if (request.Amount > packageInvoice.BalanceDue)
        {
            throw new ApiException(400, "Payment amount exceeds the remaining balance.");
        }

        packageInvoice.PaidAmount += request.Amount;
        packageInvoice.BalanceDue -= request.Amount;
        packageInvoice.PaymentStatus = packageInvoice.BalanceDue <= 0 ? "Paid" : "Partial";

        _db.Payments.Add(new PaymentEntity
        {
            InvoiceId = packageInvoice.Id,
            PatientId = cycleEntity.PatientId,
            AmountPaid = request.Amount,
            PaymentMethod = request.PaymentMethod.ToString(),
            ReferenceNumber = request.ReferenceNumber,
            PaymentDate = DateTime.UtcNow,
            AccountId = request.AccountId,
        });

        await _db.SaveChangesAsync(cancellationToken);
        return (await BuildCycleDtosAsync(new List<PatientCycleEntity> { cycleEntity }, cancellationToken)).Single();
    }

    // Soft-deletes the cycle plus every financial record it produced (sessions, invoices,
    // payments) so a mis-click can't erase a patient's billing history -- only the pure join rows
    // (SessionItems/InvoiceItems) are hard-deleted, since nothing ever queries those independently
    // of their now-hidden parent Session/Invoice.
    public async Task DeletePatientCycleAsync(int id, CancellationToken cancellationToken = default)
    {
        var cycleEntity = await _db.PatientCycles.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Patient cycle not found.");

        var sessionEntities = await _db.CycleSessions.Where(s => s.CycleId == id).ToListAsync(cancellationToken);
        var sessionIds = sessionEntities.Select(s => s.Id).ToList();
        var items = await _db.SessionItems.Where(i => sessionIds.Contains(i.SessionId)).ToListAsync(cancellationToken);
        var packageInvoiceNumber = PackageInvoiceNumber(id);
        var invoices = await _db.Invoices
            .Where(i => (i.SessionId != null && sessionIds.Contains(i.SessionId.Value)) || i.InvoiceNumber == packageInvoiceNumber)
            .ToListAsync(cancellationToken);
        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var payments = await _db.Payments.Where(p => p.InvoiceId != null && invoiceIds.Contains(p.InvoiceId.Value)).ToListAsync(cancellationToken);
        var invoiceItems = await _db.InvoiceItems.Where(i => invoiceIds.Contains(i.InvoiceId)).ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var payment in payments) { payment.IsDeleted = true; payment.DeletedAt = now; }
        foreach (var invoice in invoices) { invoice.IsDeleted = true; invoice.DeletedAt = now; }
        foreach (var session in sessionEntities) { session.IsDeleted = true; session.DeletedAt = now; }
        cycleEntity.IsDeleted = true;
        cycleEntity.DeletedAt = now;

        _db.InvoiceItems.RemoveRange(invoiceItems);
        _db.SessionItems.RemoveRange(items);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<InvoiceDto> CreateWalkInSaleAsync(CreateWalkInSaleRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new ApiException(400, "Selected patient does not exist.");

        if (request.ServiceIds.Count == 0 && request.ProductIds.Count == 0)
        {
            throw new ApiException(400, "Select at least one service or product.");
        }

        if (request.DiscountAmount < 0)
        {
            throw new ApiException(400, "Discount amount cannot be negative.");
        }

        var services = await _db.Services.Where(s => request.ServiceIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var skus = await _db.ProductSkus.Where(s => request.ProductIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken);
        var products = await _db.Products.ToDictionaryAsync(p => p.Id, cancellationToken);

        var items = new List<InvoiceItemDto>();
        var totalAmount = 0m;
        var itemId = 1;

        foreach (var serviceId in request.ServiceIds)
        {
            var service = services.GetValueOrDefault(serviceId)
                ?? throw new ApiException(400, "Selected service does not exist.");
            items.Add(new InvoiceItemDto(itemId++, "Service", service.Id, service.ServiceName, null, null, null, 1, service.StandardPrice, service.StandardPrice));
            totalAmount += service.StandardPrice;
        }

        foreach (var productId in request.ProductIds)
        {
            var sku = skus.GetValueOrDefault(productId)
                ?? throw new ApiException(400, "Selected product does not exist.");
            var productName = products.GetValueOrDefault(sku.ProductId)?.ProductName ?? sku.SkuCode;
            items.Add(new InvoiceItemDto(itemId++, "Product", null, null, sku.Id, productName, sku.SkuCode, 1, sku.SellingPrice, sku.SellingPrice));
            totalAmount += sku.SellingPrice;
        }

        if (request.DiscountAmount > totalAmount)
        {
            throw new ApiException(400, "Discount cannot exceed the invoice total.");
        }

        var invoiceType = request.ServiceIds.Count > 0 && request.ProductIds.Count > 0 ? "Mixed"
            : request.ServiceIds.Count > 0 ? "Service" : "Product";

        var settings = await _db.ClinicSettings.FirstOrDefaultAsync(cancellationToken);
        var vatAmount = settings?.IsVatEnabled == true
            ? Math.Round((totalAmount - request.DiscountAmount) * (settings.VatPercentage / 100m), 2)
            : 0m;
        var netAmount = totalAmount - request.DiscountAmount + vatAmount;

        var invoiceEntity = new InvoiceEntity
        {
            InvoiceNumber = "",
            PatientId = patient.Id,
            SessionId = null,
            InvoiceType = invoiceType,
            TotalAmount = totalAmount,
            DiscountAmount = request.DiscountAmount,
            VatAmount = vatAmount,
            NetAmount = netAmount,
            PaidAmount = netAmount,
            BalanceDue = 0m,
            PaymentStatus = "Paid",
            InvoiceDate = DateTime.UtcNow,
        };
        _db.Invoices.Add(invoiceEntity);
        await _db.SaveChangesAsync(cancellationToken);
        invoiceEntity.InvoiceNumber = $"INV-{invoiceEntity.InvoiceDate.Year}-{invoiceEntity.Id}";

        foreach (var item in items)
        {
            _db.InvoiceItems.Add(new InvoiceItemEntity
            {
                InvoiceId = invoiceEntity.Id,
                ItemType = item.ItemType,
                ServiceId = item.ServiceId,
                ProductSkuId = item.ProductSkuId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
            });
        }

        _db.Payments.Add(new PaymentEntity
        {
            InvoiceId = invoiceEntity.Id,
            PatientId = patient.Id,
            AmountPaid = netAmount,
            PaymentMethod = request.PaymentMethod.ToString(),
            ReferenceNumber = request.ReferenceNumber,
            PaymentDate = DateTime.UtcNow,
            AccountId = request.AccountId,
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new InvoiceDto(
            invoiceEntity.Id, invoiceEntity.InvoiceNumber, invoiceEntity.PatientId, patient.FullName, null, invoiceEntity.InvoiceType,
            invoiceEntity.TotalAmount, invoiceEntity.DiscountAmount, invoiceEntity.VatAmount, invoiceEntity.NetAmount,
            invoiceEntity.PaidAmount, invoiceEntity.BalanceDue,
            Enum.TryParse<PaymentStatus>(invoiceEntity.PaymentStatus, out var status) ? status : PaymentStatus.Unpaid,
            invoiceEntity.InvoiceDate, items);
    }

    // ----- Shared helpers -----

    private async Task<List<SessionItemEntity>> BuildSessionItemsAsync(
        int sessionId, IReadOnlyList<int> serviceIds, IReadOnlyList<int> productIds, CancellationToken cancellationToken)
    {
        var services = serviceIds.Count > 0
            ? await _db.Services.Where(s => serviceIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken)
            : new Dictionary<int, ServiceEntity>();
        var skus = productIds.Count > 0
            ? await _db.ProductSkus.Where(s => productIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, cancellationToken)
            : new Dictionary<int, ProductSkuEntity>();

        var items = new List<SessionItemEntity>();
        foreach (var serviceId in serviceIds)
        {
            items.Add(new SessionItemEntity
            {
                SessionId = sessionId,
                ItemType = "Service",
                ServiceId = serviceId,
                Quantity = 1,
                UnitPrice = services.GetValueOrDefault(serviceId)?.StandardPrice ?? 0m,
            });
        }

        foreach (var productId in productIds)
        {
            items.Add(new SessionItemEntity
            {
                SessionId = sessionId,
                ItemType = "Product",
                ProductSkuId = productId,
                Quantity = 1,
                UnitPrice = skus.GetValueOrDefault(productId)?.SellingPrice ?? 0m,
            });
        }

        return items;
    }

    /// <summary>Builds full PatientCycleDtos for the given cycle rows, joining in sessions, session
    /// items, and the invoices/payments that record each session's (or fixed package's) money.</summary>
    private async Task<List<PatientCycleDto>> BuildCycleDtosAsync(List<PatientCycleEntity> cycles, CancellationToken cancellationToken)
    {
        if (cycles.Count == 0)
        {
            return new List<PatientCycleDto>();
        }

        var cycleIds = cycles.Select(c => c.Id).ToList();
        var patientIds = cycles.Select(c => c.PatientId).Distinct().ToList();
        var planIds = cycles.Select(c => c.PlanId).Distinct().ToList();

        var patients = await _db.Patients.Where(p => patientIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);
        var plans = await _db.TreatmentPlans.Where(p => planIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, cancellationToken);

        var sessions = await _db.CycleSessions.Where(s => cycleIds.Contains(s.CycleId)).ToListAsync(cancellationToken);
        var sessionIds = sessions.Select(s => s.Id).ToList();
        var items = await _db.SessionItems.Where(i => sessionIds.Contains(i.SessionId)).ToListAsync(cancellationToken);

        var packageInvoiceNumbers = cycleIds.Select(PackageInvoiceNumber).ToList();
        var invoices = await _db.Invoices
            .Where(i => (i.SessionId != null && sessionIds.Contains(i.SessionId.Value)) || packageInvoiceNumbers.Contains(i.InvoiceNumber))
            .ToListAsync(cancellationToken);
        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var payments = await _db.Payments.Where(p => p.InvoiceId != null && invoiceIds.Contains(p.InvoiceId.Value)).ToListAsync(cancellationToken);

        var result = new List<PatientCycleDto>();
        foreach (var cycle in cycles)
        {
            patients.TryGetValue(cycle.PatientId, out var patient);
            plans.TryGetValue(cycle.PlanId, out var plan);
            var cycleSessions = sessions.Where(s => s.CycleId == cycle.Id).OrderBy(s => s.SessionNumber).ToList();
            var pricingModel = ParsePricingModel(cycle.PricingModel);

            var packageInvoice = pricingModel == PricingModelType.FixedPackage
                ? invoices.FirstOrDefault(i => i.InvoiceNumber == PackageInvoiceNumber(cycle.Id))
                : null;

            var sessionDtos = cycleSessions.Select(s =>
            {
                var sessionItems = items.Where(i => i.SessionId == s.Id).ToList();
                var serviceIds = sessionItems.Where(i => i.ItemType == "Service" && i.ServiceId.HasValue).Select(i => i.ServiceId!.Value).ToList();
                var productIds = sessionItems.Where(i => i.ItemType == "Product" && i.ProductSkuId.HasValue).Select(i => i.ProductSkuId!.Value).ToList();
                var sessionInvoice = invoices.FirstOrDefault(i => i.SessionId == s.Id);
                var payment = sessionInvoice is null ? null : payments.FirstOrDefault(p => p.InvoiceId == sessionInvoice.Id);

                return new PatientCycleSessionDto(
                    s.SessionNumber,
                    SessionLabel(cycle.Frequency, s.SessionNumber),
                    s.ActualScheduledDate,
                    MapSessionStatus(s),
                    serviceIds,
                    productIds,
                    sessionInvoice?.TotalAmount,
                    sessionInvoice?.DiscountAmount,
                    sessionInvoice?.VatAmount,
                    sessionInvoice?.PaidAmount,
                    payment?.Id);
            }).ToList();

            FixedPackagePaymentMode? paymentMode = null;
            decimal? totalPrice = cycle.AgreedTotalPrice;
            decimal paidAmount;
            decimal? balanceDue;

            if (pricingModel == PricingModelType.FixedPackage)
            {
                if (packageInvoice is not null)
                {
                    paymentMode = FixedPackagePaymentMode.DepositBalance;
                    paidAmount = packageInvoice.PaidAmount;
                    balanceDue = packageInvoice.BalanceDue;
                }
                else
                {
                    paymentMode = FixedPackagePaymentMode.PerSession;
                    paidAmount = sessionDtos.Sum(s => s.PaidAmount ?? 0m);
                    balanceDue = totalPrice - paidAmount;
                }
            }
            else
            {
                paidAmount = sessionDtos.Sum(s => s.PaidAmount ?? 0m);
                balanceDue = null;
            }

            var dto = new PatientCycleDto(
                cycle.Id,
                cycle.PatientId,
                patient?.FullName ?? "",
                cycle.PlanId,
                plan?.PlanName ?? cycle.CycleName,
                pricingModel,
                ParseFrequency(cycle.Frequency),
                cycleSessions.Count,
                sessionDtos,
                cycle.CreatedAt,
                PatientCycleStatus.Active,
                paymentMode,
                totalPrice,
                paidAmount,
                balanceDue);

            result.Add(WithComputedStatus(dto));
        }

        return result;
    }

    private static string PackageInvoiceNumber(int cycleId) => $"CYCLE-{cycleId}";

    private static PricingModelType ParsePricingModel(string value) =>
        Enum.TryParse<PricingModelType>(value, out var parsed) ? parsed : PricingModelType.PerVisit;

    private static PlanFrequency ParseFrequency(string value) =>
        Enum.TryParse<PlanFrequency>(value, out var parsed) ? parsed : PlanFrequency.Weekly;

    private static string SessionLabel(string frequency, int sessionNumber) => frequency switch
    {
        "Daily" => $"Day {sessionNumber}",
        "Monthly" => $"Month {sessionNumber}",
        _ => $"Week {sessionNumber}",
    };

    private static DateOnly ComputeSessionDate(DateOnly startDate, string frequency, int sessionIndex) => frequency switch
    {
        "Daily" => startDate.AddDays(sessionIndex),
        "Monthly" => startDate.AddMonths(sessionIndex),
        _ => startDate.AddDays(sessionIndex * 7),
    };

    /// <summary>cycleSessions.status only distinguishes Pending/Completed/Cancelled/Skipped -- the
    /// Upcoming vs. Rescheduled split (a UI-only distinction) comes from comparing Original vs.
    /// Actual scheduled date instead, since rescheduling never changes the DB status.</summary>
    private static PatientSessionStatus MapSessionStatus(CycleSessionEntity session) => session.Status switch
    {
        "Completed" => PatientSessionStatus.Completed,
        "Cancelled" => PatientSessionStatus.Cancelled,
        "Skipped" => PatientSessionStatus.Skipped,
        _ => session.ActualScheduledDate != session.OriginalScheduledDate ? PatientSessionStatus.Rescheduled : PatientSessionStatus.Upcoming,
    };

    private static bool IsResolved(PatientSessionStatus status) =>
        status is PatientSessionStatus.Completed or PatientSessionStatus.Cancelled or PatientSessionStatus.Skipped;

    private static PatientCycleStatus ComputeCycleStatus(IReadOnlyList<PatientCycleSessionDto> sessions)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (sessions.Any(s => !IsResolved(s.Status) && s.ScheduledDate < today))
        {
            return PatientCycleStatus.Paused;
        }

        if (sessions.All(s => IsResolved(s.Status)))
        {
            return PatientCycleStatus.Completed;
        }

        return PatientCycleStatus.Active;
    }

    private static PatientCycleDto WithComputedStatus(PatientCycleDto cycle) =>
        cycle with { Status = ComputeCycleStatus(cycle.Sessions) };

    private static ServiceTypeDto ToDto(ServiceTypeEntity e) => new(e.Id, e.TypeName, e.Description, e.IsActive);

    private static ServiceDto ToDto(ServiceEntity e, string typeName) => new(e.Id, e.ServiceTypeId, typeName, e.ServiceName, e.StandardPrice, e.Description, e.IsActive);

    private static TreatmentPlanDto ToDto(TreatmentPlanEntity p, List<TreatmentPlanItemEntity> items)
    {
        var sessions = Enumerable.Range(1, p.TotalSessions).Select(n =>
        {
            var sessionItems = items.Where(i => i.SessionNumber == n).ToList();
            var serviceIds = sessionItems.Where(i => i.ItemType == "Service" && i.ServiceId.HasValue).Select(i => i.ServiceId!.Value).ToList();
            var productIds = sessionItems.Where(i => i.ItemType == "Product" && i.ProductSkuId.HasValue).Select(i => i.ProductSkuId!.Value).ToList();
            return new TreatmentPlanSessionDto(n, SessionLabel(p.Frequency, n), serviceIds, productIds);
        }).ToList();

        return new TreatmentPlanDto(
            p.Id, p.PlanName, ParsePricingModel(p.PricingModel), p.FixedPackagePrice, ParseFrequency(p.Frequency), p.TotalSessions, sessions);
    }
}
