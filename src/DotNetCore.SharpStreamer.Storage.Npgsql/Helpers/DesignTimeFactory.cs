using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DotNetCore.SharpStreamer.Storage.Npgsql.Helpers;

public class DesignTimeFactory : IDesignTimeDbContextFactory<NpgsqlDbContext>
{
    public NpgsqlDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<NpgsqlDbContext> options = new DbContextOptionsBuilder<NpgsqlDbContext>()
            .UseNpgsql("Just connection string")
            .Options;

        return new NpgsqlDbContext(options);
    }
}