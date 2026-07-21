using ClinicMS.Web.Models.Api.Users;

namespace ClinicMS.Web.Services.Api;

public interface IUsersApiClient
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(int id, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>Looks up an active user by username or email (case-insensitive) for the login flow.</summary>
    Task<UserCredentialLookup?> FindForLoginAsync(string usernameOrEmail, CancellationToken cancellationToken = default);
}
