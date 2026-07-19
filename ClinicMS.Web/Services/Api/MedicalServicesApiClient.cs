using ClinicMS.Web.Models.Api.MedicalServices;
using ClinicMS.Web.Models.Api.Payments;

namespace ClinicMS.Web.Services.Api;

public class MedicalServicesApiClient : ApiClientBase, IMedicalServicesApiClient
{
    public MedicalServicesApiClient(HttpClient http) : base(http)
    {
    }

    public Task<IReadOnlyList<ServiceTypeDto>> GetServiceTypesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ServiceTypeDto>>("api/service-types", cancellationToken);

    public Task<ServiceTypeDto> CreateServiceTypeAsync(CreateServiceTypeRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<ServiceTypeDto>("api/service-types", request, cancellationToken);

    public Task<ServiceTypeDto> UpdateServiceTypeAsync(int id, UpdateServiceTypeRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<ServiceTypeDto>($"api/service-types/{id}", request, cancellationToken);

    public Task DeleteServiceTypeAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/service-types/{id}", cancellationToken);

    public Task<IReadOnlyList<ServiceDto>> GetServicesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ServiceDto>>("api/services", cancellationToken);

    public Task<ServiceDto> CreateServiceAsync(CreateServiceRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<ServiceDto>("api/services", request, cancellationToken);

    public Task<ServiceDto> UpdateServiceAsync(int id, UpdateServiceRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<ServiceDto>($"api/services/{id}", request, cancellationToken);

    public Task DeleteServiceAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/services/{id}", cancellationToken);

    public Task<IReadOnlyList<ProductOption>> GetProductOptionsAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<ProductOption>>("api/product-options", cancellationToken);

    public Task<IReadOnlyList<TreatmentPlanDto>> GetTreatmentPlansAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<TreatmentPlanDto>>("api/treatment-plans", cancellationToken);

    public Task<TreatmentPlanDto> CreateTreatmentPlanAsync(CreateTreatmentPlanRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<TreatmentPlanDto>("api/treatment-plans", request, cancellationToken);

    public Task<TreatmentPlanDto> UpdateTreatmentPlanAsync(int id, UpdateTreatmentPlanRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<TreatmentPlanDto>($"api/treatment-plans/{id}", request, cancellationToken);

    public Task DeleteTreatmentPlanAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/treatment-plans/{id}", cancellationToken);

    public Task<IReadOnlyList<PatientCycleDto>> GetPatientCyclesAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<PatientCycleDto>>("api/patient-cycles", cancellationToken);

    public Task<PatientCycleDto> AssignPatientCycleAsync(AssignPatientCycleRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<PatientCycleDto>("api/patient-cycles", request, cancellationToken);

    public Task<PatientCycleDto> UpdatePatientCycleSessionsAsync(int id, UpdatePatientCycleSessionsRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<PatientCycleDto>($"api/patient-cycles/{id}/sessions", request, cancellationToken);

    public Task<PatientCycleDto> ReschedulePatientCycleSessionAsync(int id, ReschedulePatientCycleSessionRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<PatientCycleDto>($"api/patient-cycles/{id}/reschedule", request, cancellationToken);

    public Task<PatientCycleDto> CompletePatientCycleSessionAsync(int id, CompletePatientCycleSessionRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<PatientCycleDto>($"api/patient-cycles/{id}/complete-session", request, cancellationToken);

    public Task<PatientCycleDto> RecordCyclePaymentAsync(int id, RecordCyclePaymentRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<PatientCycleDto>($"api/patient-cycles/{id}/payments", request, cancellationToken);

    public Task DeletePatientCycleAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/patient-cycles/{id}", cancellationToken);

    public Task<InvoiceDto> CreateWalkInSaleAsync(CreateWalkInSaleRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<InvoiceDto>("api/walk-in-sales", request, cancellationToken);
}
