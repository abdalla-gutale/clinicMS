using ClinicMS.Web.Models.Api.Payments;

namespace ClinicMS.Web.Models.Api.MedicalServices;

public record ServiceTypeDto(int Id, string TypeName, string? Description, bool IsActive);

public record CreateServiceTypeRequest(string TypeName, string? Description, bool IsActive);

public record UpdateServiceTypeRequest(string TypeName, string? Description, bool IsActive);

public record ServiceDto(
    int Id, int ServiceTypeId, string ServiceTypeName, string ServiceName, decimal Price, string? Description, bool IsActive);

public record CreateServiceRequest(int ServiceTypeId, string ServiceName, decimal Price, string? Description, bool IsActive);

public record UpdateServiceRequest(int ServiceTypeId, string ServiceName, decimal Price, string? Description, bool IsActive);

/// <summary>Standalone lightweight product list backing the Treatment Plan session grid's Products
/// multi-select -- the real Products catalog (Supply Chain > Inventory Control) isn't built yet.</summary>
public record ProductOption(int Id, string Name, decimal Price);

public enum PricingModelType
{
    FixedPackage,
    PerVisit
}

public enum PlanFrequency
{
    Daily,
    Weekly,
    Monthly
}

public record TreatmentPlanSessionDto(int SessionNumber, string Label, IReadOnlyList<int> ServiceIds, IReadOnlyList<int> ProductIds);

public record TreatmentPlanSessionRequest(int SessionNumber, string Label, IReadOnlyList<int> ServiceIds, IReadOnlyList<int> ProductIds);

public record TreatmentPlanDto(
    int Id,
    string PlanName,
    PricingModelType PricingModelType,
    decimal? FixedPackagePrice,
    PlanFrequency Frequency,
    int TotalSessions,
    IReadOnlyList<TreatmentPlanSessionDto> Sessions);

public record CreateTreatmentPlanRequest(
    string PlanName,
    PricingModelType PricingModelType,
    decimal? FixedPackagePrice,
    PlanFrequency Frequency,
    int TotalSessions,
    IReadOnlyList<TreatmentPlanSessionRequest> Sessions);

public record UpdateTreatmentPlanRequest(
    string PlanName,
    PricingModelType PricingModelType,
    decimal? FixedPackagePrice,
    PlanFrequency Frequency,
    int TotalSessions,
    IReadOnlyList<TreatmentPlanSessionRequest> Sessions);

public enum PatientSessionStatus
{
    Upcoming,
    Completed,
    Rescheduled
}

public enum PatientCycleStatus
{
    Active,
    Paused,
    Completed
}

public enum FixedPackagePaymentMode
{
    PerSession,
    DepositBalance
}

public record PatientCycleSessionDto(
    int SessionNumber,
    string Label,
    DateOnly ScheduledDate,
    PatientSessionStatus Status,
    IReadOnlyList<int> ServiceIds,
    IReadOnlyList<int> ProductIds,
    decimal? ChargeAmount,
    decimal? DiscountAmount,
    decimal? VatAmount,
    decimal? PaidAmount,
    int? PaymentId);

public record PatientCycleSessionRequest(int SessionNumber, IReadOnlyList<int> ServiceIds, IReadOnlyList<int> ProductIds);

public record ReschedulePatientCycleSessionRequest(int SessionNumber, DateOnly NewDate, bool CascadeToFollowing);

/// <summary>DiscountAmount is whatever the staff settled on client-side (a configured-discount
/// suggestion or a manual override) -- the server only bounds-checks it against the session's own
/// total. VatAmount is NOT accepted from the client; it's recomputed server-side from the clinic's
/// own VAT settings so it can't be tampered with.</summary>
public record CompletePatientCycleSessionRequest(
    int SessionNumber,
    decimal DiscountAmount,
    decimal PaidAmount,
    PaymentMethod? PaymentMethod,
    string? ReferenceNumber,
    int? AccountId);

public record RecordCyclePaymentRequest(decimal Amount, PaymentMethod PaymentMethod, string? ReferenceNumber, int? AccountId);

public record PatientCycleDto(
    int Id,
    int PatientId,
    string PatientName,
    int TreatmentPlanId,
    string PlanName,
    PricingModelType PricingModelType,
    PlanFrequency Frequency,
    int TotalSessions,
    IReadOnlyList<PatientCycleSessionDto> Sessions,
    DateTime AssignedAt,
    PatientCycleStatus Status,
    FixedPackagePaymentMode? PaymentMode,
    decimal? TotalPrice,
    decimal PaidAmount,
    decimal? BalanceDue);

public record AssignPatientCycleRequest(
    int PatientId,
    int TreatmentPlanId,
    DateOnly StartDate,
    IReadOnlyList<PatientCycleSessionRequest> Sessions,
    FixedPackagePaymentMode? PaymentMode,
    decimal? DepositAmount,
    PaymentMethod? DepositPaymentMethod,
    int? DepositAccountId);

public record UpdatePatientCycleSessionsRequest(IReadOnlyList<PatientCycleSessionRequest> Sessions);

public record CreateWalkInSaleRequest(
    int PatientId,
    IReadOnlyList<int> ServiceIds,
    IReadOnlyList<int> ProductIds,
    decimal DiscountAmount,
    PaymentMethod PaymentMethod,
    int? AccountId,
    string? ReferenceNumber);
