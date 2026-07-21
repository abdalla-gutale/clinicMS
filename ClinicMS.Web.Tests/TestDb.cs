using ClinicMS.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicMS.Web.Tests;

internal static class TestDb
{
    public static ClinicMsDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ClinicMsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ClinicMsDbContext(options);
    }
}
