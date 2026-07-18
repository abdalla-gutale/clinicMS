namespace ClinicMS.Web.Models.Api.Notifications;

public record PatientOption(int Id, string FullName, string Phone, string? Email);

public record SendPatientEmailRequest(int PatientId, int? TemplateId, string? Subject, string? CustomMessage);

public record SendPatientEmailResult(bool Sent, string ToEmail, string Subject);

public record SendPatientWhatsAppRequest(int PatientId, int? TemplateId, string? CustomMessage);

public record SendPatientWhatsAppResult(bool Sent, string ToNumber, string Message);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
