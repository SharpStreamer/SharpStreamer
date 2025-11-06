using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Storage.Npgsql.Tests.Helpers;

public class PostgresTestingDbContextFactory : IDesignTimeDbContextFactory<PostgresTestingDbContext>
{
    public PostgresTestingDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<PostgresTestingDbContext> options = new DbContextOptionsBuilder<PostgresTestingDbContext>()
            .UseNpgsql("Just connection string")
            .Options;

        return new PostgresTestingDbContext(options);
    }
}