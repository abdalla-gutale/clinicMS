using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Notifications;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Services.Api.Db;

public class DbNotificationsApiClient : INotificationsApiClient
{
    private readonly ClinicMsDbContext _db;
    private readonly ISmsApiClient _smsApiClient;

    public DbNotificationsApiClient(ClinicMsDbContext db, ISmsApiClient smsApiClient)
    {
        _db = db;
        _smsApiClient = smsApiClient;
    }

    public async Task<IReadOnlyList<PatientOption>> GetPatientsAsync(CancellationToken cancellationToken = default)
    {
        var patients = await _db.Patients.OrderBy(p => p.FullName).ToListAsync(cancellationToken);
        return patients.Select(p => new PatientOption(p.Id, p.FullName, p.Phone, p.Email)).ToList();
    }

    public async Task<SendPatientEmailResult> SendEmailAsync(SendPatientEmailRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new ApiException(400, "Selected patient does not exist.");

        if (string.IsNullOrEmpty(patient.Email))
        {
            throw new ApiException(400, "Selected patient has no email on file.");
        }

        var subject = request.Subject ?? "Message from ClinicMS";
        await _smsApiClient.SendEmailAsync(patient.Email, subject, request.CustomMessage ?? "", cancellationToken);

        return new SendPatientEmailResult(true, patient.Email, subject);
    }

    public async Task<SendPatientWhatsAppResult> SendWhatsAppAsync(SendPatientWhatsAppRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await _db.Patients.FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new ApiException(400, "Selected patient does not exist.");

        // Unlike Email, there's no confirmed request contract for the configured WasenderAPI gateway
        // (no docs to implement against) -- sending here would mean guessing at a paid third-party
        // API's shape, so this still only validates and reports what would be sent.
        return new SendPatientWhatsAppResult(true, patient.Phone, request.CustomMessage ?? "Message from ClinicMS");
    }
}
