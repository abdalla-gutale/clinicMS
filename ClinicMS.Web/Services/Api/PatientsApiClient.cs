using ClinicMS.Web.Models.Api.Patients;

namespace ClinicMS.Web.Services.Api;

public class PatientsApiClient : ApiClientBase, IPatientsApiClient
{
    public PatientsApiClient(HttpClient http) : base(http)
    {
    }

    public Task<IReadOnlyList<PatientDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<PatientDto>>("api/patients", cancellationToken);

    public Task<PatientDto> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<PatientDto>("api/patients", request, cancellationToken);

    public Task<PatientDto> UpdateAsync(int id, UpdatePatientRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<PatientDto>($"api/patients/{id}", request, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/patients/{id}", cancellationToken);
}
