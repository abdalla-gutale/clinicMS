using ClinicMS.Web.Models.Api.Notifications;

namespace ClinicMS.Web.Services.Api.Mocks;

public class MockNotificationsApiClient : INotificationsApiClient
{
    public Task<IReadOnlyList<PatientOption>> GetPatientsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PatientOption>>(MockStore.Patients.ToList());

    public Task<SendPatientEmailResult> SendEmailAsync(SendPatientEmailRequest request, CancellationToken cancellationToken = default)
    {
        var patient = MockStore.Patients.FirstOrDefault(p => p.Id == request.PatientId)
            ?? throw new ApiException(400, "Selected patient does not exist.");
        if (string.IsNullOrEmpty(patient.Email))
        {
            throw new ApiException(400, "Selected patient has no email on file.");
        }

        return Task.FromResult(new SendPatientEmailResult(true, patient.Email, request.Subject ?? "Message from ClinicMS"));
    }

    public Task<SendPatientWhatsAppResult> SendWhatsAppAsync(SendPatientWhatsAppRequest request, CancellationToken cancellationToken = default)
    {
        var patient = MockStore.Patients.FirstOrDefault(p => p.Id == request.PatientId)
            ?? throw new ApiException(400, "Selected patient does not exist.");

        return Task.FromResult(new SendPatientWhatsAppResult(true, patient.Phone, request.CustomMessage ?? "Message from ClinicMS"));
    }
}
