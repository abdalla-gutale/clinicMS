using ClinicMS.Web.Models.Api.Notifications;

namespace ClinicMS.Web.Services.Api;

public class NotificationsApiClient : ApiClientBase, INotificationsApiClient
{
    public NotificationsApiClient(HttpClient http) : base(http)
    {
    }

    public async Task<IReadOnlyList<PatientOption>> GetPatientsAsync(CancellationToken cancellationToken = default)
    {
        var page = await GetAsync<PagedResult<PatientOption>>("api/patients?page=1&pageSize=100", cancellationToken);
        return page.Items;
    }

    public Task<SendPatientEmailResult> SendEmailAsync(SendPatientEmailRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<SendPatientEmailResult>("api/notifications/email", request, cancellationToken);

    public Task<SendPatientWhatsAppResult> SendWhatsAppAsync(SendPatientWhatsAppRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<SendPatientWhatsAppResult>("api/notifications/whatsapp", request, cancellationToken);
}
