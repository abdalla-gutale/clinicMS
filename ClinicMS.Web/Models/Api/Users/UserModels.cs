namespace ClinicMS.Web.Models.Api.Users;

public record UserDto(
    int Id,
    int RoleId,
    string RoleName,
    string Username,
    string FullName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    DateTime CreatedAt);

public record CreateUserRequest(
    int RoleId,
    string Username,
    string Password,
    string FullName,
    string Email,
    string? PhoneNumber);

public record UpdateUserRequest(
    int RoleId,
    string FullName,
    string Email,
    string? PhoneNumber,
    bool IsActive);

public record AdminChangePasswordRequest(string NewPassword);
