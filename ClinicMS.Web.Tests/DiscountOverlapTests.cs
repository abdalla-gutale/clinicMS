using ClinicMS.Web.Models.Api.Settings;
using ClinicMS.Web.Services.Api;
using ClinicMS.Web.Services.Api.Db;
using Xunit;

namespace ClinicMS.Web.Tests;

public class DiscountOverlapTests
{
    private static DbSettingsApiClient NewClient() => new(TestDb.Create());

    [Fact]
    public async Task CreateDiscount_OverlappingActiveRange_Throws()
    {
        var client = NewClient();
        await client.CreateDiscountAsync(new CreateDiscountRequest(
            "New Year Promo", DiscountType.Both, 10m, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), true), default);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.CreateDiscountAsync(new CreateDiscountRequest(
            "Overlapping Promo", DiscountType.Both, 15m, new DateOnly(2026, 1, 15), new DateOnly(2026, 2, 15), true), default));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateDiscount_NonOverlappingActiveRange_Succeeds()
    {
        var client = NewClient();
        await client.CreateDiscountAsync(new CreateDiscountRequest(
            "January Promo", DiscountType.Both, 10m, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), true), default);

        var second = await client.CreateDiscountAsync(new CreateDiscountRequest(
            "February Promo", DiscountType.Both, 15m, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28), true), default);

        Assert.Equal("February Promo", second.DiscountName);
    }

    [Fact]
    public async Task CreateDiscount_OverlappingButInactive_Succeeds()
    {
        var client = NewClient();
        await client.CreateDiscountAsync(new CreateDiscountRequest(
            "January Promo", DiscountType.Both, 10m, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), true), default);

        var second = await client.CreateDiscountAsync(new CreateDiscountRequest(
            "Draft Promo", DiscountType.Both, 15m, new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 20), false), default);

        Assert.False(second.IsActive);
    }

    [Fact]
    public async Task CreateDiscount_EndDateBeforeStartDate_Throws()
    {
        var client = NewClient();
        var ex = await Assert.ThrowsAsync<ApiException>(() => client.CreateDiscountAsync(new CreateDiscountRequest(
            "Backwards", DiscountType.Both, 10m, new DateOnly(2026, 3, 1), new DateOnly(2026, 2, 1), true), default));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateDiscount_ExcludesItselfFromOverlapCheck()
    {
        var client = NewClient();
        var discount = await client.CreateDiscountAsync(new CreateDiscountRequest(
            "January Promo", DiscountType.Both, 10m, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), true), default);

        // Widening this same discount's own range must not trip the overlap check against itself.
        var updated = await client.UpdateDiscountAsync(discount.Id, new UpdateDiscountRequest(
            "January Promo", DiscountType.Both, 20m, new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 5), true), default);

        Assert.Equal(20m, updated.DiscountValue);
    }
}
