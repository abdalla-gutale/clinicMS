using ClinicMS.Web.Models.Api.Patients;

namespace ClinicMS.Web.Services.Api;

public interface IPatientsApiClient
{
    Task<IReadOnlyList<PatientDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PatientDto> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default);

    Task<PatientDto> UpdateAsync(int id, UpdatePatientRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
