using ClinicMS.Web.Models.Api.Users;

namespace ClinicMS.Web.Services.Api.Mocks;

public class MockUsersApiClient : IUsersApiClient
{
    public Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UserDto>>(MockStore.Users.ToList());

    public Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = MockStore.Users.FirstOrDefault(u => u.Id == id)
            ?? throw new ApiException(404, "User not found.");
        return Task.FromResult(user);
    }

    public Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var role = MockStore.Roles.FirstOrDefault(r => r.Id == request.RoleId)
            ?? throw new ApiException(400, "Selected role does not exist.");

        var user = new UserDto(
            MockStore.NextUserId++,
            role.Id,
            role.RoleName,
            request.Username,
            request.FullName,
            request.Email,
            request.PhoneNumber,
            true,
            DateTime.UtcNow);

        MockStore.Users.Add(user);
        return Task.FromResult(user);
    }

    public Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var index = MockStore.Users.FindIndex(u => u.Id == id);
        if (index < 0)
        {
            throw new ApiException(404, "User not found.");
        }

        var role = MockStore.Roles.FirstOrDefault(r => r.Id == request.RoleId)
            ?? throw new ApiException(400, "Selected role does not exist.");

        var existing = MockStore.Users[index];
        var updated = existing with
        {
            RoleId = role.Id,
            RoleName = role.RoleName,
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            IsActive = request.IsActive,
        };

        MockStore.Users[index] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = MockStore.Users.RemoveAll(u => u.Id == id);
        if (removed == 0)
        {
            throw new ApiException(404, "User not found.");
        }

        return Task.CompletedTask;
    }

    public Task ChangePasswordAsync(int id, string newPassword, CancellationToken cancellationToken = default)
    {
        if (!MockStore.Users.Any(u => u.Id == id))
        {
            throw new ApiException(404, "User not found.");
        }

        return Task.CompletedTask;
    }
}
