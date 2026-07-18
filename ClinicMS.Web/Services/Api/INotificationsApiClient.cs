using ClinicMS.Web.Models.Api.Notifications;

namespace ClinicMS.Web.Services.Api;

public interface INotificationsApiClient
{
    Task<IReadOnlyList<PatientOption>> GetPatientsAsync(CancellationToken cancellationToken = default);

    Task<SendPatientEmailResult> SendEmailAsync(SendPatientEmailRequest request, CancellationToken cancellationToken = default);

    Task<SendPatientWhatsAppResult> SendWhatsAppAsync(SendPatientWhatsAppRequest request, CancellationToken cancellationToken = default);
}
