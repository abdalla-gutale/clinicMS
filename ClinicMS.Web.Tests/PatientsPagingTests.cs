using ClinicMS.Web.Data;
using ClinicMS.Web.Models.Api.Patients;
using ClinicMS.Web.Services.Api.Db;
using Xunit;

namespace ClinicMS.Web.Tests;

public class PatientsPagingTests
{
    private static async Task<DbPatientsApiClient> SeedAsync(int count)
    {
        var db = TestDb.Create();
        for (var i = 1; i <= count; i++)
        {
            db.Patients.Add(new PatientEntity
            {
                FullName = $"Patient {i:D3}",
                Gender = i % 2 == 0 ? "Male" : "Female",
                Phone = $"06{i:D8}",
                Email = $"patient{i}@example.com",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
            });
        }
        await db.SaveChangesAsync();
        return new DbPatientsApiClient(db);
    }

    [Fact]
    public async Task GetPaged_ReturnsRequestedPageSizeAndCorrectTotalCount()
    {
        var client = await SeedAsync(25);

        var page1 = await client.GetPagedAsync(1, 10, null, null, default);

        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(25, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
    }

    [Fact]
    public async Task GetPaged_SecondPage_ReturnsRemainingDistinctItems()
    {
        var client = await SeedAsync(25);

        var page1 = await client.GetPagedAsync(1, 10, null, null, default);
        var page2 = await client.GetPagedAsync(2, 10, null, null, default);
        var page3 = await client.GetPagedAsync(3, 10, null, null, default);

        Assert.Equal(10, page2.Items.Count);
        Assert.Equal(5, page3.Items.Count);
        var allIds = page1.Items.Concat(page2.Items).Concat(page3.Items).Select(p => p.Id).Distinct().ToList();
        Assert.Equal(25, allIds.Count);
    }

    [Fact]
    public async Task GetPaged_SearchFiltersByNamePhoneOrEmail()
    {
        var client = await SeedAsync(5);

        var byName = await client.GetPagedAsync(1, 10, "001", null, default);
        Assert.Single(byName.Items);
        Assert.Equal("Patient 001", byName.Items[0].FullName);

        var byEmail = await client.GetPagedAsync(1, 10, "patient3@example.com", null, default);
        Assert.Single(byEmail.Items);
    }

    [Fact]
    public async Task GetPaged_GenderFilterNarrowsResults()
    {
        var client = await SeedAsync(10);

        var males = await client.GetPagedAsync(1, 100, null, PatientGender.Male, default);

        Assert.Equal(5, males.TotalCount);
        Assert.All(males.Items, p => Assert.Equal(PatientGender.Male, p.Gender));
    }

    [Fact]
    public async Task GetPaged_PageSizeIsClampedToReasonableBounds()
    {
        var client = await SeedAsync(5);

        var oversized = await client.GetPagedAsync(1, 10_000, null, null, default);
        Assert.Equal(100, oversized.PageSize);

        var zeroOrNegative = await client.GetPagedAsync(1, 0, null, null, default);
        Assert.Equal(1, zeroOrNegative.PageSize);
    }
}
