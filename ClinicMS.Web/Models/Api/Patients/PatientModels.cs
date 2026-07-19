namespace ClinicMS.Web.Models.Api.Patients;

public enum PatientGender
{
    Male,
    Female,
    Other
}

public record PatientDto(
    int Id,
    string? ImageUrl,
    string FullName,
    PatientGender Gender,
    DateOnly DateOfBirth,
    string Phone,
    string? Email,
    DateTime CreatedAt);

public record CreatePatientRequest(
    string? ImageUrl,
    string FullName,
    PatientGender Gender,
    DateOnly DateOfBirth,
    string Phone,
    string? Email);

public record UpdatePatientRequest(
    string? ImageUrl,
    string FullName,
    PatientGender Gender,
    DateOnly DateOfBirth,
    string Phone,
    string? Email);
