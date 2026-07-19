using ClinicMS.Web.Models.Api.MedicalServices;
using ClinicMS.Web.Models.Api.Payments;

namespace ClinicMS.Web.Services.Api;

public interface IMedicalServicesApiClient
{
    Task<IReadOnlyList<ServiceTypeDto>> GetServiceTypesAsync(CancellationToken cancellationToken = default);

    Task<ServiceTypeDto> CreateServiceTypeAsync(CreateServiceTypeRequest request, CancellationToken cancellationToken = default);

    Task<ServiceTypeDto> UpdateServiceTypeAsync(int id, UpdateServiceTypeRequest request, CancellationToken cancellationToken = default);

    Task DeleteServiceTypeAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default);

    Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default);

    Task<ServiceDto> UpdateServiceAsync(int id, UpdateServiceRequest request, CancellationToken cancellationToken = default);

    Task DeleteServiceAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductOption>> GetProductOptionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TreatmentPlanDto>> GetTreatmentPlansAsync(CancellationToken cancellationToken = default);

    Task<TreatmentPlanDto> CreateTreatmentPlanAsync(CreateTreatmentPlanRequest request, CancellationToken cancellationToken = default);

    Task<TreatmentPlanDto> UpdateTreatmentPlanAsync(int id, UpdateTreatmentPlanRequest request, CancellationToken cancellationToken = default);

    Task DeleteTreatmentPlanAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientCycleDto>> GetPatientCyclesAsync(CancellationToken cancellationToken = default);

    Task<PatientCycleDto> AssignPatientCycleAsync(AssignPatientCycleRequest request, CancellationToken cancellationToken = default);

    Task<PatientCycleDto> UpdatePatientCycleSessionsAsync(int id, UpdatePatientCycleSessionsRequest request, CancellationToken cancellationToken = default);

    Task<PatientCycleDto> ReschedulePatientCycleSessionAsync(int id, ReschedulePatientCycleSessionRequest request, CancellationToken cancellationToken = default);

    Task<PatientCycleDto> CompletePatientCycleSessionAsync(int id, CompletePatientCycleSessionRequest request, CancellationToken cancellationToken = default);

    Task<PatientCycleDto> RecordCyclePaymentAsync(int id, RecordCyclePaymentRequest request, CancellationToken cancellationToken = default);

    Task DeletePatientCycleAsync(int id, CancellationToken cancellationToken = default);

    Task<InvoiceDto> CreateWalkInSaleAsync(CreateWalkInSaleRequest request, CancellationToken cancellationToken = default);
}
