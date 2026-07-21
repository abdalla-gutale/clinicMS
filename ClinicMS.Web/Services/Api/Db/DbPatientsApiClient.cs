using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api;
using ClinicMS.Web.Models.Api.Patients;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Services.Api.Db;

public class DbPatientsApiClient : IPatientsApiClient
{
    private readonly ClinicMsDbContext _db;

    public DbPatientsApiClient(ClinicMsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PatientDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var patients = await _db.Patients.OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken);
        return patients.Select(ToDto).ToList();
    }

    public async Task<PagedResult<PatientDto>> GetPagedAsync(int page, int pageSize, string? search, PatientGender? gender, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.FullName.ToLower().Contains(term) ||
                p.Phone.ToLower().Contains(term) ||
                (p.Email != null && p.Email.ToLower().Contains(term)));
        }

        if (gender is PatientGender g)
        {
            var genderValue = g.ToString();
            query = query.Where(p => p.Gender == genderValue);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PatientDto>(entities.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<PatientDto> CreateAsync(CreatePatientRequest request, CancellationToken cancellationToken = default)
    {
        var patientCode = await NextPatientCodeAsync(cancellationToken);
        var entity = new PatientEntity
        {
            PatientCode = patientCode,
            FullName = request.FullName,
            Gender = request.Gender.ToString(),
            DateOfBirth = request.DateOfBirth,
            Phone = request.Phone,
            Email = request.Email,
            ImageUrl = request.ImageUrl,
            CurrentWalletCredit = 0m,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Patients.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<PatientDto> UpdateAsync(int id, UpdatePatientRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Patient not found.");

        entity.FullName = request.FullName;
        entity.Gender = request.Gender.ToString();
        entity.DateOfBirth = request.DateOfBirth;
        entity.Phone = request.Phone;
        entity.Email = request.Email;
        entity.ImageUrl = request.ImageUrl;
        await _db.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    // Soft delete -- a patient's clinical/financial history (cycles, invoices, payments) must stay
    // recoverable rather than vanish on a single accidental click. The global query filter on
    // PatientEntity keeps deleted patients out of every normal read from here on.
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new ApiException(404, "Patient not found.");
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> NextPatientCodeAsync(CancellationToken cancellationToken)
    {
        var sequence = await _db.IdSequences.FirstOrDefaultAsync(s => s.SequenceKey == "patientCode", cancellationToken);
        if (sequence is null)
        {
            return $"PT-{Guid.NewGuid():N}"[..10];
        }

        var code = $"{sequence.Prefix}{sequence.NextValue.ToString().PadLeft(sequence.PaddingLength, '0')}";
        sequence.NextValue++;
        sequence.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return code;
    }

    /// <summary>Real DB rows predate this frontend's required Gender/DateOfBirth fields, so legacy
    /// nulls are mapped to explicit sentinels rather than crashing -- Other for gender, 1900-01-01
    /// for DOB, both obviously flagging "needs re-entry" without breaking the page.</summary>
    private static PatientDto ToDto(PatientEntity p) => new(
        p.Id,
        p.ImageUrl,
        p.FullName,
        Enum.TryParse<PatientGender>(p.Gender, out var gender) ? gender : PatientGender.Other,
        p.DateOfBirth ?? new DateOnly(1900, 1, 1),
        p.Phone,
        p.Email,
        p.CreatedAt);
}
