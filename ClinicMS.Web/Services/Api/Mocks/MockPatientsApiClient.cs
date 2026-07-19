using ClinicMS.Web.Models.Api.Patients;

namespace ClinicMS.Web.Services.Api.Mocks;

public class MockPatientsApiClient : IPatientsApiClient
{
    public Task<IReadOnlyList<PatientDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PatientDto>>(MockStore.PatientRecords.OrderByDescending(p => p.CreatedAt).ToList());

    public Task<PatientDto> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default)
    {
        var patient = new PatientDto(
            MockStore.NextPatientRecordId++,
            request.ImageUrl,
            request.FullName,
            request.Gender,
            request.DateOfBirth,
            request.Phone,
            request.Email,
            DateTime.UtcNow);

        MockStore.PatientRecords.Add(patient);
        return Task.FromResult(patient);
    }

    public Task<PatientDto> UpdateAsync(int id, UpdatePatientRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.PatientRecords.FindIndex(p => p.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "Patient not found.");
        }

        var existing = MockStore.PatientRecords[index];
        var updated = existing with
        {
            ImageUrl = request.ImageUrl,
            FullName = request.FullName,
            Gender = request.Gender,
            DateOfBirth = request.DateOfBirth,
            Phone = request.Phone,
            Email = request.Email,
        };

        MockStore.PatientRecords[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = MockStore.PatientRecords.RemoveAll(p => p.Id == id);
        if (removed == 0)
        {
            throw new ApiException(404, "Patient not found.");
        }

        return Task.CompletedTask;
    }
}
