using ClinicMS.Web.Models.Api.Users;

namespace ClinicMS.Web.Services.Api;

public class UsersApiClient : ApiClientBase, IUsersApiClient
{
    public UsersApiClient(HttpClient http) : base(http)
    {
    }

    public Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        GetAsync<IReadOnlyList<UserDto>>("api/users", cancellationToken);

    public Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        GetAsync<UserDto>($"api/users/{id}", cancellationToken);

    public Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default) =>
        PostAsync<UserDto>("api/users", request, cancellationToken);

    public Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default) =>
        PutAsync<UserDto>($"api/users/{id}", request, cancellationToken);

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        DeleteAsync($"api/users/{id}", cancellationToken);

    public Task ChangePasswordAsync(int id, string newPassword, CancellationToken cancellationToken = default) =>
        PostAsync($"api/users/{id}/change-password", new AdminChangePasswordRequest(newPassword), cancellationToken);
}
