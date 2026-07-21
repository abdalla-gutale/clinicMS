using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Users;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Services.Api.Db;

public class DbUsersApiClient : IUsersApiClient
{
    private readonly ClinicMsDbContext _db;

    public DbUsersApiClient(ClinicMsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _db.Users.OrderBy(u => u.Id).ToListAsync(cancellationToken);
        var roles = await _db.Roles.ToDictionaryAsync(r => r.Id, r => r.RoleName, cancellationToken);
        return users.Select(u => ToDto(u, roles.GetValueOrDefault(u.RoleId, ""))).ToList();
    }

    public async Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new ApiException(404, "User not found.");
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == user.RoleId, cancellationToken);
        return ToDto(user, role?.RoleName ?? "");
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new ApiException(400, "Selected role does not exist.");

        if (await _db.Users.AnyAsync(u => u.Username == request.Username, cancellationToken))
        {
            throw new ApiException(400, "A user with this username already exists.");
        }

        ValidatePasswordStrength(request.Password);

        var entity = new UserEntity
        {
            RoleId = role.Id,
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Users.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity, role.RoleName);
    }

    public async Task<UserDto> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new ApiException(404, "User not found.");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new ApiException(400, "Selected role does not exist.");

        entity.RoleId = role.Id;
        entity.FullName = request.FullName;
        entity.Email = request.Email;
        entity.PhoneNumber = request.PhoneNumber;
        entity.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity, role.RoleName);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new ApiException(404, "User not found.");
        _db.Users.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(int id, string newPassword, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
            ?? throw new ApiException(404, "User not found.");

        ValidatePasswordStrength(newPassword);

        entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // Length matters far more than composition rules for real-world password strength (NIST
    // 800-63B), so 8 is the primary bar; requiring a letter and a digit on top just rules out
    // the purely-numeric or purely-alphabetic passwords someone would otherwise reuse everywhere.
    private static void ValidatePasswordStrength(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ApiException(400, "Password must be at least 8 characters.");
        }

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            throw new ApiException(400, "Password must contain at least one letter and one number.");
        }
    }

    public async Task<UserCredentialLookup?> FindForLoginAsync(string usernameOrEmail, CancellationToken cancellationToken = default)
    {
        var identifier = usernameOrEmail.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Username.ToLower() == identifier.ToLower() || u.Email.ToLower() == identifier.ToLower(),
            cancellationToken);
        if (user is null)
        {
            return null;
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == user.RoleId, cancellationToken);
        return new UserCredentialLookup(user.Id, user.RoleId, role?.RoleName ?? "", user.Username, user.FullName, user.Email, user.PasswordHash, user.IsActive);
    }

    private static UserDto ToDto(UserEntity e, string roleName) => new(
        e.Id, e.RoleId, roleName, e.Username, e.FullName, e.Email, e.PhoneNumber, e.IsActive, e.CreatedAt);
}
